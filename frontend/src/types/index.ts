export type UserRole = "Customer" | "Staff" | "Manager" | "WarehouseStaff" | "DeliveryStaff" | "Admin";

export interface User {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string;
  address?: string;
  role: UserRole;
  isActive?: boolean;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
  phoneNumber?: string;
  address?: string;
}

export interface Category {
  id: number;
  name: string;
  slug: string;
  description: string;
  imageUrl: string;
  categoryType?: "DRINK" | "FOOD" | "COMBO";
  productCount: number;

  Id?: number;
  Name?: string;
  Slug?: string;
  Description?: string;
  ImageUrl?: string;
  CategoryType?: "DRINK" | "FOOD" | "COMBO";
  ProductCount?: number;
}

export interface Topping {
  id: number;
  name: string;
  price: number;
  isAvailable: boolean;

  Id?: number;
  Name?: string;
  Price?: number;
  IsAvailable?: boolean;
}

export interface Product {
  id: number;
  categoryId: number;
  categoryName?: string;
  name: string;
  slug: string;
  description: string;
  basePrice: number;
  imageUrl: string;
  isBestSeller: boolean;
  isNew: boolean;
  isActive: boolean;
  stockStatus: number;
  productType?: "DRINK" | "FOOD" | "COMBO";
  spicyLevel?: number;
  allowSpicyCustomization?: boolean;
  allowSauceCustomization?: boolean;
  defaultSauce?: string;
  portionDescription?: string;

  Id?: number;
  CategoryId?: number;
  CategoryName?: string;
  Name?: string;
  Slug?: string;
  Description?: string;
  BasePrice?: number;
  ImageUrl?: string;
  IsBestSeller?: boolean;
  IsNew?: boolean;
  IsActive?: boolean;
  StockStatus?: number;
  ProductType?: "DRINK" | "FOOD" | "COMBO";
  SpicyLevel?: number;
  AllowSpicyCustomization?: boolean;
  AllowSauceCustomization?: boolean;
  DefaultSauce?: string;
  PortionDescription?: string;
}

export interface ProductDetail extends Product {
  availableToppings: Topping[];
  sizes: string[];
  sugarLevels: string[];
  iceLevels: string[];
  spicyOptions?: string[];
  sauceOptions?: string[];
  portionOptions?: string[];
  relatedProducts: Product[];
}

export interface DIYOptionItem {
  id: string;
  name: string;
  description: string;
  additionalPrice: number;
  colorHex?: string;
  isAvailable?: boolean;
}

export interface DIYCatalog {
  teaBases: DIYOptionItem[];
  milks: DIYOptionItem[];
  sizes: DIYOptionItem[];
  sweetnessLevels: DIYOptionItem[];
  iceLevels: DIYOptionItem[];
  toppings: Topping[];
}

export interface DIYQuoteRequest {
  teaBaseId: string;
  milkId: string;
  size: string;
  sugarLevel: string;
  iceLevel: string;
  toppingIds: number[];
  quantity: number;
}

export interface DIYQuoteResponse {
  isValid: boolean;
  recipeName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  breakdown: string[];
  errorMessage?: string;
}

export interface CartItemTopping {
  toppingId: number;
  name: string;
  price: number;
  quantity: number;
}

export interface CartItem {
  id: string;
  productId?: number;
  name: string;
  imageUrl?: string;
  itemType?: "DRINK" | "FOOD" | "COMBO";
  size: string;
  sugarLevel?: string;
  iceLevel?: string;
  spicyLevel?: string;
  dippingSauce?: string;
  portionSize?: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  isCustomDIY: boolean;
  customNotes?: string;
  toppings: CartItemTopping[];
}

export interface CreateOrderRequest {
  customerName: string;
  customerPhone: string;
  shippingAddress: string;
  note: string;
  paymentMethod: string;
  promotionCode?: string;
  items: {
    productId?: number;
    productName: string;
    itemType?: string;
    size?: string;
    sugarLevel?: string;
    iceLevel?: string;
    spicyLevel?: string;
    dippingSauce?: string;
    portionSize?: string;
    quantity: number;
    isCustomDIY: boolean;
    customNotes?: string;
    toppings: { toppingId: number; quantity: number }[];
  }[];
}

export interface OrderItemOptionResponse {
  toppingId: number;
  toppingName: string;
  price: number;
  quantity: number;
}

export interface OrderDetailResponse {
  id: number;
  productId?: number;
  productName: string;
  itemType?: string;
  size: string;
  sugarLevel?: string;
  iceLevel?: string;
  spicyLevel?: string;
  dippingSauce?: string;
  portionSize?: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  isCustomDIY: boolean;
  customNotes?: string;
  toppings: OrderItemOptionResponse[];
}

export interface OrderResponse {
  id: number;
  orderCode: string;
  userId?: number;
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  totalAmount: number;
  status: string;
  paymentStatus: string;
  paymentMethod: string;
  customerName: string;
  customerPhone: string;
  shippingAddress: string;
  note: string;
  createdAt: string;
  items: OrderDetailResponse[];

  // Delivery & Shipper info
  shipperId?: number;
  shipperName?: string;
  shipperPhone?: string;
  deliveryStatus?: string;
  codAmount?: number;
  isCodCollected?: boolean;
  codReconciled?: boolean;
}

export interface SuggestedDrink {
  productId?: number;
  name: string;
  description: string;
  estimatedPrice: number;
  sugarRecommend: string;
  iceRecommend: string;
  recommendedToppings: string[];
  reason: string;
}

export interface AIChatMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  suggestedDrinks?: SuggestedDrink[];
  isAiModelLive?: boolean;
  timestamp: string;
}

export interface DashboardSummary {
  totalRevenue: number;
  totalOrders: number;
  processingOrders: number;
  pendingOrdersCount?: number;
  deliveringOrdersCount?: number;
  totalCustomers: number;
  lowStockCount: number;
  recentOrders: OrderResponse[];
  topProducts: Product[];
  lowStockIngredients: Ingredient[];
}

export interface Ingredient {
  id: number;
  name: string;
  unit: string;
  currentStock: number;
  minAlertThreshold: number;
  unitCost: number;
  categoryType?: string;
  isLowStock?: boolean;
}

export interface StockBatch {
  id: number;
  ingredientId: number;
  ingredientName: string;
  unit: string;
  batchNumber: string;
  manufactureDate: string;
  expiryDate: string;
  initialQuantity: number;
  currentQuantity: number;
  unitCost: number;
  daysRemaining: number;
  isExpired: boolean;
  isNearExpiry: boolean;
}

export interface PurchaseReceipt {
  id: number;
  receiptCode: string;
  supplierName: string;
  totalCost: number;
  status: string;
  notes: string;
  createdAt: string;
  itemCount: number;
}

export interface StockAuditItem {
  ingredientId: number;
  ingredientName: string;
  unit: string;
  systemQuantity: number;
  actualQuantity: number;
  variance: number;
  reason: string;
}

export interface StockAudit {
  id: number;
  auditCode: string;
  auditDate: string;
  status: string;
  notes: string;
  items: StockAuditItem[];
}

export interface ShipperDashboard {
  totalAssignedOrders: number;
  inTransitOrders: number;
  deliveredToday: number;
  totalCodCollected: number;
  totalCodPendingReconcile: number;
  orders: OrderResponse[];
}

export interface Promotion {
  id: number;
  code: string;
  description: string;
  discountPercent: number;
  discountAmount: number;
  minOrderValue: number;
  isActive: boolean;
}
