using Microsoft.Extensions.Logging;

using System.Data;

using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Extensions;
using TLECrawler.Domain.TLEModel;
using TLECrawler.Helpers.Comparers;
using TLECrawler.Helpers.TypeHelper;

namespace TLECrawler.Infrastructure.Services;

public class TLEService : ITLEService
{
    private readonly ITLERepository _tleRepository;
    private readonly ILogger<TLEService> _logger;    

    public TLEService(ITLERepository tleRepository, ILogger<TLEService> logger) =>
        (_tleRepository,  _logger) = (tleRepository, logger);

    public async Task<int> PersistUnic(TLE_ST[] tles, int iterationId)
    {
        try
        {
            List<TLE> newTles = FilterSpaceTrackResponse(tles, iterationId);
            if (newTles.Count == 0) return 0;

            List<TLE> oldTles = await GetAlreadyPersistedTLEsAsync(newTles);
            
            if (oldTles.Count == 0)
            {
                await _tleRepository.InsertManyAsync(newTles);
                return newTles.Count;
            }

            if (oldTles.Count == newTles.Count)
            {
                _logger.LogInformation($"New TLEs received: 0");
                return 0;
            }

            List<TLE> uniqueTles = newTles.SubtractSet(oldTles);
         // List<TLE> uniqueTles = GetUnique(oldTles, newTles)

            string msg = $"New TLEs received: {uniqueTles.Count}";
            _logger.LogInformation("{MSG}", msg);

            await _tleRepository.InsertManyAsync(uniqueTles);
            return uniqueTles.Count;
        }
        catch (Exception ex) 
        {
            string message = "Fail to persist TLEs received from Space-Track.org";
            var exception = new TLEPersistException(message, iterationId, ex);
            _logger.LogError(exception, "{MSG}", message);
            throw exception;
        }       
    }
    
    private List<TLE> FilterSpaceTrackResponse(TLE_ST[] input, int iterationId)
    {
        if (input.Length == 0)
        {
            string message = $"Empty responce reseived from Space-Track";
            _logger.LogInformation("{MSG}", message);
            return [];
        }

        Dictionary<ReadOnlyMemory<byte>, TLE> uniqueTles = new(new ByteArrayComparer());
        int duplicatesCount = 0;

        try
        {
            foreach (var tle in input)
            {
                TLE uniqueTle = new(
                    tle.PublishDate, tle.FirstRow, tle.SecondRow, tle.CalculateHash(), iterationId); ;

                if (!uniqueTles.TryAdd(uniqueTle.Hash, uniqueTle))
                {
                    duplicatesCount++;
                }
            }

            if (duplicatesCount > 0)
            {
                string message = $"{duplicatesCount} duplicates has been filtered from the Space-Track response";
                _logger.LogInformation("{MSG}", message);
            }
            else
            {
                string message = $"All TLEs in the responce were unique";
                _logger.LogInformation("{MSG}", message);
            }

            return [..uniqueTles.Values];
        }
        catch (Exception ex)
        {
            string message = "";
            if (ex is AggregateException ae)
            {
                message = ae.Flatten().InnerException?.Message ?? "";
            }
            _logger.LogError(ex, "{MSG}", message);
            throw new Exception(ex.Message, ex.InnerException);
        }


        //Dictionary<string, byte[]> calculatedUnicHashes = new(StringComparer.Ordinal);
        //try
        //{
        //    foreach (var tle in input)
        //    {
        //        byte[] hash = tle.CalculateHash();
        //        string hashString = hash.HashToString();

        //        if (calculatedUnicHashes.TryAdd(hashString, hash))
        //        {
        //            TLE parsedTLE = new(
        //                tle.PublishDate,
        //                tle.FirstRow,
        //                tle.SecondRow,
        //                hash,
        //                iterationId);

        //            unicParsedTLEs.Add(parsedTLE);
        //        }
        //    }            

        //    int duplicatesInResponse = input.Length - unicParsedTLEs.Count;
        //    string message = $"{duplicatesInResponse}/{input.Length} duplicates filtered from the Space-Track response";
        //    _logger.LogInformation("{MSG}", message);

        //    return unicParsedTLEs;
        //}
        //catch (Exception ex)
        //{
        //    string message = "";
        //    if (ex is AggregateException ae)
        //    {
        //        message = ae.Flatten().InnerException?.Message ?? "";
        //    }
        //    _logger.LogError(ex, "{MSG}", message);
        //    throw new Exception(ex.Message, ex.InnerException);
        //}
    }
    private async Task<List<TLE>> GetAlreadyPersistedTLEsAsync(
        IEnumerable<TLE> filteredTLEs, int batchSize = 5000)
    {
        var filteredTlesArr = filteredTLEs.ToArray();

        var chunks = filteredTlesArr.Chunk(batchSize);
        var result = new List<TLE>(filteredTlesArr.Length);

        foreach (var chunk in chunks)
        {
            var hashCodes = chunk.Select(tle => tle.Hash);
            //List<TLE> groupResult = await _tleRepository.GetByHashesAsync(hashCodes);
            var chunkResult = await _tleRepository.GetByTvpHashesAsync(hashCodes);
            result.AddRange(chunkResult);
        }

        if (result.Count > 0)
        {
            string message = $"The Database have already contained {result.Count} tles";
            _logger.LogInformation("{MSG}", message);
        }
        return result;
    }
    private static List<TLE> GetUnique(List<TLE> alreadyPersistedTLEs, List<TLE> receivedFilteredTLEs)
    {
        List<byte[]> hashesOfDuplicates = [.. alreadyPersistedTLEs.Select(t => t.Hash)];
        int cnt = hashesOfDuplicates.Count;

        List<TLE> NewTLEs = [];
        for (int i = 0; i < receivedFilteredTLEs.Count; i++)
        {
            byte[] target = receivedFilteredTLEs[i].Hash;

            if (target.ContainsIn(hashesOfDuplicates)) { cnt--; continue; }

            if (cnt <= 0) { NewTLEs.AddRange(receivedFilteredTLEs[i..]); break; }

            NewTLEs.Add(receivedFilteredTLEs[i]);
        }
        return NewTLEs;
    }
}
