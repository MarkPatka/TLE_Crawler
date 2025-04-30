using System.Text.Json.Serialization;
using TLECrawler.Domain.Common.Converters;

namespace TLECrawler.Domain.TLEModel;

public class TLE_ST : ITLE
{
    [JsonPropertyName("PUBLISH_EPOCH")]
    [JsonConverter(typeof(DateTimeConverter))]
    public DateTime PublishDate { get; init; }

    [JsonPropertyName("TLE_LINE1")]
    public string FirstRow { get; init; } = null!;

    [JsonPropertyName("TLE_LINE2")]
    public string SecondRow { get; init; } = null!;
}
public class TLE_OldDB : ITLE
{
    public DateTime PublishDate { get; init; }
    public string FirstRow { get; init; } = null!;
    public string SecondRow { get; init; } = null!;
}