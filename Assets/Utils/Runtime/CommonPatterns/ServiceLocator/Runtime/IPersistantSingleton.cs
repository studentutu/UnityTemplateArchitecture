using UnityEngine;
using System.Collections;

namespace Frictionless
{
	public interface IPersistantSingleton
	{
		IEnumerator HandleNewSceneLoaded();
	}
}
