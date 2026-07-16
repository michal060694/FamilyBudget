FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/FamilyBudget.Core/FamilyBudget.Core.csproj src/FamilyBudget.Core/
COPY src/FamilyBudget.Infrastructure/FamilyBudget.Infrastructure.csproj src/FamilyBudget.Infrastructure/
COPY src/FamilyBudget.Api/FamilyBudget.Api.csproj src/FamilyBudget.Api/
RUN dotnet restore src/FamilyBudget.Api/FamilyBudget.Api.csproj

COPY src/ src/
RUN dotnet publish src/FamilyBudget.Api/FamilyBudget.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render (and most PaaS hosts) inject a PORT env var and expect the app to listen on it; default to
# 8080 for any other context (e.g. running the image locally without one set).
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet FamilyBudget.Api.dll"]
