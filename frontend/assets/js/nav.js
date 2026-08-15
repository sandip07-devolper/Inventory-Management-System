const NAV_LINKS = [
  { href: "dashboard.html", label: "Dashboard", key: "dashboard" },
  { href: "products.html", label: "Products", key: "products" }
];

function renderNavbar(activePage) {
  const container = document.getElementById("navbarContainer");
  if (!container) return;

  const linkHtml = NAV_LINKS.map(
    (link) => `
      <a class="nav-link ${link.key === activePage ? "active fw-semibold" : ""}" href="${link.href}">
        ${link.label}
      </a>`
  ).join("");

  container.innerHTML = `
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
      <div class="container-fluid">
        <a class="navbar-brand" href="dashboard.html">Inventory & Order Management</a>
        <div class="navbar-nav flex-row me-auto">${linkHtml}</div>
        <div class="d-flex align-items-center text-light">
          <span id="userGreeting" class="me-3 small"></span>
          <button id="logoutBtn" class="btn btn-outline-light btn-sm">Sign out</button>
        </div>
      </div>
    </nav>
  `;

  const user = AuthStorage.getUser();
  const greeting = document.getElementById("userGreeting");
  if (greeting && user) {
    greeting.textContent = `${user.fullName} · ${user.tenantName}`;
  }

  document.getElementById("logoutBtn")?.addEventListener("click", () => {
    AuthStorage.clear();
    window.location.href = "login.html";
  });
}
