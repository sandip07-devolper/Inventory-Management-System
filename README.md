# Inventory & Order Management System

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

## Roadmap

- [x] Solution scaffolding, multi-tenant DbContext, JWT auth pipeline
- [ ] Tenant registration & authentication endpoints
- [ ] Product / category / supplier management
- [ ] Stock tracking & purchase orders
- [ ] Sales orders & fulfillment workflow
- [ ] Reporting (stock valuation, low-stock alerts)
- [ ] Unit & integration tests
- [ ] Frontend (Bootstrap + JS)
