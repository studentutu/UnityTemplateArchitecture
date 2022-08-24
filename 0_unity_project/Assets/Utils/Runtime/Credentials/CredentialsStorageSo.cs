using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Tools
{
	[CreateAssetMenu(menuName = "Core/Credentials", fileName = "Credentials",order = 10)]
	public class CredentialsStorageSo : ScriptableObject
	{
		[Serializable]
		public class Credential
		{
			public string Login = string.Empty;
			// [Password]
			public string Pass = string.Empty;
		}

		[SerializeField] private List<Credential> credentials = new List<Credential>();
		
		internal Credential GetCredential(int counterI)
		{
			Credential usingCredential = null;
			counterI--;
			if (counterI < 0 || counterI >= credentials.Count)
			{
				counterI = 0;
			}

			if (credentials.Count > counterI)
			{
				usingCredential = credentials[counterI];
			}

			return usingCredential;
		}
	}
}