namespace TLECrawler.Domain.LogMessageModel;

public record Log(
    LogType Type,
    string Text,
    DateTime DateTime,
    int IterationId,
    string AdditionalData);
