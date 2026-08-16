let supplierModal;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("suppliers");
  supplierModal = new bootstrap.Modal(document.getElementById("supplierModal"));

  document.getElementById("addSupplierBtn").addEventListener("click", () => {
    resetSupplierForm();
    document.getElementById("supplierModalTitle").textContent = "Add Supplier";
    document.getElementById("supplierIsActiveContainer").style.display = "none";
  });

  wireSupplierForm();
  await loadSuppliers();
});

function wireSupplierForm() {
  document.getElementById("supplierForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const errorBox = document.getElementById("supplierFormError");
    errorBox.classList.add("d-none");

    const id = document.getElementById("supplierId").value;
    const payload = {
      name: document.getElementById("supplierName").value.trim(),
      contactPerson: document.getElementById("contactPerson").value.trim() || null,
      email: document.getElementById("supplierEmail").value.trim() || null,
      phone: document.getElementById("supplierPhone").value.trim() || null,
      address: document.getElementById("supplierAddress").value.trim() || null
    };

    if (id) {
      payload.isActive = document.getElementById("supplierIsActive").checked;
    }

    try {
      if (id) {
        await Api.updateSupplier(id, payload);
      } else {
        await Api.createSupplier(payload);
      }
      supplierModal.hide();
      await loadSuppliers();
    } catch (err) {
      errorBox.textContent = err.message;
      errorBox.classList.remove("d-none");
    }
  });
}

function resetSupplierForm() {
  document.getElementById("supplierForm").reset();
  document.getElementById("supplierId").value = "";
  document.getElementById("supplierFormError").classList.add("d-none");
}

async function loadSuppliers() {
  const tbody = document.getElementById("suppliersTableBody");
  document.getElementById("suppliersError").classList.add("d-none");

  try {
    const suppliers = await Api.getSuppliers();
    renderSuppliersTable(suppliers);
  } catch (err) {
    showError(`Couldn't load suppliers: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderSuppliersTable(suppliers) {
  const tbody = document.getElementById("suppliersTableBody");

  if (!suppliers || suppliers.length === 0) {
    tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-4">No suppliers yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = suppliers
    .map(
      (s) => `
      <tr>
        <td>${escapeHtml(s.name)}</td>
        <td class="text-muted">${escapeHtml(s.contactPerson || "—")}</td>
        <td class="text-muted">${escapeHtml(s.email || "—")}</td>
        <td class="text-muted">${escapeHtml(s.phone || "—")}</td>
        <td>
          <span class="badge ${s.isActive ? "bg-success" : "bg-secondary"}">
            ${s.isActive ? "Active" : "Inactive"}
          </span>
        </td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary me-1" onclick="openEditModal(${s.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteSupplier(${s.id}, '${escapeJs(s.name)}')">
            Delete
          </button>
        </td>
      </tr>`
    )
    .join("");
}

async function openEditModal(id) {
  try {
    const supplier = await apiRequest(`/suppliers/${id}`);

    document.getElementById("supplierId").value = supplier.id;
    document.getElementById("supplierName").value = supplier.name;
    document.getElementById("contactPerson").value = supplier.contactPerson || "";
    document.getElementById("supplierEmail").value = supplier.email || "";
    document.getElementById("supplierPhone").value = supplier.phone || "";
    document.getElementById("supplierAddress").value = supplier.address || "";
    document.getElementById("supplierIsActive").checked = supplier.isActive;

    document.getElementById("supplierModalTitle").textContent = `Edit ${supplier.name}`;
    document.getElementById("supplierIsActiveContainer").style.display = "block";
    document.getElementById("supplierFormError").classList.add("d-none");

    supplierModal.show();
  } catch (err) {
    showError(`Couldn't load supplier: ${err.message}`);
  }
}

async function deleteSupplier(id, name) {
  if (!confirm(`Deactivate "${name}"? Past purchase orders keep referencing it.`)) {
    return;
  }

  try {
    await Api.deleteSupplier(id);
    await loadSuppliers();
  } catch (err) {
    showError(`Couldn't delete supplier: ${err.message}`);
  }
}

function showError(message) {
  const box = document.getElementById("suppliersError");
  box.textContent = message;
  box.classList.remove("d-none");
}

function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value ?? "";
  return div.innerHTML;
}

function escapeJs(value) {
  return (value ?? "").replace(/'/g, "\\'");
}
