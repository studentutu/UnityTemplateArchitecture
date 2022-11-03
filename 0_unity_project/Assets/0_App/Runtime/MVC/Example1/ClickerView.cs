using App.Runtime.MVC.Feature1;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ClickerView : MonoBehaviour
{
	[SerializeField] private TMP_Text _text;
	[SerializeField] private Button _onClickButton;

	private ClickerModel _viewClickerModel;
	private CompositeDisposable _disposables;

	private void Awake()
	{
		_disposables = new CompositeDisposable();
	}

	private void OnDestroy()
	{
		_disposables?.Dispose();
	}

	private void OnEnable()
	{
		_viewClickerModel = ProjectContext.Instance.Container.Resolve<ClickerModel>();

		_viewClickerModel.MainText
			.Subscribe(newText => { _text.text = newText; })
			.AddTo(_disposables);

		_onClickButton.OnClickAsObservable()
			.Subscribe(x => _viewClickerModel.OnMainCLick.SetValueAndForceNotify(true))
			.AddTo(_disposables);
	}
}