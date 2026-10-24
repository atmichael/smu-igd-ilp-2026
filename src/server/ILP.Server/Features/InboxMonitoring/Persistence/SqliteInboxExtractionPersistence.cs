using System.Globalization;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Data.Sqlite;

namespace ILP.Server.Features.InboxMonitoring.Persistence;

public sealed class SqliteInboxExtractionPersistence : IInboxExtractionPersistence, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SqliteInboxExtractionPersistence(string connectionString = "Data Source=:memory:")
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS Companies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Uen TEXT NULL UNIQUE,
                Name TEXT NOT NULL,
                Address TEXT NOT NULL,
                TaxRegistrationNumber TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                ActiveStatus INTEGER NOT NULL DEFAULT 1 CHECK (ActiveStatus IN (0, 1)),
                CreatedTime TEXT NOT NULL,
                ModifiedTime TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS InboxMessages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UniqueId INTEGER NOT NULL UNIQUE,
                Subject TEXT NOT NULL,
                Sender TEXT NOT NULL,
                Body TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                ActiveStatus INTEGER NOT NULL DEFAULT 1 CHECK (ActiveStatus IN (0, 1)),
                CreatedTime TEXT NOT NULL,
                ModifiedTime TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Documents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RefNumber TEXT NOT NULL,
                TypeCode TEXT NOT NULL,
                CompanyId INTEGER NOT NULL REFERENCES Companies(Id),
                SentDate TEXT NOT NULL,
                TotalAmount TEXT NOT NULL,
                RelatedDocumentNumbers TEXT NULL,
                Subtotal TEXT NOT NULL,
                SubtotalTaxRate TEXT NOT NULL,
                SubtotalTaxAmount TEXT NOT NULL,
                Confidence TEXT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                ActiveStatus INTEGER NOT NULL DEFAULT 1 CHECK (ActiveStatus IN (0, 1)),
                CreatedTime TEXT NOT NULL,
                ModifiedTime TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS DocumentItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentId INTEGER NOT NULL REFERENCES Documents(Id),
                Description TEXT NOT NULL,
                Quantity TEXT NOT NULL,
                CurrencyCode TEXT NOT NULL,
                UnitPrice TEXT NOT NULL,
                TaxName TEXT NOT NULL,
                TaxRate TEXT NOT NULL,
                SerialNumber INTEGER NULL,
                Amount TEXT NULL,
                TaxAmount TEXT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                ActiveStatus INTEGER NOT NULL DEFAULT 1 CHECK (ActiveStatus IN (0, 1)),
                CreatedTime TEXT NOT NULL,
                ModifiedTime TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS InboxMessageAttachments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MessageId INTEGER NOT NULL REFERENCES InboxMessages(Id),
                FileName TEXT NOT NULL,
                DriveFileId TEXT NOT NULL,
                WebViewLink TEXT NOT NULL,
                Version INTEGER NOT NULL DEFAULT 1,
                ActiveStatus INTEGER NOT NULL DEFAULT 1 CHECK (ActiveStatus IN (0, 1)),
                CreatedTime TEXT NOT NULL,
                ModifiedTime TEXT NOT NULL,
                UNIQUE (MessageId, DriveFileId)
            );
            CREATE TABLE IF NOT EXISTS DocumentInboxMessages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentId INTEGER NOT NULL UNIQUE REFERENCES Documents(Id),
                MessageId INTEGER NOT NULL REFERENCES InboxMessages(Id),
                SourceAttachmentId INTEGER NULL REFERENCES InboxMessageAttachments(Id),
                Version INTEGER NOT NULL DEFAULT 1,
                ActiveStatus INTEGER NOT NULL DEFAULT 1 CHECK (ActiveStatus IN (0, 1)),
                CreatedTime TEXT NOT NULL,
                ModifiedTime TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task SaveAsync(IReadOnlyList<DocumentInboxMessageMappingDto> mappings, CancellationToken cancellationToken = default)
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

        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var transaction = _connection.BeginTransaction();
            string now = DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            long messageId = await SaveInboxMessageAsync(inboxMessage, now, transaction, cancellationToken);
            var attachmentIds = new Dictionary<string, long>(StringComparer.Ordinal);

            foreach (var attachment in inboxMessage.GoogleDriveAttachments)
            {
                attachmentIds[attachment.DriveFileId] = await SaveAttachmentAsync(attachment, messageId, now, transaction, cancellationToken);
            }

            foreach (var mapping in mappings)
            {
                long companyId = await SaveCompanyAsync(mapping.Document.Company, now, transaction, cancellationToken);
                long documentId = await SaveDocumentAsync(mapping.Document, companyId, now, transaction, cancellationToken);
                await SaveDocumentItemsAsync(mapping.Document, documentId, now, transaction, cancellationToken);
                long? attachmentId = null;
                if (mapping.SourceAttachment != null)
                {
                    if (!attachmentIds.TryGetValue(mapping.SourceAttachment.DriveFileId, out var foundAttachmentId))
                    {
                        throw new ArgumentException("The mapped source attachment must be included in the inbox message.", nameof(mappings));
                    }

                    attachmentId = foundAttachmentId;
                }

                await SaveMappingAsync(documentId, messageId, attachmentId, now, transaction, cancellationToken);
            }

            transaction.Commit();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _gate.Dispose();
    }

    private async Task<long> SaveInboxMessageAsync(
        InboxMessageDto message,
        string now,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        long? existingId = await FindIdAsync("SELECT Id FROM InboxMessages WHERE UniqueId = $uniqueId", transaction, cancellationToken,
            ("$uniqueId", message.UniqueId));

        if (existingId.HasValue)
        {
            await ExecuteAsync(
                "UPDATE InboxMessages SET Subject = $subject, Sender = $sender, Body = $body, Version = Version + 1, ModifiedTime = $modified WHERE Id = $id",
                transaction, cancellationToken,
                ("$subject", message.Subject), ("$sender", message.From), ("$body", message.Body), ("$modified", now), ("$id", existingId.Value));
            return existingId.Value;
        }

        return await InsertAsync(
            """
            INSERT INTO InboxMessages (UniqueId, Subject, Sender, Body, CreatedTime, ModifiedTime)
            VALUES ($uniqueId, $subject, $sender, $body, $created, $modified)
            """,
            transaction, cancellationToken,
            ("$uniqueId", message.UniqueId), ("$subject", message.Subject), ("$sender", message.From),
            ("$body", message.Body), ("$created", now), ("$modified", now));
    }

    private async Task<long> SaveAttachmentAsync(
        InboxMessageAttachmentDto attachment,
        long messageId,
        string now,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        long? existingId = await FindIdAsync(
            "SELECT Id FROM InboxMessageAttachments WHERE MessageId = $messageId AND DriveFileId = $fileId",
            transaction, cancellationToken, ("$messageId", messageId), ("$fileId", attachment.DriveFileId));

        if (existingId.HasValue)
        {
            await ExecuteAsync(
                "UPDATE InboxMessageAttachments SET FileName = $fileName, WebViewLink = $link, Version = Version + 1, ModifiedTime = $modified WHERE Id = $id",
                transaction, cancellationToken,
                ("$fileName", attachment.FileName), ("$link", attachment.WebViewLink), ("$modified", now), ("$id", existingId.Value));
            return existingId.Value;
        }

        return await InsertAsync(
            """
            INSERT INTO InboxMessageAttachments (MessageId, FileName, DriveFileId, WebViewLink, CreatedTime, ModifiedTime)
            VALUES ($messageId, $fileName, $fileId, $link, $created, $modified)
            """,
            transaction, cancellationToken,
            ("$messageId", messageId), ("$fileName", attachment.FileName), ("$fileId", attachment.DriveFileId),
            ("$link", attachment.WebViewLink), ("$created", now), ("$modified", now));
    }

    private async Task<long> SaveCompanyAsync(
        CompanyDto company,
        string now,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        string? uen = string.IsNullOrWhiteSpace(company.UEN) ? null : company.UEN;
        long? existingId = uen == null
            ? null
            : await FindIdAsync("SELECT Id FROM Companies WHERE Uen = $uen", transaction, cancellationToken, ("$uen", uen));

        if (existingId.HasValue)
        {
            await ExecuteAsync(
                """
                UPDATE Companies SET Name = $name, Address = $address, TaxRegistrationNumber = $tax,
                    Version = Version + 1, ModifiedTime = $modified WHERE Id = $id
                """,
                transaction, cancellationToken,
                ("$name", company.Name), ("$address", company.Address), ("$tax", company.TaxRegistrationNumber),
                ("$modified", now), ("$id", existingId.Value));
            return existingId.Value;
        }

        return await InsertAsync(
            """
            INSERT INTO Companies (Uen, Name, Address, TaxRegistrationNumber, CreatedTime, ModifiedTime)
            VALUES ($uen, $name, $address, $tax, $created, $modified)
            """,
            transaction, cancellationToken,
            ("$uen", uen), ("$name", company.Name), ("$address", company.Address),
            ("$tax", company.TaxRegistrationNumber), ("$created", now), ("$modified", now));
    }

    private async Task<long> SaveDocumentAsync(
        DocumentDto document,
        long companyId,
        string now,
        SqliteTransaction transaction,
        CancellationToken cancellationToken) =>
        await InsertAsync(
            """
            INSERT INTO Documents
                (RefNumber, TypeCode, CompanyId, SentDate, TotalAmount, RelatedDocumentNumbers, Subtotal,
                 SubtotalTaxRate, SubtotalTaxAmount, Confidence, CreatedTime, ModifiedTime)
            VALUES
                ($refNumber, $typeCode, $companyId, $sentDate, $totalAmount, $related, $subtotal,
                 $taxRate, $taxAmount, $confidence, $created, $modified)
            """,
            transaction, cancellationToken,
            ("$refNumber", document.RefNumber), ("$typeCode", document.TypeCode), ("$companyId", companyId),
            ("$sentDate", document.SentDate.ToString("O", CultureInfo.InvariantCulture)),
            ("$totalAmount", Format(document.TotalAmount)), ("$related", document.RelatedDocumentNumbers),
            ("$subtotal", Format(document.Subtotal)), ("$taxRate", Format(document.SubtotalTaxRate)),
            ("$taxAmount", Format(document.SubtotalTaxAmount)), ("$confidence", Format(document.Confidence)),
            ("$created", now), ("$modified", now));

    private async Task SaveDocumentItemsAsync(
        DocumentDto document,
        long documentId,
        string now,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var item in document.DocumentItems)
        {
            await InsertAsync(
                """
                INSERT INTO DocumentItems
                    (DocumentId, Description, Quantity, CurrencyCode, UnitPrice, TaxName, TaxRate,
                     SerialNumber, Amount, TaxAmount, CreatedTime, ModifiedTime)
                VALUES
                    ($documentId, $description, $quantity, $currency, $unitPrice, $taxName, $taxRate,
                     $serial, $amount, $taxAmount, $created, $modified)
                """,
                transaction, cancellationToken,
                ("$documentId", documentId), ("$description", item.Description), ("$quantity", Format(item.Quantity)),
                ("$currency", item.CurrencyCode), ("$unitPrice", Format(item.UnitPrice)), ("$taxName", item.TaxName),
                ("$taxRate", Format(item.TaxRate)), ("$serial", item.SerialNumber), ("$amount", Format(item.Amount)),
                ("$taxAmount", Format(item.TaxAmount)), ("$created", now), ("$modified", now));
        }
    }

    private Task<long> SaveMappingAsync(
        long documentId,
        long messageId,
        long? attachmentId,
        string now,
        SqliteTransaction transaction,
        CancellationToken cancellationToken) =>
        InsertAsync(
            """
            INSERT INTO DocumentInboxMessages (DocumentId, MessageId, SourceAttachmentId, CreatedTime, ModifiedTime)
            VALUES ($documentId, $messageId, $attachmentId, $created, $modified)
            """,
            transaction, cancellationToken,
            ("$documentId", documentId), ("$messageId", messageId), ("$attachmentId", attachmentId),
            ("$created", now), ("$modified", now));

    private async Task<long?> FindIdAsync(
        string sql,
        SqliteTransaction transaction,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = CreateCommand(sql, transaction, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? null : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task<long> InsertAsync(
        string sql,
        SqliteTransaction transaction,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = CreateCommand(sql, transaction, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var idCommand = CreateCommand("SELECT last_insert_rowid()", transaction, []);
        var result = await idCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private async Task ExecuteAsync(
        string sql,
        SqliteTransaction transaction,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = CreateCommand(sql, transaction, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteCommand CreateCommand(
        string sql,
        SqliteTransaction transaction,
        IEnumerable<(string Name, object? Value)> parameters)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string? Format(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
}
