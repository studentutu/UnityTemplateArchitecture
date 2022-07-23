using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject.Internal;

public static class TestExtensions
{
	public static void StartUpTests()
	{
		ShutDownTests();
	}

	public static void ShutDownTests()
	{
		ZenjectTestUtil.DestroyEverythingExceptTestRunner(true);
		UniRx.MessageBroker.Default.Buffered().ClearAllBuffer();
	}
}