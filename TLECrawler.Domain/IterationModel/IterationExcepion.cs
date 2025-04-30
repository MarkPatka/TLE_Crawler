namespace TLECrawler.Domain.IterationModel;

public class CreateIterationExcepion : Exception 
{
    public int IterationId { get; }

    public CreateIterationExcepion() { }

    public CreateIterationExcepion(string message)
        : base(message) { }

    public CreateIterationExcepion(string message, Exception inner)
        : base(message, inner) { }

    public CreateIterationExcepion(string message, int iterationId, Exception inner)
        : this(message, inner)
    {
        IterationId = iterationId;
    }
}
