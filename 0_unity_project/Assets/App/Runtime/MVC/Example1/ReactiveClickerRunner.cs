using App.Core.Runtime;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace App.Runtime.MVC.Feature1
{
	public class ReactiveClickerRunner
	{
		private readonly IFactory<ReactiveClickerViewController> _factory;
		private CompositeDisposable _disposable;

		public ReactiveClickerRunner(IFactory<ReactiveClickerViewController> factory)
		{
			_factory = factory;
		}

		public async UniTask RunFullViewWithController()
		{
			_disposable?.Dispose();
			_disposable = new CompositeDisposable();

			var instantiate = await Addressables.InstantiateAsync(AddressablesNames.ClickerView,
				Vector3.zero, Quaternion.identity);

			_disposable.Add(new DisposableLambda(() => { Addressables.ReleaseInstance(instantiate); }));
			RunController();
		}

		public void RunController()
		{
			_factory
				.Create()
				.Run()
				.Add(_disposable);
		}
	}
}