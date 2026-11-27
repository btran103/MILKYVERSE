"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { OrderResponse } from "@/types";
import { useAuth } from "@/context/AuthContext";
import { useCart } from "@/context/CartContext";
import { getDistinctOrderItemImage } from "@/lib/orderImageHelper";
import { getOrderStatusMeta, getPaymentStatusMeta, isCodPayment } from "@/lib/orderStatus";
import {
  Package,
  ArrowRight,
  ExternalLink,
  Clock,
  XCircle,
  AlertCircle,
  MessageSquare,
  Send,
  CheckCircle,
  ChevronLeft,
  ChevronRight,
  Sparkles,
  HelpCircle,
  LogIn,
  RotateCcw,
} from "lucide-react";

export default function OrdersListPage() {
  const router = useRouter();
  const { user, isLoading } = useAuth();
  const { addItem } = useCart();
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"ALL" | "PROCESSING" | "COMPLETED" | "CANCELLED">("ALL");

  const handleReorder = (order: OrderResponse) => {
    if (!order.items || order.items.length === 0) return;
    let addedCount = 0;
    for (const item of order.items) {
      const ok = addItem({
        productId: item.productId,
        name: item.productName,
        imageUrl: item.imageUrl,
        itemType: (item.itemType as any) || "DRINK",
        size: item.size || "M",
        sugarLevel: item.sugarLevel,
        iceLevel: item.iceLevel,
        spicyLevel: item.spicyLevel,
        dippingSauce: item.dippingSauce,
        portionSize: item.portionSize,
        unitPrice: item.unitPrice,
        quantity: item.quantity,
        totalPrice: item.totalPrice,
        isCustomDIY: item.isCustomDIY,
        customNotes: item.customNotes,
        toppings: (item.toppings || []).map((t) => ({
          toppingId: t.toppingId || 0,
          name: t.toppingName,
          price: t.price,
          quantity: t.quantity,
        })),
      });
      if (ok) addedCount++;
    }
    if (addedCount > 0) {
      router.push("/cart");
    }
  };

  // Timer tick for 2-minute countdown
  const [currentTime, setCurrentTime] = useState(Date.now());
  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);

  // Cancel Order Modal State
  const [cancellingOrder, setCancellingOrder] = useState<OrderResponse | null>(null);
  const [cancelReason, setCancelReason] = useState("Đặt nhầm món / muốn đổi món khác");
  const [customReason, setCustomReason] = useState("");
  const [cancelLoading, setCancelLoading] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);

  // Chat with Manager Modal State
  const [chatOrder, setChatOrder] = useState<OrderResponse | null>(null);
  const [chatMessages, setChatMessages] = useState<
    { sender: "customer" | "manager"; text: string; time: string }[]
  >([]);
  const [inputMsg, setInputMsg] = useState("");

  // Pagination
  const ITEMS_PER_PAGE = 5;
  const [currentPage, setCurrentPage] = useState(1);

  const loadOrders = async (silent = false) => {
    if (!user) return;
    if (!silent) setLoading(true);
    setLoadError(null);
    try {
      const data = await api.getOrders();
      setOrders(data || []);
    } catch (err: unknown) {
      if (!silent) {
        const msg = err instanceof Error ? err.message : "Đã xảy ra lỗi khi tải đơn hàng.";
        setLoadError(msg);
      }
    } finally {
      if (!silent) setLoading(false);
    }
  };

  useEffect(() => {
    if (!isLoading) {
      if (user) {
        loadOrders();
        const interval = setInterval(() => loadOrders(true), 5000);
        return () => clearInterval(interval);
      } else {
        setOrders([]);
        setLoading(false);
      }
    }
  }, [isLoading, user]);

  // Handle Cancel Order
  const handleConfirmCancel = async () => {
    if (!cancellingOrder) return;
    setCancelLoading(true);
    setCancelError(null);

    const finalReason = cancelReason === "Khác" ? customReason.trim() || "Khách hàng đổi ý" : cancelReason;

    try {
      await api.cancelOrder(cancellingOrder.id, finalReason);
      setCancellingOrder(null);
      await loadOrders();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Không thể hủy đơn hàng.";
      setCancelError(msg);
    } finally {
      setCancelLoading(false);
    }
  };

  // Open Chat with Manager
  const handleOpenChat = (order: OrderResponse) => {
    setChatOrder(order);
    const initialMsgs: { sender: "customer" | "manager"; text: string; time: string }[] = [
      {
        sender: "manager",
        text: `Chào ${order.customerName || "bạn"}, bộ phận Quản lý MilkyVerse tiếp nhận phản hồi cho đơn hàng #${order.orderCode}. ${
          order.status === "Cancelled"
            ? `Đơn hàng này hiện ở trạng thái ĐÃ HỦY. Lý do: "${order.cancelReason || "Theo yêu cầu hoặc hết nguyên liệu"}". Bạn có cần cửa hàng hỗ trợ thêm điều gì không ạ?`
            : "Bạn cần hỗ trợ điều chỉnh hay giải đáp thông tin gì về đơn hàng này ạ?"
        }`,
        time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
      },
    ];
    setChatMessages(initialMsgs);
  };

  const handleSendChat = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputMsg.trim()) return;

    const userText = inputMsg.trim();
    const nowTime = new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });

    setChatMessages((prev) => [
      ...prev,
      { sender: "customer", text: userText, time: nowTime },
    ]);
    setInputMsg("");

    // Simulate polite manager auto-reply
    setTimeout(() => {
      setChatMessages((prev) => [
        ...prev,
        {
          sender: "manager",
          text: `Quản lý MilkyVerse đã ghi nhận: "${userText}". Chúng tôi xin lỗi vì sự bất tiện này. Cửa hàng gửi tặng bạn mã giảm giá #MK2095F54 (giảm 25.000đ) cho lần đặt tiếp theo!`,
          time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
        },
      ]);
    }, 1000);
  };

  // Filter orders by tab
  const filteredOrders = orders.filter((o) => {
    if (activeTab === "ALL") return true;
    if (activeTab === "PROCESSING") {
      return ["Pending", "PendingConfirmation", "Confirmed", "Preparing", "ReadyForPickup", "Delivering", "DeliveryFailed"].includes(o.status);
    }
    if (activeTab === "COMPLETED") return o.status === "Completed" || o.status === "Delivered";
    if (activeTab === "CANCELLED") return o.status === "Cancelled" || o.status === "Rejected";
    return true;
  });

  // Pagination logic
  const totalPages = Math.ceil(filteredOrders.length / ITEMS_PER_PAGE) || 1;
  const paginatedOrders = filteredOrders.slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE);

  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
        <div className="h-12 w-72 bg-[#FAF7F2] border border-[#EFE6DC] rounded-2xl animate-pulse" />
        <div className="space-y-4">
          {[1, 2, 3].map((n) => (
            <div key={n} className="h-32 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
          ))}
        </div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center space-y-6">
        <div className="w-20 h-20 rounded-full bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-center mx-auto text-[#6F4E37] shadow-sm">
          <LogIn className="w-10 h-10" />
        </div>
        <div className="space-y-2">
          <h2 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
            Bạn Cần Đăng Nhập
          </h2>
          <p className="text-sm text-[#786C65] max-w-md mx-auto">
            Vui lòng đăng nhập tài khoản để xem lịch sử đơn hàng, theo dõi trạng thái giao hàng và phản hồi trực tiếp với Quản lý.
          </p>
        </div>
        <div className="flex flex-wrap items-center justify-center gap-3 pt-2">
          <Link
            href="/auth/login?redirect=/orders"
            className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold shadow-md hover:bg-[#4A3324] transition-colors"
          >
            <LogIn className="w-4 h-4" />
            <span>Đăng Nhập Ngay</span>
          </Link>
          <Link
            href="/auth/register"
            className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-white border border-[#EFE6DC] text-[#2D2522] text-xs font-bold hover:bg-[#FAF7F2] transition-colors"
          >
            <span>Đăng Ký Tài Khoản</span>
          </Link>
          <Link
            href="/menu"
            className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-[#6F4E37] text-xs font-bold hover:bg-[#EFE6DC] transition-colors"
          >
            <span>Khám Phá Thực Đơn</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* 1. Header & Policy Notification */}
      <div className="border-b border-[#EFE6DC] pb-6 flex flex-col md:flex-row md:items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
            Lịch Sử Đơn Hàng Của Bạn
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Theo dõi hành trình giao hàng, trạng thái thanh toán hoặc chat phản hồi trực tiếp với Quản lý
          </p>
        </div>

        <div className="flex items-center gap-2 bg-[#FAF7F2] border border-[#EFE6DC] px-3.5 py-2 rounded-2xl text-xs text-[#6F4E37] font-semibold">
          <Clock className="w-4 h-4 text-[#C5A059] shrink-0" />
          <span>Chính sách: Đơn COD thanh toán khi nhận hàng; khách có thể hủy trước khi cửa hàng pha chế</span>
        </div>
      </div>

      {/* Error alert if any */}
      {loadError && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-2xl flex items-center justify-between gap-4">
          <div className="flex items-center gap-2.5 text-xs text-red-700 font-semibold">
            <AlertCircle className="w-5 h-5 shrink-0 text-red-500" />
            <span>{loadError}</span>
          </div>
          <button
            onClick={() => loadOrders()}
            className="px-4 py-1.5 text-xs font-bold text-red-700 bg-white border border-red-200 rounded-xl hover:bg-red-50 transition-colors"
          >
            Thử lại
          </button>
        </div>
      )}

      {/* 2. Filter Tabs */}
      <div className="flex items-center gap-2 border-b border-[#EFE6DC] pb-2 overflow-x-auto scrollbar-none">
        <button
          onClick={() => {
            setActiveTab("ALL");
            setCurrentPage(1);
          }}
          className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all ${
            activeTab === "ALL"
              ? "bg-[#6F4E37] text-white shadow-sm"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          Tất cả ({orders.length})
        </button>

        <button
          onClick={() => {
            setActiveTab("PROCESSING");
            setCurrentPage(1);
          }}
          className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all ${
            activeTab === "PROCESSING"
              ? "bg-[#6F4E37] text-white shadow-sm"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          Đang xử lý & Giao hàng (
          {orders.filter((o) => ["Pending", "PendingConfirmation", "Confirmed", "Preparing", "ReadyForPickup", "Delivering", "DeliveryFailed"].includes(o.status)).length}
          )
        </button>

        <button
          onClick={() => {
            setActiveTab("COMPLETED");
            setCurrentPage(1);
          }}
          className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all ${
            activeTab === "COMPLETED"
              ? "bg-[#6F4E37] text-white shadow-sm"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          Đã hoàn thành ({orders.filter((o) => o.status === "Completed" || o.status === "Delivered").length})
        </button>

        <button
          onClick={() => {
            setActiveTab("CANCELLED");
            setCurrentPage(1);
          }}
          className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all ${
            activeTab === "CANCELLED"
              ? "bg-red-700 text-white shadow-sm"
              : "bg-white border border-[#EFE6DC] text-red-600 hover:bg-red-50"
          }`}
        >
          Lịch sử đơn đã hủy ({orders.filter((o) => o.status === "Cancelled" || o.status === "Rejected").length})
        </button>
      </div>

      {/* 3. Orders List */}
      {loading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((n) => (
            <div key={n} className="h-32 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
          ))}
        </div>
      ) : filteredOrders.length === 0 ? (
        <div className="bg-white rounded-3xl p-12 text-center border border-[#EFE6DC] space-y-4">
          <div className="w-16 h-16 rounded-full bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-center mx-auto text-[#6F4E37]">
            <Package className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-[#2D2522]">
            {activeTab === "CANCELLED" ? "Không có đơn hàng nào bị hủy" : "Không có đơn hàng nào"}
          </h3>
          <p className="text-xs text-[#786C65]">
            Khám phá ngay thực đơn trà sữa & đồ ăn vặt hấp dẫn của MilkyVerse!
          </p>
          <div className="pt-2">
            <Link
              href="/menu"
              className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold shadow-md hover:bg-[#4A3324] transition-colors"
            >
              <span>Xem Thực Đơn</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {paginatedOrders.map((o) => {
            const isCod = isCodPayment(o.paymentMethod);
            const statusMeta = getOrderStatusMeta(o.status);
            const paymentMeta = getPaymentStatusMeta(o.paymentStatus, o.paymentMethod);
            const canCancelNow =
              o.canCancel ??
              ["Pending", "PendingConfirmation", "Confirmed"].includes(o.status);

            const formatCountdown = (secs: number) => {
              const m = Math.floor(secs / 60);
              const s = secs % 60;
              return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
            };

            const onlineRemaining =
              !isCod && o.paymentStatus === "Pending" && o.paymentExpiresAt
                ? Math.max(0, Math.floor((new Date(o.paymentExpiresAt).getTime() - currentTime) / 1000))
                : 0;

            return (
              <div
                key={o.id}
                className="bg-white rounded-3xl p-6 border border-[#EFE6DC] shadow-sm hover:shadow-md transition-shadow space-y-4"
              >
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                  <div className="space-y-1">
                    <div className="flex flex-wrap items-center gap-2.5">
                      <span className="font-black text-[#2D2522] text-base">
                        #{o.orderCode}
                      </span>
                      <span
                        className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${statusMeta.badgeClass}`}
                      >
                        {statusMeta.label}
                      </span>

                      {/* COD shows cash-on-delivery notice; Online payment shows countdown strictly for MOMO/SEPAY */}
                      {isCod ? (
                        <span className="flex items-center gap-1 text-[11px] font-bold text-[#6F4E37] bg-[#FAF7F2] border border-[#EFE6DC] px-2.5 py-0.5 rounded-full">
                          <span>Thanh toán tiền mặt khi nhận hàng</span>
                        </span>
                      ) : o.paymentStatus === "Pending" && onlineRemaining > 0 ? (
                        <span className="flex items-center gap-1 text-[11px] font-black text-blue-700 bg-blue-50 border border-blue-200 px-2 py-0.5 rounded-full">
                          <Clock className="w-3 h-3" />
                          <span>Thanh toán online còn: {formatCountdown(onlineRemaining)}</span>
                        </span>
                      ) : null}
                    </div>

                    <p className="text-xs text-[#786C65]">
                      {new Date(o.createdAt).toLocaleString("vi-VN")} • {o.items?.length || 0} món
                      {o.shipperName ? ` • Shipper: ${o.shipperName}` : ""}
                    </p>
                    <p className="text-xs text-[#786C65]">
                      Địa chỉ: <span className="font-semibold text-[#2D2522]">{o.shippingAddress}</span>
                    </p>

                    {/* Display Cancellation / Failure Reason if Cancelled/Failed */}
                    {(o.status === "Cancelled" || o.status === "DeliveryFailed" || o.status === "Rejected") && (
                      <div className="mt-2 p-2.5 rounded-xl bg-red-50 border border-red-100 text-xs text-red-700">
                        <span className="font-bold">Lý do:</span>{" "}
                        {o.deliveryFailureReason || o.cancelReason || o.note || "Đơn hàng có sự cố hoặc đã hủy theo yêu cầu."}
                      </div>
                    )}
                  </div>

                  <div className="text-left sm:text-right shrink-0">
                    <span className="text-lg font-black text-[#6F4E37] block">
                      {o.totalAmount.toLocaleString("vi-VN")}đ
                    </span>
                    <span className="text-[11px] text-[#786C65]">
                      {o.paymentMethod} •{" "}
                      <span className={paymentMeta.isPaid ? "text-emerald-600 font-bold" : "text-amber-700 font-bold"}>
                        {paymentMeta.label}
                      </span>
                    </span>
                  </div>
                </div>

                {/* Items Visual Preview (Distinct non-duplicate images) */}
                {o.items && o.items.length > 0 && (
                  <div className="pt-2 border-t border-[#FAF7F2]">
                    <div className="flex items-center gap-3 overflow-x-auto py-1 scrollbar-thin">
                      {o.items.map((it, idx) => {
                        const imgUrl = getDistinctOrderItemImage(it, idx);
                        return (
                          <div
                            key={it.id || idx}
                            className="flex items-center gap-2.5 bg-[#FAF7F2] p-2 rounded-2xl border border-[#EFE6DC] shrink-0 max-w-[260px] group hover:border-[#6F4E37]/30 transition-colors"
                          >
                            <div className="relative w-12 h-12 rounded-xl overflow-hidden bg-white shrink-0 border border-[#EFE6DC]">
                              <img
                                src={imgUrl}
                                alt={it.productName}
                                className="w-full h-full object-cover group-hover:scale-110 transition-transform"
                                loading="lazy"
                              />
                              <span className="absolute bottom-0 right-0 bg-[#6F4E37] text-white text-[9px] font-bold px-1 rounded-tl-md">
                                x{it.quantity}
                              </span>
                            </div>
                            <div className="min-w-0 pr-1">
                              <p className="text-xs font-bold text-[#2D2522] truncate" title={it.productName}>
                                {it.productName}
                              </p>
                              <p className="text-[10px] text-[#786C65] truncate">
                                Size {it.size} • {it.totalPrice.toLocaleString("vi-VN")}đ
                              </p>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}

                {/* Action Buttons */}
                <div className="pt-3 border-t border-[#FAF7F2] flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-wrap items-center gap-2">
                    {/* Customer cancel button if within 2 minutes and Pending */}
                    {canCancelNow && (
                      <button
                        onClick={() => setCancellingOrder(o)}
                        className="px-3.5 py-1.5 rounded-xl bg-red-600 text-white text-xs font-bold hover:bg-red-700 transition-colors flex items-center gap-1.5 shadow-sm"
                      >
                        <XCircle className="w-3.5 h-3.5" />
                        <span>Hủy Đơn Hàng</span>
                      </button>
                    )}

                    {/* Re-order button */}
                    <button
                      onClick={() => handleReorder(o)}
                      className="px-3.5 py-1.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors flex items-center gap-1.5 shadow-sm"
                      title="Thêm lại tất cả món trong đơn này vào giỏ hàng"
                    >
                      <RotateCcw className="w-3.5 h-3.5" />
                      <span>Đặt Lại Đơn Này</span>
                    </button>

                    {/* Chat with Manager button (always available, especially helpful if cancelled) */}
                    <button
                      onClick={() => handleOpenChat(o)}
                      className="px-3.5 py-1.5 rounded-xl bg-purple-50 text-purple-700 border border-purple-200 text-xs font-bold hover:bg-purple-100 transition-colors flex items-center gap-1.5"
                    >
                      <MessageSquare className="w-3.5 h-3.5" />
                      <span>Chat Phản Hồi vs Quản Lý</span>
                    </button>
                  </div>

                  <Link
                    href={`/orders/${o.id}`}
                    className="px-4 py-1.5 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 text-[#6F4E37] text-xs font-bold transition-colors flex items-center gap-1.5 ml-auto"
                  >
                    <span>Xem Tiến Trình Chi Tiết</span>
                    <ExternalLink className="w-3.5 h-3.5" />
                  </Link>
                </div>
              </div>
            );
          })}

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <span className="text-xs text-[#786C65] font-semibold">
                Hiển thị trang {currentPage} / {totalPages} ({filteredOrders.length} đơn hàng)
              </span>
              <div className="flex items-center gap-2">
                <button
                  disabled={currentPage <= 1}
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  className="p-2 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] disabled:opacity-40 transition-colors"
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
                  className="p-2 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] disabled:opacity-40 transition-colors"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* 4. Modal Hủy Đơn Hàng */}
      {cancellingOrder && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full border border-[#EFE6DC] shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-2 border-b">
              <div className="flex items-center gap-2 text-red-600 font-bold">
                <XCircle className="w-5 h-5" />
                <h3 className="text-base text-[#2D2522]">Hủy Đơn Hàng #{cancellingOrder.orderCode}</h3>
              </div>
              <button
                onClick={() => setCancellingOrder(null)}
                className="text-gray-400 hover:text-gray-600 font-bold text-xs"
              >
                [Đóng]
              </button>
            </div>

            <p className="text-xs text-[#786C65]">
              Đơn hàng của bạn chưa bước vào giai đoạn pha chế. Vui lòng cho MilkyVerse biết lý do hủy đơn:
            </p>

            {cancelError && (
              <div className="p-3 rounded-xl bg-red-50 text-red-700 text-xs flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0" />
                <span>{cancelError}</span>
              </div>
            )}

            <div className="space-y-2">
              {[
                "Đặt nhầm món / muốn đổi món khác",
                "Thay đổi địa chỉ nhận hàng hoặc SĐT",
                "Đổi ý không muốn mua nữa",
                "Thời gian giao hàng lâu hơn dự kiến",
                "Khác",
              ].map((reason, idx) => (
                <label
                  key={idx}
                  className={`flex items-center gap-2.5 p-3 rounded-2xl border text-xs font-semibold cursor-pointer transition-colors ${
                    cancelReason === reason
                      ? "border-[#6F4E37] bg-[#FAF7F2] text-[#6F4E37]"
                      : "border-[#EFE6DC] hover:bg-[#FAF7F2] text-[#2D2522]"
                  }`}
                >
                  <input
                    type="radio"
                    name="cancelReason"
                    value={reason}
                    checked={cancelReason === reason}
                    onChange={(e) => setCancelReason(e.target.value)}
                    className="text-[#6F4E37]"
                  />
                  <span>{reason}</span>
                </label>
              ))}

              {cancelReason === "Khác" && (
                <textarea
                  rows={2}
                  placeholder="Nhập lý do hủy của bạn..."
                  value={customReason}
                  onChange={(e) => setCustomReason(e.target.value)}
                  className="w-full p-3 rounded-xl border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
              )}
            </div>

            <div className="pt-2 flex items-center justify-end gap-3">
              <button
                type="button"
                onClick={() => setCancellingOrder(null)}
                className="px-4 py-2.5 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#786C65]"
              >
                Không, giữ đơn
              </button>
              <button
                type="button"
                disabled={cancelLoading}
                onClick={handleConfirmCancel}
                className="px-5 py-2.5 rounded-xl bg-red-600 text-white text-xs font-bold hover:bg-red-700 transition-colors shadow-sm disabled:opacity-50"
              >
                {cancelLoading ? "Đang hủy đơn..." : "Xác nhận Hủy Đơn"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 5. Modal Box Chat Phản Hồi vs Quản Lý */}
      {chatOrder && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full border border-[#EFE6DC] shadow-2xl flex flex-col h-[520px] overflow-hidden">
            {/* Chat Header */}
            <div className="p-4 border-b border-[#EFE6DC] flex items-center justify-between bg-[#FAF7F2]">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-full bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs">
                  QL
                </div>
                <div>
                  <h3 className="font-extrabold text-sm text-[#2D2522]">Quản Lý Đơn Hàng MilkyVerse</h3>
                  <p className="text-[11px] text-[#786C65]">Đang hỗ trợ đơn #{chatOrder.orderCode}</p>
                </div>
              </div>
              <button
                onClick={() => setChatOrder(null)}
                className="text-gray-400 hover:text-gray-600 text-xs font-bold"
              >
                Đóng [X]
              </button>
            </div>

            {/* Chat Messages */}
            <div className="flex-1 p-4 overflow-y-auto space-y-3 bg-[#FAF7F2]/40">
              {chatMessages.map((msg, i) => (
                <div
                  key={i}
                  className={`flex flex-col ${msg.sender === "customer" ? "items-end" : "items-start"}`}
                >
                  <div
                    className={`max-w-[80%] p-3 rounded-2xl text-xs ${
                      msg.sender === "customer"
                        ? "bg-[#6F4E37] text-white rounded-br-none"
                        : "bg-white border border-[#EFE6DC] text-[#2D2522] rounded-bl-none shadow-sm"
                    }`}
                  >
                    {msg.text}
                  </div>
                  <span className="text-[10px] text-[#786C65] mt-1 px-1">{msg.time}</span>
                </div>
              ))}
            </div>

            {/* Chat Input */}
            <form onSubmit={handleSendChat} className="p-3 border-t border-[#EFE6DC] bg-white flex items-center gap-2">
              <input
                type="text"
                placeholder="Nhắn tin cho Quản lý (hỏi lý do hủy, hoàn tiền, khiếu nại...)..."
                value={inputMsg}
                onChange={(e) => setInputMsg(e.target.value)}
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
