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

    private static byte[] BuildTwoSheetWorkbook(
        string sheetName1, string[] headers1, object?[][] rows1,
        string sheetName2, string[] headers2, object?[][] rows2)
    {
        using var workbook = new XLWorkbook();
        AddSheet(workbook, sheetName1, headers1, rows1);
        AddSheet(workbook, sheetName2, headers2, rows2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddSheet(XLWorkbook workbook, string sheetName, string[] headers, object?[][] rows)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
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

    private static readonly string[] MonthlyTemplateItemColumns = ["סוג", "שם", "סכום", "חייב מעשר"];
    private static readonly string[] FixedDonationColumns = ["שם", "סכום", "בתוקף עד (YYYY-MM, ריק=ללא הגבלה)"];
    private static readonly string[] AnnualBudgetItemColumns = ["שם", "חודש יעד (שם חודש עברי או כללי)", "סכום שנתי", "הופרש בפועל", "נוצל"];
    private static readonly string[] FundColumns = ["שם", "יתרה כוללת"];
    private static readonly string[] FundEarmarkColumns = ["שם קרן", "מטרה", "סכום"];
    private static readonly string[] DebtColumns = ["שם", "סכום מקורי", "תאריך יעד", "קצב החזר", "הערות"];

    // ---------- Monthly template items ----------

    [Fact]
    public async Task ImportMonthlyTemplateItems_HappyPath_AddsItemsToTemplate()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildWorkbook(MonthlyTemplateItemColumns,
            ["הכנסה", $"משכורת {marker}", 8000m, "כן"],
            ["הוצאת הו\"ק", $"ארנונה {marker}", 500m, null]);

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<MonthlyTemplateListResponse>("/api/monthly-template", JsonOptions);
        var salary = Assert.Single(list!.Items, i => i.Name == $"משכורת {marker}");
        Assert.Equal(TransactionType.Income, salary.Type);
        Assert.Equal(true, salary.IsTitheApplicable);
        var arnona = Assert.Single(list.Items, i => i.Name == $"ארנונה {marker}");
        Assert.Equal(TransactionType.FixedExpense, arnona.Type);
        Assert.Null(arnona.IsTitheApplicable);
    }

    [Fact]
    public async Task ImportMonthlyTemplateItems_PartialFailure_SkipsBadRowKeepsGoodRows()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildWorkbook(MonthlyTemplateItemColumns,
            ["הוצאה שוטפת", $"מכולת {marker}", 1000m, null],
            ["הוצאה שוטפת", $"רע {marker}", -5m, null]);

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(1, result!.AddedCount);
        Assert.Single(result.Errors);

        var list = await client.GetFromJsonAsync<MonthlyTemplateListResponse>("/api/monthly-template", JsonOptions);
        Assert.Contains(list!.Items, i => i.Name == $"מכולת {marker}");
        Assert.DoesNotContain(list.Items, i => i.Name == $"רע {marker}");
    }

    [Fact]
    public async Task ImportMonthlyTemplateItems_DebtRepaymentType_ReturnsRowError()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(MonthlyTemplateItemColumns, ["החזר חוב", "לא נתמך", 100m, null]);

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(0, result!.AddedCount);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ImportMonthlyTemplateItems_EmptyFileHeaderOnly_ReturnsZeroAddedNoErrors()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(MonthlyTemplateItemColumns);

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(0, result!.AddedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportMonthlyTemplateItems_WrongExtension_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("not an excel file");

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes, "import.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportMonthlyTemplateItems_CorruptFile_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = Encoding.UTF8.GetBytes("this is not a valid zip/xlsx payload");

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportMonthlyTemplateItems_WrongTemplateHeader_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var bytes = BuildWorkbook(FundColumns, ["Some Fund", 100m]);

        var response = await client.PostAsync("/api/import/monthly-template-items", BuildUpload(bytes));

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

    // ---------- Funds + earmarks (one workbook, two sheets) ----------

    [Fact]
    public async Task ImportFunds_HappyPath_AddsFundsFromFundsSheetOnly()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildTwoSheetWorkbook(
            "קרנות", FundColumns, [[$"קרן {marker}", 5000m]],
            "ייעודים", FundEarmarkColumns, []);

        var response = await client.PostAsync("/api/import/funds", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(1, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds", JsonOptions);
        var fund = Assert.Single(list!.Funds, f => f.Name == $"קרן {marker}");
        Assert.Equal(5000m, fund.TotalBalance);
    }

    [Fact]
    public async Task ImportFunds_EarmarksSheet_AddsAcrossMultipleFundsByName()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var createFundA = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest($"קרן א {marker}", 1000m));
        var fundA = await createFundA.Content.ReadFromJsonAsync<FundSummaryResponse>(JsonOptions);
        var createFundB = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest($"קרן ב {marker}", 2000m));
        var fundB = await createFundB.Content.ReadFromJsonAsync<FundSummaryResponse>(JsonOptions);

        var bytes = BuildTwoSheetWorkbook(
            "קרנות", FundColumns, [],
            "ייעודים", FundEarmarkColumns,
            [[$"קרן א {marker}", "פאה", 400m], [$"קרן ב {marker}", "חופשה", 600m]]);
        var response = await client.PostAsync("/api/import/funds", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds", JsonOptions);
        var updatedFundA = Assert.Single(list!.Funds, f => f.FundId == fundA!.FundId);
        var earmarkA = Assert.Single(updatedFundA.Earmarks, e => e.PurposeLabel == "פאה");
        Assert.Equal(400m, earmarkA.Amount);
        var updatedFundB = Assert.Single(list.Funds, f => f.FundId == fundB!.FundId);
        var earmarkB = Assert.Single(updatedFundB.Earmarks, e => e.PurposeLabel == "חופשה");
        Assert.Equal(600m, earmarkB.Amount);
    }

    [Fact]
    public async Task ImportFunds_EarmarkReferencesFundCreatedInSameFile_Succeeds()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var bytes = BuildTwoSheetWorkbook(
            "קרנות", FundColumns, [[$"קרן חדשה {marker}", 3000m]],
            "ייעודים", FundEarmarkColumns, [[$"קרן חדשה {marker}", "פאה", 500m]]);

        var response = await client.PostAsync("/api/import/funds", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(2, result!.AddedCount);
        Assert.Empty(result.Errors);

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds", JsonOptions);
        var fund = Assert.Single(list!.Funds, f => f.Name == $"קרן חדשה {marker}");
        var earmark = Assert.Single(fund.Earmarks, e => e.PurposeLabel == "פאה");
        Assert.Equal(500m, earmark.Amount);
    }

    [Fact]
    public async Task ImportFunds_UnknownFundNameInEarmarksSheet_ReturnsRowError()
    {
        var client = _factory.CreateClient();
        var bytes = BuildTwoSheetWorkbook(
            "קרנות", FundColumns, [],
            "ייעודים", FundEarmarkColumns, [["קרן שלא קיימת", "מטרה", 100m]]);

        var response = await client.PostAsync("/api/import/funds", BuildUpload(bytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
        Assert.Equal(0, result!.AddedCount);
        Assert.Single(result.Errors);
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
    [InlineData("/api/import/fixed-donation-standing-orders/template")]
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

    [Theory]
    [InlineData("/api/import/monthly-template-items/template", "תבנית")]
    [InlineData("/api/import/annual-budget-items/template", "תבנית")]
    public async Task GetTemplate_WithDropdowns_HasVisibleSheetPlusHiddenListsSheet(string url, string visibleSheetName)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(["תבנית", "רשימות"], workbook.Worksheets.Select(s => s.Name).ToList());
        var visibleSheet = workbook.Worksheets.Worksheet(visibleSheetName);
        Assert.Equal(1, visibleSheet.LastRowUsed()!.RowNumber());
        Assert.NotEmpty(visibleSheet.DataValidations);

        var listSheet = workbook.Worksheets.Worksheet("רשימות");
        Assert.Equal(XLWorksheetVisibility.VeryHidden, listSheet.Visibility);
    }

    [Fact]
    public async Task GetMonthlyTemplateItemsTemplate_TypeDropdown_OffersOnlyIncomeFixedAndRegular()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/import/monthly-template-items/template");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var listSheet = workbook.Worksheets.Worksheet("רשימות");

        var typeOptions = new[] { 1, 2, 3 }.Select(r => listSheet.Cell(r, 1).GetString()).ToList();
        Assert.Equal(["הכנסה", "הוצאת הו\"ק", "הוצאה שוטפת"], typeOptions);

        var yesNoOptions = new[] { 1, 2 }.Select(r => listSheet.Cell(r, 2).GetString()).ToList();
        Assert.Equal(["כן", "לא"], yesNoOptions);
    }

    [Fact]
    public async Task GetAnnualBudgetItemsTemplate_TargetMonthDropdown_OffersGeneralPlusTwelveHebrewMonths()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/import/annual-budget-items/template");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var listSheet = workbook.Worksheets.Worksheet("רשימות");

        var options = Enumerable.Range(1, 13).Select(r => listSheet.Cell(r, 1).GetString()).ToList();
        Assert.Equal(13, options.Count);
        Assert.Equal("כללי", options[0]);
        Assert.Contains("תשרי", options);
        Assert.Contains("אלול", options);
    }

    [Fact]
    public async Task GetFundsTemplate_ReturnsTwoHeaderOnlySheets()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/import/funds/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(["קרנות", "ייעודים"], workbook.Worksheets.Select(s => s.Name).ToList());
        var fundsSheet = workbook.Worksheets.Worksheet("קרנות");
        var earmarksSheet = workbook.Worksheets.Worksheet("ייעודים");
        Assert.Equal(1, fundsSheet.LastRowUsed()!.RowNumber());
        Assert.Equal(1, earmarksSheet.LastRowUsed()!.RowNumber());
        Assert.Equal(FundColumns, FundColumns.Select((_, i) => fundsSheet.Cell(1, i + 1).GetString()).ToList());
        Assert.Equal(FundEarmarkColumns, FundEarmarkColumns.Select((_, i) => earmarksSheet.Cell(1, i + 1).GetString()).ToList());
    }

    [Fact]
    public async Task GetMonthlyTemplateItemsTemplate_HeaderMatchesExpectedColumns()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/import/monthly-template-items/template");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.Worksheet("תבנית");

        var headerRow = MonthlyTemplateItemColumns.Select((_, i) => sheet.Cell(1, i + 1).GetString()).ToList();
        Assert.Equal(MonthlyTemplateItemColumns, headerRow);
    }
}
