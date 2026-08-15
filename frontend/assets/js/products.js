const state = {
  pageNumber: 1,
  pageSize: 10,
  search: "",
  categoryId: "",
  isActive: "",
  categories: []
};

let productModal;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("products");
  productModal = new bootstrap.Modal(document.getElementById("productModal"));

  wireFilters();
  wireProductForm();
  wireAddButton();

  await loadCategories();
  await loadProducts();
});

function wireFilters() {
  const searchInput = document.getElementById("searchInput");
  let debounceTimer;
  searchInput.addEventListener("input", () => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      state.search = searchInput.value.trim();
      state.pageNumber = 1;
      loadProducts();
    }, 350);
  });

  document.getElementById("categoryFilter").addEventListener("change", (e) => {
    state.categoryId = e.target.value;
    state.pageNumber = 1;
    loadProducts();
  });

  document.getElementById("statusFilter").addEventListener("change", (e) => {
    state.isActive = e.target.value;
    state.pageNumber = 1;
    loadProducts();
  });

  document.getElementById("clearFiltersBtn").addEventListener("click", () => {
    searchInput.value = "";
    document.getElementById("categoryFilter").value = "";
    document.getElementById("statusFilter").value = "";
    state.search = "";
    state.categoryId = "";
    state.isActive = "";
    state.pageNumber = 1;
    loadProducts();
  });
}

function wireAddButton() {
  document.getElementById("addProductBtn").addEventListener("click", () => {
    resetProductForm();
    document.getElementById("productModalTitle").textContent = "Add Product";
    document.getElementById("isActiveContainer").style.display = "none";
  });
}

function wireProductForm() {
  document.getElementById("productForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const errorBox = document.getElementById("productFormError");
    errorBox.classList.add("d-none");

    const id = document.getElementById("productId").value;
    const payload = {
      sku: document.getElementById("sku").value.trim(),
      name: document.getElementById("name").value.trim(),
      description: document.getElementById("description").value.trim() || null,
      unitPrice: parseFloat(document.getElementById("unitPrice").value),
      costPrice: parseFloat(document.getElementById("costPrice").value),
      reorderLevel: parseInt(document.getElementById("reorderLevel").value, 10),
      categoryId: parseInt(document.getElementById("categoryId").value, 10)
    };

    if (id) {
      payload.isActive = document.getElementById("isActive").checked;
    }

    try {
      if (id) {
        await Api.updateProduct(id, payload);
      } else {
        await Api.createProduct(payload);
      }
      productModal.hide();
      await loadProducts();
    } catch (err) {
      errorBox.textContent = err.message;
      errorBox.classList.remove("d-none");
    }
  });
}

function resetProductForm() {
  document.getElementById("productForm").reset();
  document.getElementById("productId").value = "";
  document.getElementById("productFormError").classList.add("d-none");
}

async function loadCategories() {
  try {
    state.categories = await Api.getCategories();

    const filterSelect = document.getElementById("categoryFilter");
    const modalSelect = document.getElementById("categoryId");

    const options = state.categories
      .map((c) => `<option value="${c.id}">${escapeHtml(c.name)}</option>`)
      .join("");

    filterSelect.insertAdjacentHTML("beforeend", options);
    modalSelect.insertAdjacentHTML("beforeend", options);
  } catch (err) {
    showListError(`Couldn't load categories: ${err.message}`);
  }
}

