using System.Data;

namespace TLECrawler.Helpers.SqlHelper;

public static class SQLTypesConverter
{
    public static SqlDbType? ConvertToDBValueFrom(object value)
    {
        if (value == null) return null;

        Type type = value.GetType();

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int32:    return SqlDbType.Int;
            case TypeCode.Int64:    return SqlDbType.BigInt;
            case TypeCode.String:   return SqlDbType.VarChar;
            case TypeCode.Boolean:  return SqlDbType.Bit;
            case TypeCode.DateTime: return SqlDbType.DateTime;
            case TypeCode.Decimal:  return SqlDbType.Decimal;
            case TypeCode.Double:   return SqlDbType.Float;
            case TypeCode.Object:
                if (type == typeof(byte[]))
                {
                    return SqlDbType.VarBinary;
                }
                else if (type == typeof(Guid))
                {
                    return SqlDbType.UniqueIdentifier;
                }
            break;
        }
        throw new ArgumentException($"Unknown type: {type}");
    }
}
