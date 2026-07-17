using ClosedXML.Excel;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Api.Services;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FamilyBudget.Api.Endpoints;

public static class ImportEndpoints
{
    private const long MaxUploadBytes = 5_000_000;
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static void MapImportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/import/transactions", ImportTransactions).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/transactions/template", GetTransactionsTemplate);

        app.MapPost("/api/import/monthly-expense-budgets", ImportMonthlyExpenseBudgets).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/monthly-expense-budgets/template", GetMonthlyExpenseBudgetsTemplate);

        app.MapPost("/api/import/fixed-donation-standing-orders", ImportFixedDonationStandingOrders).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/fixed-donation-standing-orders/template", GetFixedDonationStandingOrdersTemplate);

        app.MapPost("/api/import/annual-budget-items", ImportAnnualBudgetItems).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/annual-budget-items/template", GetAnnualBudgetItemsTemplate);

        app.MapPost("/api/import/funds", ImportFunds).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/funds/template", GetFundsTemplate);

        app.MapPost("/api/import/funds/{fundId:guid}/earmarks", ImportFundEarmarks).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/funds/earmarks/template", GetFundEarmarksTemplate);

        app.MapPost("/api/import/debts/receivables", ImportReceivableDebts).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapPost("/api/import/debts/payables", ImportPayableDebts).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));
        app.MapGet("/api/import/debts/template", GetDebtsTemplate);
    }

    private static ImportResultResponse ToResponse(ImportResult result) => new(
        result.AddedCount,
        result.Errors.Select(e => new ImportRowErrorResponse(e.RowNumber, e.Message)).ToList());

    private static string? ValidateExcelFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "The uploaded file is empty.";
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return "Only .xlsx files are supported.";
        }

        return null;
    }

    private static async Task<IResult> HandleImportAsync(IFormFile file, Func<XLWorkbook, Task<ImportResult>> importAsync)
    {
        var validationError = ValidateExcelFile(file);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            return Results.BadRequest("The uploaded file is not a valid Excel workbook.");
        }

        using (workbook)
        {
            try
            {
                var result = await importAsync(workbook);
                return Results.Ok(ToResponse(result));
            }
            catch (ImportTemplateMismatchException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }
    }

    // ---------- Transactions ----------

    private static Task<IResult> ImportTransactions(IFormFile file, ExcelImportService importService, CancellationToken cancellationToken) =>
        HandleImportAsync(file, workbook => importService.ImportTransactionsAsync(workbook, cancellationToken));

    private static async Task<IResult> GetTransactionsTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildTransactionsTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-עסקאות.xlsx");
    }

    // ---------- Monthly expense budget items ----------

    private static Task<IResult> ImportMonthlyExpenseBudgets(
        int year, int month, IFormFile file, ExcelImportService importService, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            return Task.FromResult(Results.BadRequest("month must be between 1 and 12."));
        }

        return HandleImportAsync(file, workbook => importService.ImportMonthlyExpenseBudgetsAsync(workbook, year, month, cancellationToken));
    }

    private static async Task<IResult> GetMonthlyExpenseBudgetsTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildMonthlyExpenseBudgetsTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-הוצאות-חודשיות.xlsx");
    }

    // ---------- Fixed donation standing orders ----------

    private static Task<IResult> ImportFixedDonationStandingOrders(IFormFile file, ExcelImportService importService, CancellationToken cancellationToken) =>
        HandleImportAsync(file, workbook => importService.ImportFixedDonationStandingOrdersAsync(workbook, cancellationToken));

    private static async Task<IResult> GetFixedDonationStandingOrdersTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildFixedDonationStandingOrdersTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-תרומות-קבועות.xlsx");
    }

    // ---------- Annual budget items ----------

    private static Task<IResult> ImportAnnualBudgetItems(int year, IFormFile file, ExcelImportService importService, CancellationToken cancellationToken) =>
        HandleImportAsync(file, workbook => importService.ImportAnnualBudgetItemsAsync(workbook, year, cancellationToken));

    private static async Task<IResult> GetAnnualBudgetItemsTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildAnnualBudgetItemsTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-תקציב-שנתי.xlsx");
    }

    // ---------- Funds ----------

    private static Task<IResult> ImportFunds(IFormFile file, ExcelImportService importService, CancellationToken cancellationToken) =>
        HandleImportAsync(file, workbook => importService.ImportFundsAsync(workbook, cancellationToken));

    private static async Task<IResult> GetFundsTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildFundsTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-קרנות.xlsx");
    }

    // ---------- Fund earmarks ----------

    private static async Task<IResult> ImportFundEarmarks(
        Guid fundId, IFormFile file, IFundRepository fundRepository, ExcelImportService importService, CancellationToken cancellationToken)
    {
        var fund = await fundRepository.GetByIdAsync(fundId, cancellationToken);
        if (fund is null)
        {
            return Results.NotFound();
        }

        return await HandleImportAsync(file, workbook => importService.ImportFundEarmarksAsync(workbook, fundId, cancellationToken));
    }

    private static async Task<IResult> GetFundEarmarksTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildFundEarmarksTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-ייעודי-קרן.xlsx");
    }

    // ---------- Debts ----------

    private static Task<IResult> ImportReceivableDebts(IFormFile file, ExcelImportService importService, CancellationToken cancellationToken) =>
        HandleImportAsync(file, workbook => importService.ImportDebtsAsync(workbook, DebtDirection.Receivable, cancellationToken));

    private static Task<IResult> ImportPayableDebts(IFormFile file, ExcelImportService importService, CancellationToken cancellationToken) =>
        HandleImportAsync(file, workbook => importService.ImportDebtsAsync(workbook, DebtDirection.Payable, cancellationToken));

    private static async Task<IResult> GetDebtsTemplate(ExcelImportService importService)
    {
        var bytes = await importService.BuildDebtsTemplateAsync();
        return Results.File(bytes, XlsxContentType, "תבנית-חובות.xlsx");
    }
}
