FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/PurchaseService.Api/PurchaseService.Api.csproj", "src/PurchaseService.Api/"]
RUN dotnet restore "src/PurchaseService.Api/PurchaseService.Api.csproj"

COPY . .
WORKDIR "/src/src/PurchaseService.Api"
RUN dotnet publish "PurchaseService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PurchaseService.Api.dll"]
