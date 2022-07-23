using System.Collections;
using System.Collections.Generic;
using App.Runtime.MVC.Example1;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

[TestFixture]
public class EditmodeTests : ZenjectUnitTestFixture
{
	[SetUp]
	[UnitySetUp]
	public override void Setup()
	{
		base.Setup();
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
		ReactiveClickerInstaller.Install(Container);

		var resolveFromContainer = Container.Resolve<ReactiveCLickerRunner>();
		resolveFromContainer.Should().NotBeNull();
		10.Should().BeGreaterOrEqualTo(10);
		Assert.Pass();
	}

	[Test]
	public void TestMVC()
	{
		ReactiveClickerInstaller.Install(Container);
		var resolveRunner = Container.Resolve<ReactiveCLickerRunner>();
		resolveRunner.Should().NotBeNull();
		var model = Container.Resolve<ClickerModel>();
		resolveRunner.RunController();
		model.OnMainCLick.SetValueAndForceNotify(true);
		model.MainText.Value.Should().Be("1");
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