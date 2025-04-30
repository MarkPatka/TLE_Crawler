namespace TLECrawler.Domain.TLEModel;

public record TLE(
    DateTime PublishDate, 
    string FirstRow, 
    string SecondRow, 
    byte[] Hash, 
    int IterationId) : ITLE;