document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  const user = AuthStorage.getUser();
  const greeting = document.getElementById("userGreeting");
  if (greeting && user) {
    greeting.textContent = `${user.fullName} · ${user.tenantName}`;
  }

  const logoutBtn = document.getElementById("logoutBtn");
  if (logoutBtn) {
    logoutBtn.addEventListener("click", () => {
      AuthStorage.clear();
      window.location.href = "login.html";
    });
  }

  await loadDashboardData();
});

async function loadDashboardData() {
  const errorBox = document.getElementById("dashboardError");
  errorBox?.classList.add("d-none");

  try {
    const [lowStock, valuation] = await Promise.all([
      Api.getLowStockReport(),
      Api.getInventoryValuation()
    ]);

    setText("lowStockCount", lowStock.totalItemsBelowReorder);
    setText("inventoryCostValue", formatCurrency(valuation.totalCostValue));
    setText("inventoryRetailValue", formatCurrency(valuation.totalRetailValue));
    setText("unitsOnHand", valuation.totalUnitsOnHand);

    renderLowStockTable(lowStock.items);
  } catch (err) {
    if (errorBox) {
      errorBox.textContent = `Couldn't load dashboard data: ${err.message}`;
      errorBox.classList.remove("d-none");
    }
  }
}

function setText(elementId, value) {
  const el = document.getElementById(elementId);
  if (el) el.textContent = value;
}

function formatCurrency(value) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(value);
}

function renderLowStockTable(items) {
  const tbody = document.getElementById("lowStockTableBody");
  if (!tbody) return;

  if (!items || items.length === 0) {
    tbody.innerHTML = `
      <tr><td colspan="4" class="text-center text-muted py-4">
        No products are below their reorder level.
      </td></tr>`;
    return;
  }

  tbody.innerHTML = items
    .map(
      (item) => `
      <tr>
        <td><code>${escapeHtml(item.sku)}</code></td>
        <td>${escapeHtml(item.productName)}</td>
        <td>${escapeHtml(item.categoryName)}</td>
        <td>${item.quantityOnHand} / ${item.reorderLevel}
          <span class="badge bg-danger ms-1">short ${item.shortageQuantity}</span>
        </td>
      </tr>`
    )
    .join("");
}

function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value ?? "";
  return div.innerHTML;
}
