using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using TLECrawler.Domain.TLEModel;
using TLECrawler.Helpers.TypeHelper;

namespace TLECrawler.Domain.Common.Extensions;

public static class TleExtensions
{
    public static byte[] CalculateHash(this ITLE tle)
    {
        string row = string.Join(' ', tle.FirstRow, tle.SecondRow); 
        var rows = Encoding.UTF8.GetBytes(row);
        byte[] hashBytes = MD5.HashData(rows);
        return hashBytes;
    }

    public static List<TLE[]> GetBatches(this IEnumerable<TLE> tles, int batchSize)
    {
        List<TLE[]> batches =
            tles
            .Select((x, i) => new
            {
                Index = i,
                Value = x
            })
            .GroupBy(x => x.Index / batchSize)
            .Select(x => x.Select(v => v.Value)
            .ToArray())
            .ToList();

        return batches;
    }

    public static List<TLE> SubtractSet(this IEnumerable<TLE> filteredResponse, IEnumerable<TLE> alreadyPersisted)
    {
        List<TLE> response = filteredResponse.ToList();
        List<byte[]> databaseTLEsHashCodes = alreadyPersisted
            .Select(t => t.Hash)
            .ToList();

        List<TLE> result = [];
        foreach (TLE tle in response)
        {
            if (!tle.Hash.ContainsIn(databaseTLEsHashCodes))
            {
                result.Add(tle);
            }
        }
        return result;
    }
    
    public static bool ContainsIn(this byte[] target, ConcurrentBag<TLE> templates)
    {
        foreach (TLE item in templates)
        {
            if (target.SimpleEqualityCheck(item.Hash))
            {
                return true;
            }
        }
        return false;
    }


    //ЭТА ФУНКЦИЯ ХЭШИРОВАНИЯ ГОВНО, НО ВЫ МОЖЕТЕ ПОПЫТАТЬСЯ ЕЁ УЛУЧШИТЬ
    //private static long Hash(long value)
    //{
    //    Int32 murmurHash = 0x5bd1e995;
    //
    //    value = ~value + (value << 21);                 // Негатив + сдвиг
    //    value ^= (value >> 24);                         // XOR с правым сдвигом
    //    value = (value + (value << 3)) + (value << 8);  // Сдвиги и сложение
    //    value ^= (value >> 14);                         // XOR с правым сдвигом
    //    value = (value + (value << 5)) + (value << 11); // Сдвиги и сложение
    //    value ^= (value >> 28);                         // XOR с правым сдвигом
    //
    //    value = (value * murmurHash) ^ (value >> 15); //
    //    value = (value * murmurHash) ^ (value >> 13); //
    //    value = (value * murmurHash) ^ (value >> 12); // Умножение и XOR
    //                                                  //
    //    value = (value ^ (value >> 27)) * murmurHash; //
    //    return value;
    //}
    //
}
