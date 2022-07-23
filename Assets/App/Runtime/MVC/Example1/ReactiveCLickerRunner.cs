using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace App.Runtime.MVC.Example1
{
	public class ReactiveCLickerRunner
	{
		private readonly IFactory<ReactiveClickerViewController> _factory;

		public ReactiveCLickerRunner(IFactory<ReactiveClickerViewController> factory)
		{
			_factory = factory;
		}

		public async UniTask RunFullViewWithController()
		{
			var loadAddressable = await Addressables.LoadAssetAsync<GameObject>(AddressablesNames.ClickerView);

			var instantiate = await Addressables.InstantiateAsync(AddressablesNames.ClickerView,
				Vector3.zero, Quaternion.identity);
			RunController();
		}

		public void RunController()
		{
			_factory.Create().Run();
		}
	}
}