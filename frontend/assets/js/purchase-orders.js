const state = {
  pageNumber: 1,
  pageSize: 10,
  status: "",
  suppliers: [],
  products: []
};

let createOrderModal;
let viewOrderModal;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("purchase-orders");
  createOrderModal = new bootstrap.Modal(document.getElementById("createOrderModal"));
  viewOrderModal = new bootstrap.Modal(document.getElementById("viewOrderModal"));

  wireFilters();
  wireCreateForm();

  document.getElementById("addOrderBtn").addEventListener("click", async () => {
    if (state.suppliers.length === 0 || state.products.length === 0) {
      await loadDropdownData();
    }
    resetCreateForm();
  });

  await Promise.all([loadDropdownData(), loadOrders()]);
});

async function loadDropdownData() {
  try {
    const [suppliers, productPage] = await Promise.all([
      Api.getSuppliers(),
      Api.getProducts("?pageSize=100&isActive=true")
    ]);

    state.suppliers = suppliers;
    state.products = productPage.items;

    const supplierSelect = document.getElementById("orderSupplierId");
    supplierSelect.insertAdjacentHTML(
      "beforeend",
      suppliers.map((s) => `<option value="${s.id}">${escapeHtml(s.name)}</option>`).join("")
    );
  } catch (err) {
    showError(`Couldn't load suppliers/products: ${err.message}`);
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
    const result = await Api.getPurchaseOrders(`?${params.toString()}`);
    renderOrdersTable(result.items);
    renderPaginationControls("pagination", result, (page) => {
      state.pageNumber = page;
      loadOrders();
    });
    renderResultsSummaryText("resultsSummary", result);
  } catch (err) {
    showError(`Couldn't load purchase orders: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderOrdersTable(orders) {
  const tbody = document.getElementById("ordersTableBody");

  if (!orders || orders.length === 0) {
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">No purchase orders yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = orders
    .map(
      (o) => `
      <tr>
        <td><code>${escapeHtml(o.orderNumber)}</code></td>
        <td>${escapeHtml(o.supplierName)}</td>
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
  const colors = { Draft: "bg-warning text-dark", Received: "bg-success", Cancelled: "bg-secondary" };
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
      supplierId: parseInt(document.getElementById("orderSupplierId").value, 10),
      notes: document.getElementById("orderNotes").value.trim() || null,
      items
    };

    try {
      await Api.createPurchaseOrder(payload);
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
    .map((p) => `<option value="${p.id}" data-cost="${p.costPrice}">${escapeHtml(p.sku)} - ${escapeHtml(p.name)}</option>`)
    .join("");

  row.innerHTML = `
    <td>
      <select class="form-select form-select-sm item-product" required>
        <option value="" disabled selected>Choose a product…</option>
        ${productOptions}
      </select>
    </td>
    <td><input type="number" min="1" value="1" class="form-control form-control-sm item-qty" required /></td>
    <td><input type="number" min="0" step="0.01" value="0" class="form-control form-control-sm item-cost" required /></td>
    <td><button type="button" class="btn btn-sm btn-outline-danger remove-item-row">&times;</button></td>
  `;

  const productSelect = row.querySelector(".item-product");
  const costInput = row.querySelector(".item-cost");
  const qtyInput = row.querySelector(".item-qty");

  productSelect.addEventListener("change", () => {
    const selected = productSelect.selectedOptions[0];
    if (selected?.dataset.cost) {
      costInput.value = selected.dataset.cost;
    }
    updateTotalPreview();
  });

  costInput.addEventListener("input", updateTotalPreview);
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
    const unitCost = parseFloat(row.querySelector(".item-cost").value);

    if (productId && quantity > 0 && unitCost >= 0) {
      items.push({ productId: parseInt(productId, 10), quantity, unitCost });
    }
  });

  return items;
}

function updateTotalPreview() {
  const total = collectItemRows().reduce((sum, item) => sum + item.quantity * item.unitCost, 0);
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
    const order = await Api.getPurchaseOrder(id);
    document.getElementById("viewOrderTitle").textContent = `Purchase Order ${order.orderNumber}`;

    const itemsRows = order.items
      .map(
        (i) => `
        <tr>
          <td><code>${escapeHtml(i.sku)}</code></td>
          <td>${escapeHtml(i.productName)}</td>
          <td class="text-end">${i.quantity}</td>
          <td class="text-end">${formatCurrency(i.unitCost)}</td>
          <td class="text-end">${formatCurrency(i.lineTotal)}</td>
        </tr>`
      )
      .join("");

    bodyEl.innerHTML = `
      <dl class="row small mb-3">
        <dt class="col-sm-3">Supplier</dt><dd class="col-sm-9">${escapeHtml(order.supplierName)}</dd>
        <dt class="col-sm-3">Status</dt><dd class="col-sm-9">${statusBadge(order.status)}</dd>
        <dt class="col-sm-3">Order date</dt><dd class="col-sm-9">${formatDate(order.orderDate)}</dd>
        ${order.receivedDate ? `<dt class="col-sm-3">Received</dt><dd class="col-sm-9">${formatDate(order.receivedDate)}</dd>` : ""}
        ${order.notes ? `<dt class="col-sm-3">Notes</dt><dd class="col-sm-9">${escapeHtml(order.notes)}</dd>` : ""}
      </dl>
      <table class="table table-sm">
        <thead><tr><th>SKU</th><th>Product</th><th class="text-end">Qty</th><th class="text-end">Unit Cost</th><th class="text-end">Line Total</th></tr></thead>
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
    <button class="btn btn-success" id="receiveOrderBtn">Receive Stock</button>
  `;

  document.getElementById("receiveOrderBtn").addEventListener("click", () => runOrderAction(order.id, "receive"));
  document.getElementById("cancelOrderBtn").addEventListener("click", () => runOrderAction(order.id, "cancel"));
  document.getElementById("deleteOrderBtn").addEventListener("click", () => runOrderAction(order.id, "delete"));
}

async function runOrderAction(id, action) {
  const errorBox = document.getElementById("viewOrderError");
  errorBox.classList.add("d-none");

  const confirmations = {
    receive: "Mark this order as received? Product stock will be increased immediately.",
    cancel: "Cancel this draft order?",
    delete: "Delete this draft order permanently?"
  };

  if (!confirm(confirmations[action])) return;

  try {
    if (action === "receive") await Api.receivePurchaseOrder(id);
    if (action === "cancel") await Api.cancelPurchaseOrder(id);
    if (action === "delete") await Api.deletePurchaseOrder(id);

    viewOrderModal.hide();
    await loadOrders();
  } catch (err) {
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
