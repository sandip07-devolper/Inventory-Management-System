const API_BASE_URL = window.APP_CONFIG?.apiBaseUrl || "https://localhost:7080/api";

const TOKEN_KEY = "ioms_token";
const USER_KEY = "ioms_user";

const AuthStorage = {
  getToken: () => sessionStorage.getItem(TOKEN_KEY),

  setSession(token, user) {
    sessionStorage.setItem(TOKEN_KEY, token);
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  getUser() {
    const raw = sessionStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  },

  clear() {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
  },

  isAuthenticated: () => !!sessionStorage.getItem(TOKEN_KEY)
};

/**
 * Core fetch wrapper. Attaches the bearer token for authenticated calls,
 * redirects to login on 401 (expired/invalid session), and surfaces the
 * API's problem+json "detail" (or validation "errors") as a plain message.
 */
async function apiRequest(path, { method = "GET", body, auth = true } = {}) {
  const headers = { "Content-Type": "application/json" };

  if (auth) {
    const token = AuthStorage.getToken();
    if (!token) {
      window.location.href = "login.html";
      throw new Error("Not authenticated");
    }
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined
  });

  if (response.status === 401) {
    AuthStorage.clear();
    window.location.href = "login.html";
    throw new Error("Session expired. Please sign in again.");
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get("content-type") || "";
  const data = contentType.includes("application/json") ? await response.json() : null;

  if (!response.ok) {
    const message =
      data?.errors?.join?.(" ") ||
      data?.detail ||
      data?.message ||
      data?.title ||
      `Request failed (${response.status}).`;
    throw new Error(message);
  }

  return data;
}

const Api = {
  register: (payload) => apiRequest("/auth/register", { method: "POST", body: payload, auth: false }),
  login: (payload) => apiRequest("/auth/login", { method: "POST", body: payload, auth: false }),

  getLowStockReport: () => apiRequest("/reports/low-stock"),
  getInventoryValuation: () => apiRequest("/reports/inventory-valuation"),

  getCategories: () => apiRequest("/categories"),
  createCategory: (payload) => apiRequest("/categories", { method: "POST", body: payload }),
  updateCategory: (id, payload) => apiRequest(`/categories/${id}`, { method: "PUT", body: payload }),
  deleteCategory: (id) => apiRequest(`/categories/${id}`, { method: "DELETE" }),

  getProducts: (query = "") => apiRequest(`/products${query}`),
  createProduct: (payload) => apiRequest("/products", { method: "POST", body: payload }),
  updateProduct: (id, payload) => apiRequest(`/products/${id}`, { method: "PUT", body: payload }),
  deleteProduct: (id) => apiRequest(`/products/${id}`, { method: "DELETE" }),

  getPurchaseOrders: (query = "") => apiRequest(`/purchase-orders${query}`),
  getPurchaseOrder: (id) => apiRequest(`/purchase-orders/${id}`),
  createPurchaseOrder: (payload) => apiRequest("/purchase-orders", { method: "POST", body: payload }),
  receivePurchaseOrder: (id) => apiRequest(`/purchase-orders/${id}/receive`, { method: "POST" }),
  cancelPurchaseOrder: (id) => apiRequest(`/purchase-orders/${id}/cancel`, { method: "POST" }),
  deletePurchaseOrder: (id) => apiRequest(`/purchase-orders/${id}`, { method: "DELETE" }),

  getSuppliers: () => apiRequest("/suppliers"),
  createSupplier: (payload) => apiRequest("/suppliers", { method: "POST", body: payload }),
  updateSupplier: (id, payload) => apiRequest(`/suppliers/${id}`, { method: "PUT", body: payload }),
  deleteSupplier: (id) => apiRequest(`/suppliers/${id}`, { method: "DELETE" }),

  getSalesOrders: (query = "") => apiRequest(`/sales-orders${query}`),
  getSalesOrder: (id) => apiRequest(`/sales-orders/${id}`),
  createSalesOrder: (payload) => apiRequest("/sales-orders", { method: "POST", body: payload }),
  fulfillSalesOrder: (id) => apiRequest(`/sales-orders/${id}/fulfill`, { method: "POST" }),
  cancelSalesOrder: (id) => apiRequest(`/sales-orders/${id}/cancel`, { method: "POST" }),
  deleteSalesOrder: (id) => apiRequest(`/sales-orders/${id}`, { method: "DELETE" }),

  getCustomers: () => apiRequest("/customers"),
  createCustomer: (payload) => apiRequest("/customers", { method: "POST", body: payload }),
  updateCustomer: (id, payload) => apiRequest(`/customers/${id}`, { method: "PUT", body: payload }),
  deleteCustomer: (id) => apiRequest(`/customers/${id}`, { method: "DELETE" }),

  getUsers: () => apiRequest("/users"),
  createUser: (payload) => apiRequest("/users", { method: "POST", body: payload }),
  updateUser: (id, payload) => apiRequest(`/users/${id}`, { method: "PUT", body: payload }),
  deactivateUser: (id) => apiRequest(`/users/${id}`, { method: "DELETE" })
};
