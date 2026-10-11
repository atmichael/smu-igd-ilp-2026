using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.InboxMonitoring.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ILP.Shared.Test.InboxMonitoring.Worker;

public class InboxMonitorWorkerTest
{
    private sealed class CountingSync(bool fail = false) : IInboxSyncService
    {
        private int _count;
        public int Count => _count;
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<InboxSyncResultDto> SyncAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            Called.TrySetResult();
            if (fail)
            {
                throw new InvalidOperationException("fail");
            }

            return Task.FromResult(new InboxSyncResultDto());
        }
    }

    private static InboxMonitorWorker Create(IInboxSyncService sync, bool enabled) =>
        new(sync, Options.Create(new InboxMonitoringOptions { Enabled = enabled, PollIntervalSeconds = 1 }),
            NullLogger<InboxMonitorWorker>.Instance);

    [Fact]
    public async Task Worker_WhenEnabled_RunsSyncOnStartup()
    {
        var sync = new CountingSync();
        var worker = Create(sync, true);

        await worker.StartAsync(CancellationToken.None);
        await sync.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(sync.Count >= 1);
    }

    [Fact]
    public async Task Worker_WhenSyncThrows_KeepsRunning()
    {
        var sync = new CountingSync(fail: true);
        var worker = Create(sync, true);

        await worker.StartAsync(CancellationToken.None);
        await sync.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(sync.Count >= 1);
    }

    [Fact]
    public async Task Worker_WhenDisabled_NeverSyncs()
    {
        var sync = new CountingSync();
        var worker = Create(sync, false);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(0, sync.Count);
    }

    [Fact]
    public void Options_PollInterval_UsesConfiguredSecondsWithMinimumOfOne()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), new InboxMonitoringOptions { PollIntervalSeconds = 30 }.PollInterval);
        Assert.Equal(TimeSpan.FromSeconds(1), new InboxMonitoringOptions { PollIntervalSeconds = 0 }.PollInterval);
    }
}
