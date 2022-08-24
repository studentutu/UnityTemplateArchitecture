using System;
using System.Collections;
using System.Collections.Generic;
using App.Runtime.MVC.Feature1;
using UniRx;

public class ReactiveClickerViewController : IDisposable
{
	private readonly ClickerModel _injectedClickerModel;
	private readonly CompositeDisposable _disposable;
	private int numberOfTimesClicked = 0;

	public ReactiveClickerViewController(ClickerModel injectedClickerModel)
	{
		_injectedClickerModel = injectedClickerModel;
		_disposable = new CompositeDisposable();
	}

	public CompositeDisposable Run()
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
		return _disposable;
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