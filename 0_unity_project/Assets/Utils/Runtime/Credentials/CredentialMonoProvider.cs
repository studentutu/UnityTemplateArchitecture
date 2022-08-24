using System;
using UnityEngine;
using UnityEngine.Events;

namespace App.Core.Tools
{
	[DisallowMultipleComponent]
	[ExecutionOrder(-150)]
	public class CredentialMonoProvider : MonoBehaviour
	{
		[Serializable]
		public class CredentialEvent: UnityEvent<CredentialsStorageSo.Credential>
		{
		}

		[SerializeField] private CredentialEvent OnCredentialsFound = new CredentialEvent();
		[SerializeField] private CredentialsStorageSo storageSo;
		[SerializeField] 
		[Range(1,255)]
		private int credentialToUse = 0;

#pragma warning disable
		[TextArea] 
		[SerializeField]
		private string info = "Fill in StepTransitionHandler";

		[SerializeField] private UnityEngine.Object handler;
#pragma warning restore
		
		
		[UnityEngine.Scripting.Preserve]
		public void TryChangeCredentialsDebug(int index)
		{
			credentialToUse = index;
			ChangeCredentialOn(handler);
		}
		
		[UnityEngine.Scripting.Preserve]
		public void UseProvidedCredentials()
		{
			ChangeCredentialOn(handler);
		}

		private void ChangeCredentialOn(UnityEngine.Object transitionToUse)
		{
			var cred = storageSo.GetCredential(credentialToUse);
			if (cred != null)
			{
				OnCredentialsFound?.Invoke(cred);
				ChangeCredentialTo(cred,transitionToUse);
			}
			else
			{
				Debug.LogError("Credential is missing : " + credentialToUse, this);
			}
		}

		protected virtual void ChangeCredentialTo(CredentialsStorageSo.Credential newCredential, UnityEngine.Object transitionToUse )
		{
		}
	}
}