using ClosedXML.Excel;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Services;

/// <summary>
/// Builds a single .xlsx workbook with one sheet per tab of the single-page UI (annual budget,
/// monthly overview, funds, debts), reusing the same query services the API endpoints use so the
/// export always matches what's on screen.
/// </summary>
public class ExcelExportService
{
    private static readonly string[] HebrewMonthNames =
        ["", "תשרי", "חשוון", "כסלו", "טבת", "שבט", "אדר", "ניסן", "אייר", "סיוון", "תמוז", "אב", "אלול"];

    private const string AmountFormat = "#,##0.00";

    private readonly AnnualBudgetQueryService _annualBudgetQueryService;
    private readonly CalendarYearCycle _calendarYearCycle;
    private readonly MonthlyOverviewQueryService _monthlyOverviewQueryService;
    private readonly FundSummaryQueryService _fundSummaryQueryService;
    private readonly IDebtRepository _debtRepository;

    public ExcelExportService(
        AnnualBudgetQueryService annualBudgetQueryService,
        CalendarYearCycle calendarYearCycle,
        MonthlyOverviewQueryService monthlyOverviewQueryService,
        FundSummaryQueryService fundSummaryQueryService,
        IDebtRepository debtRepository)
    {
        _annualBudgetQueryService = annualBudgetQueryService;
        _calendarYearCycle = calendarYearCycle;
        _monthlyOverviewQueryService = monthlyOverviewQueryService;
        _fundSummaryQueryService = fundSummaryQueryService;
        _debtRepository = debtRepository;
    }

