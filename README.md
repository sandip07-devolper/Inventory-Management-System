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

## Getting Started

### Prerequisites

- .NET 8 SDK
- MySQL 8+

### Setup

1. Create a MySQL database and update `ConnectionStrings:DefaultConnection` in
   `src/InventoryOrderSystem.API/appsettings.json` (or use `dotnet user-secrets`).
2. Set a real `Jwt:Key` (32+ char random secret) in the same file / user-secrets.
3. Restore and run:

```bash
dotnet restore
dotnet ef database update --project src/InventoryOrderSystem.Infrastructure --startup-project src/InventoryOrderSystem.API
dotnet run --project src/InventoryOrderSystem.API
```

4. Open `https://localhost:7080/swagger` to explore the API.

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

See [`frontend/README.md`](frontend/README.md) for how to run the frontend
against this API.

