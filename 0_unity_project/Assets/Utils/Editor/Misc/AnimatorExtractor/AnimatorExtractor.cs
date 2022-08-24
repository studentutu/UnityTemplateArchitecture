#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEditor.Animations;
using UnityEngine;

namespace App.Core.Tools.AnimatorExtractor
{
	public struct State
	{
		public AnimatorState valueState;
		public BlendTree valueBlendState;
		public bool isDefault;
		public int stateNameLength;
		public int motionNameLength;
		public StateMachineBehaviour[] StateMachineBehaviours;
		public AnimatorStateTransition[] transitions ;

		public State( AnimatorState value)
		{
			isDefault = false;
			valueState = value;
			valueBlendState = null;
			stateNameLength = value.name.Length;
			if (value.motion != null)
			{
				motionNameLength = value.motion.name.Length;
			}
			else
			{
				motionNameLength = 0;
			}

			StateMachineBehaviours = value.behaviours;
			transitions = value.transitions;
		}

		public State(BlendTree value)
		{
			isDefault = false;
			valueState = null;
			valueBlendState = value;
			stateNameLength = value.name.Length;
			motionNameLength = value.children.ToList()
				.OrderBy(i => i.motion.name)
				.First().motion.name.Length;
			StateMachineBehaviours = null;
			transitions = null;
		}
	}
	
	public class AnimatorExtractor : MonoBehaviour
	{
		[SerializeField] private AnimatorController graphToAnalyse;
		[SerializeField]
#if ODIN_INSPECTOR
		[FolderPath]
#endif
#pragma warning disable
		private string extractIntoFolder = null;
#pragma  warning restore
		
		[ContextMenu("Extract info from graph")]
		private void GetAllNodesAndTransitions()
		{
			// Parameters
			// var soForParameters = ScriptableObject.CreateInstance<StateAndParameters>();
			// var asIStateEditoHelper = soForParameters as IStateEditoHelper;
			// asIStateEditoHelper.CopyParamsFrom(graphToAnalyse);
			// if (!AssetDatabase.IsValidFolder(extractIntoFolder))
			// {
			// 	Debug.LogWarning("Folder does not exists. Creating folder ...");
			// 	var indexOfLastSlash = extractIntoFolder.LastIndexOf('/');
			// 	var actualNameOfFolder = extractIntoFolder.Substring(indexOfLastSlash);
			// 	
			// 	var odinAttribute = GetCustomAttribute<FolderPathAttribute>(nameof(extractIntoFolder));
			// 	if (odinAttribute != null)
			// 	{
			// 		// returns GUID of a folder
			// 		AssetDatabase.CreateFolder(odinAttribute.ParentFolder, actualNameOfFolder);
			// 	}
			// 	else
			// 	{
			// 		Debug.LogError("Implement extracting parent folder");
			// 	}
			// }
			//
			// AssetDatabase.CreateAsset(soForParameters, extractIntoFolder);
			// AssetDatabase.SaveAssets();
			// AssetDatabase.Refresh();
			
			
			// Get all states

			var allLayers = MappingAnimator(graphToAnalyse);
			foreach (var layer in allLayers)
			{
				Debug.LogWarning("------------------------");
				Debug.LogWarning("Inspecting layer : " + layer.Key + " States : " + layer.Value.Count);
				foreach (var dictionaryOfStates in layer.Value)
				{
					Debug.LogWarning("--------------------Inspecting state : " +
					                 dictionaryOfStates.Key + 
					                 (!dictionaryOfStates.Value.isDefault? "": " default") +
					                 "----------------");
					// foreach (var behaviours in dictionaryOfStates.Value.StateMachineBehaviours)
					// {
					// 	Debug.LogWarning("State has behaviour : " + behaviours.GetType().Name);
					// }
					
					// foreach (var transition in dictionaryOfStates.Value.transitions)
					// {
					// 	Debug.LogWarning("----------------State has transition: " + transition.name +
					// 	                 "To : " + transition.destinationState.name
					// 	                 + "---------------------");
					// 	Debug.LogWarning("Is Fixed duration: " + transition.hasFixedDuration);
					// 	Debug.LogWarning("Exit time: " + transition.exitTime);
					// 	Debug.LogWarning("Duration: " + transition.duration);
					// 	
					// 	if (transition.conditions != null && transition.conditions.Length > 0)
					// 	{
					// 		Debug.LogWarning("-------Inspecting Conditions--------");
					// 		foreach (var condition in transition.conditions)
					// 		{
					// 			Debug.LogWarning(" Condition : " + condition.parameter+
					// 			                 " Type : " + condition.mode);
					// 		}
					// 	}
					// }
				}

			}
			

		}
		
		/// <summary>
		/// Can return null!
		/// </summary>
		/// <param name="nameOfFieldOrProperty"> specify filed or property to get attribute from</param>
		/// <typeparam name="T"> Custom Attribute </typeparam>
		private static T GetCustomAttribute<T>( string nameOfFieldOrProperty)
			where  T : Attribute
		{
			var fieldInfo = typeof(AnimatorExtractor)
				.GetField(nameOfFieldOrProperty);
			if (fieldInfo != null)
			{
				return fieldInfo.GetCustomAttribute<T>();
			}

			var propertyInfo = typeof(AnimatorExtractor)
				.GetProperty(nameOfFieldOrProperty);
			if (propertyInfo != null)
			{
				return propertyInfo.GetCustomAttribute<T>();
			}

			return null;
		}
		
