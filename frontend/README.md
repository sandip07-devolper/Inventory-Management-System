# Frontend

Static HTML/CSS/JS (Bootstrap 5, vanilla JS, no build step) that consumes the
API directly via `fetch`. JWTs are kept in `sessionStorage` - cleared when the
browser tab closes.

## Pages

- `login.html` / `register.html` - authentication
- `dashboard.html` - KPI cards (low-stock count, units on hand, inventory
  cost/retail value) and a low-stock table, both from `/api/reports/*`
- `products.html` - searchable/filterable/paginated product list with a
  create/edit modal; deactivate (soft-delete) with a confirm prompt
- `categories.html` - simple list with a create/edit modal (small
  reference table, no pagination needed)
- `suppliers.html` - same pattern as categories, for supplier records
- `purchase-orders.html` - status-filtered, paginated order list; a create
  form with dynamic add/remove line-item rows (selecting a product
  pre-fills its cost, editable); a view modal showing full order detail
  plus Receive/Cancel/Delete actions when the order is still Draft
- `customers.html` - same pattern as suppliers, for customer records
- `sales-orders.html` - mirrors purchase-orders.html, but Fulfill instead
  of Receive. Fulfilling reduces stock and will fail (with the API's
  exact shortage message) if there isn't enough on hand - that response
  is surfaced in the view modal as-is
- `users.html` - Admin-only staff management (the nav link itself is
  hidden for non-Admins via `roles` on the stored session, and the page
  also checks on load as defense-in-depth, since the API enforces this
  regardless of what the frontend shows). Create sets a password; edit
  changes name/role/active status but not email or password.

## Running it

This is plain static files - serve them with anything. A couple of options:

```bash
# Python (built-in, no install)
cd frontend
python3 -m http.server 5500

# or, with Node
npx serve frontend -l 5500
```

Then open `http://localhost:5500`.

## Connecting to the API

1. Run the API (`dotnet run --project src/InventoryOrderSystem.API`) and note
   the HTTPS port from `Properties/launchSettings.json` (default `7080`).
2. Update `assets/js/config.js` if your port differs from the default.
3. The API's CORS policy (`appsettings.json` -> `Cors:AllowedOrigins`) only
   allows a handful of common local dev origins/ports out of the box
   (`5500`, `3000`, `8080`). Add yours there if you serve on a different port.

## Architecture notes

- `assets/js/api-client.js` is the single place that knows about the API base
  URL, attaches the bearer token, and normalizes error messages from the
  API's `problem+json` responses. Every page-specific script (`auth.js`,
  `dashboard.js`, `products.js`) goes through it rather than calling `fetch`
  directly.
- `assets/js/nav.js` renders the shared navbar into a `#navbarContainer`
  placeholder on each authenticated page, so adding a new page means adding
  one entry to `NAV_LINKS` rather than editing markup in every HTML file.
- `assets/js/pagination.js` renders numbered pagination controls and the
  "Showing X-Y of Z" summary text from any `PagedResult` response - shared
  by `products.js`, `purchase-orders.js`, and `sales-orders.js` rather
  than duplicated.
- No frontend framework/build step by design - this stays a thin client over
  a documented REST API, which is also exactly what the Swagger UI already
  demonstrates for API-only testing.
