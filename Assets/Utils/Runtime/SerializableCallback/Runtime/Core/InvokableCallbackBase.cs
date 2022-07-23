namespace App.Core.Tools.SerializableFunc.Hidden
{
	public abstract class InvokableCallbackBase<TReturn>
	{
		public abstract TReturn Invoke(params object[] args);
	}
}