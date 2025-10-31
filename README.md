# Purchase Service

Purchase Service is an ASP.NET Core 9.0 minimal API that stores purchase transactions in PostgreSQL and retrieves them converted to foreign currencies using the U.S. Treasury Reporting Rates of Exchange.

## Requirements

- .NET SDK 9.0.306 or later
- Docker (for optional local database and integration tests)

## Configuration

The service reads configuration from `appsettings.json` and environment variables. Key settings include:

- `Database:ConnectionString` – PostgreSQL connection string. Defaults to `Host=localhost;Port=5432;Database=purchases;Username=postgres;Password=postgres`.
- `TreasuryRates:BaseUrl` – base URL for the Treasury exchange-rate API.

Environment variables can override configuration using the `__` separator, e.g.

```bash
export Database__ConnectionString="Host=localhost;Port=5432;Database=purchases;Username=postgres;Password=postgres"
```

## Running the API locally

1. Ensure PostgreSQL is available and the configured connection string is valid.
2. Restore and build the solution:
   ```bash
   dotnet build
   ```
3. Run the API:
   ```bash
   dotnet run --project src/PurchaseService.Api
   ```
4. The API listens on `http://localhost:8080` inside the Docker container and the default Kestrel port when run locally. Use the included HTTP file `src/PurchaseService.Api/PurchaseService.Api.http` or tools such as `curl` to exercise the endpoints.

## Docker Compose

A `docker-compose.yml` file is provided to run both the API and PostgreSQL:

```bash
docker compose up --build
```

The API becomes available on `http://localhost:8080` and uses the compose-provisioned database service.

## API Endpoints

- `POST /purchases`
  - Body: `{ "description": "Coffee", "transactionDate": "2024-06-25", "amount": 12.34 }`
  - Stores a purchase and returns the persisted payload with the generated identifier.
- `GET /purchases/{id}?currency=EUR`
  - Retrieves the purchase in the requested currency, returning the exchange rate and converted amount.

Validation rules:

- Description: required, max 50 characters.
- Transaction date: required, ISO-8601 date.
- Amount: positive and rounded to two decimal places.

Conversion rules:

- Uses the latest Treasury exchange rate on or before the purchase date, up to six months prior.
- Returns a 400 error if no eligible rate exists.

## Automated Tests

Restore and run the test suite:

```bash
dotnet test
```

Unit tests run by default. Integration tests that depend on Docker are skipped unless explicitly enabled:

```bash
export RUN_DOCKER_TESTS=1
DOTNET_CLI_HOME=$PWD/.dotnet dotnet test
```

Ensure Docker is running before enabling these tests.
