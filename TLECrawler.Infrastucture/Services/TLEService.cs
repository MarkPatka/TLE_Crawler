using Microsoft.Extensions.Logging;

using System.Data;

using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Extensions;
using TLECrawler.Domain.TLEModel;
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
            List<TLE> UnicTLEsFromSpaceTrackResponse = [.. FilterSpaceTrackResponse(tles, iterationId)];
            List<TLE> AlreadyPersistedTLEs = await GetAlreadyPersistedTLEsAsync(UnicTLEsFromSpaceTrackResponse);
            List<TLE> unicTles = [.. UnicTLEsFromSpaceTrackResponse];
            
            if (AlreadyPersistedTLEs.Count > 0) 
            {
                unicTles = GetUnic(AlreadyPersistedTLEs, UnicTLEsFromSpaceTrackResponse);
            }

            string msg = $"Unic TLEs received: {unicTles.Count}";
            _logger.LogInformation("{MSG}", msg);

            if (unicTles.Count > 0)
            {
                await _tleRepository.InsertManyAsync(unicTles);
                return unicTles.Count;
            }
            return 0;
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
        List<TLE> unicParsedTLEs = [];
        Dictionary<string, byte[]> calculatedUnicHashes = new(StringComparer.Ordinal);

        try
        {
            foreach (var tle in input)
            {
                byte[] hash = tle.CalculateHash();
                string hashString = hash.HashToString();

                if (calculatedUnicHashes.TryAdd(hashString, hash))
                {
                    TLE parsedTLE = new(
                        tle.PublishDate,
                        tle.FirstRow,
                        tle.SecondRow,
                        hash,
                        iterationId);

                    unicParsedTLEs.Add(parsedTLE);
                }
            }            

            int duplicatesInResponse = input.Length - unicParsedTLEs.Count;
            string message = $"{duplicatesInResponse}/{input.Length} duplicates filtered from the Space-Track response";
            _logger.LogInformation("{MSG}", message);

            return unicParsedTLEs;
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
    }
    private async Task<List<TLE>> GetAlreadyPersistedTLEsAsync(IEnumerable<TLE> filteredTLEsToCheck, int batchSize = 500)
    {
        var filteredTLEs = filteredTLEsToCheck.ToArray();
        List<TLE> PersistedTLEs = new(filteredTLEs.Length);
        if (filteredTLEs.Length == 0) return [.. PersistedTLEs];

        var batches = filteredTLEs.GetBatches(batchSize);

        foreach (var batch in batches)
        {
            var hashCodes = batch.Select(tle => tle.Hash);
            List<TLE> groupResult = await _tleRepository.GetByHashes(hashCodes);
            PersistedTLEs.AddRange(groupResult);
        }
        if (PersistedTLEs.Count > 0)
        {
            string message = 
               $"The Database have already contained {PersistedTLEs.Count} " +
                "tles from the sample received from the Space-Track";

            _logger.LogInformation("{MSG}", message);
        }
        return PersistedTLEs;
    }
    private static List<TLE> GetUnic(List<TLE> alreadyPersistedTLEs, List<TLE> receivedFilteredTLEs)
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
