using System;
using UniRx;

namespace App.Runtime.MVC.Example1
{
	public class ClickerModel
	{
		// By default -> all Reactive Properties cache their result -> Skip(1) if needed
		public StringReactiveProperty MainText = new StringReactiveProperty("Not Yet Set.");

		public BoolReactiveProperty OnMainCLick = new BoolReactiveProperty();
	}
}