namespace TLECrawler.Domain.Common.Configurations;

public record SessionSettings
{
    public int SleepTime            { get; init; }
    public int RepeatTimes          { get; init; }
    public int RepeatAfterMinutes   { get; init; }
    public int AddDays              { get; init; }
    public bool UseSleepTime        { get; init; }
    public bool NeedRepeatAfterFail { get; init; }
    public bool UseFromDateTime     { get; init; }
    public DateTime FromDateTime    { get; init; }
    public string Epoch             { get; init; } = null!;
    public string[] CheckHours      { get; init; } = null!;
    public int IterationsCount => CheckHours.Length;
}
