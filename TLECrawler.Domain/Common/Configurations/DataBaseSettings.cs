namespace TLECrawler.Domain.Common.Configurations;

public record DataBaseSettings
{
    public string DataSource     { get; init; } = null!;
    public string InitialCatalog { get; init; } = null!;
    public string UserID         { get; init; } = null!;
    public string Password       { get; init; } = null!;
    public int Timeout           { get; init; } = 600;
    public bool Encrypt          { get; init; } = true;
}
