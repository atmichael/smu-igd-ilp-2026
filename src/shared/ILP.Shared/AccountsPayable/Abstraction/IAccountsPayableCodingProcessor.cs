using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.TransactionDocument.Dto;

namespace ILP.Shared.AccountsPayable.Abstraction;

public interface IAccountsPayableCodingProcessor
{
    Task<DocumentAPJournalDto> ProcessAsync(DocumentDto document, CancellationToken cancellationToken);
}
