using System.Text.Json.Serialization;
using System.Text.Json;

namespace TLECrawler.Domain.Common.Converters;

public class NORADConverter : JsonConverter<Int32>
{

    public override Int32 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string noradString = reader.GetString() 
            ?? throw new Exception("Input NORAD has incorrect format");

        return Int32.Parse(noradString);
    }
    
    public override void Write(Utf8JsonWriter writer, Int32 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
