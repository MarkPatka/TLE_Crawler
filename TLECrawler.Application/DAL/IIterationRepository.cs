using TLECrawler.Domain.IterationModel;

namespace TLECrawler.Application.DAL;

public interface IIterationRepository
{
    public int InitializeIteration();
    public Task<int> InitializeIterationAsync();

    public void CompleteIteration(int id, Iteration iteration);
    public Iteration? GetById(int id);
    public Iteration GetLast();
}
