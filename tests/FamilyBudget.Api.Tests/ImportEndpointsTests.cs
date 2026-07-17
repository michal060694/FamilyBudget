using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Tests;

public class ImportEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public ImportEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    private static byte[] BuildWorkbook(string[] headers, params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                SetCellValue(sheet.Cell(r + 2, c + 1), rows[r][c]);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case decimal d:
                cell.Value = d;
                break;
            case string s:
                cell.Value = s;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static MultipartFormDataContent BuildUpload(byte[] fileBytes, string fileName = "import.xlsx")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private static readonly string[] TransactionColumns = ["תאריך", "סוג", "סכום", "אמצעי תשלום", "חייב מעשר", "תיאור"];
    private static readonly string[] MonthlyExpenseBudgetColumns = ["סוג", "שם", "תקציב", "נוצל", "כלול בסה\"כ"];
    private static readonly string[] FixedDonationColumns = ["שם", "סכום", "בתוקף עד (YYYY-MM, ריק=ללא הגבלה)"];
    private static readonly string[] AnnualBudgetItemColumns = ["שם", "חודש יעד (שם חודש עברי או כללי)", "סכום שנתי", "הופרש בפועל", "נוצל"];
    private static readonly string[] FundColumns = ["שם", "יתרה כוללת"];
    private static readonly string[] FundEarmarkColumns = ["מטרה", "סכום"];
    private static readonly string[] DebtColumns = ["שם", "סכום מקורי", "תאריך יעד", "קצב החזר", "הערות"];

    // ---------- Transactions ----------

    [Fact]
    public async Task ImportTransactions_HappyPath_AddsRowsAndTheyAppearInMonth()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(TransactionColumns,
            ["2071-01-05", "הכנסה", 1000m, "העברה בנקאית", "כן", "משכורת"],
            ["2071-01-06", "הוצאה שוטפת", 50m, "מזומן", null, "מכולת"]);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/transactions?year=2071&month=1", JsonOptions);
        Assert.Equal(2, list!.Transactions.Count);
        Assert.Contains(list.Transactions, t => t.Description == "משכורת" && t.Amount == 1000m && t.IsTitheApplicable == true);
        Assert.Contains(list.Transactions, t => t.Description == "מכולת" && t.Amount == 50m && t.IsTitheApplicable == null);
    }

    [Fact]
    public async Task ImportTransactions_PartialFailure_SkipsBadRowKeepsGoodRows()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(TransactionColumns,
            ["2071-02-01", "הוצאה שוטפת", 10m, "מזומן", null, "Good 1"],
            ["2071-02-02", "הוצאה שוטפת", -5m, "מזומן", null, "Bad: negative amount"],
            ["2071-02-03", "הוצאה שוטפת", 20m, "מזומן", null, "Good 2"]);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        var error = Assert.Single(result.Errors);
        Assert.Equal(3, error.RowNumber);

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/transactions?year=2071&month=2", JsonOptions);
        Assert.Equal(2, list!.Transactions.Count);
        Assert.DoesNotContain(list.Transactions, t => t.Description == "Bad: negative amount");
    }

    [Fact]
    public async Task ImportTransactions_BlankRowInMiddle_IsIgnoredWithoutError()
    {
        var client = _factory.CreateClient();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");
        for (var i = 0; i < TransactionColumns.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = TransactionColumns[i];
        }

        sheet.Cell(2, 1).Value = "2071-03-01";
        sheet.Cell(2, 2).Value = "הוצאה שוטפת";
        sheet.Cell(2, 3).Value = 10m;
        sheet.Cell(2, 4).Value = "מזומן";
        // row 3 intentionally left entirely blank
        sheet.Cell(4, 1).Value = "2071-03-02";
        sheet.Cell(4, 2).Value = "הוצאה שוטפת";
        sheet.Cell(4, 3).Value = 20m;
        sheet.Cell(4, 4).Value = "מזומן";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(stream.ToArray()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportTransactions_EmptyFileHeaderOnly_ReturnsZeroAddedNoErrors()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(TransactionColumns);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(0, result!.AddedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportTransactions_WrongExtension_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("not an excel file");

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes, "import.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportTransactions_CorruptFile_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("this is not a valid zip/xlsx payload");

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportTransactions_WrongTemplateHeader_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(FundColumns, ["Some Fund", 100m]);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportTransactions_AllTransactionTypes_ParseToCorrectEnumValues()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(TransactionColumns,
            ["2081-05-01", "הכנסה", 100m, "אשראי", "כן", null],
            ["2081-05-02", "הוצאת הו\"ק", 100m, "אשראי", null, null],
            ["2081-05-03", "הוצאה שוטפת", 100m, "אשראי", null, null],
            ["2081-05-04", "תרומה קבועה", 100m, "אשראי", null, null],
            ["2081-05-05", "צדקה קטנה", 100m, "אשראי", null, null],
            ["2081-05-06", "החזר חוב", 100m, "אשראי", null, null]);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(6, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/transactions?year=2081&month=5", JsonOptions);
        var types = list!.Transactions.Select(t => t.Type).OrderBy(t => t).ToList();
        var expected = new[]
        {
            TransactionType.Income, TransactionType.FixedExpense, TransactionType.RegularExpense,
            TransactionType.FixedDonation, TransactionType.SmallCharityExpense, TransactionType.DebtRepayment,
        }.OrderBy(t => t).ToList();
        Assert.Equal(expected, types);
    }

    [Fact]
    public async Task ImportTransactions_AllPaymentMethods_ParseToCorrectEnumValues()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(TransactionColumns,
            ["2082-06-01", "הוצאה שוטפת", 10m, "אשראי", null, null],
            ["2082-06-02", "הוצאה שוטפת", 10m, "העברה בנקאית", null, null],
            ["2082-06-03", "הוצאה שוטפת", 10m, "מזומן", null, null],
            ["2082-06-04", "הוצאה שוטפת", 10m, "צ'ק", null, null]);

        var response = await client.PostAsync("/api/import/transactions", BuildUpload(bytes));
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(4, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/transactions?year=2082&month=6", JsonOptions);
        var methods = list!.Transactions.Select(t => t.PaymentMethod).OrderBy(m => m).ToList();
        var expected = new[]
        {
            PaymentMethod.CreditCard, PaymentMethod.BankTransfer, PaymentMethod.Cash, PaymentMethod.Check,
        }.OrderBy(m => m).ToList();
        Assert.Equal(expected, methods);
    }

    // ---------- Monthly expense budget items ----------

    [Fact]
    public async Task ImportMonthlyExpenseBudgets_HappyPath_AddsItemsForRequestedMonth()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(MonthlyExpenseBudgetColumns,
            ["הוצאת הו\"ק", "ארנונה", 500m, 500m, "כן"],
            ["הוצאה שוטפת", "מכולת", 1500m, null, null]);

        var response = await client.PostAsync("/api/import/monthly-expense-budgets?year=2073&month=4", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<MonthlyExpenseBudgetListResponse>(
            "/api/monthly-expense-budgets?year=2073&month=4", JsonOptions);
        Assert.Equal(2, list!.Items.Count);
        var groceries = Assert.Single(list.Items, i => i.Name == "מכולת");
        Assert.Equal(1500m, groceries.BudgetedAmount);
        Assert.Equal(0m, groceries.UsedAmount);
        Assert.True(groceries.IncludeInOutflowTotal);
        var arnona = Assert.Single(list.Items, i => i.Name == "ארנונה");
        Assert.Equal(TransactionType.FixedExpense, arnona.Type);
    }

    [Fact]
    public async Task ImportMonthlyExpenseBudgets_InvalidType_ReturnsRowError()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(MonthlyExpenseBudgetColumns,
            ["הכנסה", "לא תקין", 100m, null, null]);

        var response = await client.PostAsync("/api/import/monthly-expense-budgets?year=2074&month=5", BuildUpload(bytes));

        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(0, result!.AddedCount);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ImportMonthlyExpenseBudgets_InvalidMonthQueryParam_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(MonthlyExpenseBudgetColumns);

        var response = await client.PostAsync("/api/import/monthly-expense-budgets?year=2074&month=13", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Fixed donation standing orders ----------

    [Fact]
    public async Task ImportFixedDonationStandingOrders_HappyPath_ParsesValidUntil()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildWorkbook(FixedDonationColumns,
            [$"תרומה קבועה {marker} א", 100m, null],
            [$"תרומה קבועה {marker} ב", 200m, "2030-06"]);

        var response = await client.PostAsync("/api/import/fixed-donation-standing-orders", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<FixedDonationStandingOrderListResponse>(
            "/api/fixed-donation-standing-orders?year=2029&month=1", JsonOptions);
        var noEnd = Assert.Single(list!.Items, i => i.Name == $"תרומה קבועה {marker} א");
        Assert.Null(noEnd.ValidUntilYear);
        var withEnd = Assert.Single(list.Items, i => i.Name == $"תרומה קבועה {marker} ב");
        Assert.Equal(2030, withEnd.ValidUntilYear);
        Assert.Equal(6, withEnd.ValidUntilMonth);
    }

    [Fact]
    public async Task ImportFixedDonationStandingOrders_InvalidValidUntilFormat_ReturnsRowError()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(FixedDonationColumns,
            ["תרומה לא תקינה", 100m, "לא תאריך"]);

        var response = await client.PostAsync("/api/import/fixed-donation-standing-orders", BuildUpload(bytes));

        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(0, result!.AddedCount);
        Assert.Single(result.Errors);
    }

    // ---------- Annual budget items ----------

    [Fact]
    public async Task ImportAnnualBudgetItems_HappyPath_ParsesHebrewMonthAndGeneral()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(AnnualBudgetItemColumns,
            ["חנוכה", "כסלו", 1200m, 300m, null],
            ["ביטוח כללי", "כללי", 2400m, null, 100m]);

        var response = await client.PostAsync("/api/import/annual-budget-items?year=2077", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var budget = await client.GetFromJsonAsync<AnnualBudgetResponse>("/api/annual-budget?year=2077", JsonOptions);
        var chanukah = Assert.Single(budget!.Items, i => i.Name == "חנוכה");
        Assert.Equal(3, chanukah.TargetMonth);
        Assert.Equal(300m, chanukah.AmountAlreadySetAside);
        var insurance = Assert.Single(budget.Items, i => i.Name == "ביטוח כללי");
        Assert.Null(insurance.TargetMonth);
        Assert.Equal(100m, insurance.AmountUsed);
    }

    // ---------- Funds ----------

    [Fact]
    public async Task ImportFunds_HappyPath_AddsFunds()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildWorkbook(FundColumns,
            [$"קרן {marker}", 5000m]);

        var response = await client.PostAsync("/api/import/funds", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(1, result!.AddedCount);

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds", JsonOptions);
        var fund = Assert.Single(list!.Funds, f => f.Name == $"קרן {marker}");
        Assert.Equal(5000m, fund.TotalBalance);
    }

    // ---------- Fund earmarks ----------

    [Fact]
    public async Task ImportFundEarmarks_HappyPath_AddsUnderExistingFund()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var createFund = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest($"קרן ייעודים {marker}", 1000m));
        var fund = await createFund.Content.ReadFromJsonAsync<FundSummaryResponse>(JsonOptions);

        var bytes = BuildWorkbook(FundEarmarkColumns, ["פאה", 400m]);
        var response = await client.PostAsync($"/api/import/funds/{fund!.FundId}/earmarks", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(1, result!.AddedCount);

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds", JsonOptions);
        var updatedFund = Assert.Single(list!.Funds, f => f.FundId == fund.FundId);
        var earmark = Assert.Single(updatedFund.Earmarks, e => e.PurposeLabel == "פאה");
        Assert.Equal(400m, earmark.Amount);
    }

    [Fact]
    public async Task ImportFundEarmarks_UnknownFundId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(FundEarmarkColumns, ["מטרה", 100m]);

        var response = await client.PostAsync($"/api/import/funds/{Guid.NewGuid()}/earmarks", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Debts ----------

    [Fact]
    public async Task ImportDebts_Receivables_AddsWithOpenStatusAndBalanceEqualsOriginal()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildWorkbook(DebtColumns,
            [$"לקוח {marker}", 1000m, "2027-01-01", null, "הערה"]);

        var response = await client.PostAsync("/api/import/debts/receivables", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(1, result!.AddedCount);

        var ledger = await client.GetFromJsonAsync<DebtsLedgerResponse>("/api/debts", JsonOptions);
        var debt = Assert.Single(ledger!.Receivables, d => d.CounterpartyName == $"לקוח {marker}");
        Assert.Equal(DebtDirection.Receivable, debt.Direction);
        Assert.Equal(DebtStatus.Open, debt.Status);
        Assert.Equal(1000m, debt.CurrentBalance);
        Assert.Equal(new DateOnly(2027, 1, 1), debt.TargetDate);
    }

    [Fact]
    public async Task ImportDebts_Payables_AddsWithOpenStatusAndBalanceEqualsOriginal()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildWorkbook(DebtColumns,
            [$"ספק {marker}", 2000m, null, 300m, null]);

        var response = await client.PostAsync("/api/import/debts/payables", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(1, result!.AddedCount);

        var ledger = await client.GetFromJsonAsync<DebtsLedgerResponse>("/api/debts", JsonOptions);
        var debt = Assert.Single(ledger!.Payables, d => d.CounterpartyName == $"ספק {marker}");
        Assert.Equal(DebtDirection.Payable, debt.Direction);
        Assert.Equal(DebtStatus.Open, debt.Status);
        Assert.Equal(2000m, debt.CurrentBalance);
        Assert.Equal(300m, debt.RepaymentRate);
    }

    // ---------- Templates ----------

    [Theory]
    [InlineData("/api/import/transactions/template")]
    [InlineData("/api/import/monthly-expense-budgets/template")]
    [InlineData("/api/import/fixed-donation-standing-orders/template")]
    [InlineData("/api/import/annual-budget-items/template")]
    [InlineData("/api/import/funds/template")]
    [InlineData("/api/import/funds/earmarks/template")]
    [InlineData("/api/import/debts/template")]
    public async Task GetTemplate_ReturnsHeaderOnlyWorkbook(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType!.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.Single();

        Assert.Equal(1, sheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public async Task GetTransactionsTemplate_HeaderMatchesExpectedColumns()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/import/transactions/template");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.Single();

        var headerRow = TransactionColumns.Select((_, i) => sheet.Cell(1, i + 1).GetString()).ToList();
        Assert.Equal(TransactionColumns, headerRow);
    }
}
