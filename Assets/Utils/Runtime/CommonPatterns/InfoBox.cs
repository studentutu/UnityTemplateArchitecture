using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable
namespace App.Core.Tools
{
	[DisallowMultipleComponent]
	public class InfoBox : MonoBehaviour
	{
		[SerializeField]
		[TextArea(10, 50)]
		private string info = string.Empty;
		
	}
}
#pragma warning restore