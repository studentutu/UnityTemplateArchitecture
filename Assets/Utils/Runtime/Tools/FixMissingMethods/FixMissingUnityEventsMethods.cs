using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace App.Core.Tools
{
	public class FixMissingUnityEventsMethods : MonoBehaviour
	{
		private static Dictionary<string, List<string>> scriptNameToListOfNameSpaces =
			new Dictionary<string, List<string>>();

		private static System.Type typeOfUnityObject = typeof(UnityEngine.Object);
		private const string PERSISTANT_CALLS = "m_PersistentCalls";
		private const string CALLS = "m_Calls"; // // PERSISTANT_CALLS/CALLS
		private const string CACHED_ARGUMENTS = "m_Arguments"; // PERSISTANT_CALLS/CALLS/CACHED_ARGUMENTS
		private const string SHORT_TYPE_ASSEMBLY = "m_ObjectArgumentAssemblyTypeName"; // PERSISTANT_CALLS/CALLS/CACHED_ARGUMENTS/SHORT_TYPE_ASSEMBLY
		private const string METHOD_NAME = "m_MethodName"; // PERSISTANT_CALLS/CALLS/METHOD_NAME

		[Serializable]
		public class RefactoredInto
		{
			public string OriginalClass;
			public string RenamedIntoClass;

			[Header("Use Only if changed method names")]
			public RefactoredMethod methods;

			public bool IsMatchedMethods(string fullString)
			{
				if (string.IsNullOrEmpty(fullString) || 
				    string.IsNullOrEmpty(methods.OldPathToMethod) || 
				    string.IsNullOrEmpty(methods.NewPathToMethod))
				{
					return false;
				}

				return fullString.ToLower().Equals(methods.OldPathToMethod.ToLower());
			}

		}
		
		[Serializable]
		public class RefactoredMethod
		{
			[Header("Duplicate full ClassName. Should be Namespace.Other1.MethodName1")]
			public string OldPathToMethod;
			public string NewPathToMethod;
		}

		[FormerlySerializedAs("changedDOne")] [SerializeField]
		private List<RefactoredInto> changedInto = new List<RefactoredInto>();

#if UNITY_EDITOR

		[ContextMenu("Fix UnityEvents")]
		private void FixMethods()
		{
			scriptNameToListOfNameSpaces.Clear();
			var assemblyCurrent = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assemblyIn in assemblyCurrent)
			{
				var toLower = assemblyIn.GetName().Name.ToLower();
				if (IgnoreAssembliesOrNamsePsaces(toLower))
				{
					continue;
				}

				var types = assemblyIn.GetTypes();
				foreach (var targetType in types)
				{
					var shortNamesapce = targetType.Namespace;
					if (targetType.IsAbstract ||
					    targetType.IsInterface ||
					    targetType.IsGenericType ||
					    IgnoreAssembliesOrNamsePsaces(shortNamesapce))
					{
						continue;
					}
					// have the following naming on the type :
					// [Full TYpe name with class], [Assembly Name]
					// App.Core.Tools.WorkflowStateEnterActions, App.Core
					var nameOfScript = targetType.FullName;
					var getOnlyLastPart = nameOfScript.LastIndexOf('.');
					getOnlyLastPart++;
					var origType = nameOfScript.Substring(getOnlyLastPart);

					if (!scriptNameToListOfNameSpaces.ContainsKey(origType))
					{
						scriptNameToListOfNameSpaces.Add(origType, new List<string>());
					}

					scriptNameToListOfNameSpaces[origType].Add(targetType.AssemblyQualifiedName);
				}
			}

			foreach (var refactored in changedInto)
			{
				if (string.IsNullOrEmpty(refactored.OriginalClass))
				{
					continue;
				}

				if (string.IsNullOrEmpty(refactored.RenamedIntoClass))
				{
					continue;
				}

				if (!scriptNameToListOfNameSpaces.ContainsKey(refactored.RenamedIntoClass))
				{
					Debug.LogError(" Renamed class does not exists " + refactored.RenamedIntoClass);
				}
				else
				{
					if (!scriptNameToListOfNameSpaces.ContainsKey(refactored.OriginalClass))
					{
						scriptNameToListOfNameSpaces.Add(refactored.OriginalClass, new List<string>());
					}

					scriptNameToListOfNameSpaces[refactored.OriginalClass]
						.AddRange(scriptNameToListOfNameSpaces[refactored.RenamedIntoClass]);
					// Debug.LogWarning(" Adding refactored scripts " + refactored.OriginalClass +" To " + refactored.RenamedIntoClass);
				}
			}

			var allRootObjects = gameObject.scene.GetRootGameObjects();
			for (int i = 0; i < allRootObjects.Length; i++)
			{
				float currentProgrss = Mathf.Max(i - 0.5f, 0) / allRootObjects.Length;
				EditorUtility.DisplayProgressBar("Refactoring Progress", allRootObjects[i].name, currentProgrss);

				GoThroughtGameObjects(allRootObjects[i], i / allRootObjects.Length);
				EditorUtility.DisplayProgressBar("Refactoring Progress", allRootObjects[i].name,
					i / allRootObjects.Length);

			}

			scriptNameToListOfNameSpaces.Clear();
			EditorUtility.ClearProgressBar();
		}

		private static bool IgnoreAssembliesOrNamsePsaces(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}

			var toLower = name.ToLower();

			if (toLower.StartsWith("System".ToLower()) ||
			    toLower.StartsWith("Mono".ToLower()) ||
			    toLower.StartsWith("NUnit".ToLower()) ||
			    toLower.StartsWith("UnityEditor".ToLower()) ||
			    toLower.StartsWith("Internal.runtime".ToLower()) ||
			    toLower.StartsWith("Microsoft".ToLower()) ||
			    toLower.StartsWith("Unity.PackageManager".ToLower()) ||
			    toLower.StartsWith("mscorlib".ToLower()) ||
			    toLower.StartsWith("Unity.MemoryProfiler".ToLower()) ||
			    toLower.StartsWith("JetBrains".ToLower()) ||
			    toLower.StartsWith("ICSharpCode".ToLower()) ||
			    toLower.StartsWith("UnityEngine".ToLower())
			)
			{

				return true;
			}

			return false;
		}

		private static Component[] GetALlComponentsFromObject(GameObject go)
		{
			return go.GetComponents<Component>();
		}

		private void GoThroughtGameObjects(GameObject mainGO, float currentProgrss)
		{
			GoThroughtSerializedObjects(mainGO);
			float diff = currentProgrss / mainGO.transform.childCount;
			for (int i = 0; i < mainGO.transform.childCount; i++)
			{
				if (currentProgrss >= 0)
				{
					EditorUtility.DisplayProgressBar("Refactoring Progress", 
						mainGO.transform.GetChild(i).name,
						(i * diff) / currentProgrss);
				}

				GoThroughtGameObjects(mainGO.transform.GetChild(i).gameObject, -1f);
			}
		}

		private void GoThroughtSerializedObjects(GameObject go)
		{
			var getAllComponents = GetALlComponentsFromObject(go);
			foreach (var component in getAllComponents)
			{
				GoInRecursion(new SerializedObject(component));
			}
		}

		private void GoInRecursion(SerializedObject so)
		{
			var enumaratorCustom = so.GetIterator();
			// enumaratorCustom.Next(true);
			do
			{
				// end and try go into  the object
				ObserveSerializedProperty(enumaratorCustom);

				if (enumaratorCustom.isArray)
				{
					for (int i = 0; i < enumaratorCustom.arraySize; i++)
					{
						if (enumaratorCustom.GetArrayElementAtIndex(i) != null)
						{
							// end and try go into  the object
							ObserveSerializedProperty(enumaratorCustom.GetArrayElementAtIndex(i));
						}
					}
				}
			} while (enumaratorCustom.Next(true));
		}

		private void ObserveSerializedProperty(SerializedProperty property)
		{
			// DO seek 
			CHeckTheUnityEventFunctions(property);
		}

		private void CHeckTheUnityEventFunctions(SerializedProperty objectToLookAsUnityEvent)
		{
			var findProperty = objectToLookAsUnityEvent.FindPropertyRelative(PERSISTANT_CALLS);
			if (findProperty != null)
			{
				var findCalls = findProperty.FindPropertyRelative(CALLS);
				if (findCalls != null)
				{
					for (int i = 0; i < findCalls.arraySize; i++)
					{
						var call = findCalls.GetArrayElementAtIndex(i);
						if (call != null)
						{
							var findAllArguments = call.FindPropertyRelative(CACHED_ARGUMENTS);
							var findAssemblyMethodTarget =
								findAllArguments.FindPropertyRelative(SHORT_TYPE_ASSEMBLY);
							var argumentAssemblyTypeName = findAssemblyMethodTarget.stringValue;
							
							if (string.IsNullOrEmpty(argumentAssemblyTypeName))
							{
								continue;
							}
							var findMethodName = call.FindPropertyRelative(METHOD_NAME);
							
							var getPrevType = argumentAssemblyTypeName.Split(',');
							var prevType = getPrevType[0];

							string findMatchWithPrev = prevType + "." + findMethodName.stringValue;
							var matchMethod = changedInto.Find(obj => obj.IsMatchedMethods(findMatchWithPrev));
							
							if (matchMethod != null)
							{
								var findLastIndexOfForMethod = matchMethod.methods.NewPathToMethod.LastIndexOf('.');
								findLastIndexOfForMethod++;
								findMethodName.stringValue = matchMethod.methods.NewPathToMethod.Substring(findLastIndexOfForMethod);
								findMethodName.serializedObject.ApplyModifiedProperties();
								Debug.LogWarning(
									"Changed Method" + objectToLookAsUnityEvent.serializedObject.targetObject.name,
									objectToLookAsUnityEvent.serializedObject.targetObject);
							}
							
							System.Type argumentAssemblyType = typeOfUnityObject;
							if (!string.IsNullOrEmpty(argumentAssemblyTypeName))
							{
								argumentAssemblyType =
									System.Type.GetType(argumentAssemblyTypeName, false) ??
									typeOfUnityObject;
							}

							if (argumentAssemblyType == typeOfUnityObject)
							{
								argumentAssemblyType = null;
								string prevAssembly = null;
								if (getPrevType.Length > 1)
								{
									prevAssembly = getPrevType[1];
								}

								var getOnlyLastPart = prevType.LastIndexOf('.');
								getOnlyLastPart++;
								prevType = prevType.Substring(getOnlyLastPart);

								// changed namespace or fully refactored (needs the list of updated script)
								// or simply placed under assembly definition
								// always respects the current assembly definitions if used
								if (scriptNameToListOfNameSpaces.ContainsKey(prevType))
								{
									var getListofNameSpaces = scriptNameToListOfNameSpaces[prevType];

									int index = 0;
									for (int j = 0; j < getListofNameSpaces.Count; j++)
									{
										var nameSPace = TidyAssemblyTypeName(getListofNameSpaces[j]);
										var splitted = nameSPace.Split(',');
										string checkAssembly = null;
										if (splitted.Length > 1)
										{
											checkAssembly = splitted[1];
										}

										if (checkAssembly == prevAssembly)
										{
											index = j;
										}
									}

									prevType = getListofNameSpaces[index];
									prevType = TidyAssemblyTypeName(prevType);
									findAssemblyMethodTarget.stringValue = prevType;
									objectToLookAsUnityEvent.serializedObject.ApplyModifiedProperties();
									Debug.LogWarning(
										"Changed " + objectToLookAsUnityEvent.serializedObject.targetObject.name,
										objectToLookAsUnityEvent.serializedObject.targetObject);
								}
							}
						}
					}
				}
			}
		}

		private static string TidyAssemblyTypeName(string FullQualifiedTypeName)
		{
			if (string.IsNullOrEmpty(FullQualifiedTypeName))
			{
				return null;
			}

			string result = FullQualifiedTypeName;

			int num = int.MaxValue;
			int val1_1 = result.IndexOf(", Version=");
			if (val1_1 != -1)
			{
				num = Mathf.Min(val1_1, num);
			}

			int val1_2 = result.IndexOf(", Culture=");
			if (val1_2 != -1)
			{
				num = Mathf.Min(val1_2, num);
			}

			int val1_3 = result.IndexOf(", PublicKeyToken=");
			if (val1_3 != -1)
			{
				num = Mathf.Min(val1_3, num);
			}

			if (num != int.MaxValue)
			{
				result = result.Substring(0, num);
			}

			int length = result.IndexOf(", UnityEngine.");
			if (length == -1 || !result.EndsWith("Module"))
			{
				return result;
			}

			result = result.Substring(0, length) + ", UnityEngine";
			return result;
		}
#endif
	}
}