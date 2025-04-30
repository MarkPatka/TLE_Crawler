using TLECrawler.Domain.Common;

namespace TLECrawler.Domain.IterationModel;

public sealed class IterationStatus(int id, string name, string? description = null)
    : Enumeration(id, name, description)
{
    public static readonly IterationStatus UNKNOWN = new(0, "Default");
    public static readonly IterationStatus OK      = new(1, "Data is loaded");
    public static readonly IterationStatus ERROR   = new(2, "Processing error");
    public static readonly IterationStatus EMPTY   = new(3, "Data not exists");
}
