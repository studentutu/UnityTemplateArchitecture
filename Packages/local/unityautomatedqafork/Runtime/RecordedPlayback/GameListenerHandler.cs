using System;
using System.Collections;
using System.Collections.Generic;
#if AQA_USE_TMP
using TMPro;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.EventSystems.RecordingInputModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.AutomatedQA.Listeners
{
	public class GameListenerHandler : MonoBehaviour
	{
		private class LegacyInputStates<T> 
		{
			internal class State
			{
				internal TouchData td = null;
				internal float pressStartTime = 0.0f;
			}

			private List<(T Key, State Press)> Presses = new List<(T Key, State Press)>();
			private List<T> RecordableKeys = new List<T>();
			private TouchData.type eventType;
			
			internal LegacyInputStates(TouchData.type eventType)
			{
				this.eventType = eventType;
			}

			internal void SetRecordableKeys(IEnumerable<T> keys)
			{
				RecordableKeys = new List<T>(keys);
			}

			internal List<T> GetRecordableKeys()
			{
				return RecordableKeys;
			}
			
			private bool UIInteractionJustOccured()
			{
				float cooldownTime = 0.1f;
			
				if (Instance.GetTouchData() == null || Instance.GetTouchData().Count == 0)
				{
					return false;
				}
			
				var lastTouchData = Instance.GetTouchData().Last();
				if (lastTouchData != null && Time.time - Instance.GetLastEventTime() < cooldownTime)
				{
					return ElementQuery.Find(!string.IsNullOrEmpty(lastTouchData.querySelector) ? lastTouchData.querySelector : lastTouchData.GetObjectScenePath()) != null;

				}
				return false;
			}

			/// <summary>
			/// Records a key press action as TouchData.
			/// </summary>
			/// <param name="key">Key that was pressed.</param>
			/// <param name="isKeyDown">Is the key pressed down or was it released.</param>
			internal void HandleInputAction(T key, bool isKeyDown)
			{
				if (UIInteractionJustOccured())
				{
					// ignore old input system events while user is interacting with the UI
					new AQALogger().LogDebug($"Ignored input '{key} {(isKeyDown ? "down" : "up")}' while interacting with UI");
					return;
				}
				
				if (isKeyDown)
				{
					// add touch data on key down
					TouchData td = new TouchData
					{
						eventType = this.eventType,
						timeDelta = Time.time - Instance.GetLastEventTime(),
						inputDuration = 1f,
						positional = false,
						scene = SceneManager.GetActiveScene().name,
						keyCode = key.ToString()
					};
					Instance.AddFullTouchData(td, true);
					
					(T Key, State press) keyAction = (key, new State()
					{
						td = td,
						pressStartTime = Time.time,
					});
					Presses.Add(keyAction);
				}
				else
				{
					// update input duration on release
					var originalPress = Presses.Find(x => x.Key.Equals(key));
					if (originalPress.Press != null)
					{
						originalPress.Press.td.inputDuration = Time.time - originalPress.Press.pressStartTime;
					}
					
					Presses.Remove(originalPress);

				}
			
			}
		}
		
		private LegacyInputStates<KeyCode> KeyCodePresses = new LegacyInputStates<KeyCode>(TouchData.type.key);
		private LegacyInputStates<string> KeyNamePresses = new LegacyInputStates<string>(TouchData.type.keyName);
		private LegacyInputStates<string> ButtonPresses = new LegacyInputStates<string>(TouchData.type.button);

		private static bool waitAFrame { get; set; }
		private static bool forceRefresh { get; set; } = true;
		private static float lastRunTime { get; set; }
		private static readonly float UPDATE_COOLDOWN = 5f;

		internal static List<GameObject> AllActiveAndInteractableGameObjects { get; set; } = new List<GameObject>();
		internal static AutomationInput currentInput { get; set; }
		internal enum ActableTypes { Clickable, Draggable, Input, KeyDown, Scroll, Screenshot, TextForAssert, Wait }

		internal static List<AutomationListener> ActiveListeners
		{
			get
			{
				_activeListeners.RemoveAll(al => al == null || !al.isActiveAndEnabled);
				return _activeListeners;
			}
			private set
			{
				_activeListeners = value;
			}
		}
		private static List<AutomationListener> _activeListeners = new List<AutomationListener>();

		private void Start()
		{
			SetupLegacyInput();
			StartCoroutine(Runner());
			SceneManager.sceneLoaded += Refresh;
		}

		private void SetupLegacyInput()
		{
			// Key Codes
			var keyCodes = new List<KeyCode>((KeyCode[])Enum.GetValues(typeof(KeyCode)));
			// Touch inputs are handled in RecordingInputModule, so we will ignore them here.
			keyCodes.RemoveAll(x => x.ToString().StartsWith("Mouse") || x.ToString().StartsWith("Joystick"));
			KeyCodePresses.SetRecordableKeys(keyCodes);

			// Key Names
			KeyNamePresses.SetRecordableKeys(InputCodes.KeyNames);
			
			// Button Names
			#if UNITY_EDITOR
			var axisNames = new List<string>();
			var inputManagerAsset =
				AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/InputManager.asset");
			if (inputManagerAsset != null)
			{
				var serializedObject = new SerializedObject(inputManagerAsset);
				var axesPropertyList = serializedObject.FindProperty("m_Axes");
				for (int i = 0; i < axesPropertyList.arraySize; i++)
				{
					SerializedProperty axesProperty = axesPropertyList.GetArrayElementAtIndex(i);
					var nameProp = axesProperty.FindPropertyRelative("m_Name");
					axisNames.Add(nameProp.stringValue);
				}
			}
			ButtonPresses.SetRecordableKeys(axisNames);
			#endif
				
		}
		
		void Update()
		{
			// Handle keypresses NOT in the context of an input field.
			if (AutomatedQASettings.RecordInputManager)
			{
				HandleOldInputSystemActions();
			}
			
			
			//HandleNewInputSystemActions();

			/// Handle keypresses in the context of an input field.
			if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				StartCoroutine(DelayedRefresh());
			}
			waitAFrame = false;
		}



		private void HandleOldInputSystemActions()
		{
			if (!waitAFrame && !IsInputSet(currentInput))
			{
				// KeyCode input
				foreach (KeyCode key in KeyCodePresses.GetRecordableKeys())
				{
					if (Input.GetKeyDown(key))
					{
						KeyCodePresses.HandleInputAction(key, true);
					}
					else if (Input.GetKeyUp(key))
					{
						KeyCodePresses.HandleInputAction(key, false);
					}
				}
				
				// String based key input
				foreach (string key in KeyNamePresses.GetRecordableKeys())
				{
					if (Input.GetKeyDown(key))
					{
						KeyNamePresses.HandleInputAction(key, true);
					}
					else if (Input.GetKeyUp(key))
					{
						KeyNamePresses.HandleInputAction(key, false);
					}
				}
				
				// Joystick/Button input
				foreach (string key in ButtonPresses.GetRecordableKeys())
				{
					if (Input.GetButtonDown(key))
					{
						ButtonPresses.HandleInputAction(key, true);
					}
					else if (Input.GetButtonUp(key))
					{
						ButtonPresses.HandleInputAction(key, false);
					}
				}
			}
		}

		private void HandleNewInputSystemActions()
		{
			// TODO: Setup.
		}

		
		internal static void FinalizeAnyTextInputInProgress()
		{
			if (IsInputSet(currentInput))
			{
				FinalizeTextInput();
				currentInput = new AutomationInput();
				waitAFrame = true;
			}
		}

		private static void FinalizeTextInput()
		{
			GameObject inputGo = null;
			string text = string.Empty;
			if (currentInput.InputField != null)
			{
				inputGo = currentInput.InputField.gameObject;
				text = currentInput.InputField.text;
			}
#if AQA_USE_TMP
			else if (currentInput.TmpInput != null)
			{
				inputGo = currentInput.TmpInput.gameObject;
				text = currentInput.TmpInput.text;
			}
#endif
			TouchData td = new TouchData
			{
				eventType = TouchData.type.input,
				objectName = inputGo.name,
				objectHierarchy = string.Join("/", AutomatedQaTools.GetHierarchy(inputGo)),
				querySelector = ElementQuery.ConstructQuerySelectorString(inputGo),
				timeDelta = currentInput.StartTime - Instance.GetLastEventTime(),
				inputDuration = currentInput.LastInputTime - currentInput.StartTime,
				positional = true,
				scene = SceneManager.GetActiveScene().name,
				inputText = text
			};
			Instance.AddFullTouchData(td);
		}

		private IEnumerator Runner()
		{
			while (true)
			{
				if (forceRefresh || Time.time - lastRunTime > UPDATE_COOLDOWN)
				{
					forceRefresh = false;
					ActiveListeners.RemoveAll(al => al == null);
					AllActiveAndInteractableGameObjects.RemoveAll(al => al == null);
					AllActiveAndInteractableGameObjects = AllActiveAndInteractableGameObjects.GetUniqueObjectsBetween(ElementQuery.Instance.GetAllActiveGameObjects());

					for (int x = 0; x < AllActiveAndInteractableGameObjects.Count; x++)
					{
						if (AllActiveAndInteractableGameObjects[x] == null)
						{
							continue;
						}

						AutomationListener al = AllActiveAndInteractableGameObjects[x].GetComponent<AutomationListener>();
						if (al == null)
						{
							List<MonoBehaviour> components = AllActiveAndInteractableGameObjects[x].GetComponents<MonoBehaviour>().ToList();
							for (int co = 0; co < components.Count; co++)
							{
								// Handle GameObjects with missing component references.
								if (components[co] == null || components[co].GetType() == null)
									continue;

								string scriptName = components[co].GetType().Name;
								#region Clickables
								//if (scriptName == "Collider" || typeof(Collider).IsAssignableFrom(components[co].GetType()))
								//{
								//	if (AllActiveAndInteractableGameObjects[x].GetComponent<Collider>().isTrigger)
								//	{
								//		AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
								//		continue;
								//	}
								//}
								if (scriptName == "Dropdown" || typeof(Dropdown).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
								if (scriptName == "Button" || typeof(Button).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
								if (scriptName == "Toggle" || typeof(Toggle).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
								if (scriptName == "Scrollbar" || typeof(Scrollbar).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Draggable);
									continue;
								}
								if (scriptName == "Slider" || typeof(Slider).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Draggable);
									continue;
								}
								if (scriptName == "Selectable" || typeof(Selectable).IsAssignableFrom(components[co].GetType()))
								{
									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Clickable);
									continue;
								}
#endregion

#region Inputs
								if (scriptName == "InputField" || components[co].GetType().IsAssignableFrom(typeof(InputField)))
								{

									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Input);
									continue;

								}
#if AQA_USE_TMP
								if (scriptName == "TMP_InputField" || components[co].GetType().IsAssignableFrom(typeof(TMP_InputField)))
								{

									AddListener(AllActiveAndInteractableGameObjects[x], ActableTypes.Input);
									continue;

								}
#endif
#endregion
							}
						}
						else if (!ActiveListeners.Contains(al))
						{
							ActiveListeners.Add(al);
						}
					}
					lastRunTime = Time.time;
				}
				yield return null;
			}
		}

		public static void Refresh()
		{
			forceRefresh = true;
		}

		public void Refresh(Scene scene, LoadSceneMode mod)
		{
			forceRefresh = true;
		}

		public IEnumerator DelayedRefresh()
		{
			yield return new WaitForSeconds(0.25f);
			Refresh();
			yield break;
		}

		internal void AddListener(GameObject obj, ActableTypes type)
		{
			if (obj.GetComponent<AutomationListener>() == null)
			{
				AutomationListener listener = obj.AddComponent<AutomationListener>();
				ActiveListeners.Add(listener);
			}
		}

		public static bool IsInputSet(AutomationInput input)
		{
			if (input != default(AutomationInput))
			{
				if (input.InputField != null && input.InputField.text.Length > 0)
				{
					return true;
				}
#if AQA_USE_TMP
				if (input.TmpInput != null && input.TmpInput.text.Length > 0)
				{
					return true;
				}
#endif
			}
			return false;
		}

		public class AutomationInput
		{
			public AutomationInput() { }
			public AutomationInput(InputField inputField, float startTime, float lastInputTime)
			{
				InputField = inputField;
				StartTime = startTime;
				LastInputTime = lastInputTime;
			}
#if AQA_USE_TMP
			public AutomationInput(TMP_InputField tmpInput, float startTime, float lastInputTime)
			{
				TmpInput = tmpInput;
				StartTime = startTime;
				LastInputTime = lastInputTime;
			}
			public TMP_InputField TmpInput { get; set; }
#endif
			public InputField InputField { get; set; }
			public float StartTime { get; set; }
			public float LastInputTime { get; set; }
		}
	}
}