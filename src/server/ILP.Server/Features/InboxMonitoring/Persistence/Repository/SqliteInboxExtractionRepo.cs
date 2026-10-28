using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.EntityFrameworkCore;

namespace ILP.Server.Features.InboxMonitoring.Persistence.Repository;

public sealed class SqliteInboxExtractionRepo(IDbContextFactory<InboxExtractionDbContext> contextFactory)
    : IInboxExtractionPersistence
{
    public async Task<bool> ExistsAsync(DocumentDto document, CancellationToken cancellationToken = default)
    {
        string? uen = document.Company.UEN;
        if (string.IsNullOrWhiteSpace(uen))
        {
            return false;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Documents.AnyAsync(
            existing => existing.Company.Uen == uen
                && existing.TypeCode == document.TypeCode
                && existing.RefNumber == document.RefNumber,
            cancellationToken);
    }

    public async Task SaveAsync(
        IReadOnlyList<DocumentInboxMessageMappingDto> mappings,
        CancellationToken cancellationToken = default)
    {
        if (mappings.Count == 0)
        {
            return;
        }

        var inboxMessage = mappings[0].InboxMessage;
        if (mappings.Any(mapping => mapping.InboxMessage.UniqueId != inboxMessage.UniqueId))
        {
            throw new ArgumentException("All mappings in a save operation must refer to the same inbox message.", nameof(mappings));
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        DateTime now = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(8), DateTimeKind.Unspecified);
        var companyEntities = new Dictionary<string, CompanyEntity>(StringComparer.OrdinalIgnoreCase);

        var messageEntity = await context.InboxMessages
            .Include(message => message.Attachments)
            .SingleOrDefaultAsync(message => message.UniqueId == inboxMessage.UniqueId, cancellationToken);

        if (messageEntity == null)
        {
            messageEntity = new InboxMessageEntity
            {
                UniqueId = inboxMessage.UniqueId,
                CreatedTime = now
            };
            context.InboxMessages.Add(messageEntity);
        }
        else
        {
            messageEntity.Version++;
        }

        messageEntity.Subject = inboxMessage.Subject;
        messageEntity.Sender = inboxMessage.From;
        messageEntity.Body = inboxMessage.Body;
        messageEntity.ModifiedTime = now;

        var attachmentEntities = new Dictionary<string, InboxMessageAttachmentEntity>(StringComparer.Ordinal);
        foreach (var attachment in inboxMessage.GoogleDriveAttachments)
        {
            var attachmentEntity = messageEntity.Attachments
                .SingleOrDefault(existing => existing.DriveFileId == attachment.DriveFileId);
            if (attachmentEntity == null)
            {
                attachmentEntity = new InboxMessageAttachmentEntity
                {
                    Message = messageEntity,
                    DriveFileId = attachment.DriveFileId,
                    CreatedTime = now
                };
                messageEntity.Attachments.Add(attachmentEntity);
            }
            else
            {
                attachmentEntity.Version++;
            }

            attachmentEntity.FileName = attachment.FileName;
            attachmentEntity.WebViewLink = attachment.WebViewLink;
            attachmentEntity.ModifiedTime = now;
            attachmentEntities[attachment.DriveFileId] = attachmentEntity;
        }

        foreach (var mapping in mappings)
        {
            var company = await FindOrAddCompanyAsync(context, mapping.Document.Company, companyEntities, now, cancellationToken);
            var document = MapDocument(mapping.Document, company, now);

            InboxMessageAttachmentEntity? sourceAttachment = null;
            if (mapping.SourceAttachment != null
                && !attachmentEntities.TryGetValue(mapping.SourceAttachment.DriveFileId, out sourceAttachment))
            {
                throw new ArgumentException("The mapped source attachment must be included in the inbox message.", nameof(mappings));
            }

            context.Documents.Add(document);
            context.DocumentInboxMessages.Add(new DocumentInboxMessageEntity
            {
                Document = document,
                Message = messageEntity,
                SourceAttachment = sourceAttachment,
                CreatedTime = now,
                ModifiedTime = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<CompanyEntity> FindOrAddCompanyAsync(
        InboxExtractionDbContext context,
        CompanyDto company,
        IDictionary<string, CompanyEntity> companyEntities,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string? uen = string.IsNullOrWhiteSpace(company.UEN) ? null : company.UEN;
        if (uen != null && companyEntities.TryGetValue(uen, out var existingCompanyInBatch))
        {
            return existingCompanyInBatch;
        }

        var entity = uen == null
            ? null
            : await context.Companies.SingleOrDefaultAsync(existing => existing.Uen == uen, cancellationToken);

        if (entity == null)
        {
            entity = new CompanyEntity
            {
                Uen = uen,
                CreatedTime = now
            };
            context.Companies.Add(entity);
        }
        else
        {
            entity.Version++;
        }

        entity.Name = company.Name;
        entity.Address = company.Address;
        entity.TaxRegistrationNumber = company.TaxRegistrationNumber;
        entity.ModifiedTime = now;
        if (uen != null)
        {
            companyEntities[uen] = entity;
        }

        return entity;
    }

    private static DocumentEntity MapDocument(DocumentDto document, CompanyEntity company, DateTime now)
    {
        var entity = new DocumentEntity
        {
            RefNumber = document.RefNumber,
            TypeCode = document.TypeCode,
            Company = company,
            SentDate = document.SentDate,
            TotalAmount = document.TotalAmount,
            RelatedDocumentNumbers = document.RelatedDocumentNumbers,
            Subtotal = document.Subtotal,
            SubtotalTaxRate = document.SubtotalTaxRate,
            SubtotalTaxAmount = document.SubtotalTaxAmount,
            Confidence = document.Confidence,
            CreatedTime = now,
            ModifiedTime = now
        };

        foreach (var item in document.DocumentItems)
        {
            entity.DocumentItems.Add(new DocumentItemEntity
            {
                Description = item.Description,
                Quantity = item.Quantity,
                CurrencyCode = item.CurrencyCode,
                UnitPrice = item.UnitPrice,
                TaxName = item.TaxName,
                TaxRate = item.TaxRate,
                SerialNumber = item.SerialNumber,
                Amount = item.Amount,
                TaxAmount = item.TaxAmount,
                CreatedTime = now,
                ModifiedTime = now
            });
        }

        return entity;
    }
}
