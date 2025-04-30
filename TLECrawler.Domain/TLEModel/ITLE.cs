namespace TLECrawler.Domain.TLEModel;

public interface ITLE
{
    string FirstRow  { get; }
    string SecondRow { get; }
    DateTime PublishDate { get; }
}
