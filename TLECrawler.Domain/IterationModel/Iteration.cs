namespace TLECrawler.Domain.IterationModel;

public record Iteration(
    DateTime StartDateTime,
    IterationStatus? Status,
    int? TLECount,
    DateTime? EndDateTime, 
    bool? IsRepeat);


