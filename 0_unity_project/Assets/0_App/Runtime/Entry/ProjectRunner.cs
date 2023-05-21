using System.Collections;
using System.Collections.Generic;
using _0_App.Runtime.Entry;
using App.Runtime.MVC.Feature1;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProjectRunner : MonoBehaviour
{
	private void OnEnable()
	{
		var runner = MainDI.Get().Resolve<ReactiveClickerRunner>();
		runner.RunFullViewWithController().Forget();
	}
}