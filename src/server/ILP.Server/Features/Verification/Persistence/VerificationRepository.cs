using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Shared.Verification.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Concurrent;

namespace ILP.Server.Features.Verification.Persistence;

public interface IVerificationRepository
{
    VerificationRunDto SaveRun(VerificationRunDto run);
    VerificationRunDto? GetRun(string id);
    void Resolve(string reviewItemId, string userId, string reason);
}

public sealed class VerificationRepository : IVerificationRepository
{
    private readonly IDbContextFactory<InboxExtractionDbContext>? _contextFactory;
    private readonly ConcurrentDictionary<string, VerificationRunDto> _runs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, VerificationReviewResolutionEntity> _resolutions = new(StringComparer.Ordinal);

    public VerificationRepository()
    {
    }

    public VerificationRepository(IDbContextFactory<InboxExtractionDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public VerificationRunDto SaveRun(VerificationRunDto run)
    {
        if (_contextFactory is null)
        {
            _runs[run.VerificationRunId] = run;
            return run;
        }
        using var context = _contextFactory.CreateDbContext();
        context.VerificationRuns.Add(new VerificationRunEntity
        {
            VerificationRunId = run.VerificationRunId,
            DocumentId = run.DocumentId,
            RunJson = JsonSerializer.Serialize(run, JsonOptions),
            Status = run.Status.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        });
        context.SaveChanges();
        return run;
    }

    public VerificationRunDto? GetRun(string id)
    {
        if (_contextFactory is null)
            return _runs.TryGetValue(id, out var run) ? run : null;
        using var context = _contextFactory.CreateDbContext();
        var entity = context.VerificationRuns.AsNoTracking().SingleOrDefault(run => run.VerificationRunId == id);
        return entity is null ? null : JsonSerializer.Deserialize<VerificationRunDto>(entity.RunJson, JsonOptions);
    }

    public void Resolve(string reviewItemId, string userId, string reason)
    {
        var resolution = new VerificationReviewResolutionEntity
        {
            ReviewItemId = reviewItemId,
            UserId = userId,
            ResolutionReason = reason,
            ResolvedAt = DateTimeOffset.UtcNow
        };
        if (_contextFactory is null)
        {
            _resolutions[reviewItemId] = resolution;
            return;
        }
        using var context = _contextFactory.CreateDbContext();
        context.VerificationReviewResolution.Add(resolution);
        context.SaveChanges();
    }
}
