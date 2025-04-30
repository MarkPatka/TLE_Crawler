using TLECrawler.Domain.TLEModel;

namespace TLECrawler.Application.DAL;

public interface ITLERepository
{
    public TLE Get(byte[] HashCode, int year);
    public TLE Get(int id);
    public List<TLE> Get(IEnumerable<byte[]> hashCodes, int year);
    public Task<List<TLE>> GetAsync(IEnumerable<byte[]> hashCodes, int year);
    public List<TLE> GetFromPartition(int partitionYear);
    public Task<List<TLE>> GetByHashes(IEnumerable<byte[]> hashCodes);
    public DateTime GetDateTimeOfLastUploadedTLE();
    public void InsertOne(TLE tle);
    public void InsertMany(IEnumerable<TLE> tles);
    public Task InsertManyAsync(List<TLE> tles);
    public List<TLE> FetchTLEs(IEnumerable<byte[]> hashCodes, int offset, int batchSize, int year);
    public Task<List<TLE>> FetchTLEsAsync(IEnumerable<byte[]> hashCodes, int offset, int batchSize, int year);
}

