
namespace TLECrawler.Domain.Common.Errors;

public readonly record struct ErrorOrResult<T>
    : IError<T> where T : class
{
    private readonly T? _value = default;
    private readonly List<Exception> _errors = [];

    public T Value => _value!;
    public List<Exception> Exceptions => IsError ? _errors : [];
    public Exception? FirstError => Exceptions.FirstOrDefault();
    public bool IsError { get; }

    public static implicit operator ErrorOrResult<T>(T value)
    {
        return new ErrorOrResult<T>(value);
    }
    public static implicit operator ErrorOrResult<T>(List<Exception> errors)
    {
        return new ErrorOrResult<T>(errors);
    }

    private ErrorOrResult(List<Exception> ex)
    {
        _errors = ex;
        IsError = true;
    }
    private ErrorOrResult(T? value)
    {
        _value = value;
        IsError = false;
    }
}
