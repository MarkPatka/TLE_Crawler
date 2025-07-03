using TLECrawler.Domain.TLEModel;

namespace TLECrawler.Application.DAL;

public interface ITLERepository
{
    public Task<TLE> GetAsync(byte[] HashCode, int year);
    public Task<TLE> GetAsync(int id);
    public Task<List<TLE>> GetAsync(IEnumerable<byte[]> hashCodes, int year);
    public Task<List<TLE>> GetFromPartitionAsync(int partitionYear);
    public Task<List<TLE>> GetByHashesAsync(IEnumerable<byte[]> hashCodes);
    public Task<List<TLE>> GetByTvpHashesAsync(IEnumerable<byte[]> hashCodes);
    public Task<DateTime> GetDateTimeOfLastUploadedTLEAsync();
    public Task InsertOneAsync(TLE tle);
    public Task InsertManyAsync(IEnumerable<TLE> tles);
    public Task InsertManyAsync(List<TLE> tles);
    public Task<List<TLE>> FetchTLEsAsync(IEnumerable<byte[]> hashCodes, int offset, int batchSize, int year);
}