async function loadProducts() {
  const tbody = document.getElementById("productsTableBody");
  tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">Loading…</td></tr>`;
  document.getElementById("productsError").classList.add("d-none");

  const params = new URLSearchParams();
  params.set("pageNumber", state.pageNumber);
  params.set("pageSize", state.pageSize);
  if (state.search) params.set("search", state.search);
  if (state.categoryId) params.set("categoryId", state.categoryId);
  if (state.isActive !== "") params.set("isActive", state.isActive);

  try {
    const result = await Api.getProducts(`?${params.toString()}`);
    renderProductsTable(result.items);
    renderPagination(result);
    renderResultsSummary(result);
  } catch (err) {
    showListError(`Couldn't load products: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderProductsTable(items) {
  const tbody = document.getElementById("productsTableBody");

  if (!items || items.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">No products found.</td></tr>`;
    return;
  }

  tbody.innerHTML = items
    .map(
      (p) => `
      <tr>
        <td><code>${escapeHtml(p.sku)}</code></td>
        <td>${escapeHtml(p.name)}</td>
        <td>${escapeHtml(p.categoryName)}</td>
        <td class="text-end">${formatCurrency(p.unitPrice)}</td>
        <td class="text-end">${p.quantityOnHand}</td>
        <td>
          <span class="badge ${p.isActive ? "bg-success" : "bg-secondary"}">
            ${p.isActive ? "Active" : "Inactive"}
          </span>
        </td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary me-1" onclick="openEditModal(${p.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteProduct(${p.id}, '${escapeJs(p.name)}')">
            Delete
          </button>
        </td>
      </tr>`
    )
    .join("");
}

function renderPagination(result) {
  const pagination = document.getElementById("pagination");
  pagination.innerHTML = "";

  if (result.totalPages <= 1) return;

  const addPageItem = (label, page, disabled, active) => {
    const li = document.createElement("li");
    li.className = `page-item ${disabled ? "disabled" : ""} ${active ? "active" : ""}`;
    const a = document.createElement("a");
    a.className = "page-link";
    a.href = "#";
    a.textContent = label;
    a.addEventListener("click", (e) => {
      e.preventDefault();
      if (disabled || active) return;
      state.pageNumber = page;
      loadProducts();
    });
    li.appendChild(a);
    pagination.appendChild(li);
  };

  addPageItem("«", result.pageNumber - 1, result.pageNumber === 1, false);

  for (let page = 1; page <= result.totalPages; page++) {
    addPageItem(String(page), page, false, page === result.pageNumber);
  }

  addPageItem("»", result.pageNumber + 1, result.pageNumber === result.totalPages, false);
}

function renderResultsSummary(result) {
  const summary = document.getElementById("resultsSummary");
  if (result.totalCount === 0) {
    summary.textContent = "No results";
    return;
  }
  const start = (result.pageNumber - 1) * result.pageSize + 1;
  const end = Math.min(result.pageNumber * result.pageSize, result.totalCount);
  summary.textContent = `Showing ${start}–${end} of ${result.totalCount}`;
}

async function openEditModal(id) {
  try {
    const product = await apiRequest(`/products/${id}`);

    document.getElementById("productId").value = product.id;
    document.getElementById("sku").value = product.sku;
    document.getElementById("name").value = product.name;
    document.getElementById("description").value = product.description || "";
    document.getElementById("unitPrice").value = product.unitPrice;
    document.getElementById("costPrice").value = product.costPrice;
    document.getElementById("reorderLevel").value = product.reorderLevel;
    document.getElementById("categoryId").value = product.categoryId;
    document.getElementById("isActive").checked = product.isActive;

    document.getElementById("productModalTitle").textContent = `Edit ${product.name}`;
    document.getElementById("isActiveContainer").style.display = "block";
    document.getElementById("productFormError").classList.add("d-none");

    productModal.show();
  } catch (err) {
    showListError(`Couldn't load product: ${err.message}`);
  }
}

async function deleteProduct(id, name) {
  if (!confirm(`Deactivate "${name}"? It will no longer appear in active product lists.`)) {
    return;
  }

  try {
    await Api.deleteProduct(id);
    await loadProducts();
  } catch (err) {
    showListError(`Couldn't delete product: ${err.message}`);
  }
}

function showListError(message) {
  const box = document.getElementById("productsError");
  box.textContent = message;
  box.classList.remove("d-none");
}

function formatCurrency(value) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(value);
}

function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value ?? "";
  return div.innerHTML;
}

function escapeJs(value) {
  return (value ?? "").replace(/'/g, "\\'");
}
