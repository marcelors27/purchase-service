FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files separately to leverage Docker layer caching for restore.
COPY PurchaseService.sln ./
COPY src/PurchaseService.Api/PurchaseService.Api.csproj src/PurchaseService.Api/

RUN dotnet restore src/PurchaseService.Api/PurchaseService.Api.csproj

COPY src/ ./src/

RUN dotnet publish src/PurchaseService.Api/PurchaseService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PurchaseService.Api.dll"]
