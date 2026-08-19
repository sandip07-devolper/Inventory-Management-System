# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached across builds as long
# as dependencies haven't changed, even if source files have.
COPY src/InventoryOrderSystem.Domain/InventoryOrderSystem.Domain.csproj src/InventoryOrderSystem.Domain/
COPY src/InventoryOrderSystem.Infrastructure/InventoryOrderSystem.Infrastructure.csproj src/InventoryOrderSystem.Infrastructure/
COPY src/InventoryOrderSystem.API/InventoryOrderSystem.API.csproj src/InventoryOrderSystem.API/
RUN dotnet restore src/InventoryOrderSystem.API/InventoryOrderSystem.API.csproj

COPY src/ src/
RUN dotnet publish src/InventoryOrderSystem.API/InventoryOrderSystem.API.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8's ASP.NET runtime image already listens on 8080 by default (ASPNETCORE_HTTP_PORTS=8080).
EXPOSE 8080
ENTRYPOINT ["dotnet", "InventoryOrderSystem.API.dll"]
