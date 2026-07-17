namespace FamilyBudget.Api.Contracts;

public record ImportRowErrorResponse(int RowNumber, string Message);

public record ImportResultResponse(int AddedCount, IReadOnlyList<ImportRowErrorResponse> Errors);
