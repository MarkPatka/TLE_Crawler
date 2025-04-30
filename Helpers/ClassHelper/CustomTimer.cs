namespace TLECrawler.Helpers.ClassHelper;

public delegate ValueTask TimerCallback(CancellationToken token);
public enum TimerStatus { Released, Locked }

public class CustomTimer
{
    private readonly TimeOnly[] _triggers;
    private readonly TimerCallback _callBack;
    private int _currentTriggerId;

    public TimerStatus Status { get; private set; }

    public CustomTimer(TimerCallback callBack, IEnumerable<TimeOnly> timeTriggers)
    {
        ArgumentNullException.ThrowIfNull(callBack, nameof(callBack));

        _callBack = callBack;
        _triggers = timeTriggers.ToArray();
        _currentTriggerId = StartFromNearest();
        Status = TimerStatus.Released;
    }

    public ValueTask GetIterationCallBack(CancellationToken cancellationToken)
    {
        return _callBack(cancellationToken);
    }

    public async Task WaitForNextIterationAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            await ValueTask.FromCanceled(cancellationToken);

        Status = TimerStatus.Locked;
        try
        {
            await WaitForNextTickAsync(
                _triggers[_currentTriggerId].ToTimeSpan());

            int count = Interlocked.Increment(ref _currentTriggerId);
            if (count == _triggers.Length) ResetIterator();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex.InnerException);
        }
        finally { Status = TimerStatus.Released; }
    }

    private async static Task WaitForNextTickAsync(TimeSpan targetTime)
    {
        while (true)
        {
            var now = DateTime.Now.TimeOfDay;

            if (now.Hours == targetTime.Hours && now.Minutes == targetTime.Minutes)
            {
                break;
            }

            var waitTime = (targetTime - now).TotalMilliseconds;

            if (waitTime > 0)
            {
                await Task.Delay((int)waitTime);
            }
        }
    }

    private int StartFromNearest()
    {
        int left = 0;
        int right = _triggers.Length - 1;

        TimeSpan currentTime = DateTime.Now.TimeOfDay;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;

            if (_triggers[mid].ToTimeSpan() > currentTime)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        if (left < _triggers.Length)
        {
            return left;
        }
        return 0;
    }

    private void ResetIterator()
    {
        lock (this)
        {
            _currentTriggerId = 0;
        }
    }
}
