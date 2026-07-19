using ClosedXML.Excel;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Services;

/// <summary>
/// Reads add-only Excel imports for each of the app's editable tables, and builds the matching
/// blank (header-only) templates for the user to fill in. Every import commits each row
/// independently (see <see cref="ExcelTableReader"/>) — there is no update/upsert/matching-by-Id and
/// no "replace existing data" behavior; every row in the file becomes a brand new database row.
/// </summary>
public class ExcelImportService
{
    // Same Hebrew "target month" vocabulary already written by ExcelExportService's Annual Budget
    // sheet, so importing a value copied out of that export's own "חודש" column round-trips cleanly.
    // Duplicated here (rather than shared) to keep the import feature fully isolated from the
    // working export code.
    private static readonly string[] HebrewMonthNames =
        ["", "תשרי", "חשוון", "כסלו", "טבת", "שבט", "אדר", "ניסן", "אייר", "סיוון", "תמוז", "אב", "אלול"];

    private static readonly Dictionary<string, int> MonthByHebrewName = HebrewMonthNames
        .Select((name, index) => (name, index))
        .Where(x => x.index > 0)
        .ToDictionary(x => x.name, x => x.index);

    private static readonly Dictionary<string, TransactionType> TransactionTypeByHebrew = new()
    {
        ["הכנסה"] = TransactionType.Income,
        ["הוצאת הו\"ק"] = TransactionType.FixedExpense,
        ["הוצאה שוטפת"] = TransactionType.RegularExpense,
        ["תרומה קבועה"] = TransactionType.FixedDonation,
        ["צדקה קטנה"] = TransactionType.SmallCharityExpense,
        ["החזר חוב"] = TransactionType.DebtRepayment,
    };

    private readonly IMonthlyTemplateItemRepository _monthlyTemplateItemRepository;
    private readonly IFixedDonationStandingOrderRepository _fixedDonationStandingOrderRepository;
    private readonly IAnnualBudgetItemRepository _annualBudgetItemRepository;
    private readonly IFundRepository _fundRepository;
    private readonly IFundEarmarkRepository _fundEarmarkRepository;
    private readonly IDebtRepository _debtRepository;

    public ExcelImportService(
        IMonthlyTemplateItemRepository monthlyTemplateItemRepository,
        IFixedDonationStandingOrderRepository fixedDonationStandingOrderRepository,
        IAnnualBudgetItemRepository annualBudgetItemRepository,
        IFundRepository fundRepository,
        IFundEarmarkRepository fundEarmarkRepository,
        IDebtRepository debtRepository)
    {
        _monthlyTemplateItemRepository = monthlyTemplateItemRepository;
        _fixedDonationStandingOrderRepository = fixedDonationStandingOrderRepository;
        _annualBudgetItemRepository = annualBudgetItemRepository;
        _fundRepository = fundRepository;
        _fundEarmarkRepository = fundEarmarkRepository;
        _debtRepository = debtRepository;
    }

    // ---------- Monthly template items (recurring plan — applied to a specific month separately via MonthlyTemplateApplyService) ----------

    private static readonly string[] MonthlyTemplateItemColumns = ["סוג", "שם", "סכום", "חייב מעשר"];

