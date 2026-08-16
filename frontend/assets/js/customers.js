let customerModal;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("customers");
  customerModal = new bootstrap.Modal(document.getElementById("customerModal"));

  document.getElementById("addCustomerBtn").addEventListener("click", () => {
    resetCustomerForm();
    document.getElementById("customerModalTitle").textContent = "Add Customer";
    document.getElementById("customerIsActiveContainer").style.display = "none";
  });

  wireCustomerForm();
  await loadCustomers();
});

function wireCustomerForm() {
  document.getElementById("customerForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const errorBox = document.getElementById("customerFormError");
    errorBox.classList.add("d-none");

    const id = document.getElementById("customerId").value;
    const payload = {
      name: document.getElementById("customerName").value.trim(),
      email: document.getElementById("customerEmail").value.trim() || null,
      phone: document.getElementById("customerPhone").value.trim() || null,
      address: document.getElementById("customerAddress").value.trim() || null
    };

    if (id) {
      payload.isActive = document.getElementById("customerIsActive").checked;
    }

    try {
      if (id) {
        await Api.updateCustomer(id, payload);
      } else {
        await Api.createCustomer(payload);
      }
      customerModal.hide();
      await loadCustomers();
    } catch (err) {
      errorBox.textContent = err.message;
      errorBox.classList.remove("d-none");
    }
  });
}

function resetCustomerForm() {
  document.getElementById("customerForm").reset();
  document.getElementById("customerId").value = "";
  document.getElementById("customerFormError").classList.add("d-none");
}

async function loadCustomers() {
  const tbody = document.getElementById("customersTableBody");
  document.getElementById("customersError").classList.add("d-none");

  try {
    const customers = await Api.getCustomers();
    renderCustomersTable(customers);
  } catch (err) {
    showError(`Couldn't load customers: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderCustomersTable(customers) {
  const tbody = document.getElementById("customersTableBody");

  if (!customers || customers.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No customers yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = customers
    .map(
      (c) => `
      <tr>
        <td>${escapeHtml(c.name)}</td>
        <td class="text-muted">${escapeHtml(c.email || "—")}</td>
        <td class="text-muted">${escapeHtml(c.phone || "—")}</td>
        <td>
          <span class="badge ${c.isActive ? "bg-success" : "bg-secondary"}">
            ${c.isActive ? "Active" : "Inactive"}
          </span>
        </td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary me-1" onclick="openEditModal(${c.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteCustomer(${c.id}, '${escapeJs(c.name)}')">
            Delete
          </button>
        </td>
      </tr>`
    )
    .join("");
}

async function openEditModal(id) {
  try {
    const customer = await apiRequest(`/customers/${id}`);

    document.getElementById("customerId").value = customer.id;
    document.getElementById("customerName").value = customer.name;
    document.getElementById("customerEmail").value = customer.email || "";
    document.getElementById("customerPhone").value = customer.phone || "";
    document.getElementById("customerAddress").value = customer.address || "";
    document.getElementById("customerIsActive").checked = customer.isActive;

    document.getElementById("customerModalTitle").textContent = `Edit ${customer.name}`;
    document.getElementById("customerIsActiveContainer").style.display = "block";
    document.getElementById("customerFormError").classList.add("d-none");

    customerModal.show();
  } catch (err) {
    showError(`Couldn't load customer: ${err.message}`);
  }
}

async function deleteCustomer(id, name) {
  if (!confirm(`Deactivate "${name}"? Past sales orders keep referencing it.`)) {
    return;
  }

  try {
    await Api.deleteCustomer(id);
    await loadCustomers();
  } catch (err) {
    showError(`Couldn't delete customer: ${err.message}`);
  }
}

function showError(message) {
  const box = document.getElementById("customersError");
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
