"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { OrderResponse, Product, User, Topping } from "@/types";
import { useAuth } from "@/context/AuthContext";
import { getDistinctOrderItemImage } from "@/lib/orderImageHelper";
import {
  ShoppingBag,
  CheckCircle,
  Truck,
  RotateCcw,
  Search,
  Sparkles,
  Phone,
  MapPin,
  Store,
  DollarSign,
  QrCode,
  Tag,
  MessageSquare,
  Send,
  Plus,
  Minus,
  Trash2,
  Printer,
  ChevronLeft,
  ChevronRight,
  UserCheck,
} from "lucide-react";

export default function CounterStaffPage() {
  const router = useRouter();
  const { user, isStaff, isManager, isLoading } = useAuth();

  const [activeMainTab, setActiveMainTab] = useState<"ORDERS" | "POS">("ORDERS");
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [shippers, setShippers] = useState<User[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [toppings, setToppings] = useState<Topping[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");

  // Assign Shipper modal state
  const [assigningOrder, setAssigningOrder] = useState<OrderResponse | null>(null);
  const [selectedShipperId, setSelectedShipperId] = useState<number | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  // Chat with Customer modal state
  const [chatOrder, setChatOrder] = useState<OrderResponse | null>(null);
  const [chatMessages, setChatMessages] = useState<
    { sender: "counter" | "customer"; text: string; time: string }[]
  >([]);
  const [chatInput, setChatInput] = useState("");

  // POS Order Form State
  const [posCustomerName, setPosCustomerName] = useState("Khách T");
  const [posCustomerPhone, setPosCustomerPhone] = useState("0908888777");
  const [posPaymentMethod, setPosPaymentMethod] = useState<"COD" | "VIETQR">("VIETQR");
  const [posPromoCode, setPosPromoCode] = useState("#MK2095F54");
  const [posDiscount, setPosDiscount] = useState(25000);
  const [posCart, setPosCart] = useState<
    { product: Product; size: string; sugar: string; ice: string; quantity: number }[]
  >([]);
  const [posCreating, setPosCreating] = useState(false);
  const [posSuccessMsg, setPosSuccessMsg] = useState<string | null>(null);

  // Orders Pagination
  const ITEMS_PER_PAGE = 5;
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => {
    if (!isLoading && (!user || (!isStaff && !isManager))) {
      router.push("/auth/login?redirect=/counter");
    }
  }, [user, isStaff, isManager, isLoading, router]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [orderList, shipperList, productList, toppingList] = await Promise.all([
        api.getOrders(),
        api.getShippers().catch(() => []),
        api.getProducts().catch(() => []),
        api.getToppings().catch(() => []),
      ]);
      setOrders(orderList || []);
      setShippers(shipperList || []);
      setProducts(productList || []);
      setToppings(toppingList || []);
      if (shipperList && shipperList.length > 0) {
        setSelectedShipperId(shipperList[0].id);
      }
    } catch (err) {
      console.error("Lỗi khi tải dữ liệu quầy:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user && (isStaff || isManager)) {
      loadData();
      const interval = setInterval(loadData, 10000);
      return () => clearInterval(interval);
    }
  }, [user, isStaff, isManager]);

  // Confirm Order (For COD, only confirm order status without changing PaymentStatus from Unpaid)
  const handleConfirmOrderOnly = async (orderId: number, paymentMethod: string) => {
    setActionLoading(true);
    try {
      const isCod = (paymentMethod || "").toUpperCase() === "COD";
      await api.updateOrderStatus(
        orderId,
        "Confirmed",
        isCod ? undefined : "Paid",
        isCod
          ? "Quầy đã xác nhận đơn COD (Giữ trạng thái Unpaid, thu tiền mặt khi giao hàng)"
          : "Quầy đã xác nhận thanh toán thành công"
      );
      await loadData();
    } catch (err) {
      alert(err instanceof Error ? err.message : "Lỗi xác nhận đơn hàng");
      console.error("Lỗi xác nhận đơn:", err);
    } finally {
      setActionLoading(false);
    }
  };

  // Assign Shipper
  const handleAssignShipper = async () => {
    if (!assigningOrder || !selectedShipperId) return;
    setActionLoading(true);
    try {
      await api.assignShipper({
        orderId: assigningOrder.id,
        shipperId: selectedShipperId,
        notes: "Đơn quầy giao cho shipper",
      });
      setAssigningOrder(null);
      await loadData();
    } catch (err) {
      console.error("Lỗi bàn giao shipper:", err);
    } finally {
      setActionLoading(false);
    }
  };

  // Open Chat with Customer
  const handleOpenChat = (order: OrderResponse) => {
    setChatOrder(order);
    setChatMessages([
      {
        sender: "counter",
        text: `Kính chào ${order.customerName || "quý khách"}, quầy thu ngân MilkyVerse liên hệ xác nhận đơn hàng #${order.orderCode}. Phương thức thanh toán: ${order.paymentMethod} (${order.paymentStatus === "Paid" ? "Đã thanh toán" : "Chưa thanh toán"}). Chúng tôi có thể hỗ trợ gì cho bạn không ạ?`,
        time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
      },
    ]);
  };

  const handleSendChat = (e: React.FormEvent) => {
    e.preventDefault();
    if (!chatInput.trim()) return;
    const text = chatInput.trim();
    const time = new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

    setChatMessages((prev) => [...prev, { sender: "counter", text, time }]);
    setChatInput("");

    setTimeout(() => {
      setChatMessages((prev) => [
        ...prev,
        {
          sender: "customer",
          text: `Khách hàng ${chatOrder?.customerName || "T"}: Dạ em đã nhận được tin nhắn từ quầy, em cảm ơn quầy nhiều ạ!`,
          time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
        },
      ]);
    }, 1000);
  };

  // POS Add item
  const handlePosAddToCart = (product: Product) => {
    const existingIndex = posCart.findIndex((i) => (i.product.Id ?? i.product.id) === (product.Id ?? product.id));
    if (existingIndex >= 0) {
      const updated = [...posCart];
      updated[existingIndex].quantity += 1;
      setPosCart(updated);
    } else {
      setPosCart([...posCart, { product, size: "M", sugar: "50%", ice: "70%", quantity: 1 }]);
    }
  };

  const handlePosRemove = (idx: number) => {
    setPosCart(posCart.filter((_, i) => i !== idx));
  };

  // POS Apply Promo Code
  const handleApplyPromo = () => {
    const code = posPromoCode.trim().toUpperCase();
    if (code === "#MK2095F54" || code === "MK2095F54" || code.startsWith("#MK") || code.startsWith("MK")) {
      setPosDiscount(25000);
      alert(`Áp dụng mã ưu đãi ${code}: Giảm ngay 25.000đ!`);
    } else {
      setPosDiscount(15000);
      alert(`Áp dụng mã ${code}: Giảm 15.000đ`);
    }
  };

  // POS Submit Order
  const handlePosSubmit = async () => {
    if (posCart.length === 0) {
      alert("Giỏ hàng tại quầy đang trống. Vui lòng chọn món.");
      return;
    }

    setPosCreating(true);
    try {
      const itemsPayload = posCart.map((i) => ({
        productId: i.product.Id ?? i.product.id,
        productName: i.product.Name ?? i.product.name ?? "Sản phẩm",
        itemType: i.product.ProductType ?? i.product.productType ?? "DRINK",
        size: i.size,
        sugarLevel: i.sugar,
        iceLevel: i.ice,
        quantity: i.quantity,
        isCustomDIY: false,
        toppings: [],
      }));

      await api.createOrder({
        customerName: posCustomerName.trim() || "Khách Hàng T",
        customerPhone: posCustomerPhone.trim() || "0908888777",
        shippingAddress: "Dùng tại quầy / Mang đi (Chi nhánh Quận 1)",
        paymentMethod: posPaymentMethod,
        promotionCode: posPromoCode.trim() || "#MK2095F54",
        note: `Đơn đặt tại quầy POS bởi ${user?.fullName || "Thu ngân"}`,
        items: itemsPayload,
      });

      setPosSuccessMsg(`Đã tạo đơn tại quầy thành công cho ${posCustomerName}! Đơn đã được chuyển tức thời đến Bếp nấu.`);
      setPosCart([]);
      await loadData();
      setTimeout(() => setPosSuccessMsg(null), 4000);
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Tạo đơn quầy thất bại");
    } finally {
      setPosCreating(false);
    }
  };

  // Calculation for POS Cart
  const posSubtotal = posCart.reduce((sum, item) => {
    const price = item.product.BasePrice ?? item.product.basePrice ?? 0;
    return sum + price * item.quantity;
  }, 0);
  const posTotal = Math.max(0, posSubtotal - posDiscount);

  // Filter orders
  const filteredOrders = orders.filter((o) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    const matchCode = o.orderCode?.toLowerCase().includes(q);
    const matchName = o.customerName?.toLowerCase().includes(q);
    const matchPhone = o.customerPhone?.includes(q);
    return matchCode || matchName || matchPhone;
  });

  const totalPages = Math.ceil(filteredOrders.length / ITEMS_PER_PAGE) || 1;
  const paginatedOrders = filteredOrders.slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* 1. Header & Actor Separation Badge */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
            <Store className="w-4 h-4 text-amber-700" />
            <span>Actor Riêng Biệt: Nhân Viên Phục Vụ Tại Quầy & Thu Ngân</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
            Bảng Quầy POS & Xác Nhận Thanh Toán
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Xác nhận thanh toán online / tại quầy, kiểm tra khách hàng tên gì, phương thức thanh toán, mã giảm giá, bàn giao shipper và chat phản hồi.
          </p>
        </div>

        <div className="flex items-center gap-2 bg-[#FAF7F2] p-1.5 rounded-2xl border border-[#EFE6DC]">
          <button
            onClick={() => setActiveMainTab("ORDERS")}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
              activeMainTab === "ORDERS" ? "bg-[#6F4E37] text-white shadow-sm" : "text-[#786C65] hover:text-[#2D2522]"
            }`}
          >
            Quản Lý Đơn & Thu Tiền ({orders.length})
          </button>
          <button
            onClick={() => setActiveMainTab("POS")}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all ${
              activeMainTab === "POS" ? "bg-[#6F4E37] text-white shadow-sm" : "text-[#786C65] hover:text-[#2D2522]"
            }`}
          >
            POS Đặt Món Cho Khách Tại Quầy
          </button>
        </div>
      </div>

      {posSuccessMsg && (
        <div className="p-4 rounded-2xl bg-emerald-50 text-emerald-800 border border-emerald-200 text-xs font-bold flex items-center gap-2">
          <CheckCircle className="w-5 h-5 text-emerald-600 shrink-0" />
          <span>{posSuccessMsg}</span>
        </div>
      )}

      {/* TAB 1: QUẢN LÝ ĐƠN & XÁC NHẬN THANH TOÁN */}
      {activeMainTab === "ORDERS" && (
        <div className="space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="relative w-full sm:w-80">
              <input
                type="text"
                placeholder="Tìm khách hàng tên T, mã đơn, số điện thoại..."
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value);
                  setCurrentPage(1);
                }}
                className="w-full pl-9 pr-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <Search className="w-4 h-4 text-[#786C65] absolute left-3 top-2.5" />
            </div>

            <span className="text-xs font-bold text-[#786C65]">
              Tổng cộng {filteredOrders.length} đơn hàng
            </span>
          </div>

          <div className="space-y-4">
            {paginatedOrders.map((o) => {
              const isPaid = o.paymentStatus === "Paid" || o.paymentStatus === "Success";
              const isCod = (o.paymentMethod || "").toUpperCase() === "COD";
              const isAwaitingConfirm = o.status === "Pending" || o.status === "PendingConfirmation";

              return (
                <div
                  key={o.id}
                  className="bg-white rounded-3xl p-6 border border-[#EFE6DC] shadow-sm hover:shadow-md transition-all space-y-4"
                >
                  <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-3 border-b border-[#FAF7F2]">
                    <div className="space-y-1">
                      <div className="flex items-center gap-3 flex-wrap">
                        <span className="font-mono font-black text-base text-[#2D2522]">
                          #{o.orderCode}
                        </span>
                        <span
                          className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${
                            o.status === "Completed" || o.status === "Delivered"
                              ? "bg-emerald-100 text-emerald-800"
                              : o.status === "Cancelled" || o.status === "Rejected" || o.status === "DeliveryFailed"
                              ? "bg-red-100 text-red-800"
                              : "bg-blue-100 text-blue-800"
                          }`}
                        >
                          Trạng thái: {o.status}
                        </span>

                        <span
                          className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${
                            isPaid ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"
                          }`}
                        >
                          {isPaid ? "✓ Đã thanh toán" : isCod ? "💵 COD: Chưa thu tiền (Unpaid)" : "⚠ Chờ xác nhận thanh toán"}
                        </span>
                      </div>

                      {/* Display explicit customer info as required */}
                      <p className="text-xs text-[#2D2522]">
                        <span className="font-extrabold text-[#6F4E37]">Khách hàng:</span> {o.customerName} • SĐT:{" "}
                        <span className="font-semibold">{o.customerPhone}</span>
                      </p>
                      <p className="text-xs text-[#786C65]">
                        Địa chỉ: {o.shippingAddress}
                      </p>
                    </div>

                    {/* Payment details info box */}
                    <div className="bg-[#FAF7F2] p-3.5 rounded-2xl border border-[#EFE6DC] text-xs space-y-1 text-right min-w-[240px]">
                      <div className="flex justify-between text-[#786C65]">
                        <span>Phương thức:</span>
                        <span className="font-bold text-[#2D2522]">
                          {o.paymentMethod === "VIETQR" ? "Chuyển khoản VietQR" : "Tiền mặt COD"}
                        </span>
                      </div>
                      {o.discountAmount > 0 && (
                        <div className="flex justify-between text-emerald-700 font-bold">
                          <span>Mã giảm giá áp dụng:</span>
                          <span>-{o.discountAmount.toLocaleString("vi-VN")}đ</span>
                        </div>
                      )}
                      <div className="flex justify-between items-baseline pt-1 border-t border-[#EFE6DC]">
                        <span className="font-bold text-[#2D2522]">Tổng tiền:</span>
                        <span className="font-black text-base text-[#6F4E37]">
                          {o.totalAmount.toLocaleString("vi-VN")}đ
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Items snapshot */}
                  <div className="space-y-1.5">
                    <span className="text-[11px] font-bold uppercase text-[#786C65] tracking-wider block">
                      Các món trong đơn:
                    </span>
                    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2">
                      {o.items.map((it, idx) => {
                        const itImg = getDistinctOrderItemImage(it, idx);
                        return (
                          <div key={idx} className="p-2 rounded-xl bg-[#FAF7F2] text-xs flex items-center justify-between gap-2 border border-[#EFE6DC]">
                            <div className="flex items-center gap-2 min-w-0">
                              <img src={itImg} alt={it.productName} className="w-8 h-8 rounded-lg object-cover shrink-0 border border-[#EFE6DC]" />
                              <div className="truncate">
                                <span className="font-bold text-[#2D2522] block truncate">{it.productName}</span>
                                <span className="text-[11px] text-[#786C65] block">
                                  {it.itemType === "DRINK" ? `Size ${it.size}` : it.portionSize || "Tiêu chuẩn"}
                                </span>
                              </div>
                            </div>
                            <span className="font-black text-[#6F4E37] shrink-0">x{it.quantity}</span>
                          </div>
                        );
                      })}
                    </div>
                  </div>

                  {/* Actions for Counter Staff */}
                  <div className="pt-3 border-t border-[#FAF7F2] flex flex-wrap items-center justify-between gap-3">
                    <div className="flex flex-wrap items-center gap-2">
                      {/* Confirm order / online payment */}
                      {isAwaitingConfirm && (
                        <button
                          onClick={() => handleConfirmOrderOnly(o.id, o.paymentMethod)}
                          disabled={actionLoading}
                          className="px-4 py-2 rounded-xl bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 shadow-sm flex items-center gap-1.5 transition-all"
                        >
                          <CheckCircle className="w-4 h-4" />
                          <span>
                            {isCod ? "Xác Nhận Đơn COD (Chờ Shipper Thu Tiền)" : `Xác Nhận Đã Thanh Toán (${o.paymentMethod})`}
                          </span>
                        </button>
                      )}

                      {!isAwaitingConfirm && !isPaid && !isCod && o.status !== "Cancelled" && (
                        <button
                          onClick={() => handleConfirmOrderOnly(o.id, o.paymentMethod)}
                          disabled={actionLoading}
                          className="px-4 py-2 rounded-xl bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 shadow-sm flex items-center gap-1.5 transition-all"
                        >
                          <CheckCircle className="w-4 h-4" />
                          <span>Xác Nhận Đã Thanh Toán ({o.paymentMethod})</span>
                        </button>
                      )}

                      {/* Bàn giao Shipper (when confirmed/preparing/ready/failed) */}
                      {(o.status === "ReadyForPickup" || o.status === "Confirmed" || o.status === "Preparing" || o.status === "DeliveryFailed") && (
                        <button
                          onClick={() => setAssigningOrder(o)}
                          className="px-4 py-2 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow-sm flex items-center gap-1.5 transition-all"
                        >
                          <Truck className="w-4 h-4" />
                          <span>{o.status === "DeliveryFailed" ? "Phân Công Giao Lại" : "Bàn Giao Cho Shipper Đi Giao"}</span>
                        </button>
                      )}

                      {/* Chat with Customer */}
                      <button
                        onClick={() => handleOpenChat(o)}
                        className="px-4 py-2 rounded-xl bg-purple-50 text-purple-700 border border-purple-200 text-xs font-bold hover:bg-purple-100 transition-colors flex items-center gap-1.5"
                      >
                        <MessageSquare className="w-4 h-4" />
                        <span>Box Chat Phản Hồi vs Khách</span>
                      </button>
                    </div>

                    <div className="text-xs text-[#786C65]">
                      {o.shipperName ? (
                        <span className="font-bold text-emerald-700">Shipper: {o.shipperName}</span>
                      ) : (
                        <span>Chưa phân công shipper</span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4 border-t border-[#EFE6DC]">
              <span className="text-xs text-[#786C65] font-semibold">
                Trang {currentPage} / {totalPages}
              </span>
              <div className="flex items-center gap-2">
                <button
                  disabled={currentPage <= 1}
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  className="p-2 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] disabled:opacity-40"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>
                {Array.from({ length: totalPages }, (_, i) => i + 1).map((pg) => (
                  <button
                    key={pg}
                    onClick={() => setCurrentPage(pg)}
                    className={`w-8 h-8 rounded-xl text-xs font-bold transition-all ${
                      currentPage === pg
                        ? "bg-[#6F4E37] text-white"
                        : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
                    }`}
                  >
                    {pg}
                  </button>
                ))}
                <button
                  disabled={currentPage >= totalPages}
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  className="p-2 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] disabled:opacity-40"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: POS ĐẶT MÓN CHO KHÁCH TẠI QUẦY */}
      {activeMainTab === "POS" && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* Menu Selection (Left) */}
          <div className="lg:col-span-7 bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h2 className="font-extrabold text-base text-[#2D2522]">
              Chọn Món Cho Khách Tại Quầy
            </h2>

            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 max-h-[560px] overflow-y-auto pr-1">
              {products.map((p) => {
                const id = p.Id ?? p.id;
                const name = p.Name ?? p.name;
                const price = p.BasePrice ?? p.basePrice ?? 0;
                const img = (p.ImageUrl ?? p.imageUrl) || "/logo.png";

                return (
                  <div
                    key={id}
                    onClick={() => handlePosAddToCart(p)}
                    className="p-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] hover:border-[#6F4E37] cursor-pointer transition-all flex flex-col justify-between group"
                  >
                    <div className="aspect-square rounded-xl overflow-hidden bg-white mb-2">
                      <img src={img} alt={name} className="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                    </div>
                    <div>
                      <h4 className="font-bold text-xs text-[#2D2522] line-clamp-1 group-hover:text-[#6F4E37]">
                        {name}
                      </h4>
                      <p className="font-black text-xs text-[#6F4E37] mt-1">
                        {price.toLocaleString("vi-VN")}đ
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* POS Bill / Cart Summary (Right) */}
          <div className="lg:col-span-5 bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-5">
            <div className="border-b border-[#FAF7F2] pb-3">
              <h3 className="font-black text-base text-[#2D2522]">Hóa Đơn Tại Quầy POS</h3>
              <p className="text-xs text-[#786C65]">Nhân viên thu ngân: {user?.fullName || "Nguyễn Thu Ngân"}</p>
            </div>

            {/* Customer Inputs */}
            <div className="space-y-3">
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Tên khách hàng:</label>
                <input
                  type="text"
                  placeholder="Khách hàng T"
                  value={posCustomerName}
                  onChange={(e) => setPosCustomerName(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs font-bold focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Số điện thoại:</label>
                <input
                  type="tel"
                  placeholder="0908888777"
                  value={posCustomerPhone}
                  onChange={(e) => setPosCustomerPhone(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs font-bold focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Phương thức thanh toán:</label>
                <div className="grid grid-cols-2 gap-2">
                  <button
                    type="button"
                    onClick={() => setPosPaymentMethod("VIETQR")}
                    className={`py-2 px-3 rounded-xl text-xs font-bold border transition-all ${
                      posPaymentMethod === "VIETQR"
                        ? "bg-[#6F4E37] text-white border-[#6F4E37]"
                        : "bg-[#FAF7F2] text-[#2D2522] border-[#EFE6DC]"
                    }`}
                  >
                    Chuyển Khoản QR
                  </button>
                  <button
                    type="button"
                    onClick={() => setPosPaymentMethod("COD")}
                    className={`py-2 px-3 rounded-xl text-xs font-bold border transition-all ${
                      posPaymentMethod === "COD"
                        ? "bg-[#6F4E37] text-white border-[#6F4E37]"
                        : "bg-[#FAF7F2] text-[#2D2522] border-[#EFE6DC]"
                    }`}
                  >
                    Tiền Mặt Tại Quầy
                  </button>
                </div>
              </div>

              {/* Voucher Input */}
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Mã giảm giá cho khách:</label>
                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    placeholder="#MK2095F54"
                    value={posPromoCode}
                    onChange={(e) => setPosPromoCode(e.target.value)}
                    className="flex-1 px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs font-mono font-bold focus:outline-none focus:border-[#6F4E37]"
                  />
                  <button
                    type="button"
                    onClick={handleApplyPromo}
                    className="px-3 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324]"
                  >
                    Áp dụng
                  </button>
                </div>
              </div>
            </div>

            {/* Cart Items List */}
            <div className="space-y-2 border-t border-b border-[#FAF7F2] py-3 max-h-48 overflow-y-auto pr-1">
              {posCart.length === 0 ? (
                <p className="text-xs text-[#786C65] text-center py-4">Chưa chọn món nào</p>
              ) : (
                posCart.map((item, idx) => (
                  <div key={idx} className="flex items-center justify-between text-xs p-2 rounded-xl bg-[#FAF7F2]">
                    <div className="flex-1">
                      <p className="font-bold text-[#2D2522]">{item.product.Name || item.product.name}</p>
                      <p className="text-[10px] text-[#786C65]">Size {item.size} • {item.sugar} đường • {item.ice} đá</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="font-black text-[#6F4E37]">x{item.quantity}</span>
                      <button
                        onClick={() => handlePosRemove(idx)}
                        className="text-red-500 hover:text-red-700 p-1"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Total Summary */}
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between text-[#786C65]">
                <span>Tạm tính:</span>
                <span className="font-bold">{posSubtotal.toLocaleString("vi-VN")}đ</span>
              </div>
              {posDiscount > 0 && (
                <div className="flex justify-between text-emerald-600 font-bold">
                  <span>Giảm giá ({posPromoCode}):</span>
                  <span>-{posDiscount.toLocaleString("vi-VN")}đ</span>
                </div>
              )}
              <div className="flex justify-between items-baseline pt-2 border-t border-[#EFE6DC]">
                <span className="font-bold text-sm text-[#2D2522]">Tổng thanh toán:</span>
                <span className="text-xl font-black text-[#6F4E37]">{posTotal.toLocaleString("vi-VN")}đ</span>
              </div>
            </div>

            <button
              type="button"
              disabled={posCreating || posCart.length === 0}
              onClick={handlePosSubmit}
              className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md flex items-center justify-center gap-2 transition-all disabled:opacity-50"
            >
              <CheckCircle className="w-4 h-4" />
              <span>{posCreating ? "Đang tạo đơn..." : "Tạo Đơn Hàng & Chuyển Sang Bếp"}</span>
            </button>
          </div>
        </div>
      )}

      {/* Modal Bàn Giao Shipper */}
      {assigningOrder && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full border border-[#EFE6DC] shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-2 border-b">
              <h3 className="font-black text-base text-[#2D2522]">
                Bàn Giao Shipper Cho Đơn #{assigningOrder.orderCode}
              </h3>
              <button onClick={() => setAssigningOrder(null)} className="text-gray-400 font-bold text-xs">
                [Đóng]
              </button>
            </div>

            <div className="space-y-3">
              <label className="text-xs font-bold text-[#2D2522] block">Chọn Shipper nhận giao:</label>
              <div className="space-y-2">
                {shippers.map((s) => (
                  <label
                    key={s.id}
                    className={`flex items-center justify-between p-3 rounded-2xl border text-xs cursor-pointer transition-colors ${
                      selectedShipperId === s.id ? "border-[#6F4E37] bg-[#FAF7F2]" : "border-[#EFE6DC]"
                    }`}
                  >
                    <div className="flex items-center gap-2">
                      <input
                        type="radio"
                        name="shipperRadio"
                        checked={selectedShipperId === s.id}
                        onChange={() => setSelectedShipperId(s.id)}
                      />
                      <span className="font-bold text-[#2D2522]">{s.fullName}</span>
                    </div>
                    <span className="text-[#786C65]">{s.phoneNumber}</span>
                  </label>
                ))}
              </div>
            </div>

            <div className="pt-2 flex items-center justify-end gap-3">
              <button
                onClick={() => setAssigningOrder(null)}
                className="px-4 py-2 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#786C65]"
              >
                Hủy
              </button>
              <button
                disabled={actionLoading || !selectedShipperId}
                onClick={handleAssignShipper}
                className="px-5 py-2 rounded-xl bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
              >
                {actionLoading ? "Đang bàn giao..." : "Xác Nhận Bàn Giao"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Box Chat với Khách Hàng */}
      {chatOrder && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full border border-[#EFE6DC] shadow-2xl flex flex-col h-[520px] overflow-hidden">
            <div className="p-4 border-b border-[#EFE6DC] flex items-center justify-between bg-[#FAF7F2]">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-full bg-amber-700 text-white flex items-center justify-center font-bold text-xs">
                  POS
                </div>
                <div>
                  <h3 className="font-extrabold text-sm text-[#2D2522]">Quầy Phục Vụ MilkyVerse</h3>
                  <p className="text-[11px] text-[#786C65]">Chat với {chatOrder.customerName} (#{chatOrder.orderCode})</p>
                </div>
              </div>
              <button onClick={() => setChatOrder(null)} className="text-gray-400 font-bold text-xs">
                Đóng [X]
              </button>
            </div>

            <div className="flex-1 p-4 overflow-y-auto space-y-3 bg-[#FAF7F2]/40">
              {chatMessages.map((m, i) => (
                <div
                  key={i}
                  className={`flex flex-col ${m.sender === "counter" ? "items-end" : "items-start"}`}
                >
                  <div
                    className={`max-w-[80%] p-3 rounded-2xl text-xs ${
                      m.sender === "counter"
                        ? "bg-[#6F4E37] text-white rounded-br-none"
                        : "bg-white border border-[#EFE6DC] text-[#2D2522] rounded-bl-none shadow-sm"
                    }`}
                  >
                    {m.text}
                  </div>
                  <span className="text-[10px] text-[#786C65] mt-1 px-1">{m.time}</span>
                </div>
              ))}
            </div>

            <form onSubmit={handleSendChat} className="p-3 border-t border-[#EFE6DC] bg-white flex items-center gap-2">
              <input
                type="text"
                placeholder="Nhắn phản hồi cho khách hàng..."
                value={chatInput}
                onChange={(e) => setChatInput(e.target.value)}
                className="flex-1 px-4 py-2.5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <button
                type="submit"
                className="p-2.5 rounded-2xl bg-[#6F4E37] text-white hover:bg-[#4A3324] transition-colors shadow-sm"
              >
                <Send className="w-4 h-4" />
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
