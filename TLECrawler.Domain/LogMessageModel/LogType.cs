using TLECrawler.Domain.Common;

namespace TLECrawler.Domain.LogMessageModel;

public class LogType(int id, string name, string? description = null) 
    : Enumeration(id, name, description)
{
    public static readonly LogType INFO = new(1, "Информация");
    public static readonly LogType ERROR = new(1, "Ошибка");
    public static readonly LogType WARNING = new(2, "Предупреждение");
    public static readonly LogType SERVICE = new(3, "Служебная информация");
    public static readonly LogType COMPLETE = new(3, "ИИ обработан");
}

