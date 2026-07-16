using ClosedXML.Excel;

namespace FamilyBudget.Api.Tests;

public class ExportEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private readonly FamilyBudgetApiFactory _factory;

    public ExportEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExportExcel_ReturnsWorkbookWithFourExpectedSheets()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/export/excel?annualYear=2070&overviewYear=2070&overviewMonth=3");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType!.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        var sheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();
        Assert.Equal(["תקציב שנתי", "סקירה חודשית", "קרנות", "חובות"], sheetNames);
    }

    [Fact]
    public async Task ExportExcel_InvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/export/excel?annualYear=2070&overviewYear=2070&overviewMonth=13");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
