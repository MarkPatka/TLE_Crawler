namespace TLECrawler.Domain.Common.Errors;

public interface IError<out T> where T : class
{
    T Value { get; }
    List<Exception>? Exceptions { get; }
    bool IsError { get; }
}
