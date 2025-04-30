using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLECrawler.Domain.Common.Converters;

public class DateTimeConverter : JsonConverter<DateTime>
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string dateString = reader.GetString() 
            ?? throw new Exception("Input date string has incorrect format");

        return DateTime.ParseExact(dateString, DateTimeFormat, null);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateTimeFormat));
    }
}
