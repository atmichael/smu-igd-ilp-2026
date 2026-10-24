namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public abstract class AuditedEntity
{
    public long Id { get; set; }

    public int Version { get; set; } = 1;

    public bool ActiveStatus { get; set; } = true;

    public DateTime CreatedTime { get; set; }

    public DateTime ModifiedTime { get; set; }
}
