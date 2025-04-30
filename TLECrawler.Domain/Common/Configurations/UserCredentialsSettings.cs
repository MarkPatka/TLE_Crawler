namespace TLECrawler.Domain.Common.Configurations;

public record UserCredentialsSettings
{
    public string Login    { get; init; } = null!;
    public string Password { get; init; } = null!;
}
