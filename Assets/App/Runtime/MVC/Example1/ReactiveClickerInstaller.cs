using Zenject;

namespace App.Runtime.MVC.Feature1
{
	public class ReactiveClickerInstaller : Installer<ReactiveClickerInstaller>
	{
		public override void InstallBindings()
		{
			// Models can be used throughout lifetime of application - non lazy is good indication of it.
			Container.BindInterfacesAndSelfTo<ClickerModel>().AsSingle().NonLazy();
			Container.BindIFactory<ReactiveClickerViewController>().AsSingle();
			Container.BindInterfacesAndSelfTo<ReactiveClickerRunner>().AsSingle();
		}
	}
}