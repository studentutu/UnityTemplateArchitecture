namespace App.Core.CommonPatterns
{
    public interface IResult
    {

    }

    public interface IResult<T> : IResult where T : class
    {
        T GetResult();
    }
}