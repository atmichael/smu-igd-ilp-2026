using ILP.Server.Features.AccountsPayable.Model;
using ILP.Shared.AccountsPayable.Dto;

namespace ILP.Server.Features.AccountsPayable.Service;

public interface IAccountsPayableService
{
    Task<AccountsPayableReferenceData> GetReferenceDataAsync(CancellationToken cancellationToken);

    Task<AccountsPayableCodingResult> EvaluateAsync(
        AccountsPayableInvoiceRequest invoice,
        CancellationToken cancellationToken);

    Task<DocumentAPJournalDto?> GetCodingAsync(long documentId, CancellationToken cancellationToken);
}
