# Inventory & Order Management System

[![CI](https://github.com/sandip07-devolper/Inventory-Management-System/actions/workflows/ci.yml/badge.svg)](https://github.com/sandip07-devolper/Inventory-Management-System/actions/workflows/ci.yml)

A multi-tenant inventory and order management REST API built with ASP.NET Core, EF Core, and MySQL.

## Stack

- ASP.NET Core 8 Web API
- Entity Framework Core (Pomelo MySQL provider)
- ASP.NET Core Identity + JWT authentication
- Swagger / OpenAPI
- MySQL

## Architecture

```
src/
  InventoryOrderSystem.Domain          Entities, interfaces (no external deps)
  InventoryOrderSystem.Infrastructure  EF Core DbContext, data access
  InventoryOrderSystem.API             Controllers, DI wiring, auth, Swagger
```

### Multi-tenancy

Shared-database, discriminator-column strategy. Every tenant-scoped entity implements
`ITenantEntity`. `AppDbContext` automatically:

- Applies a global EF Core query filter (`WHERE TenantId = @current`) to every such entity.
- Stamps `TenantId` on new entities based on the caller's JWT (`tenantId` claim) on save.

This means feature code (controllers/services) never has to remember to filter by tenant.

### Roles & authorization

Two roles per tenant: `Admin` (created automatically for the user who registers
the tenant) and `Staff`. Both are seeded once at application startup. Most
endpoints only require `[Authorize]` (any authenticated user in the tenant),
but `/api/users` is restricted to `Admin` via `[Authorize(Roles = "Admin")]` -
the one place role membership actually gates access right now. An Admin can
invite additional users into their tenant, assign either role, and
deactivate/reactivate accounts (with a guard against deactivating your own
account by mistake).

## Getting Started

### Prerequisites

- .NET 8 SDK
- MySQL 8+

### Setup

1. Create a MySQL database and update `ConnectionStrings:DefaultConnection` in
   `src/InventoryOrderSystem.API/appsettings.json` (or use `dotnet user-secrets`).
2. Set a real `Jwt:Key` (32+ char random secret) in the same file / user-secrets.
3. **Generate the initial migration** (one-time, only needed once - not yet
   committed to this repo):

```bash
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef migrations add InitialCreate \
  --project src/InventoryOrderSystem.Infrastructure \
  --startup-project src/InventoryOrderSystem.API
```

4. Restore, apply the migration, and run:

```bash
dotnet restore
dotnet ef database update --project src/InventoryOrderSystem.Infrastructure --startup-project src/InventoryOrderSystem.API
dotnet run --project src/InventoryOrderSystem.API
```

5. Open `https://localhost:7080/swagger` to explore the API.

### Running with Docker

```bash
cp .env.example .env   # then edit JWT_KEY / MYSQL_ROOT_PASSWORD if you like
docker-compose up --build
```

This starts MySQL and the API together. The API applies any pending EF Core
migrations automatically on startup (`Database.Migrate()` in `Program.cs`) -
**but this only works once the `InitialCreate` migration above has been
generated and committed**, since Docker can't generate migrations, only apply
ones that already exist in the image. Once that's done, `docker-compose up`
is genuinely one command for anyone cloning the repo.

The API is available at `http://localhost:8080` (Swagger at
`http://localhost:8080/swagger` since the container doesn't use HTTPS by
default). MySQL is exposed on the usual `3306` if you want to connect with a
GUI client.

### Health check

```
GET /api/health
```

### Logging

Structured logging via Serilog: console + rolling daily file (`logs/log-.txt`,
14-day retention). Every request is enriched with `TenantId`/`UserId` from the
JWT claims once authenticated, so log lines can be filtered per tenant without
services having to pass that context around manually. Key business events
(order created, received/fulfilled, cancelled, insufficient-stock rejections,
auth register/login) are logged explicitly at the service layer.

### Running tests

```bash
dotnet test tests/InventoryOrderSystem.Tests
```

Service-layer tests run against EF Core's InMemory provider with a fixed
tenant context, so tenant query filters and audit stamping behave the same
as they would against MySQL - no mocking of the DbContext required.

## Roadmap

- [x] Solution scaffolding, multi-tenant DbContext, JWT auth pipeline
- [x] Tenant registration & authentication endpoints
- [x] Product / category / supplier management
- [x] Stock tracking & purchase orders (Draft -> Received/Cancelled)
- [x] Sales orders & fulfillment workflow (Draft -> Fulfilled/Cancelled)
- [x] Unit tests for order state machines and stock validation
- [x] Reporting (stock valuation, low-stock alerts)
- [x] Structured logging (Serilog)
- [x] Pagination/filtering on high-volume endpoints
- [x] Frontend: auth, dashboard, and full CRUD for Categories, Products,
      Suppliers, Purchase Orders, Customers, Sales Orders
- [x] CI (GitHub Actions: build + test on every push)
- [x] Dockerfile + docker-compose for one-command local setup
- [x] Role-based authorization (Admin/Staff) and tenant user management

See [`frontend/README.md`](frontend/README.md) for how to run the frontend
against this API.
