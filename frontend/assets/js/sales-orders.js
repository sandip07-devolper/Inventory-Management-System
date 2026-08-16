const state = {
  pageNumber: 1,
  pageSize: 10,
  status: "",
  customers: [],
  products: []
};

let createOrderModal;
let viewOrderModal;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("sales-orders");
  createOrderModal = new bootstrap.Modal(document.getElementById("createOrderModal"));
  viewOrderModal = new bootstrap.Modal(document.getElementById("viewOrderModal"));

  wireFilters();
  wireCreateForm();

  document.getElementById("addOrderBtn").addEventListener("click", async () => {
    if (state.customers.length === 0 || state.products.length === 0) {
      await loadDropdownData();
    }
    resetCreateForm();
  });

  await Promise.all([loadDropdownData(), loadOrders()]);
});

async function loadDropdownData() {
  try {
    const [customers, productPage] = await Promise.all([
      Api.getCustomers(),
      Api.getProducts("?pageSize=100&isActive=true")
    ]);

    state.customers = customers;
    state.products = productPage.items;

    const customerSelect = document.getElementById("orderCustomerId");
    customerSelect.insertAdjacentHTML(
      "beforeend",
      customers.map((c) => `<option value="${c.id}">${escapeHtml(c.name)}</option>`).join("")
    );
  } catch (err) {
    showError(`Couldn't load customers/products: ${err.message}`);
  }
}

function wireFilters() {
  document.getElementById("statusFilter").addEventListener("change", (e) => {
    state.status = e.target.value;
    state.pageNumber = 1;
    loadOrders();
  });
}