    public async Task<ImportResult> ImportMonthlyTemplateItemsAsync(XLWorkbook workbook, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, MonthlyTemplateItemColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var type = ParseTemplateItemType(row, 1);
            var name = ExcelTableReader.RequiredText(row, 2, "שם");
            var amount = ExcelTableReader.RequiredDecimal(row, 3, "סכום");
            var isTitheApplicableText = ExcelTableReader.OptionalText(row, 4);

            bool? isTitheApplicable;
            if (type == TransactionType.Income)
            {
                isTitheApplicable = isTitheApplicableText switch
                {
                    "כן" => true,
                    "לא" => false,
                    _ => throw new FormatException("חייב מעשר must be כן or לא for הכנסה rows."),
                };
            }
            else
            {
                if (isTitheApplicableText is not null)
                {
                    throw new FormatException("חייב מעשר must be empty for non-הכנסה rows.");
                }

                isTitheApplicable = null;
            }

            var item = new MonthlyTemplateItem(Guid.NewGuid(), type, name, amount, isTitheApplicable);
            await _monthlyTemplateItemRepository.AddAsync(item, cancellationToken);
        });
    }

    private static readonly string[] MonthlyTemplateItemTypeOptions = ["הכנסה", "הוצאת הו\"ק", "הוצאה שוטפת"];
    private static readonly string[] YesNoOptions = ["כן", "לא"];

    public Task<byte[]> BuildMonthlyTemplateItemsTemplateAsync() => Task.FromResult(BuildTemplate(
        MonthlyTemplateItemColumns,
        (Column: 1, Options: MonthlyTemplateItemTypeOptions),
        (Column: 4, Options: YesNoOptions)));

    // ---------- Fixed donation standing orders ----------

    private static readonly string[] FixedDonationColumns = ["שם", "סכום", "בתוקף עד (YYYY-MM, ריק=ללא הגבלה)"];

    public async Task<ImportResult> ImportFixedDonationStandingOrdersAsync(XLWorkbook workbook, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, FixedDonationColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var name = ExcelTableReader.RequiredText(row, 1, "שם");
            var amount = ExcelTableReader.RequiredDecimal(row, 2, "סכום");
            var (validUntilYear, validUntilMonth) = ParseValidUntil(row, 3);

            var order = new FixedDonationStandingOrder(Guid.NewGuid(), name, amount, validUntilYear, validUntilMonth);
            await _fixedDonationStandingOrderRepository.AddAsync(order, cancellationToken);
        });
    }

    public Task<byte[]> BuildFixedDonationStandingOrdersTemplateAsync() => Task.FromResult(BuildTemplate(FixedDonationColumns));

    // ---------- Annual budget items ----------

    private static readonly string[] AnnualBudgetItemColumns = ["שם", "חודש יעד (שם חודש עברי או כללי)", "סכום שנתי", "הופרש בפועל", "נוצל"];

    public async Task<ImportResult> ImportAnnualBudgetItemsAsync(XLWorkbook workbook, int year, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, AnnualBudgetItemColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var name = ExcelTableReader.RequiredText(row, 1, "שם");
            var targetMonth = ParseTargetMonth(row, 2);
            var totalAmount = ExcelTableReader.RequiredDecimal(row, 3, "סכום שנתי");
            var amountAlreadySetAside = ExcelTableReader.OptionalDecimal(row, 4, "הופרש בפועל");
            var amountUsed = ExcelTableReader.OptionalDecimal(row, 5, "נוצל");

            var item = new AnnualBudgetItem(Guid.NewGuid(), year, name, totalAmount, targetMonth, amountAlreadySetAside, amountUsed);
            await _annualBudgetItemRepository.AddAsync(item, cancellationToken);
        });
    }

    private static readonly string[] TargetMonthOptions = ["כללי", .. HebrewMonthNames.Skip(1)];

    public Task<byte[]> BuildAnnualBudgetItemsTemplateAsync() => Task.FromResult(BuildTemplate(
        AnnualBudgetItemColumns,
        (Column: 2, Options: TargetMonthOptions)));

    // ---------- Funds + earmarks (one workbook, two sheets) ----------

    private const string FundsSheetName = "קרנות";
    private const string FundEarmarksSheetName = "ייעודים";
    private static readonly string[] FundColumns = ["שם", "יתרה כוללת"];
    private static readonly string[] FundEarmarkColumns = ["שם קרן", "מטרה", "סכום"];

    /// <summary>
    /// Imports both new funds and their earmarks from one workbook: a "<c>קרנות</c>" sheet (new
    /// funds) processed first, then a "<c>ייעודים</c>" sheet whose rows reference a fund by
    /// <see cref="Fund.Name"/> — including a fund created moments earlier by the same file's own
    /// funds sheet, since the name lookup is built only after the funds sheet finishes.
    /// </summary>
    public async Task<ImportResult> ImportFundsAsync(XLWorkbook workbook, CancellationToken cancellationToken = default)
    {
        var fundsSheet = ExcelTableReader.GetSheetOrThrow(workbook, FundsSheetName, FundColumns[0]);
        var fundsResult = await ExcelTableReader.ProcessRowsAsync(fundsSheet, async row =>
        {
            var name = ExcelTableReader.RequiredText(row, 1, "שם");
            var totalBalance = ExcelTableReader.RequiredDecimal(row, 2, "יתרה כוללת");

            var fund = new Fund(Guid.NewGuid(), name, totalBalance);
            await _fundRepository.AddAsync(fund, cancellationToken);
        });

        var earmarksSheet = ExcelTableReader.GetSheetOrThrow(workbook, FundEarmarksSheetName, FundEarmarkColumns[0]);
        var funds = await _fundRepository.GetAllAsync(cancellationToken);
        var fundIdByName = funds.ToDictionary(f => f.Name, f => f.Id);

        var earmarksResult = await ExcelTableReader.ProcessRowsAsync(earmarksSheet, async row =>
        {
            var fundName = ExcelTableReader.RequiredText(row, 1, "שם קרן");
            var purposeLabel = ExcelTableReader.RequiredText(row, 2, "מטרה");
            var amount = ExcelTableReader.RequiredDecimal(row, 3, "סכום");

            if (!fundIdByName.TryGetValue(fundName, out var fundId))
            {
                throw new FormatException($"Unrecognized שם קרן value: '{fundName}'.");
            }

            var earmark = new FundEarmark(Guid.NewGuid(), fundId, purposeLabel, amount);
            await _fundEarmarkRepository.AddAsync(earmark, cancellationToken);
        });

        var combinedErrors = fundsResult.Errors.Select(e => new ImportRowError(e.RowNumber, $"[{FundsSheetName}] {e.Message}"))
            .Concat(earmarksResult.Errors.Select(e => new ImportRowError(e.RowNumber, $"[{FundEarmarksSheetName}] {e.Message}")))
            .ToList();

        return new ImportResult(fundsResult.AddedCount + earmarksResult.AddedCount, combinedErrors);
    }

    public Task<byte[]> BuildFundsTemplateAsync() => Task.FromResult(BuildTwoSheetTemplate(
        FundsSheetName, FundColumns, FundEarmarksSheetName, FundEarmarkColumns));

    // ---------- Debts ----------

    private static readonly string[] DebtColumns = ["שם", "סכום מקורי", "תאריך יעד", "קצב החזר", "הערות"];

    public async Task<ImportResult> ImportDebtsAsync(XLWorkbook workbook, DebtDirection direction, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, DebtColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var counterpartyName = ExcelTableReader.RequiredText(row, 1, "שם");
            var originalAmount = ExcelTableReader.RequiredDecimal(row, 2, "סכום מקורי");
            var targetDate = ExcelTableReader.OptionalDate(row, 3, "תאריך יעד");
            var repaymentRate = ExcelTableReader.OptionalNullableDecimal(row, 4, "קצב החזר");
            var notes = ExcelTableReader.OptionalText(row, 5);

            var debt = new Debt(Guid.NewGuid(), direction, counterpartyName, originalAmount, targetDate, repaymentRate, notes);
            await _debtRepository.AddAsync(debt, cancellationToken);
        });
    }

    public Task<byte[]> BuildDebtsTemplateAsync() => Task.FromResult(BuildTemplate(DebtColumns));

    // ---------- Shared parsing/template helpers ----------

    private static TransactionType ParseTransactionType(IXLRow row, int column)
    {
        var text = ExcelTableReader.RequiredText(row, column, "סוג");
        return TransactionTypeByHebrew.TryGetValue(text, out var type)
            ? type
            : throw new FormatException($"Unrecognized סוג value: '{text}'.");
    }

    private static TransactionType ParseTemplateItemType(IXLRow row, int column)
    {
        var type = ParseTransactionType(row, column);
        if (type is not (TransactionType.Income or TransactionType.FixedExpense or TransactionType.RegularExpense))
        {
            throw new FormatException("סוג must be הכנסה, הוצאת הו\"ק, or הוצאה שוטפת.");
        }

        return type;
    }

    private static int? ParseTargetMonth(IXLRow row, int column)
    {
        var text = ExcelTableReader.OptionalText(row, column);
        if (text is null || text == "כללי")
        {
            return null;
        }

        return MonthByHebrewName.TryGetValue(text, out var month)
            ? month
            : throw new FormatException($"Unrecognized חודש יעד value: '{text}'. Use a Hebrew month name or כללי.");
    }

    private static (int? Year, int? Month) ParseValidUntil(IXLRow row, int column)
    {
        var text = ExcelTableReader.OptionalText(row, column);
        if (text is null)
        {
            return (null, null);
        }

        var match = System.Text.RegularExpressions.Regex.Match(text, @"^(\d{4})-(\d{1,2})$");
        if (!match.Success)
        {
            throw new FormatException("בתוקף עד must be in YYYY-MM format, or empty.");
        }

        var year = int.Parse(match.Groups[1].Value);
        var month = int.Parse(match.Groups[2].Value);
        if (month is < 1 or > 12)
        {
            throw new FormatException("בתוקף עד month must be between 1 and 12.");
        }

        return (year, month);
    }

    /// <summary>Row count the dropdown validation covers below the header — generous enough for any realistic hand-filled sheet.</summary>
    private const int DropdownRowCount = 500;

    private static byte[] BuildTemplate(string[] columns, params (int Column, string[] Options)[] dropdowns)
    {
        using var workbook = new XLWorkbook();
        var sheet = AddTemplateSheet(workbook, "תבנית", columns);
        foreach (var (column, options) in dropdowns)
        {
            ApplyDropdown(workbook, sheet, column, options);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildTwoSheetTemplate(string sheetName1, string[] columns1, string sheetName2, string[] columns2)
    {
        using var workbook = new XLWorkbook();
        AddTemplateSheet(workbook, sheetName1, columns1);
        AddTemplateSheet(workbook, sheetName2, columns2);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IXLWorksheet AddTemplateSheet(XLWorkbook workbook, string sheetName, string[] columns)
    {
        var sheet = workbook.Worksheets.Add(sheetName);
        sheet.RightToLeft = true;

        for (var i = 0; i < columns.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2E8E6");
        }

        sheet.Columns().AdjustToContents();
        return sheet;
    }

    /// <summary>
    /// Restricts a column to an Excel in-cell dropdown of <paramref name="options"/>, so the user
    /// picks a value instead of guessing what's valid (e.g. the exact spelling of "הוצאת הו\"ק").
    /// The options are written into a hidden helper sheet and referenced by range rather than as an
    /// inline literal list, since some option text (like the embedded quote in "הו\"ק") isn't safe to
    /// splice into an inline comma-separated validation formula.
    /// </summary>
    private static void ApplyDropdown(XLWorkbook workbook, IXLWorksheet sheet, int column, string[] options)
    {
        const string listSheetName = "רשימות";
        if (!workbook.Worksheets.TryGetWorksheet(listSheetName, out var listSheet))
        {
            listSheet = workbook.Worksheets.Add(listSheetName);
            listSheet.Visibility = XLWorksheetVisibility.VeryHidden;
        }

        var listColumn = (listSheet.LastColumnUsed()?.ColumnNumber() ?? 0) + 1;
        for (var i = 0; i < options.Length; i++)
        {
            listSheet.Cell(i + 1, listColumn).Value = options[i];
        }

        var optionsRange = listSheet.Range(1, listColumn, options.Length, listColumn);
        var targetRange = sheet.Range(2, column, DropdownRowCount, column);
        targetRange.CreateDataValidation().List(optionsRange, true);
    }
}
