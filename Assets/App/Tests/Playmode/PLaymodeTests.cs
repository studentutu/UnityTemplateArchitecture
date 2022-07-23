using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;


public class PLaymodeTests : ZenjectIntegrationTestFixture
{
	
	[SetUp]
	[UnitySetUp]
	public void SetupForTests()
	{
		TestExtensions.StartUpTests();
	}

	[TearDown]
	[UnityTearDown]
	public void ShutDownTests()
	{
		TestExtensions.ShutDownTests();
	}

	// A Test behaves as an ordinary method
	[Test]
	public void TestUtilsSimplePasses()
	{
		// var resolveFromContainer = Container.Resolve();
		Assert.Pass();
	}

	// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
	// `yield return null;` to skip a frame.
	[UnityTest]
	public IEnumerator TestUtilsWithEnumeratorPasses()
	{
		// Use the Assert class to test conditions.
		// Use yield to skip a frame.
		yield return null;
		Assert.Pass();
	}
}