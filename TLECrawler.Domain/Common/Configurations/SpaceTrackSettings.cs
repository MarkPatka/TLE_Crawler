namespace TLECrawler.Domain.Common.Configurations;

public record SpaceTrackSettings
{
    public string BaseURL { get; init; } = null!;
    public string AuthURL { get; init; } = null!;
    public string DataURL { get; init; } = null!;
}
