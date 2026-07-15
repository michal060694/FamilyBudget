using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Endpoints;

public static class FundEndpoints
{
    public static void MapFundEndpoints(this WebApplication app)
    {
        app.MapGet("/api/funds", GetFunds);
        app.MapPost("/api/funds", CreateFund);
        app.MapPut("/api/funds/{id:guid}", UpdateFund);
        app.MapDelete("/api/funds/{id:guid}", DeleteFund);

        app.MapPost("/api/funds/{fundId:guid}/earmarks", CreateEarmark);
        app.MapPut("/api/funds/earmarks/{id:guid}", UpdateEarmark);
        app.MapDelete("/api/funds/earmarks/{id:guid}", DeleteEarmark);
    }

    private static FundEarmarkResponse ToResponse(FundEarmark earmark) => new(
        earmark.Id, earmark.PurposeLabel, earmark.Amount);

    private static FundSummaryResponse ToResponse(FundSummary summary) => new(
        summary.FundId,
        summary.Name,
        summary.TotalBalance,
        summary.EarmarkedTotal,
        summary.Discrepancy,
        summary.Earmarks.Select(ToResponse).ToList());

    private static async Task<IResult> GetFunds(FundSummaryQueryService queryService)
    {
        var summaries = await queryService.GetAllAsync();
        return Results.Ok(new FundListResponse(summaries.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> CreateFund(CreateFundRequest request, IFundRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.TotalBalance < 0)
        {
            return Results.BadRequest("totalBalance must be >= 0.");
        }

        var fund = new Fund(Guid.NewGuid(), request.Name, request.TotalBalance);
        await repository.AddAsync(fund);

        var summary = FundSummaryQueryService.BuildSummary(fund, Array.Empty<FundEarmark>());
        return Results.Created($"/api/funds/{fund.Id}", ToResponse(summary));
    }

    private static async Task<IResult> UpdateFund(
        Guid id, UpdateFundRequest request, IFundRepository fundRepository, IFundEarmarkRepository earmarkRepository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.TotalBalance < 0)
        {
            return Results.BadRequest("totalBalance must be >= 0.");
        }

        var fund = await fundRepository.GetByIdAsync(id);
        if (fund is null)
        {
            return Results.NotFound();
        }

        fund.Rename(request.Name);
        fund.SetTotalBalance(request.TotalBalance);
        await fundRepository.SaveChangesAsync();

        var earmarks = await earmarkRepository.GetByFundIdAsync(id);
        var summary = FundSummaryQueryService.BuildSummary(fund, earmarks);
        return Results.Ok(ToResponse(summary));
    }

    private static async Task<IResult> DeleteFund(Guid id, IFundRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> CreateEarmark(
        Guid fundId, CreateFundEarmarkRequest request, IFundRepository fundRepository, IFundEarmarkRepository earmarkRepository)
    {
        if (string.IsNullOrWhiteSpace(request.PurposeLabel))
        {
            return Results.BadRequest("purposeLabel must not be empty.");
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest("amount must be strictly positive.");
        }

        var fund = await fundRepository.GetByIdAsync(fundId);
        if (fund is null)
        {
            return Results.NotFound();
        }

        var earmark = new FundEarmark(Guid.NewGuid(), fundId, request.PurposeLabel, request.Amount);
        await earmarkRepository.AddAsync(earmark);

        return Results.Created($"/api/funds/earmarks/{earmark.Id}", ToResponse(earmark));
    }

    private static async Task<IResult> UpdateEarmark(
        Guid id, UpdateFundEarmarkRequest request, IFundEarmarkRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.PurposeLabel))
        {
            return Results.BadRequest("purposeLabel must not be empty.");
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest("amount must be strictly positive.");
        }

        var earmark = await repository.GetByIdAsync(id);
        if (earmark is null)
        {
            return Results.NotFound();
        }

        earmark.Update(request.PurposeLabel, request.Amount);
        await repository.SaveChangesAsync();

        return Results.Ok(ToResponse(earmark));
    }

    private static async Task<IResult> DeleteEarmark(Guid id, IFundEarmarkRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
