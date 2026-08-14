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

  getProducts: (query = "") => apiRequest(`/products${query}`),
  getPurchaseOrders: (query = "") => apiRequest(`/purchase-orders${query}`),
  getSalesOrders: (query = "") => apiRequest(`/sales-orders${query}`)
};
