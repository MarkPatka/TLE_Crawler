using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;

using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Domain.IterationModel;
using TLECrawler.Domain.TLEModel;

namespace TLECrawler.Infrastructure.Services;

public delegate Task IterationUnit();

public class IterationService : IIterationService
{
    private readonly IOptions<SessionSettings> _options;
    private readonly IIterationRepository _iterationsRepository;
    private readonly ISpaceTrackService _spaceTrackService;
    private readonly ITLEService _tleService;
    private readonly ILogger<IterationService> _logger;

    private int _currentIterationId = -1;
    private TLE_ST[] _receivedTLEs = [];
    private int _savedTLEsCount = -1;
    private IterationStatus _iterationStatus;

    public IterationService(
        IIterationRepository iterationsRepository, 
        ISpaceTrackService spaceTrackService,
        ITLEService tleService,
        IOptions<SessionSettings> options,
        ILogger<IterationService> logger)
    {
        _iterationsRepository = iterationsRepository;
        _spaceTrackService = spaceTrackService;
        _tleService = tleService;
        _options = options;
        _iterationStatus = IterationStatus.UNKNOWN;
        _logger = logger;
    }
   
    public async Task StartIterationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("New Iteration started");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // initialize iteration
            Task init = InitIteration();
            await init;

            // get new tles from web source
            Task get = GetTLEsFromSpaceTrack();
            await get;

            // filter and persist unique tles tnto the database
            Task persist = PersistFilteredTLEs();
            await persist;
        }      
        finally { CompleteIteration(); }
    }

    private async Task InitIteration()
    {
        try
        {
            await Init();
        }
        catch (CreateIterationExcepion ex) 
        {
            await ProcessException(Init, ex);
        }
    }
    private async Task Init()
    {
        _currentIterationId = await _iterationsRepository
            .InitializeIterationAsync();
    }
    
    private async Task GetTLEsFromSpaceTrack()
    {
        try
        {
            await Get();
        }
        catch (TLEParseException ex)
        {
            await ProcessException(Get, ex);
        }
    }
    private async Task Get()
    {        
        _receivedTLEs = await _spaceTrackService
            .GetNewTLEsFromSpaceTrack();

        string msg = $"{_receivedTLEs.Length} TLEs received from ST";
        _logger.LogInformation("{MSG}", msg);
    }

    private async Task PersistFilteredTLEs()
    {
        try
        {
            await Persist();
        }
        catch (TLEPersistException ex)
        {
            await ProcessException(Persist, ex);
        }
    }
    private async Task Persist()
    {
        _savedTLEsCount = await _tleService
            .PersistUnic(_receivedTLEs, _currentIterationId);
        
        if (_savedTLEsCount >= 1)
        {
            _iterationStatus = IterationStatus.OK;
            
            string msg = $"{_savedTLEsCount} TLEs saved successfully";
            _logger.LogInformation("{MSG}", msg);

            return;
        }

        if (_savedTLEsCount == 0)
        {
            _iterationStatus = IterationStatus.EMPTY;
            return;
        }
    }

    private async Task<TaskStatus> RepeatIterationUnitAsync(IterationUnit unit)
    {
        int repeats = 0;
        int sleepMinutes = _options.Value.SleepTime;
        TaskStatus status = TaskStatus.Faulted;

        while (repeats < _options.Value.RepeatTimes)
        {
            int number = repeats + 1;

            string msg1 = $"Attempt to repeat \"{unit.Method.Name}\" method. Try number: {number}";
            _logger.LogInformation("{MSG}", msg1);
            try
            {
                Task taskUnit = unit.Invoke();
                await taskUnit;
                if (taskUnit.IsCompletedSuccessfully)
                {
                    _logger.LogInformation("Attempt is completed successfully");
                    status = TaskStatus.RanToCompletion;
                    break;
                }
                status = TaskStatus.Faulted;
            }
            catch (Exception ex) 
            {
                var nextRepeat = TimeSpan.FromMinutes(sleepMinutes);

                string msg2 = 
                    $"The {number} attempt to repeat the operation was unsuccessful. " +
                    $"Waiting for {sleepMinutes} minutes to the next attempt";

                _logger.LogError(ex, "{MSG}", msg2);

                repeats++;
                status = TaskStatus.Faulted;
                await Task.Delay(nextRepeat);
            }          
        }
        return status;
    }
    private async Task ProcessException(IterationUnit task, Exception ex)
    {
        _logger.LogError(ex.InnerException, "{MSG}", ex.Message);
        Task<TaskStatus> tryRepeat = RepeatIterationUnitAsync(task);
        await tryRepeat;

        if (tryRepeat.Result != TaskStatus.RanToCompletion)
        {
            StateErrorWithMessage(); 
            return;
        }
        _iterationStatus = IterationStatus.OK;
    }
    
    private void CompleteIteration()
    {
        try
        {
            var currentIteration = _iterationsRepository
                .GetById(_currentIterationId);

            if (currentIteration is not null)
            {
                _iterationsRepository.CompleteIteration(
                    _currentIterationId,
                    new Iteration(
                        StartDateTime: currentIteration.StartDateTime,
                        EndDateTime: DateTime.UtcNow,
                        Status: _iterationStatus,
                        TLECount: _savedTLEsCount,
                        IsRepeat: false));

                string msgIterationCompleted = $"Iteration {_currentIterationId} persisted with status: {_iterationStatus.Name}";
                _logger.LogInformation("{MSG}", msgIterationCompleted);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to complete iteration: {ex}", ex.Message);
        }
        finally 
        {
            _iterationStatus = IterationStatus.UNKNOWN;
            _receivedTLEs = [];
            _savedTLEsCount = -1;
            _currentIterationId = -1;
        }
        return;
    }
    private void StateErrorWithMessage()
    {
        _iterationStatus = IterationStatus.ERROR;
        string msg = "Failed to execute operation after multiple attempts";
        _logger.LogError("{MSG}", msg);
    }
}
