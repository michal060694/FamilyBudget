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

    private static readonly Dictionary<string, PaymentMethod> PaymentMethodByHebrew = new()
    {
        ["אשראי"] = PaymentMethod.CreditCard,
        ["העברה בנקאית"] = PaymentMethod.BankTransfer,
        ["מזומן"] = PaymentMethod.Cash,
        ["צ'ק"] = PaymentMethod.Check,
    };

    private readonly ITransactionRepository _transactionRepository;
    private readonly IMonthlyExpenseBudgetItemRepository _monthlyExpenseBudgetItemRepository;
    private readonly IFixedDonationStandingOrderRepository _fixedDonationStandingOrderRepository;
    private readonly IAnnualBudgetItemRepository _annualBudgetItemRepository;
    private readonly IFundRepository _fundRepository;
    private readonly IFundEarmarkRepository _fundEarmarkRepository;
    private readonly IDebtRepository _debtRepository;

    public ExcelImportService(
        ITransactionRepository transactionRepository,
        IMonthlyExpenseBudgetItemRepository monthlyExpenseBudgetItemRepository,
        IFixedDonationStandingOrderRepository fixedDonationStandingOrderRepository,
        IAnnualBudgetItemRepository annualBudgetItemRepository,
        IFundRepository fundRepository,
        IFundEarmarkRepository fundEarmarkRepository,
        IDebtRepository debtRepository)
    {
        _transactionRepository = transactionRepository;
        _monthlyExpenseBudgetItemRepository = monthlyExpenseBudgetItemRepository;
        _fixedDonationStandingOrderRepository = fixedDonationStandingOrderRepository;
        _annualBudgetItemRepository = annualBudgetItemRepository;
        _fundRepository = fundRepository;
        _fundEarmarkRepository = fundEarmarkRepository;
        _debtRepository = debtRepository;
    }

    // ---------- Transactions ----------

    private static readonly string[] TransactionColumns = ["תאריך", "סוג", "סכום", "אמצעי תשלום", "חייב מעשר", "תיאור"];

    public async Task<ImportResult> ImportTransactionsAsync(XLWorkbook workbook, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, TransactionColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var date = ExcelTableReader.RequiredDate(row, 1, "תאריך");
            var type = ParseTransactionType(row, 2);
            var amount = ExcelTableReader.RequiredDecimal(row, 3, "סכום");
            var paymentMethod = ParsePaymentMethod(row, 4);
            var isTitheApplicableText = ExcelTableReader.OptionalText(row, 5);
            var description = ExcelTableReader.OptionalText(row, 6);

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

            var transaction = new Transaction(Guid.NewGuid(), date, amount, type, paymentMethod, isTitheApplicable, description);
            await _transactionRepository.AddAsync(transaction, cancellationToken);
        });
    }

    public Task<byte[]> BuildTransactionsTemplateAsync() => Task.FromResult(BuildTemplate(TransactionColumns));

    // ---------- Monthly expense budget items ----------

    private static readonly string[] MonthlyExpenseBudgetColumns = ["סוג", "שם", "תקציב", "נוצל", "כלול בסה\"כ"];

    public async Task<ImportResult> ImportMonthlyExpenseBudgetsAsync(
        XLWorkbook workbook, int year, int month, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, MonthlyExpenseBudgetColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var type = ParseExpenseType(row, 1);
            var name = ExcelTableReader.RequiredText(row, 2, "שם");
            var budgeted = ExcelTableReader.RequiredDecimal(row, 3, "תקציב");
            var used = ExcelTableReader.OptionalDecimal(row, 4, "נוצל");
            var include = ExcelTableReader.OptionalBool(row, 5, "כלול בסה\"כ");

            var item = new MonthlyExpenseBudgetItem(
                Guid.NewGuid(), year, month, name, type, budgeted, used, includeInOutflowTotal: include);
            await _monthlyExpenseBudgetItemRepository.AddAsync(item, cancellationToken);
        });
    }

    public Task<byte[]> BuildMonthlyExpenseBudgetsTemplateAsync() => Task.FromResult(BuildTemplate(MonthlyExpenseBudgetColumns));

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

    public Task<byte[]> BuildAnnualBudgetItemsTemplateAsync() => Task.FromResult(BuildTemplate(AnnualBudgetItemColumns));

    // ---------- Funds ----------

    private static readonly string[] FundColumns = ["שם", "יתרה כוללת"];

    public async Task<ImportResult> ImportFundsAsync(XLWorkbook workbook, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, FundColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var name = ExcelTableReader.RequiredText(row, 1, "שם");
            var totalBalance = ExcelTableReader.RequiredDecimal(row, 2, "יתרה כוללת");

            var fund = new Fund(Guid.NewGuid(), name, totalBalance);
            await _fundRepository.AddAsync(fund, cancellationToken);
        });
    }

    public Task<byte[]> BuildFundsTemplateAsync() => Task.FromResult(BuildTemplate(FundColumns));

    // ---------- Fund earmarks ----------

    private static readonly string[] FundEarmarkColumns = ["מטרה", "סכום"];

    public async Task<ImportResult> ImportFundEarmarksAsync(XLWorkbook workbook, Guid fundId, CancellationToken cancellationToken = default)
    {
        var sheet = ExcelTableReader.GetSheetOrThrow(workbook, FundEarmarkColumns[0]);

        return await ExcelTableReader.ProcessRowsAsync(sheet, async row =>
        {
            var purposeLabel = ExcelTableReader.RequiredText(row, 1, "מטרה");
            var amount = ExcelTableReader.RequiredDecimal(row, 2, "סכום");

            var earmark = new FundEarmark(Guid.NewGuid(), fundId, purposeLabel, amount);
            await _fundEarmarkRepository.AddAsync(earmark, cancellationToken);
        });
    }

    public Task<byte[]> BuildFundEarmarksTemplateAsync() => Task.FromResult(BuildTemplate(FundEarmarkColumns));

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

    private static TransactionType ParseExpenseType(IXLRow row, int column)
    {
        var type = ParseTransactionType(row, column);
        if (type is not (TransactionType.FixedExpense or TransactionType.RegularExpense))
        {
            throw new FormatException("סוג must be הוצאת הו\"ק or הוצאה שוטפת.");
        }

        return type;
    }

    private static PaymentMethod ParsePaymentMethod(IXLRow row, int column)
    {
        var text = ExcelTableReader.RequiredText(row, column, "אמצעי תשלום");
        return PaymentMethodByHebrew.TryGetValue(text, out var method)
            ? method
            : throw new FormatException($"Unrecognized אמצעי תשלום value: '{text}'.");
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

    private static byte[] BuildTemplate(string[] columns)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("תבנית");
        sheet.RightToLeft = true;

        for (var i = 0; i < columns.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2E8E6");
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
