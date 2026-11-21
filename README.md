# ASP.NET Core PostgreSQL and NServiceBus Pub-Sub Example

Sample ASP.NET Core pub/sub proof-of-concept using **PostgreSQL**, **Entity Framework Core**, and **NServiceBus**.

`Sample.Api` accepts ticket submissions and publishes a `TicketSubmitted` event to NServiceBus (`PostgreSqlTransport`), `Sample.Worker` consumes the event and persists tickets to PostgreSQL via EF Core, and `GET /api/ticket` reads persisted tickets back with optional sorting and interactive testing via Scalar.

## Solution Structure

* **`Sample.Api`** — ASP.NET Core web app exposing `POST /api/ticket`, `GET /api/ticket`, OpenAPI, and Scalar UI (`/scalar`).
* **`Sample.Worker`** — Generic host running NServiceBus message handling (`TicketSubmittedHandler`) to write tickets into PostgreSQL.
* **`Sample.Common`** — Shared contracts (`CreateTicketRequest`, `TicketResponse`, `TicketOrderBy`), NServiceBus event definitions (`TicketSubmitted : IEvent`), and EF Core persistence (`SampleDbContext`, `Ticket`).

## How It Works

1. **`POST /api/ticket`** validates `CreateTicketRequest`, generates a ticket ID, publishes `TicketSubmitted` via `IMessageSession`, and returns `202 Accepted`.
2. **`Sample.Worker`** (`TicketSubmittedHandler` implementing `IHandleMessages<TicketSubmitted>`) receives `TicketSubmitted`, skips duplicates if the ticket ID already exists, and saves the `Ticket` to the `app.tickets` table via `SampleDbContext`.
3. **`GET /api/ticket`** queries `SampleDbContext` and returns a JSON array of `TicketResponse` items (`id`, `text`, `createdAt`), supporting an optional `orderBy` query parameter (`CreatedAtDesc`, `CreatedAtAsc`, `TextAsc`, `TextDesc`) serialized as strings.

## Configuration & Local Setup

Configure `ConnectionStrings:DefaultConnection` and `Messaging:EndpointName` in each service's configuration (for example `appsettings.Development.json` or user secrets):

* **`Sample.Api`**: `Messaging:EndpointName` set to `"Sample.Api"` and `ConnectionStrings:DefaultConnection` pointing to PostgreSQL.
* **`Sample.Worker`**: `Messaging:EndpointName` set to `"Sample.Worker"` and `ConnectionStrings:DefaultConnection` pointing to PostgreSQL.

In `Development`:
* NServiceBus installers (`EnableInstallers()`) create required transport/queue tables automatically.
* `SampleDbContext.Database.EnsureCreatedAsync()` creates the schema/tables if they don't exist yet (switch to EF Core migrations once schema changes need versioning).

## Running Locally

1. Start PostgreSQL and ensure the connection string is configured for both `Sample.Api` and `Sample.Worker`.
2. Run `Sample.Worker` so it starts listening for `TicketSubmitted` events.
3. Run `Sample.Api` and open `/scalar` to test `POST /api/ticket` and `GET /api/ticket` (including `?orderBy=...`).

## AI Disclaimer

Gemini LLM model was used to suggest and generate some parts of the code, which were manually cleaned up and simplified afterwards.