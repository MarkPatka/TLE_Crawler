using System.ComponentModel;

namespace TLECrawler.Domain.TLEModel;

public class TLEParseException : Exception
{
    public int IterationId { get; }

    public TLEParseException() { }

    public TLEParseException(string message)
        : base(message) { }

    public TLEParseException(string message, Exception inner)
        : base(message, inner) { }

    public TLEParseException(string message, int iterationId, Exception inner)
        : this(message, inner)
    {
        IterationId = iterationId;
    }
}

public class TLEPersistException : Exception
{
    public int IterationId { get; }

    public TLEPersistException() { }

    public TLEPersistException(string message)
        : base(message) { }

    public TLEPersistException(string message, Exception inner)
        : base(message, inner) { }

    public TLEPersistException(string message, int iterationId, Exception inner)
        : this(message, inner)
    {
        IterationId = iterationId;
    }
}