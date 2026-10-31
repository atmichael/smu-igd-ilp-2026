namespace ILP.Shared.InboxMonitoring.Dto;

public sealed record DocumentNotificationDto(string? DocumentType, string? DocumentReferenceNumber, string? CompanyUen);
