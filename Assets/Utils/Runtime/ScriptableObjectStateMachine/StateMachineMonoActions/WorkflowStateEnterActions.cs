using System;
using System.Collections;
using System.Collections.Generic;
using App.Core.Tools.StateMachine;
using App.Core.Attributes;
using UnityEngine;

namespace App.Core.Tools
{
	
	[Serializable]
	public class StateAndAction
	{
		public WorkflowStateAndParameters actualKey;
		public UnityEngine.Events.UnityEvent events;
	}

	public class WorkflowStateEnterActions : MonoBehaviour
	{
#pragma warning disable
		[DrawIf(nameof(checkIfHasTheSameStatesInList),true,ComparisonType.Equals)]
		[SerializeField] 
		private string showIfDuplicated = null;
		
		[HideInInspector] [SerializeField]  private bool checkIfHasTheSameStatesInList = false;
#pragma warning restore
		
		[SerializeField] 
		protected StateAndAction[] containerActions = new StateAndAction[0];
		
		
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (UnityEditor.EditorApplication.isPlaying)
			{
				return;
			}
			checkIfHasTheSameStatesInList = false;
			for (int i = 0; i < containerActions.Length && !checkIfHasTheSameStatesInList; i++)
			{
				for (int j = i+1; j < containerActions.Length && !checkIfHasTheSameStatesInList; j++)
				{
					checkIfHasTheSameStatesInList |= checkIfHasTheSameStatesInList && containerActions[i].actualKey != null &&
					                                 containerActions[i].actualKey == containerActions[j].actualKey;
					if (checkIfHasTheSameStatesInList)
					{
						showIfDuplicated = string.Format("<Color=red>{0}</Color>", containerActions[i].actualKey.name + " has duplicates in list.");
					}
				}
			}
		}
#endif

		[UnityEngine.Scripting.Preserve]
		public void OnStateEnter(WorkflowStateAndParameters workflowStateEntered)
		{
			for (int i = 0; i < containerActions.Length; i++)
			{
				if (containerActions[i].actualKey == workflowStateEntered)
				{
					containerActions[i].events?.Invoke();
					return;
				}
			}
			
		}
	}
}