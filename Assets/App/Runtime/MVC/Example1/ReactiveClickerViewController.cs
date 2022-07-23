using System;
using System.Collections;
using System.Collections.Generic;
using App.Runtime.MVC.Example1;
using UniRx;

public class ReactiveClickerViewController : IDisposable
{
	private readonly ClickerModel _injectedClickerModel;
	private CompositeDisposable _disposable;
	private int numberOfTimesClicked = 0;

	public ReactiveClickerViewController(ClickerModel injectedClickerModel)
	{
		_injectedClickerModel = injectedClickerModel;
		_disposable = new CompositeDisposable();
	}

	public void Run()
	{
		OnMainTextNeedChange();
		_injectedClickerModel.OnMainCLick
			.Skip(1)
			.Subscribe(x =>
			{
				numberOfTimesClicked++;
				OnMainTextNeedChange();
			})
			.AddTo(_disposable);
	}

	private void OnMainTextNeedChange()
	{
		_injectedClickerModel.MainText.SetValueAndForceNotify(numberOfTimesClicked.ToString());
	}

	public void Dispose()
	{
		_disposable?.Dispose();
	}
}