using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ILP.Shared.Test.InboxMonitoring.Worker;

public class InboxWatchRenewalWorkerTest
{
    private sealed class FakeWatchClient(bool fail = false) : IInboxWatchClient
    {
        public List<string> Topics { get; } = [];
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WatchAsync(string topicName, CancellationToken cancellationToken = default)
        {
            Topics.Add(topicName);
            Called.TrySetResult();
            return fail ? throw new InvalidOperationException("fail") : Task.CompletedTask;
        }
    }

    private static InboxWatchRenewalWorker Create(IInboxWatchClient client, bool enabled) =>
        new(client, Options.Create(new InboxMonitoringOptions { Enabled = enabled }),
            NullLogger<InboxWatchRenewalWorker>.Instance);

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public async Task Worker_WhenEnabled_RegistersWatchOnStartupWithConfiguredTopic()
    {
        InboxWatchConfig.Initialize(Config(new() { ["AppSettings:InboxWatch:TopicName"] = "projects/p/topics/t" }));
        var client = new FakeWatchClient();
        var worker = Create(client, true);

        await worker.StartAsync(CancellationToken.None);
        await client.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal("projects/p/topics/t", Assert.Single(client.Topics));
    }

    [Fact]
    public async Task Worker_WhenWatchThrows_KeepsRunning()
    {
        InboxWatchConfig.Initialize(Config([]));
        var client = new FakeWatchClient(fail: true);
        var worker = Create(client, true);

        await worker.StartAsync(CancellationToken.None);
        await client.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.Single(client.Topics);
    }

    [Fact]
    public async Task Worker_WhenDisabled_NeverWatches()
    {
        var client = new FakeWatchClient();
        var worker = Create(client, false);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Empty(client.Topics);
    }

    [Fact]
    public void Config_Defaults()
    {
        InboxWatchConfig.Initialize(Config([]));

        Assert.Equal("projects/ethereal-yen-232614/subscriptions/ilpteam5-inbox", InboxWatchConfig.TopicName);
        Assert.Equal(TimeSpan.FromHours(24), InboxWatchConfig.RenewInterval);
    }

    [Fact]
    public void Config_UsesConfiguredIntervalAndIgnoresInvalidValues()
    {
        InboxWatchConfig.Initialize(Config(new() { ["AppSettings:InboxWatch:RenewIntervalHours"] = "6" }));
        Assert.Equal(TimeSpan.FromHours(6), InboxWatchConfig.RenewInterval);

        InboxWatchConfig.Initialize(Config(new() { ["AppSettings:InboxWatch:RenewIntervalHours"] = "0" }));
        Assert.Equal(TimeSpan.FromHours(24), InboxWatchConfig.RenewInterval);
        InboxWatchConfig.Initialize(Config([]));
    }

    [Fact]
    public void Options_PollIntervalDefaultsTo24Hours()
    {
        Assert.Equal(TimeSpan.FromHours(24), new InboxMonitoringOptions().PollInterval);
    }
}