async function loadOrders() {
  const tbody = document.getElementById("ordersTableBody");
  tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">Loading…</td></tr>`;
  document.getElementById("ordersError").classList.add("d-none");

  const params = new URLSearchParams();
  params.set("pageNumber", state.pageNumber);
  params.set("pageSize", state.pageSize);
  if (state.status) params.set("status", state.status);

  try {
    const result = await Api.getSalesOrders(`?${params.toString()}`);
    renderOrdersTable(result.items);
    renderPaginationControls("pagination", result, (page) => {
      state.pageNumber = page;
      loadOrders();
    });
    renderResultsSummaryText("resultsSummary", result);
  } catch (err) {
    showError(`Couldn't load sales orders: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderOrdersTable(orders) {
  const tbody = document.getElementById("ordersTableBody");

  if (!orders || orders.length === 0) {
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">No sales orders yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = orders
    .map(
      (o) => `
      <tr>
        <td><code>${escapeHtml(o.orderNumber)}</code></td>
        <td>${escapeHtml(o.customerName)}</td>
        <td>${statusBadge(o.status)}</td>
        <td>${formatDate(o.orderDate)}</td>
        <td class="text-end">${formatCurrency(o.totalAmount)}</td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-secondary" onclick="openViewModal(${o.id})">View</button>
        </td>
      </tr>`
    )
    .join("");
}

function statusBadge(status) {
  const colors = { Draft: "bg-warning text-dark", Fulfilled: "bg-success", Cancelled: "bg-secondary" };
  return `<span class="badge ${colors[status] || "bg-light text-dark"}">${status}</span>`;
}

// ---- Create form: dynamic line items ----

function wireCreateForm() {
  document.getElementById("addItemRowBtn").addEventListener("click", () => addItemRow());

  document.getElementById("createOrderForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const errorBox = document.getElementById("createOrderError");
    errorBox.classList.add("d-none");

    const items = collectItemRows();
    if (items.length === 0) {
      errorBox.textContent = "Add at least one line item.";
      errorBox.classList.remove("d-none");
      return;
    }

    const payload = {
      customerId: parseInt(document.getElementById("orderCustomerId").value, 10),
      notes: document.getElementById("orderNotes").value.trim() || null,
      items
    };

    try {
      await Api.createSalesOrder(payload);
      createOrderModal.hide();
      resetCreateForm();
      state.pageNumber = 1;
      await loadOrders();
    } catch (err) {
      errorBox.textContent = err.message;
      errorBox.classList.remove("d-none");
    }
  });
}

function resetCreateForm() {
  document.getElementById("createOrderForm").reset();
  document.getElementById("createOrderError").classList.add("d-none");
  document.getElementById("orderItemsBody").innerHTML = "";
  addItemRow();
  updateTotalPreview();
}

function addItemRow() {
  const tbody = document.getElementById("orderItemsBody");
  const row = document.createElement("tr");

  const productOptions = state.products
    .map(
      (p) =>
        `<option value="${p.id}" data-price="${p.unitPrice}">${escapeHtml(p.sku)} - ${escapeHtml(p.name)} (${p.quantityOnHand} on hand)</option>`
    )
    .join("");

  row.innerHTML = `
    <td>
      <select class="form-select form-select-sm item-product" required>
        <option value="" disabled selected>Choose a product…</option>
        ${productOptions}
      </select>
    </td>
    <td><input type="number" min="1" value="1" class="form-control form-control-sm item-qty" required /></td>
    <td><input type="number" min="0" step="0.01" value="0" class="form-control form-control-sm item-price" required /></td>
    <td><button type="button" class="btn btn-sm btn-outline-danger remove-item-row">&times;</button></td>
  `;

  const productSelect = row.querySelector(".item-product");
  const priceInput = row.querySelector(".item-price");
  const qtyInput = row.querySelector(".item-qty");

  productSelect.addEventListener("change", () => {
    const selected = productSelect.selectedOptions[0];
    if (selected?.dataset.price) {
      priceInput.value = selected.dataset.price;
    }
    updateTotalPreview();
  });

  priceInput.addEventListener("input", updateTotalPreview);
  qtyInput.addEventListener("input", updateTotalPreview);

  row.querySelector(".remove-item-row").addEventListener("click", () => {
    row.remove();
    updateTotalPreview();
  });

  tbody.appendChild(row);
}

function collectItemRows() {
  const rows = document.querySelectorAll("#orderItemsBody tr");
  const items = [];

  rows.forEach((row) => {
    const productId = row.querySelector(".item-product").value;
    const quantity = parseInt(row.querySelector(".item-qty").value, 10);
    const unitPrice = parseFloat(row.querySelector(".item-price").value);

    if (productId && quantity > 0 && unitPrice >= 0) {
      items.push({ productId: parseInt(productId, 10), quantity, unitPrice });
    }
  });

  return items;
}

function updateTotalPreview() {
  const total = collectItemRows().reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
  document.getElementById("orderTotalPreview").textContent = formatCurrency(total);
}

// ---- View / manage modal ----

async function openViewModal(id) {
  const bodyEl = document.getElementById("viewOrderBody");
  const actionsEl = document.getElementById("viewOrderActions");
  const errorBox = document.getElementById("viewOrderError");
  errorBox.classList.add("d-none");
  bodyEl.innerHTML = `<p class="text-muted">Loading…</p>`;
  actionsEl.innerHTML = "";

  viewOrderModal.show();

  try {
    const order = await Api.getSalesOrder(id);
    document.getElementById("viewOrderTitle").textContent = `Sales Order ${order.orderNumber}`;

    const itemsRows = order.items
      .map(
        (i) => `
        <tr>
          <td><code>${escapeHtml(i.sku)}</code></td>
          <td>${escapeHtml(i.productName)}</td>
          <td class="text-end">${i.quantity}</td>
          <td class="text-end">${formatCurrency(i.unitPrice)}</td>
          <td class="text-end">${formatCurrency(i.lineTotal)}</td>
        </tr>`
      )
      .join("");

    bodyEl.innerHTML = `
      <dl class="row small mb-3">
        <dt class="col-sm-3">Customer</dt><dd class="col-sm-9">${escapeHtml(order.customerName)}</dd>
        <dt class="col-sm-3">Status</dt><dd class="col-sm-9">${statusBadge(order.status)}</dd>
        <dt class="col-sm-3">Order date</dt><dd class="col-sm-9">${formatDate(order.orderDate)}</dd>
        ${order.fulfilledDate ? `<dt class="col-sm-3">Fulfilled</dt><dd class="col-sm-9">${formatDate(order.fulfilledDate)}</dd>` : ""}
        ${order.notes ? `<dt class="col-sm-3">Notes</dt><dd class="col-sm-9">${escapeHtml(order.notes)}</dd>` : ""}
      </dl>
      <table class="table table-sm">
        <thead><tr><th>SKU</th><th>Product</th><th class="text-end">Qty</th><th class="text-end">Unit Price</th><th class="text-end">Line Total</th></tr></thead>
        <tbody>${itemsRows}</tbody>
        <tfoot><tr><th colspan="4" class="text-end">Total</th><th class="text-end">${formatCurrency(order.totalAmount)}</th></tr></tfoot>
      </table>
    `;

    renderViewActions(order);
  } catch (err) {
    bodyEl.innerHTML = "";
    errorBox.textContent = err.message;
    errorBox.classList.remove("d-none");
  }
}

function renderViewActions(order) {
  const actionsEl = document.getElementById("viewOrderActions");
  actionsEl.innerHTML = "";

  if (order.status !== "Draft") {
    actionsEl.innerHTML = `<button class="btn btn-outline-secondary" data-bs-dismiss="modal">Close</button>`;
    return;
  }

  actionsEl.innerHTML = `
    <button class="btn btn-outline-danger" id="deleteOrderBtn">Delete</button>
    <button class="btn btn-outline-secondary" id="cancelOrderBtn">Cancel Order</button>
    <button class="btn btn-success" id="fulfillOrderBtn">Fulfill Order</button>
  `;

  document.getElementById("fulfillOrderBtn").addEventListener("click", () => runOrderAction(order.id, "fulfill"));
  document.getElementById("cancelOrderBtn").addEventListener("click", () => runOrderAction(order.id, "cancel"));
  document.getElementById("deleteOrderBtn").addEventListener("click", () => runOrderAction(order.id, "delete"));
}

async function runOrderAction(id, action) {
  const errorBox = document.getElementById("viewOrderError");
  errorBox.classList.add("d-none");

  const confirmations = {
    fulfill: "Fulfill this order? Product stock will be reduced immediately - this will fail if there isn't enough stock.",
    cancel: "Cancel this draft order?",
    delete: "Delete this draft order permanently?"
  };

  if (!confirm(confirmations[action])) return;

  try {
    if (action === "fulfill") await Api.fulfillSalesOrder(id);
    if (action === "cancel") await Api.cancelSalesOrder(id);
    if (action === "delete") await Api.deleteSalesOrder(id);

    viewOrderModal.hide();
    await loadOrders();
  } catch (err) {
    // Insufficient-stock rejections land here with the API's detailed
    // shortage message (e.g. "Widget (SKU ...): requested 10, only 3 in
    // stock") - shown as-is rather than a generic failure message.
    errorBox.textContent = err.message;
    errorBox.classList.remove("d-none");
  }
}

// ---- helpers ----

function showError(message) {
  const box = document.getElementById("ordersError");
  box.textContent = message;
  box.classList.remove("d-none");
}

function formatCurrency(value) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(value);
}

function formatDate(value) {
  return new Date(value).toLocaleDateString();
}

function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value ?? "";
  return div.innerHTML;
}
