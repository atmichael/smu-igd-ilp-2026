using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using Microsoft.EntityFrameworkCore;

namespace ILP.Server.Features.InboxMonitoring.Persistence;

public sealed class InboxExtractionDbContext(DbContextOptions<InboxExtractionDbContext> options) : DbContext(options)
{
    public DbSet<CompanyEntity> Companies => Set<CompanyEntity>();

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    public DbSet<DocumentItemEntity> DocumentItems => Set<DocumentItemEntity>();

    public DbSet<InboxMessageEntity> InboxMessages => Set<InboxMessageEntity>();

    public DbSet<InboxMessageAttachmentEntity> InboxMessageAttachments => Set<InboxMessageAttachmentEntity>();

    public DbSet<DocumentInboxMessageEntity> DocumentInboxMessages => Set<DocumentInboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyEntity>(entity =>
        {
            entity.ToTable("Companies", table => table.HasCheckConstraint("CK_Companies_ActiveStatus", "\"ActiveStatus\" IN (0, 1)"));
            entity.HasIndex(company => company.Uen).IsUnique();
            entity.Property(company => company.Name).IsRequired();
            entity.Property(company => company.Address).IsRequired();
            entity.Property(company => company.TaxRegistrationNumber).IsRequired();
            ConfigureAuditFields(entity);
        });

        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("Documents", table => table.HasCheckConstraint("CK_Documents_ActiveStatus", "\"ActiveStatus\" IN (0, 1)"));
            entity.HasOne(document => document.Company)
                .WithMany(company => company.Documents)
                .HasForeignKey(document => document.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(document => new { document.CompanyId, document.TypeCode, document.RefNumber }).IsUnique();
            entity.Property(document => document.Confidence).HasPrecision(5, 4);
            ConfigureAuditFields(entity);
        });

        modelBuilder.Entity<DocumentItemEntity>(entity =>
        {
            entity.ToTable("DocumentItems", table => table.HasCheckConstraint("CK_DocumentItems_ActiveStatus", "\"ActiveStatus\" IN (0, 1)"));
            entity.HasOne(item => item.Document)
                .WithMany(document => document.DocumentItems)
                .HasForeignKey(item => item.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            ConfigureAuditFields(entity);
        });

        modelBuilder.Entity<InboxMessageEntity>(entity =>
        {
            entity.ToTable("InboxMessages", table => table.HasCheckConstraint("CK_InboxMessages_ActiveStatus", "\"ActiveStatus\" IN (0, 1)"));
            entity.HasIndex(message => message.UniqueId).IsUnique();
            ConfigureAuditFields(entity);
        });

        modelBuilder.Entity<InboxMessageAttachmentEntity>(entity =>
        {
            entity.ToTable("InboxMessageAttachments", table => table.HasCheckConstraint("CK_InboxMessageAttachments_ActiveStatus", "\"ActiveStatus\" IN (0, 1)"));
            entity.HasIndex(attachment => new { attachment.MessageId, attachment.DriveFileId }).IsUnique();
            entity.HasOne(attachment => attachment.Message)
                .WithMany(message => message.Attachments)
                .HasForeignKey(attachment => attachment.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            ConfigureAuditFields(entity);
        });

        modelBuilder.Entity<DocumentInboxMessageEntity>(entity =>
        {
            entity.ToTable("DocumentInboxMessages", table => table.HasCheckConstraint("CK_DocumentInboxMessages_ActiveStatus", "\"ActiveStatus\" IN (0, 1)"));
            entity.HasIndex(mapping => mapping.DocumentId).IsUnique();
            entity.HasOne(mapping => mapping.Document)
                .WithOne(document => document.InboxMessageMapping)
                .HasForeignKey<DocumentInboxMessageEntity>(mapping => mapping.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mapping => mapping.Message)
                .WithMany(message => message.DocumentMappings)
                .HasForeignKey(mapping => mapping.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mapping => mapping.SourceAttachment)
                .WithMany(attachment => attachment.DocumentMappings)
                .HasForeignKey(mapping => mapping.SourceAttachmentId)
                .OnDelete(DeleteBehavior.Restrict);
            ConfigureAuditFields(entity);
        });
    }

    private static void ConfigureAuditFields<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : AuditedEntity
    {
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Version).HasDefaultValue(1);
        entity.Property(record => record.Version).IsConcurrencyToken();
        entity.Property(record => record.ActiveStatus).HasDefaultValue(true);
        entity.Property(record => record.CreatedTime).IsRequired();
        entity.Property(record => record.ModifiedTime).IsRequired();
    }
}
