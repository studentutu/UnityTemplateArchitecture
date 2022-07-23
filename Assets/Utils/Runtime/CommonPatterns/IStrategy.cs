namespace App.Core.CommonPatterns
{
    public interface IContext<T> where T : class
    {
        void SetStrategy(IStrategy<T> targetStrategy);
    }

    public interface IStrategy<T> where T : class
    {
        void Execute(T data);
    }
}