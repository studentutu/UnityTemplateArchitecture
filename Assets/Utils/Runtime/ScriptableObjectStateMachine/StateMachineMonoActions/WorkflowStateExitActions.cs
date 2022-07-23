using System.Collections;
using System.Collections.Generic;
using App.Core.Tools.StateMachine;
using App.Core.Attributes;
using UnityEngine;

namespace App.Core.Tools
{
	public class WorkflowStateExitActions : MonoBehaviour
	{
#pragma warning disable
		[DrawIf(nameof(checkIfHasTheSameStatesInList),true,ComparisonType.Equals)]
		[SerializeField] 
		private string showIfDuplicated = null;
#pragma warning restore
		
		[SerializeField] protected StateAndAction[] containerActions = new StateAndAction[0];
		
		[HideInInspector] [SerializeField] private bool checkIfHasTheSameStatesInList = false;
		
		private void OnValidate()
		{
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
		[UnityEngine.Scripting.Preserve]
		public void OnStateExit(WorkflowStateAndParameters workflowStateEntered)
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