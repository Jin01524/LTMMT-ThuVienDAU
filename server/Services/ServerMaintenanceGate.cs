namespace ThuVienDAU.Server.Services;

public sealed class ServerMaintenanceGate
{
    private readonly SemaphoreSlim stateLock = new(1, 1);
    private readonly SemaphoreSlim maintenanceQueue = new(1, 1);
    private int activeRequests;
    private bool maintenancePending;
    private TaskCompletionSource<bool> requestsDrained = CompletedSource();
    private TaskCompletionSource<bool> maintenanceEnded = CompletedSource();

    public async ValueTask<IDisposable> EnterRequestAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await stateLock.WaitAsync(cancellationToken);
            if (!maintenancePending)
            {
                if (activeRequests++ == 0)
                    requestsDrained = NewSource();
                stateLock.Release();
                return new Lease(ReleaseRequest);
            }

            var resume = maintenanceEnded.Task;
            stateLock.Release();
            await resume.WaitAsync(cancellationToken);
        }
    }

    public async ValueTask<IDisposable> EnterMaintenanceAsync(CancellationToken cancellationToken)
    {
        await maintenanceQueue.WaitAsync(cancellationToken);
        var markedPending = false;
        try
        {
            await stateLock.WaitAsync(cancellationToken);
            maintenancePending = true;
            maintenanceEnded = NewSource();
            var drained = activeRequests == 0 ? Task.CompletedTask : requestsDrained.Task;
            markedPending = true;
            stateLock.Release();

            await drained.WaitAsync(cancellationToken);
            return new Lease(ReleaseMaintenance);
        }
        catch
        {
            if (markedPending)
                ReleaseMaintenance();
            else
                maintenanceQueue.Release();
            throw;
        }
    }

    private void ReleaseRequest()
    {
        stateLock.Wait();
        try
        {
            if (--activeRequests == 0)
                requestsDrained.TrySetResult(true);
        }
        finally
        {
            stateLock.Release();
        }
    }

    private void ReleaseMaintenance()
    {
        stateLock.Wait();
        try
        {
            maintenancePending = false;
            maintenanceEnded.TrySetResult(true);
        }
        finally
        {
            stateLock.Release();
            maintenanceQueue.Release();
        }
    }

    private static TaskCompletionSource<bool> NewSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<bool> CompletedSource()
    {
        var source = NewSource();
        source.SetResult(true);
        return source;
    }

    private sealed class Lease(Action release) : IDisposable
    {
        private Action? release = release;

        public void Dispose() => Interlocked.Exchange(ref release, null)?.Invoke();
    }
}
