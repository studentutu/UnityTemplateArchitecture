using System.Collections;
using System.Collections.Generic;
using App.Runtime.MVC.Feature1;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

[TestFixture]
public class PLaymodeTests : ZenjectUnitTestFixture
{
	[SetUp]
	[UnitySetUp]
	public override void Setup()
	{
		base.Setup();
		ReactiveClickerInstaller.Install(Container);
	}

	[TearDown]
	[UnityTearDown]
	public override void Teardown()
	{
		base.Teardown();
		TestExtensions.ShutDownTests();
	}

	// A Test behaves as an ordinary method
	[Test]
	public void TestUtilsSimplePasses()
	{
		var resolveFromContainer = Container.Resolve<ReactiveClickerRunner>();
		resolveFromContainer.Should().NotBeNull();
		Assert.Pass();
		10.Should().BeGreaterOrEqualTo(10);
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