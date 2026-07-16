using FamilyBudget.Api.Services;

namespace FamilyBudget.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this WebApplication app)
    {
        app.MapGet("/api/export/excel", ExportExcel);
    }

    private static async Task<IResult> ExportExcel(
        int annualYear, int overviewYear, int overviewMonth, ExcelExportService exportService)
    {
        if (overviewMonth is < 1 or > 12)
        {
            return Results.BadRequest("overviewMonth must be between 1 and 12.");
        }

        var bytes = await exportService.BuildWorkbookAsync(annualYear, overviewYear, overviewMonth);

        return Results.File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"תקציב-{overviewYear}-{overviewMonth:D2}.xlsx");
    }
}
