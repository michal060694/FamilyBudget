using ClosedXML.Excel;

namespace FamilyBudget.Api.Services;

public record ImportRowError(int RowNumber, string Message);

public record ImportResult(int AddedCount, IReadOnlyList<ImportRowError> Errors);

/// <summary>Thrown when an uploaded workbook doesn't match the expected template for the target endpoint.</summary>
public sealed class ImportTemplateMismatchException(string message) : Exception(message);

/// <summary>
/// Shared row-iteration and cell-parsing helpers for all Excel import endpoints. Row 1 is always the
/// header, data starts at row 2, blank rows are skipped. Every import is add-only: each row is parsed,
/// constructed, and persisted independently in the same pass, so one row's validation failure can
/// never roll back an already-committed row — bad rows are skipped and reported, good rows still land.
/// </summary>
internal static class ExcelTableReader
{
    public static async Task<ImportResult> ProcessRowsAsync(IXLWorksheet sheet, Func<IXLRow, Task> processRowAsync)
    {
        var addedCount = 0;
        var errors = new List<ImportRowError>();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (row.IsEmpty())
            {
                continue;
            }

            try
            {
                await processRowAsync(row);
                addedCount++;
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                errors.Add(new ImportRowError(rowNumber, ex.Message));
            }
        }

        return new ImportResult(addedCount, errors);
    }

    /// <summary>Returns the workbook's first worksheet, or throws if its first header cell doesn't match (a cheap "wrong template" guard — column position, not header text, drives actual parsing).</summary>
    public static IXLWorksheet GetSheetOrThrow(XLWorkbook workbook, string expectedFirstColumn)
    {
        var sheet = workbook.Worksheets.First();
        return ValidateHeader(sheet, expectedFirstColumn);
    }

    /// <summary>Same as <see cref="GetSheetOrThrow(XLWorkbook, string)"/>, but for a workbook with multiple named sheets (e.g. funds + earmarks sharing one file) — looks the sheet up by name instead of just taking the first one.</summary>
    public static IXLWorksheet GetSheetOrThrow(XLWorkbook workbook, string sheetName, string expectedFirstColumn)
    {
        if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var sheet))
        {
            throw new ImportTemplateMismatchException(
                $"This file doesn't look like the expected import template (missing a sheet named '{sheetName}').");
        }

        return ValidateHeader(sheet, expectedFirstColumn);
    }

    private static IXLWorksheet ValidateHeader(IXLWorksheet sheet, string expectedFirstColumn)
    {
        var firstHeaderCell = sheet.Cell(1, 1).GetString().Trim();
        if (firstHeaderCell != expectedFirstColumn)
        {
            throw new ImportTemplateMismatchException(
                $"This file doesn't look like the expected import template (first column should be '{expectedFirstColumn}').");
        }

        return sheet;
    }

    public static string RequiredText(IXLRow row, int column, string columnName)
    {
        var value = row.Cell(column).GetString().Trim();
        if (value.Length == 0)
        {
            throw new FormatException($"{columnName} is required.");
        }

        return value;
    }

    public static string? OptionalText(IXLRow row, int column)
    {
        var value = row.Cell(column).GetString().Trim();
        return value.Length == 0 ? null : value;
    }

    public static decimal RequiredDecimal(IXLRow row, int column, string columnName)
    {
        var cell = row.Cell(column);
        if (cell.IsEmpty() || !cell.TryGetValue(out decimal value))
        {
            throw new FormatException($"{columnName} is required and must be a number.");
        }

        return value;
    }

    public static decimal OptionalDecimal(IXLRow row, int column, string columnName, decimal defaultValue = 0m)
    {
        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return defaultValue;
        }

        if (!cell.TryGetValue(out decimal value))
        {
            throw new FormatException($"{columnName} must be a number.");
        }

        return value;
    }

    public static decimal? OptionalNullableDecimal(IXLRow row, int column, string columnName)
    {
        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (!cell.TryGetValue(out decimal value))
        {
            throw new FormatException($"{columnName} must be a number.");
        }

        return value;
    }

    public static DateOnly RequiredDate(IXLRow row, int column, string columnName)
    {
        var date = OptionalDate(row, column, columnName);
        if (date is null)
        {
            throw new FormatException($"{columnName} is required.");
        }

        return date.Value;
    }

    public static DateOnly? OptionalDate(IXLRow row, int column, string columnName)
    {
        var cell = row.Cell(column);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue(out DateTime dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        var text = cell.GetString().Trim();
        if (DateOnly.TryParse(text, out var date))
        {
            return date;
        }

        throw new FormatException($"{columnName} must be a valid date (yyyy-MM-dd).");
    }

    public static bool OptionalBool(IXLRow row, int column, string columnName, bool defaultValue = true)
    {
        var text = OptionalText(row, column);
        if (text is null)
        {
            return defaultValue;
        }

        return text switch
        {
            "כן" => true,
            "לא" => false,
            _ => throw new FormatException($"{columnName} must be כן or לא."),
        };
    }
}