    public async Task<byte[]> BuildWorkbookAsync(
        int annualYear, int overviewYear, int overviewMonth, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();

        await AddAnnualBudgetSheetAsync(workbook, annualYear, cancellationToken);
        await AddMonthlyOverviewSheetAsync(workbook, overviewYear, overviewMonth, cancellationToken);
        await AddFundsSheetAsync(workbook, cancellationToken);
        await AddDebtsSheetAsync(workbook, cancellationToken);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IXLWorksheet NewSheet(XLWorkbook workbook, string name)
    {
        var sheet = workbook.Worksheets.Add(name);
        sheet.RightToLeft = true;
        return sheet;
    }

    private static IXLCell SectionHeader(IXLWorksheet sheet, ref int row, string text)
    {
        var cell = sheet.Cell(row, 1);
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 13;
        row++;
        return cell;
    }

    private static void TableHeader(IXLWorksheet sheet, int row, params string[] columns)
    {
        for (var i = 0; i < columns.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2E8E6");
        }
    }

    private static void AmountCell(IXLWorksheet sheet, int row, int col, decimal value)
    {
        var cell = sheet.Cell(row, col);
        cell.Value = value;
        cell.Style.NumberFormat.Format = AmountFormat;
    }

    private async Task AddAnnualBudgetSheetAsync(XLWorkbook workbook, int year, CancellationToken cancellationToken)
    {
        var sheet = NewSheet(workbook, "תקציב שנתי");
        var items = await _annualBudgetQueryService.GetOrderedForYearAsync(year, cancellationToken);
        var (_, currentMonth, currentDay) = _calendarYearCycle.GetCurrent();
        var summary = await _annualBudgetQueryService.GetSummaryAsync(year, currentMonth, currentDay, cancellationToken);

        var row = 1;
        SectionHeader(sheet, ref row, $"תקציב שנתי {year}");
        row++;

        sheet.Cell(row, 1).Value = "סה\"כ תקציב שנתי";
        AmountCell(sheet, row, 2, summary.TotalAnnualBudget);
        sheet.Cell(row, 3).Value = "כמה יש בקופה";
        AmountCell(sheet, row, 4, summary.ReserveOnHand);
        row++;
        sheet.Cell(row, 1).Value = "כמה עוד לא נוצל";
        AmountCell(sheet, row, 2, summary.NotYetCovered);
        sheet.Cell(row, 3).Value = "הפרשה נדרשת לכל חודש";
        AmountCell(sheet, row, 4, summary.MonthlyAllocation);
        row += 2;

        TableHeader(sheet, row, "חודש", "שם", "סכום שנתי", "נוצל", "יתרה לנצול");
        row++;
        foreach (var item in items)
        {
            sheet.Cell(row, 1).Value = item.TargetMonth is { } month ? HebrewMonthNames[month] : "כללי (לא תלוי חודש)";
            sheet.Cell(row, 2).Value = item.Name;
            AmountCell(sheet, row, 3, item.TotalAmount);
            AmountCell(sheet, row, 4, item.AmountUsed);
            AmountCell(sheet, row, 5, item.TotalAmount - item.AmountUsed);
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private async Task AddMonthlyOverviewSheetAsync(XLWorkbook workbook, int year, int month, CancellationToken cancellationToken)
    {
        var sheet = NewSheet(workbook, "סקירה חודשית");
        var overview = await _monthlyOverviewQueryService.GetOverviewAsync(year, month, cancellationToken);
        var obligation = overview.TitheObligation;

        var row = 1;
        SectionHeader(sheet, ref row, $"סקירה חודשית {HebrewMonthNames[month]} ({month}/{year})");
        row += 2;

        row = WriteTransactionsTable(sheet, row, "הכנסות חייבות במעשר", overview.TitheApplicableIncomeLines);
        row = WriteTransactionsTable(sheet, row, "הכנסות פטורות ממעשר", overview.NonTitheApplicableIncomeLines);

        SectionHeader(sheet, ref row, "סיכום חודשי");
        sheet.Cell(row, 1).Value = "סה\"כ הכנסות";
        AmountCell(sheet, row, 2, overview.TotalIncome);
        sheet.Cell(row, 3).Value = "החזרי חובות";
        AmountCell(sheet, row, 4, overview.DebtRepaymentsSummary);
        row++;
        sheet.Cell(row, 1).Value = "סה\"כ יוצא (תרומות + הוצאות + חובות)";
        AmountCell(sheet, row, 2, overview.TotalOutflow);
        sheet.Cell(row, 3).Value = "נשאר לחיסכון";
        AmountCell(sheet, row, 4, overview.RemainingToSave);
        row += 2;

        SectionHeader(sheet, ref row, "מעשרות");
        sheet.Cell(row, 1).Value = "א. מעשרות לתרומה";
        AmountCell(sheet, row, 2, obligation.GrossTitheTarget);
        row++;
        sheet.Cell(row, 1).Value = "ב. תרומות קבועות בחודש";
        AmountCell(sheet, row, 2, obligation.FixedDonationsThisMonth);
        row++;
        row = WriteTable(sheet, row, null, ["שם תרומה", "סכום"],
            overview.FixedDonationStandingOrders,
            o => [o.Name, o.Amount]);
        sheet.Cell(row, 1).Value = "ג. קיזוז שהועבר מחודש קודם";
        AmountCell(sheet, row, 2, obligation.PriorMonthSmallCharityTotal);
        row++;
        row = WriteTransactionsTable(sheet, row, null, overview.PriorMonthSmallCharityDonations);
        sheet.Cell(row, 1).Value = "ד. תרומות לתרומה";
        AmountCell(sheet, row, 2, obligation.StillToDonateAfterFixed);
        row += 2;

        row = WriteTable(sheet, row, "הוצאות שצריך להביא מהקופה השנתית", ["שם", "סכום למשיכה"],
            overview.AnnualWithdrawalItems,
            i => [i.Name, i.TotalAmount]);

        row = WriteTable(sheet, row, "הוצאות בהוראת קבע", ["שם", "מתוכנן"],
            overview.FixedExpenseItems,
            i => [i.Name, i.BudgetedAmount]);

        row = WriteTable(sheet, row, "הוצאות שוטפות", ["שם", "מתוכנן", "נוצל", "יתרה", "כלול בסה\"כ"],
            overview.RegularExpenseItems,
            i => [i.Name, i.BudgetedAmount, i.UsedAmount, i.Remaining, i.IncludeInOutflowTotal ? "כן" : "לא"]);

        sheet.Columns().AdjustToContents();
    }

    private int WriteTransactionsTable(IXLWorksheet sheet, int row, string? title, IReadOnlyList<Transaction> lines)
    {
        return WriteTable(sheet, row, title, ["תאריך", "תיאור", "סכום"],
            lines, t => [t.Date.ToString("yyyy-MM-dd"), t.Description ?? "", t.Amount]);
    }

    /// <summary>
    /// Writes an optional bold section title, a header row, then one row per item. Each row value is
    /// either a <see cref="decimal"/> (written as a real numeric cell with <see cref="AmountFormat"/>,
    /// so totals stay sum-able in Excel) or anything else (written as plain text). Returns the next
    /// free row.
    /// </summary>
    private static int WriteTable<T>(IXLWorksheet sheet, int row, string? title, string[] columns, IReadOnlyList<T> items, Func<T, object[]> toRow)
    {
        if (title is not null)
        {
            SectionHeader(sheet, ref row, title);
        }

        TableHeader(sheet, row, columns);
        row++;

        if (items.Count == 0)
        {
            sheet.Cell(row, 1).Value = "(אין נתונים)";
            sheet.Cell(row, 1).Style.Font.Italic = true;
            row++;
        }
        else
        {
            foreach (var item in items)
            {
                var values = toRow(item);
                for (var i = 0; i < values.Length; i++)
                {
                    var cell = sheet.Cell(row, i + 1);
                    if (values[i] is decimal amount)
                    {
                        cell.Value = amount;
                        cell.Style.NumberFormat.Format = AmountFormat;
                    }
                    else
                    {
                        cell.Value = values[i]?.ToString() ?? "";
                    }
                }
                row++;
            }
        }

        return row + 1;
    }

    private async Task AddFundsSheetAsync(XLWorkbook workbook, CancellationToken cancellationToken)
    {
        var sheet = NewSheet(workbook, "קרנות");
        var funds = await _fundSummaryQueryService.GetAllAsync(cancellationToken);

        var row = 1;
        SectionHeader(sheet, ref row, "קרנות");
        row++;

        if (funds.Count == 0)
        {
            sheet.Cell(row, 1).Value = "(אין קרנות מוגדרות)";
        }

        foreach (var fund in funds)
        {
            var header = sheet.Cell(row, 1);
            header.Value = fund.Name;
            header.Style.Font.Bold = true;
            header.Style.Font.FontSize = 12;
            row++;

            sheet.Cell(row, 1).Value = "יתרה כוללת";
            AmountCell(sheet, row, 2, fund.TotalBalance);
            sheet.Cell(row, 3).Value = "סה\"כ צבוע";
            AmountCell(sheet, row, 4, fund.EarmarkedTotal);
            sheet.Cell(row, 5).Value = "איזון";
            AmountCell(sheet, row, 6, fund.Discrepancy);
            row++;

            row = WriteTable(sheet, row, null, ["מטרה", "סכום"],
                fund.Earmarks, e => [e.PurposeLabel, e.Amount]);
        }

        sheet.Columns().AdjustToContents();
    }

    private async Task AddDebtsSheetAsync(XLWorkbook workbook, CancellationToken cancellationToken)
    {
        var sheet = NewSheet(workbook, "חובות");
        var debts = await _debtRepository.GetAllAsync(cancellationToken);
        var receivables = debts.Where(d => d.Direction == DebtDirection.Receivable).ToList();
        var payables = debts.Where(d => d.Direction == DebtDirection.Payable).ToList();

        var row = 1;
        row = WriteTable(sheet, row, "חייבים לנו", ["שם", "יתרה", "תאריך יעד", "הערות", "סטטוס"],
            receivables,
            d => [d.CounterpartyName, d.CurrentBalance, d.TargetDate?.ToString("yyyy-MM-dd") ?? "", d.Notes ?? "", StatusLabel(d.Status)]);

        row = WriteTable(sheet, row, "אנחנו חייבים", ["שם", "יתרה", "קצב החזר", "סטטוס"],
            payables,
            d => [d.CounterpartyName, d.CurrentBalance, d.RepaymentRate.HasValue ? (object)d.RepaymentRate.Value : "", StatusLabel(d.Status)]);

        _ = row;
        sheet.Columns().AdjustToContents();
    }

    private static string StatusLabel(DebtStatus status) => status == DebtStatus.Closed ? "סגור" : "פתוח";
}
