using ILP.Server.Features.EvidenceStorage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ILP.Server.Tests;

public sealed class EvidenceApiFactory : WebApplicationFactory<Program>
{
    public MutableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero));

    public FlakyEvidenceRepository Repository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("EvidenceStorage:Provider", "InMemory");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IEvidenceRepository>();
            services.AddSingleton<IEvidenceRepository>(Repository);
        });
    }
}

public sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public MutableTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}

/// <summary>Simulates storage outages for failed-save scenarios.</summary>
public sealed class FlakyEvidenceRepository : InMemoryEvidenceRepository
{
    private int _failNextSaves;

    public void FailNextSaves(int count) => Interlocked.Exchange(ref _failNextSaves, count);

    public override void Save(ILP.Shared.Evidence.EvidencePackage package)
    {
        if (Interlocked.Decrement(ref _failNextSaves) >= 0)
        {
            throw new IOException("Simulated storage failure.");
        }

        Interlocked.Exchange(ref _failNextSaves, 0);
        base.Save(package);
    }
}
