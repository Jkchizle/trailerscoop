using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.TrailerScoop.Services;

internal sealed class DownloadQueue : IAsyncDisposable
{
    private readonly SemaphoreSlim _throttle;
    private readonly List<Task> _running = new();

    public DownloadQueue(int maxParallel)
    {
        _throttle = new SemaphoreSlim(Math.Max(1, maxParallel));
    }

    public Task RunAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        var t = Task.Run(async () =>
        {
            await _throttle.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await action(ct).ConfigureAwait(false);
            }
            finally
            {
                _throttle.Release();
            }
        }, ct);
        _running.Add(t);
        return t;
    }

    public async Task WhenAllAsync(IEnumerable<Task> tasks) => await Task.WhenAll(tasks).ConfigureAwait(false);

    public ValueTask DisposeAsync()
    {
        _throttle.Dispose();
        return ValueTask.CompletedTask;
    }
}
