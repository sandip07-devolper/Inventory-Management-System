let userModal;
let currentUserId;

document.addEventListener("DOMContentLoaded", async () => {
  if (!AuthStorage.isAuthenticated()) {
    window.location.href = "login.html";
    return;
  }

  renderNavbar("users");

  const user = AuthStorage.getUser();
  currentUserId = user?.userId;

  if (!user?.roles?.includes("Admin")) {
    document.getElementById("accessDenied").classList.remove("d-none");
    document.getElementById("usersCard").classList.add("d-none");
    document.getElementById("addUserBtn").classList.add("d-none");
    return;
  }

  userModal = new bootstrap.Modal(document.getElementById("userModal"));

  document.getElementById("addUserBtn").addEventListener("click", () => {
    resetUserForm();
    document.getElementById("userModalTitle").textContent = "Add User";
    setEditMode(false);
  });

  wireUserForm();
  await loadUsers();
});

function setEditMode(isEdit) {
  document.getElementById("userEmail").disabled = isEdit;
  document.getElementById("emailLockedHint").style.display = isEdit ? "block" : "none";
  document.getElementById("userPasswordContainer").style.display = isEdit ? "none" : "block";
  document.getElementById("userPassword").required = !isEdit;
  document.getElementById("userIsActiveContainer").style.display = isEdit ? "block" : "none";
}

function wireUserForm() {
  document.getElementById("userForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const errorBox = document.getElementById("userFormError");
    errorBox.classList.add("d-none");

    const id = document.getElementById("userId").value;

    try {
      if (id) {
        await Api.updateUser(id, {
          fullName: document.getElementById("userFullName").value.trim(),
          role: document.getElementById("userRole").value,
          isActive: document.getElementById("userIsActive").checked
        });
      } else {
        await Api.createUser({
          fullName: document.getElementById("userFullName").value.trim(),
          email: document.getElementById("userEmail").value.trim(),
          password: document.getElementById("userPassword").value,
          role: document.getElementById("userRole").value
        });
      }
      userModal.hide();
      await loadUsers();
    } catch (err) {
      errorBox.textContent = err.message;
      errorBox.classList.remove("d-none");
    }
  });
}

function resetUserForm() {
  document.getElementById("userForm").reset();
  document.getElementById("userId").value = "";
  document.getElementById("userFormError").classList.add("d-none");
}

async function loadUsers() {
  const tbody = document.getElementById("usersTableBody");
  document.getElementById("usersError").classList.add("d-none");

  try {
    const users = await Api.getUsers();
    renderUsersTable(users);
  } catch (err) {
    showError(`Couldn't load users: ${err.message}`);
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">Failed to load.</td></tr>`;
  }
}

function renderUsersTable(users) {
  const tbody = document.getElementById("usersTableBody");

  if (!users || users.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No users yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = users
    .map((u) => {
      const isSelf = String(u.id) === String(currentUserId);
      return `
      <tr>
        <td>${escapeHtml(u.fullName)}${isSelf ? ' <span class="badge bg-info text-dark">You</span>' : ""}</td>
        <td class="text-muted">${escapeHtml(u.email)}</td>
        <td>${u.roles.map((r) => `<span class="badge bg-primary me-1">${escapeHtml(r)}</span>`).join("")}</td>
        <td>
          <span class="badge ${u.isActive ? "bg-success" : "bg-secondary"}">
            ${u.isActive ? "Active" : "Inactive"}
          </span>
        </td>
        <td class="text-end">
          <button class="btn btn-sm btn-outline-primary me-1" onclick="openEditModal(${u.id})">Edit</button>
          <button
            class="btn btn-sm btn-outline-danger"
            onclick="deactivateUser(${u.id}, '${escapeJs(u.fullName)}')"
            ${isSelf ? "disabled title=\"You can't deactivate your own account\"" : ""}
          >
            Deactivate
          </button>
        </td>
      </tr>`;
    })
    .join("");
}

async function openEditModal(id) {
  try {
    const user = await apiRequest(`/users/${id}`);

    document.getElementById("userId").value = user.id;
    document.getElementById("userFullName").value = user.fullName;
    document.getElementById("userEmail").value = user.email;
    document.getElementById("userRole").value = user.roles[0] || "Staff";
    document.getElementById("userIsActive").checked = user.isActive;

    document.getElementById("userModalTitle").textContent = `Edit ${user.fullName}`;
    document.getElementById("userFormError").classList.add("d-none");
    setEditMode(true);

    userModal.show();
  } catch (err) {
    showError(`Couldn't load user: ${err.message}`);
  }
}

async function deactivateUser(id, name) {
  if (!confirm(`Deactivate "${name}"? They won't be able to sign in anymore.`)) {
    return;
  }

  try {
    await Api.deactivateUser(id);
    await loadUsers();
  } catch (err) {
    showError(`Couldn't deactivate user: ${err.message}`);
  }
}

function showError(message) {
  const box = document.getElementById("usersError");
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