		private static Dictionary<string, Dictionary<string, State>> MappingAnimator(AnimatorController animator)
		{
			Dictionary<string, Dictionary<string, State>> motionsWithLayerKey =
				new Dictionary<string, Dictionary<string, State>>();
			
			var typeOfBlendTree = typeof(BlendTree);
			
			animator.layers.ToList()
				.ForEach(l =>
				{ 
					var defaultState = GetDefaultAnimatorState(animator);
					Dictionary<string, State> motions = new Dictionary<string, State>();
					
					// Any State
					var animatorStateValue = new AnimatorState();
					animatorStateValue.transitions = l.stateMachine.anyStateTransitions;
					animatorStateValue.name = l.name + " AnySate ";
					motions.Add(animatorStateValue.name, new State(animatorStateValue));
						
					l.stateMachine.states
						.ToList()
						.ForEach(s =>
						{
							if (s.state.motion != null && 
							    s.state.motion.GetType() == typeOfBlendTree)
							{
								BlendTree blendtree = (BlendTree) s.state.motion;
								MappingBlendTree(blendtree)
									.ToList()
									.ForEach(a =>
									{
										motions.Add(s.state.name + "/" + a.Key, new State(a.Value));
									});
								
								//BlendTreeToMotion(blendtree)
								//.ToList()
								//.ForEach(a =>
								//{
								//    motions.Add("[BlendTree]" + s.state.name + "/" + a.Key, a.Value);
								//});
							}
							else
							{
								motions.Add(s.state.name, new State(s.state));
							}

							if (s.state == defaultState)
							{
								var state = motions[s.state.name];
								state.isDefault = true;
								motions[s.state.name] = state;
							}
						});
					motionsWithLayerKey.Add(l.name, motions);
			});
			
			return motionsWithLayerKey;
		}

		private static void OverrideBlendTree(BlendTree value, int childIndex, Motion newMotion)
		{
			List<ChildMotion> cloneMotions = new List<ChildMotion>();
			value.children.ToList().ForEach(i => cloneMotions.Add(i));

			for (int i = value.children.Length - 1; i >= 0; i--)
			{
				value.RemoveChild(i);
			}

			for (int i = 0; i < cloneMotions.Count; i++)
			{
				if (i == childIndex)
				{
					value.AddChild(newMotion);
				}
				else
				{
					value.AddChild(cloneMotions[i].motion);
				}
			}

			for (int i = 0; i < value.children.Length; i++)
			{
				value.children[i] = cloneMotions[i];
			}
		}

		private static Dictionary<string, BlendTree> MappingBlendTree(BlendTree value)
		{
			Dictionary<string, BlendTree> motions = new Dictionary<string, BlendTree>();
			int duplicateKey = 0;
			var typeOfBlendTree = typeof(BlendTree);

			//motions.Add(value.name, value);
			for (int i = 0; i < value.children.Length; i++)
			{
				if (value.children[i].motion!=null && value.children[i].motion.GetType() == typeOfBlendTree)
				{
					motions.Add(value.name + "[" + i + "]", value);
					MappingBlendTree((BlendTree) value.children[i].motion).ToList()
						.ForEach(a =>
					{
						motions.Add(value.name + "/" + a.Key, a.Value);
					});
				}
				else
				{
					try
					{
						motions.Add(value.name + "[" + i + "]", value);
					}
					catch (ArgumentException)
					{
						motions.Add(value.name + " " + duplicateKey++, value);
					}
				}
			}

			return motions;
		}

		private static Dictionary<string, Motion> MappingBlendTreeInMotion(BlendTree value)
		{
			Dictionary<string, Motion> motions = new Dictionary<string, Motion>();
			int duplicateKey = 0;
			var typeOfBlendTree = typeof(BlendTree);

			value.children.ToList().ForEach(i =>
			{
				try
				{
					if (i.motion!=null && i.motion.GetType() == typeOfBlendTree)
					{
						MappingBlendTreeInMotion((BlendTree) i.motion).ToList()
							.ForEach(a =>
						{
							motions.Add(value.name + "/" + a.Key, a.Value);
						});
					}
					else
					{
						try
						{
							motions.Add(value.name + "/" + i.motion.name, i.motion);
						}
						catch (ArgumentException)
						{
							motions.Add(value.name + "/" + 
							            i.motion.name + " " + 
							            duplicateKey++, i.motion);
						}
					}
				}
				catch (NullReferenceException)
				{
					motions.Add(value.name + "/" + i.motion.name, i.motion);
				}
			});
			return motions;
		}

		private static AnimatorState GetDefaultAnimatorState(AnimatorController animator)
		{
			return GetDefaultAnimatorState(animator, 0);
		}

		private static AnimatorState GetDefaultAnimatorState(
			AnimatorController animator, 
			int layerIndex)
		{
			return animator.layers[layerIndex].stateMachine.defaultState;
		}
	}
}
#endif