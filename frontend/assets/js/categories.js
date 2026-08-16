let categoryModal;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("categories");
  categoryModal = new bootstrap.Modal(document.getElementById("categoryModal"));

  document.getElementById("addCategoryBtn").addEventListener("click", () => {
    resetCategoryForm();
    document.getElementById("categoryModalTitle").textContent = "Add Category";
    document.getElementById("categoryIsActiveContainer").style.display = "none";
  });

  wireCategoryForm();
  await loadCategories();
});

function wireCategoryForm() {
  document.getElementById("categoryForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const errorBox = document.getElementById("categoryFormError");
    errorBox.classList.add("d-none");

    const id = document.getElementById("categoryId").value;
    const payload = {
      name: document.getElementById("categoryName").value.trim(),
      description: document.getElementById("categoryDescription").value.trim() || null
    };

    if (id) {
      payload.isActive = document.getElementById("categoryIsActive").checked;
    }

    try {
      if (id) {
        await Api.updateCategory(id, payload);
      } else {
        await Api.createCategory(payload);
      }
      categoryModal.hide();
      await loadCategories();
    } catch (err) {
      errorBox.textContent = err.message;
      errorBox.classList.remove("d-none");
    }
  });
}

function resetCategoryForm() {
  document.getElementById("categoryForm").reset();
  document.getElementById("categoryId").value = "";
  document.getElementById("categoryFormError").classList.add("d-none");
}

async function loadCategories() {
  const tbody = document.getElementById("categoriesTableBody");
  document.getElementById("categoriesError").classList.add("d-none");

  try {
    const categories = await Api.getCategories();
    renderCategoriesTable(categories);
  } catch (err) {
    showError(`Couldn't load categories: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderCategoriesTable(categories) {
  const tbody = document.getElementById("categoriesTableBody");

  if (!categories || categories.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No categories yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = categories
    .map(
      (c) => `
      <tr>
        <td>${escapeHtml(c.name)}</td>
        <td class="text-muted">${escapeHtml(c.description || "—")}</td>
        <td class="text-end">${c.productCount}</td>
        <td>
          <span class="badge ${c.isActive ? "bg-success" : "bg-secondary"}">
            ${c.isActive ? "Active" : "Inactive"}
          </span>
        </td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary me-1" onclick="openEditModal(${c.id})">Edit</button>
          <button class="btn btn-sm btn-outline-danger" onclick="deleteCategory(${c.id}, '${escapeJs(c.name)}')">
            Delete
          </button>
        </td>
      </tr>`
    )
    .join("");
}

async function openEditModal(id) {
  try {
    const category = await apiRequest(`/categories/${id}`);

    document.getElementById("categoryId").value = category.id;
    document.getElementById("categoryName").value = category.name;
    document.getElementById("categoryDescription").value = category.description || "";
    document.getElementById("categoryIsActive").checked = category.isActive;

    document.getElementById("categoryModalTitle").textContent = `Edit ${category.name}`;
    document.getElementById("categoryIsActiveContainer").style.display = "block";
    document.getElementById("categoryFormError").classList.add("d-none");

    categoryModal.show();
  } catch (err) {
    showError(`Couldn't load category: ${err.message}`);
  }
}

async function deleteCategory(id, name) {
  if (!confirm(`Deactivate "${name}"? Existing products keep their category, but it won't be offered for new ones.`)) {
    return;
  }

  try {
    await Api.deleteCategory(id);
    await loadCategories();
  } catch (err) {
    showError(`Couldn't delete category: ${err.message}`);
  }
}

function showError(message) {
  const box = document.getElementById("categoriesError");
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
