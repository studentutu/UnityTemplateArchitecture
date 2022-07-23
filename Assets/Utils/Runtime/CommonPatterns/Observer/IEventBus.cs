namespace App.Core.CommonPatterns
{
    /// <summary>
    /// Interface for messages sent and received on the message bus.
    /// To receive use EventBus.Subscription"TargetGenericType".Add().Remove().
    /// To create new one inherit from MessageBus"TArgument"
    /// </summary>
    public interface IEventBus : IInitBus
    {
    }

    public interface IInitBus
    {
        void Init();
    }
}