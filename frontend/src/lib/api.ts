import {
  AuthResponse,
  Category,
  CreateOrderRequest,
  DashboardSummary,
  DIYCatalog,
  DIYQuoteRequest,
  DIYQuoteResponse,
  Ingredient,
  OrderResponse,
  Product,
  ProductDetail,
  Promotion,
  PurchaseReceipt,
  ShipperDashboard,
  StockAudit,
  StockBatch,
  Topping,
  User,
} from "@/types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  const token = typeof window !== "undefined" ? localStorage.getItem("milky_token") : null;

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  try {
    const res = await fetch(url, { ...options, headers });
    const data = await res.json();

    if (!res.ok) {
      throw new Error(data.message || `Request failed with status ${res.status}`);
    }

    return data.data !== undefined ? data.data : data;
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : "Đã xảy ra lỗi kết nối với máy chủ.";
    throw new Error(message);
  }
}

export const api = {
  // Auth
  sendOtp: (email: string) =>
    request<{ email: string; code: string; message: string; sentViaSmtp: boolean }>("/auth/send-otp", { method: "POST", body: JSON.stringify({ email }) }),

  verifyOtp: (email: string, code: string) =>
    request<{ message: string }>("/auth/verify-otp", { method: "POST", body: JSON.stringify({ email, code }) }),

  register: (payload: { fullName: string; email: string; password: string; verificationCode?: string; phoneNumber?: string; address?: string }) =>
    request<AuthResponse>("/auth/register", { method: "POST", body: JSON.stringify(payload) }),

  login: (payload: { email: string; password: string }) =>
    request<AuthResponse>("/auth/login", { method: "POST", body: JSON.stringify(payload) }),

  getProfile: () => request<User>("/auth/me"),

  updateProfile: (payload: { fullName: string; phoneNumber: string; address: string; password?: string }) =>
    request<User>("/auth/profile", { method: "PUT", body: JSON.stringify(payload) }),

  // Products & Categories
  getCategories: () => request<Category[]>("/categories"),

  getProducts: (params?: { categoryId?: number; search?: string; minPrice?: number; maxPrice?: number; sortBy?: string; productType?: string }) => {
    const q = new URLSearchParams();
    if (params?.categoryId) q.set("categoryId", params.categoryId.toString());
    if (params?.search) q.set("search", params.search);
    if (params?.minPrice) q.set("minPrice", params.minPrice.toString());
    if (params?.maxPrice) q.set("maxPrice", params.maxPrice.toString());
    if (params?.sortBy) q.set("sortBy", params.sortBy);
    if (params?.productType) q.set("productType", params.productType);
    return request<Product[]>(`/products?${q.toString()}`);
  },

  getProductBySlug: (slug: string) => request<ProductDetail>(`/products/slug/${slug}`),
  getProductById: (id: number) => request<ProductDetail>(`/products/${id}`),
  getToppings: () => request<Topping[]>("/products/toppings"),

  createProduct: (payload: Partial<Product>) => request<Product>("/products", { method: "POST", body: JSON.stringify(payload) }),
  updateProduct: (id: number, payload: Partial<Product>) => request<Product>(`/products/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  deleteProduct: (id: number) => request<boolean>(`/products/${id}`, { method: "DELETE" }),

  // DIY Studio
  getDIYCatalog: () => request<DIYCatalog>("/drinks/options"),
  quoteDIYDrink: (payload: DIYQuoteRequest) =>
    request<DIYQuoteResponse>("/drinks/quote", { method: "POST", body: JSON.stringify(payload) }),

  // Orders & Checkout
  createOrder: (payload: CreateOrderRequest) =>
    request<OrderResponse>("/orders", { method: "POST", body: JSON.stringify(payload) }),

  getOrders: () => request<OrderResponse[]>("/orders"),
  getOrderById: (id: number) => request<OrderResponse>(`/orders/${id}`),

  updateOrderStatus: (id: number, status: string, paymentStatus?: string, note?: string) =>
    request<OrderResponse>(`/orders/${id}/status`, {
      method: "PUT",
      body: JSON.stringify({ status, paymentStatus, note }),
    }),

  validatePromo: (code: string, subtotal: number) =>
    request<{ isValid: boolean; code: string; discountAmount: number; message: string }>("/orders/validate-promo", {
      method: "POST",
      body: JSON.stringify({ code, orderSubtotal: subtotal }),
    }),

  // AI Barista
  sendAIChat: (message: string, sessionId?: number) =>
    request<{ sessionId: number; reply: string; suggestedDrinks: ProductDetail["relatedProducts"]; isAiModelLive: boolean }>("/ai/chat", {
      method: "POST",
      body: JSON.stringify({ message, sessionId }),
    }),

  // Admin & Manager
  getDashboard: () => request<DashboardSummary>("/admin/dashboard"),
  getUsers: (role?: string) => request<User[]>(`/admin/users${role ? `?role=${role}` : ""}`),
  createEmployee: (payload: { fullName: string; email: string; password: string; phoneNumber?: string; address?: string; role: string }) =>
    request<User>("/admin/users/employee", { method: "POST", body: JSON.stringify(payload) }),
  updateUserRole: (userId: number, payload: { role: string; isActive: boolean }) =>
    request<User>(`/admin/users/${userId}/role`, { method: "PUT", body: JSON.stringify(payload) }),
  getCustomers: () => request<User[]>("/admin/customers"),
  getPromotions: () => request<Promotion[]>("/admin/promotions"),
  createPromotion: (promo: Partial<Promotion>) => request<Promotion>("/admin/promotions", { method: "POST", body: JSON.stringify(promo) }),
  getShippers: () => request<User[]>("/admin/shippers"),

  // Warehouse (Thủ kho & FEFO)
  getIngredients: () => request<Ingredient[]>("/admin/ingredients"),
  createIngredient: (payload: Partial<Ingredient>) => request<Ingredient>("/admin/ingredients", { method: "POST", body: JSON.stringify(payload) }),
  updateIngredient: (id: number, payload: Partial<Ingredient>) => request<Ingredient>(`/admin/ingredients/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  adjustStock: (ingredientId: number, transactionType: "IMPORT" | "EXPORT" | "ADJUST", quantity: number, note?: string) =>
    request<boolean>("/admin/inventory/adjust", {
      method: "POST",
      body: JSON.stringify({ ingredientId, transactionType, quantity, note }),
    }),
  getStockBatches: (ingredientId?: number) => request<StockBatch[]>(`/admin/inventory/batches${ingredientId ? `?ingredientId=${ingredientId}` : ""}`),
  getPurchaseReceipts: () => request<PurchaseReceipt[]>("/admin/inventory/receipts"),
  createPurchaseReceipt: (payload: { supplierName: string; notes?: string; items: { ingredientId: number; quantity: number; unitPrice: number; batchNumber?: string; expiryDate: string }[] }) =>
    request<PurchaseReceipt>("/admin/inventory/receipts", { method: "POST", body: JSON.stringify(payload) }),
  getStockAudits: () => request<StockAudit[]>("/admin/inventory/audits"),
  createStockAudit: (payload: { notes?: string; items: { ingredientId: number; actualQuantity: number; reason: string }[] }) =>
    request<StockAudit>("/admin/inventory/audits", { method: "POST", body: JSON.stringify(payload) }),

  // Delivery & Shipper
  getShipperDashboard: () => request<ShipperDashboard>("/delivery/dashboard"),
  getAssignedOrders: () => request<OrderResponse[]>("/delivery/assigned"),
  assignShipper: (payload: { orderId: number; shipperId: number; notes?: string }) =>
    request<boolean>("/delivery/assign", { method: "POST", body: JSON.stringify(payload) }),
  updateDeliveryStatus: (orderId: number, payload: { status: string; failureReason?: string; notes?: string; isCodCollected?: boolean; proofImageUrl?: string }) =>
    request<boolean>(`/delivery/orders/${orderId}/status`, { method: "PUT", body: JSON.stringify(payload) }),
  reconcileCod: (orderIds: number[]) =>
    request<boolean>("/delivery/reconcile-cod", { method: "POST", body: JSON.stringify({ orderIds }) }),
};
