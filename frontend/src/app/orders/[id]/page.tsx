"use client";

export const dynamic = "force-dynamic";

import React, { useEffect, useState, use } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { OrderResponse, SePayQrResponse } from "@/types";
import { useCart } from "@/context/CartContext";
import { getDistinctOrderItemImage } from "@/lib/orderImageHelper";
import { getOrderStatusMeta, getPaymentStatusMeta, isCodPayment } from "@/lib/orderStatus";
import confetti from "canvas-confetti";
import {
  Clock,
  CheckCircle2,
  Package,
  Truck,
  Check,
  XCircle,
  ArrowLeft,
  MapPin,
  Phone,
  User,
  Copy,
  RotateCcw,
  Radio,
  AlertTriangle,
  ShieldCheck,
  Coffee,
  Banknote,
  History,
} from "lucide-react";

export default function OrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const resolvedParams = use(params);
  const router = useRouter();
  const { addItem } = useCart();
  const [order, setOrder] = useState<OrderResponse | null>(null);
  const [sePaySession, setSePaySession] = useState<SePayQrResponse | null>(null);
  const [remainingPaymentSecs, setRemainingPaymentSecs] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copiedField, setCopiedField] = useState<string | null>(null);

  // Cancel order modal state
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancelReason, setCancelReason] = useState("Đặt nhầm món / muốn đổi món khác");
  const [customReason, setCustomReason] = useState("");
  const [cancelLoading, setCancelLoading] = useState(false);

  // Chat with Manager modal state
  const [showChatModal, setShowChatModal] = useState(false);
  const [chatMessages, setChatMessages] = useState<
    { sender: "customer" | "manager"; text: string; time: string }[]
  >([]);
  const [chatInput, setChatInput] = useState("");

  const handleReorder = () => {
    if (!order || !order.items || order.items.length === 0) return;
    let addedCount = 0;
    for (const item of order.items) {
      const ok = addItem({
        productId: item.productId,
        name: item.productName,
        imageUrl: item.imageUrl,
        itemType: (item.itemType as "DRINK" | "FOOD" | "COMBO") || "DRINK",
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

  const handleCopy = (text: string, field: string) => {
    navigator.clipboard.writeText(text);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  useEffect(() => {
    async function fetchOrderAndPayment() {
      const orderIdNum = parseInt(resolvedParams.id, 10);
      try {
        const data = await api.getOrderById(orderIdNum);
        setOrder(data);
        if (data.paymentMethod !== "COD") {
          setRemainingPaymentSecs(data.remainingPaymentSeconds ?? 0);
        }

        if (data.paymentMethod === "SEPAY" && data.paymentStatus !== "Paid" && data.status !== "Cancelled") {
          const accountNumber = "00000107071";
          const bank = "TPBank";
          const accountName = "HA BAO TRAN";
          const description = `MILKY ${data.orderCode}`;
          const qrUrl = `https://qr.sepay.vn/img?acc=${encodeURIComponent(accountNumber)}&bank=${encodeURIComponent(bank)}&amount=${Math.round(data.totalAmount)}&des=${encodeURIComponent(description)}&template=compact`;

          let qr: SePayQrResponse = {
            orderId: data.id,
            orderCode: data.orderCode,
            amount: data.totalAmount,
            description,
            bank,
            accountNumber,
            accountName,
            qrUrl,
            paymentStatus: data.paymentStatus,
            expiredSeconds: data.remainingPaymentSeconds ?? 900,
          };

          try {
            const apiQr = await api.getSePayQr(orderIdNum);
            if (apiQr) qr = apiQr;
          } catch {
            // fallback
          }
          setSePaySession(qr);
          setRemainingPaymentSecs(qr.expiredSeconds ?? 900);
        }
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : "Không tìm thấy đơn hàng.";
        setError(msg);
      } finally {
        setLoading(false);
      }
    }
    fetchOrderAndPayment();
  }, [resolvedParams.id]);

  // Countdown strictly for ONLINE payment methods (SEPAY / MOMO), NEVER for COD
  useEffect(() => {
    if (!order || isCodPayment(order.paymentMethod) || order.paymentStatus === "Paid" || order.paymentStatus === "Success" || order.status === "Cancelled") {
      return;
    }

    const tick = setInterval(() => {
      setRemainingPaymentSecs((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => clearInterval(tick);
  }, [order?.paymentMethod, order?.paymentStatus, order?.status]);

  // Live order tracking polling while active (2s for online payment awaiting confirmation, 5s for fulfillment & shipper delivery)
  useEffect(() => {
    if (!order) return;
    const isTerminal = ["Completed", "Delivered", "Cancelled", "Rejected"].includes(order.status);
    if (isTerminal) return;

    const intervalMs = order.paymentMethod === "SEPAY" && order.paymentStatus !== "Paid" ? 2000 : 5000;

    const pollInterval = setInterval(async () => {
      try {
        const latestOrder = await api.getOrderById(order.id);
        setOrder(latestOrder);
        if (latestOrder.paymentMethod === "SEPAY" && (latestOrder.paymentStatus === "Paid" || latestOrder.status === "Confirmed") && order.paymentStatus !== "Paid") {
          confetti({
            particleCount: 150,
            spread: 80,
            origin: { y: 0.6 },
            colors: ["#2563EB", "#4E7D55", "#6F4E37", "#C5A059"],
          });
        }
      } catch {
        // network retry
      }
    }, intervalMs);

    return () => clearInterval(pollInterval);
  }, [order?.id, order?.paymentMethod, order?.paymentStatus, order?.status]);

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-sm font-bold text-[#786C65]">Đang tải hành trình đơn hàng...</p>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="max-w-xl mx-auto px-4 py-20 text-center space-y-4">
        <h2 className="text-xl font-bold text-[#2D2522]">Không tìm thấy thông tin đơn hàng</h2>
        <p className="text-sm text-[#786C65]">{error}</p>
        <Link
          href="/orders"
          className="inline-flex items-center gap-1.5 px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Danh sách đơn hàng</span>
        </Link>
      </div>
    );
  }

  const isCod = isCodPayment(order.paymentMethod);
  const statusMeta = getOrderStatusMeta(order.status);
  const paymentMeta = getPaymentStatusMeta(order.paymentStatus, order.paymentMethod);
  const isCancelled = order.status === "Cancelled";
  const isDeliveryFailed = order.status === "DeliveryFailed";
  const isRejected = order.status === "Rejected";
  const isException = isCancelled || isDeliveryFailed || isRejected;
  const isDelivering = order.status === "Delivering";
  const isDelivered = order.status === "Delivered" || order.status === "Completed";

  const canCancelNow =
    order.canCancel ??
    ["Pending", "PendingConfirmation", "Confirmed"].includes(order.status);

  // 7-step Visual Timeline (aligned across COD & Online)
  const timelineSteps = [
    { key: "Created", label: "1. Đặt hàng thành công", icon: Clock },
    { key: "Confirmed", label: "2. Cửa hàng xác nhận đơn", icon: CheckCircle2 },
    { key: "Preparing", label: "3. Đang chuẩn bị đồ uống", icon: Coffee },
    { key: "ReadyForPickup", label: "4. Đơn đã sẵn sàng giao", icon: Package },
    { key: "Delivering", label: "5. Shipper đang giao hàng", icon: Truck },
    { key: "Delivered", label: "6. Giao hàng thành công", icon: Check },
    {
      key: "PaymentDone",
      label: isCod ? "7. Thanh toán COD thành công" : "7. Thanh toán online thành công",
      icon: Banknote,
    },
  ];

  const getCurrentStepIndex = () => {
    if (isDelivered && paymentMeta.isPaid) return 6;
    if (isDelivered) return 5;
    if (order.status === "Delivering") return 4;
    if (order.status === "ReadyForPickup") return 3;
    if (order.status === "Preparing") return 2;
    if (order.status === "Confirmed") return 1;
    return 0;
  };
  const currentStepIndex = getCurrentStepIndex();

  const lastUpdatedAt =
    order.updatedAt ||
    (order.statusHistories && order.statusHistories.length > 0
      ? order.statusHistories[order.statusHistories.length - 1].createdAt
      : order.createdAt);

  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  };

  const handleConfirmCancel = async () => {
    setCancelLoading(true);
    const finalReason = cancelReason === "Khác" ? customReason.trim() || "Khách hàng đổi ý" : cancelReason;
    try {
      const updated = await api.cancelOrder(order.id, finalReason);
      setOrder(updated);
      setShowCancelModal(false);
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Hủy đơn thất bại");
    } finally {
      setCancelLoading(false);
    }
  };

  const handleSendChat = (e: React.FormEvent) => {
    e.preventDefault();
    if (!chatInput.trim()) return;
    const text = chatInput.trim();
    const time = new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
    setChatMessages((prev) => [...prev, { sender: "customer", text, time }]);
    setChatInput("");

    setTimeout(() => {
      setChatMessages((prev) => [
        ...prev,
        {
          sender: "manager",
          text: `Quản lý MilkyVerse: Cảm ơn bạn đã phản hồi về đơn #${order.orderCode}. Bộ phận chăm sóc khách hàng đã tiếp nhận xử lý và sẽ phản hồi qua số điện thoại ${order.customerPhone} trong ít phút!`,
          time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
        },
      ]);
    }, 1000);
  };

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Top Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-[#EFE6DC]">
        <div>
          <Link
            href="/orders"
            className="inline-flex items-center gap-1.5 text-xs font-bold text-[#6F4E37] mb-2 hover:underline"
          >
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Tất cả đơn hàng</span>
          </Link>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] flex flex-wrap items-center gap-3">
            <span>Mã Đơn: #{order.orderCode}</span>
            <span className={`text-xs font-bold px-3 py-1 rounded-full ${statusMeta.badgeClass}`}>
              {statusMeta.label}
            </span>
          </h1>
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-[#786C65] mt-1.5">
            <span>Đặt lúc: <strong>{new Date(order.createdAt).toLocaleString("vi-VN")}</strong></span>
            <span>Cập nhật gần nhất: <strong>{new Date(lastUpdatedAt).toLocaleString("vi-VN")}</strong></span>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {/* Customer Cancel Button when allowed — NO fake 2-min auto-cancel countdown on COD */}
          {canCancelNow && (
            <button
              onClick={() => setShowCancelModal(true)}
              className="px-4 py-2 rounded-xl bg-red-600 text-white text-xs font-bold hover:bg-red-700 transition-colors shadow-sm"
            >
              Hủy Đơn Hàng
            </button>
          )}

          {/* Re-order button */}
          <button
            onClick={handleReorder}
            className="px-4 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shadow-sm flex items-center gap-1.5"
            title="Thêm lại tất cả món trong đơn này vào giỏ hàng"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span>Đặt Lại Đơn Này</span>
          </button>

          {/* Chat with Manager button */}
          <button
            onClick={() => {
              setShowChatModal(true);
              if (chatMessages.length === 0) {
                setChatMessages([
                  {
                    sender: "manager",
                    text: `Chào ${order.customerName || "bạn"}, bộ phận Quản lý MilkyVerse đang hỗ trợ đơn #${order.orderCode}. ${
                      isCancelled
                        ? `Đơn hàng này hiện ở trạng thái ĐÃ HỦY. Lý do: "${order.cancelReason || "Theo yêu cầu"}".`
                        : "Bạn có câu hỏi hoặc cần điều chỉnh gì cho đơn này không ạ?"
                    }`,
                    time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
                  },
                ]);
              }
            }}
            className="px-4 py-2 rounded-xl bg-purple-50 text-purple-700 border border-purple-200 text-xs font-bold hover:bg-purple-100 transition-colors flex items-center gap-1.5"
          >
            <span>Chat vs Quản Lý</span>
          </button>
        </div>
      </div>

      {/* Live Delivery Banner when Shipper is delivering */}
      {isDelivering && (
        <div className="p-5 rounded-3xl bg-cyan-50 border-2 border-cyan-300 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-xs">
          <div className="flex items-start gap-3">
            <div className="w-11 h-11 rounded-2xl bg-cyan-600 text-white flex items-center justify-center shrink-0 animate-bounce">
              <Truck className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-black text-sm text-cyan-950">
                Đơn hàng đang trên đường giao đến bạn (Đang giao hàng)!
              </h3>
              <p className="text-xs text-cyan-800 mt-0.5">
                {order.shipperName
                  ? `Shipper ${order.shipperName} (${order.shipperPhone}) đang vận chuyển đơn hàng tới địa chỉ ${order.shippingAddress}.`
                  : "Shipper đang vận chuyển đơn hàng đến địa chỉ nhận của bạn."}
                {isCod && !paymentMeta.isPaid && ` Vui lòng chuẩn bị sẵn ${order.totalAmount.toLocaleString("vi-VN")}đ tiền mặt khi nhận hàng.`}
              </p>
            </div>
          </div>
          {order.shipperPhone && (
            <a
              href={`tel:${order.shipperPhone}`}
              className="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-2xl bg-cyan-700 text-white text-xs font-bold hover:bg-cyan-800 shrink-0"
            >
              <Phone className="w-3.5 h-3.5" />
              <span>Gọi Shipper ({order.shipperPhone})</span>
            </a>
          )}
        </div>
      )}

      {/* 7-Step Visual Timeline */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="font-black text-sm uppercase tracking-wider text-[#2D2522]">
            Hành Trình Đơn Hàng (7 Bước Trực Quan)
          </h2>
          {isCod && (
            <span className="text-[11px] font-bold px-2.5 py-1 rounded-full bg-amber-50 text-amber-800 border border-amber-200">
              Quy trình COD • Không tự hủy theo thời gian
            </span>
          )}
        </div>

        {isException ? (
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 text-red-700 bg-red-50 p-5 rounded-2xl border border-red-200">
            <div className="flex items-start gap-3">
              <XCircle className="w-6 h-6 shrink-0 mt-0.5" />
              <div>
                <h4 className="font-black text-sm">
                  {isDeliveryFailed
                    ? "Giao hàng không thành công"
                    : isRejected
                    ? "Đơn hàng bị từ chối nhận"
                    : "Đơn hàng đã bị hủy"}
                </h4>
                <p className="text-xs text-red-600 mt-0.5">
                  <span className="font-bold">Nguyên nhân:</span>{" "}
                  {order.deliveryFailureReason || order.cancelReason || order.note || "Đơn hàng gặp sự cố hoặc đã được hủy."}
                </p>
                {isCod && !paymentMeta.isPaid && (
                  <p className="text-[11px] text-amber-800 font-semibold mt-1">
                    Trạng thái thanh toán COD vẫn giữ nguyên là Chưa thanh toán (Unpaid).
                  </p>
                )}
              </div>
            </div>
            <button
              onClick={() => setShowChatModal(true)}
              className="px-4 py-2 rounded-xl bg-red-600 text-white text-xs font-bold hover:bg-red-700 shadow-sm transition-colors"
            >
              Liên hệ Quản lý
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-7 gap-3">
            {timelineSteps.map((st, idx) => {
              const Icon = st.icon;
              const isPassed = idx < currentStepIndex || (idx === currentStepIndex && (idx === 6 || isDelivered));
              const isCurrent = idx === currentStepIndex && !isPassed;
              const isPaymentStepActive = idx === 6 && paymentMeta.isPaid;
              const completedStep = isPassed || isPaymentStepActive;

              return (
                <div
                  key={st.key}
                  className={`p-3 rounded-2xl border flex flex-col items-center text-center space-y-2 transition-all ${
                    isCurrent
                      ? "bg-[#FAF7F2] border-[#6F4E37] ring-2 ring-[#F7D6D0]"
                      : completedStep
                      ? "bg-emerald-50/70 border-emerald-200"
                      : "bg-gray-50/60 border-[#EFE6DC] opacity-65"
                  }`}
                >
                  <div
                    className={`w-10 h-10 rounded-xl flex items-center justify-center font-bold shadow-xs ${
                      isCurrent
                        ? "bg-[#6F4E37] text-white"
                        : completedStep
                        ? "bg-[#4E7D55] text-white"
                        : "bg-white text-[#786C65] border border-[#EFE6DC]"
                    }`}
                  >
                    <Icon className="w-4 h-4" />
                  </div>
                  <span
                    className={`text-[11px] font-bold leading-snug ${
                      isCurrent
                        ? "text-[#6F4E37]"
                        : completedStep
                        ? "text-emerald-800"
                        : "text-[#786C65]"
                    }`}
                  >
                    {st.label}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Details Grid */}
      <div className="grid grid-cols-1 md:grid-cols-12 gap-8 items-start">
        {/* Left: Items Snapshot + Status History Log */}
        <div className="md:col-span-7 space-y-6">
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider">
              Danh sách món trong đơn ({order.items.length})
            </h3>

            <div className="space-y-4">
              {order.items.map((item, idx) => {
                const itemImg = getDistinctOrderItemImage(item, idx);
                return (
                  <div
                    key={item.id || idx}
                    className="p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] flex items-start gap-4 hover:border-[#6F4E37]/30 transition-all"
                  >
                    <div className="relative w-16 h-16 sm:w-20 sm:h-20 rounded-2xl overflow-hidden bg-white border border-[#EFE6DC] shadow-sm shrink-0">
                      <img
                        src={itemImg}
                        alt={item.productName}
                        className="w-full h-full object-cover"
                        loading="lazy"
                        onError={(e) => {
                          (e.target as HTMLImageElement).src = "/images/menu/ca-vien-chien-thap-cam-met-lon.jpg";
                        }}
                      />
                      <span className="absolute bottom-1 right-1 bg-[#6F4E37] text-white text-[10px] font-bold px-1.5 py-0.5 rounded-md shadow-sm">
                        x{item.quantity}
                      </span>
                    </div>

                    <div className="flex-1 min-w-0 space-y-1.5">
                      <div className="flex justify-between items-start gap-2">
                        <h4 className="font-extrabold text-sm sm:text-base text-[#2D2522]">
                          {item.productName}
                        </h4>
                        <span className="font-black text-[#6F4E37] text-sm sm:text-base shrink-0">
                          {item.totalPrice.toLocaleString("vi-VN")}đ
                        </span>
                      </div>

                      <div className="text-xs text-[#786C65] space-y-0.5">
                        <p>
                          <strong>Cấu hình:</strong> Size {item.size}
                          {item.sugarLevel ? ` • ${item.sugarLevel} đường` : ""}
                          {item.iceLevel ? ` • ${item.iceLevel} đá` : ""}
                          {item.spicyLevel ? ` • ${item.spicyLevel}` : ""}
                          {item.dippingSauce ? ` • Sốt: ${item.dippingSauce}` : ""}
                          {item.portionSize ? ` • ${item.portionSize}` : ""}
                        </p>
                        {item.toppings?.length > 0 && (
                          <p className="text-[#6F4E37] font-semibold">
                            Topping: {item.toppings.map((t) => `${t.toppingName} (x${t.quantity})`).join(", ")}
                          </p>
                        )}
                        {item.customNotes && (
                          <p className="italic text-[11px] text-[#786C65]">
                            Ghi chú: {item.customNotes}
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Full Status Transition History */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider flex items-center gap-2">
              <History className="w-4 h-4 text-[#6F4E37]" />
              <span>Lịch Sử Di Chuyển Trạng Thái</span>
            </h3>

            {order.statusHistories && order.statusHistories.length > 0 ? (
              <div className="space-y-3 border-l-2 border-[#EFE6DC] ml-2 pl-4">
                {order.statusHistories.map((h) => {
                  const hMeta = getOrderStatusMeta(h.status);
                  return (
                    <div key={h.id} className="relative text-xs space-y-0.5">
                      <span className="w-2.5 h-2.5 rounded-full bg-[#6F4E37] absolute -left-[21px] top-1 ring-2 ring-white" />
                      <div className="flex flex-wrap items-center gap-2">
                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${hMeta.badgeClass}`}>
                          {hMeta.label}
                        </span>
                        <span className="text-[11px] font-mono text-[#786C65]">
                          {new Date(h.createdAt).toLocaleString("vi-VN")}
                        </span>
                        {h.changedByName && (
                          <span className="text-[10px] text-[#6F4E37] font-semibold">
                            • {h.changedByName} {h.changedByRole ? `(${h.changedByRole})` : ""}
                          </span>
                        )}
                      </div>
                      <p className="text-[#2D2522] font-medium">{h.note}</p>
                    </div>
                  );
                })}
              </div>
            ) : (
              <p className="text-xs text-[#786C65]">Chưa có nhật ký thay đổi trạng thái.</p>
            )}
          </div>
        </div>

        {/* Right: Customer, Shipper & Financial Recap */}
        <div className="md:col-span-5 space-y-6">
          {/* Shipping & Recipient */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3 text-xs">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider mb-2">
              Thông Tin Nhận Hàng
            </h3>
            <div className="flex items-center gap-2 text-[#2D2522]">
              <User className="w-4 h-4 text-[#6F4E37] shrink-0" />
              <span className="font-bold">{order.customerName}</span>
            </div>
            <div className="flex items-center gap-2 text-[#2D2522]">
              <Phone className="w-4 h-4 text-[#6F4E37] shrink-0" />
              <span>{order.customerPhone}</span>
            </div>
            <div className="flex items-start gap-2 text-[#2D2522]">
              <MapPin className="w-4 h-4 text-[#6F4E37] shrink-0 mt-0.5" />
              <span>{order.shippingAddress}</span>
            </div>
            {order.note && (
              <p className="italic text-[#786C65] pt-1 border-t border-[#FAF7F2]">
                &ldquo;{order.note}&rdquo;
              </p>
            )}
          </div>

          {/* Assigned Shipper Info Card */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-2.5 text-xs">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider flex items-center gap-1.5">
              <Truck className="w-4 h-4 text-[#6F4E37]" />
              <span>Thông Tin Giao Hàng (Shipper)</span>
            </h3>
            {order.shipperName ? (
              <div className="space-y-1.5 pt-1">
                <div className="flex justify-between">
                  <span className="text-[#786C65]">Shipper phụ trách:</span>
                  <span className="font-bold text-[#2D2522]">{order.shipperName}</span>
                </div>
                {order.shipperPhone && (
                  <div className="flex justify-between">
                    <span className="text-[#786C65]">Số điện thoại:</span>
                    <a href={`tel:${order.shipperPhone}`} className="font-bold text-[#6F4E37] hover:underline">
                      {order.shipperPhone}
                    </a>
                  </div>
                )}
                {order.deliveryStatus && (
                  <div className="flex justify-between">
                    <span className="text-[#786C65]">Tiến độ giao hàng:</span>
                    <span className="font-bold text-blue-700">{order.deliveryStatus}</span>
                  </div>
                )}
                {order.pickedUpAt && (
                  <div className="flex justify-between">
                    <span className="text-[#786C65]">Bắt đầu đi giao:</span>
                    <span>{new Date(order.pickedUpAt).toLocaleString("vi-VN")}</span>
                  </div>
                )}
                {order.deliveredAt && (
                  <div className="flex justify-between">
                    <span className="text-[#786C65]">Giao thành công lúc:</span>
                    <span className="font-bold text-emerald-700">{new Date(order.deliveredAt).toLocaleString("vi-VN")}</span>
                  </div>
                )}
              </div>
            ) : (
              <p className="text-[#786C65]">Đơn hàng đang chờ cửa hàng phân công shipper.</p>
            )}
          </div>

          {/* Payment & Total */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3 text-xs">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider mb-2">
              Thanh Toán
            </h3>
            <div className="flex justify-between text-[#786C65]">
              <span>Phương thức:</span>
              <span className="font-bold text-[#2D2522]">
                {isCod ? "Tiền mặt khi nhận hàng (COD)" : order.paymentMethod}
              </span>
            </div>
            <div className="flex justify-between items-center text-[#786C65]">
              <span>Trạng thái thanh toán:</span>
              <span className={`font-bold px-2 py-0.5 rounded-md ${paymentMeta.badgeClass}`}>
                {paymentMeta.label}
              </span>
            </div>
            {order.paidAt && (
              <div className="flex justify-between text-[#786C65]">
                <span>Thời gian thanh toán:</span>
                <span className="font-mono font-bold text-emerald-700">
                  {new Date(order.paidAt).toLocaleString("vi-VN")}
                </span>
              </div>
            )}
            {order.paymentConfirmedByName && (
              <div className="flex justify-between text-[#786C65]">
                <span>Người xác nhận thu tiền:</span>
                <span className="font-bold text-[#2D2522]">{order.paymentConfirmedByName}</span>
              </div>
            )}
            <div className="flex justify-between text-[#786C65]">
              <span>Tạm tính:</span>
              <span className="font-bold text-[#2D2522]">{order.subtotal.toLocaleString("vi-VN")}đ</span>
            </div>
            {order.discountAmount > 0 && (
              <div className="flex justify-between text-[#4E7D55] font-semibold">
                <span>Giảm giá:</span>
                <span>-{order.discountAmount.toLocaleString("vi-VN")}đ</span>
              </div>
            )}
            <div className="flex justify-between text-[#786C65]">
              <span>Phí giao hàng:</span>
              <span className="font-bold text-[#2D2522]">
                {order.shippingFee === 0 ? "Miễn phí" : `${order.shippingFee.toLocaleString("vi-VN")}đ`}
              </span>
            </div>

            <div className="pt-2 border-t border-[#EFE6DC] flex justify-between items-baseline">
              <span className="font-bold text-sm text-[#2D2522]">Tổng tiền:</span>
              <span className="text-xl font-black text-[#6F4E37]">
                {order.totalAmount.toLocaleString("vi-VN")}đ
              </span>
            </div>
          </div>

          {/* SePay VietQR TPBank Card */}
          {!paymentMeta.isPaid && order.paymentMethod === "SEPAY" && !isCancelled && sePaySession && (
            <div className="bg-white p-6 rounded-3xl border-2 border-[#2563EB] shadow-lg space-y-4 text-center">
              <div className="flex items-center justify-between pb-2 border-b border-[#FAF7F2]">
                <div className="flex items-center gap-2 text-xs font-black text-[#2D2522]">
                  <Radio className={`w-4 h-4 ${paymentMeta.isExpired ? "text-red-600" : "text-blue-600 animate-pulse"}`} />
                  <span>Thanh Toán SePay • VietQR TPBank</span>
                </div>
                <div className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-blue-50 text-blue-700 font-mono font-bold text-xs border border-blue-200">
                  <Clock className="w-3.5 h-3.5 text-blue-600" />
                  <span>{formatTime(remainingPaymentSecs)}</span>
                </div>
              </div>

              <div className="p-3 rounded-2xl bg-amber-50 border border-amber-200 text-amber-900 text-xs text-left space-y-1">
                <div className="flex items-center gap-1.5 font-bold text-amber-800">
                  <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
                  <span>Trạng thái: CHỜ CHUYỂN KHOẢN (Hiệu lực 15 phút)</span>
                </div>
                <p className="text-[11px] text-amber-700 leading-normal">
                  Quét mã VietQR bằng app ngân hàng của bạn. Tiền vào TPBank là SePay <strong>tự động duyệt đơn ngay lập tức</strong>!
                </p>
              </div>

              <div className="space-y-1.5">
                <div className="relative mx-auto max-w-[220px] bg-white p-2 rounded-2xl border-2 border-[#2563EB] shadow-md">
                  <img
                    src={sePaySession.qrUrl}
                    alt="Mã VietQR SePay TPBank"
                    className="w-full h-auto object-contain rounded-xl"
                  />
                </div>
                <p className="text-[11px] font-bold text-[#2563EB]">
                  TPBank • {sePaySession.accountName}
                </p>
              </div>

              <div className="bg-[#FAF7F2] p-3 rounded-2xl border border-[#EFE6DC] text-left text-xs space-y-2">
                <div className="flex justify-between items-center text-[#786C65]">
                  <span>Số tài khoản TPBank:</span>
                  <strong className="font-mono font-bold text-[#2D2522]">{sePaySession.accountNumber}</strong>
                </div>
                <div className="flex justify-between items-center text-[#786C65]">
                  <span>Số tiền cần chuyển:</span>
                  <strong className="text-base font-black text-emerald-700">
                    {sePaySession.amount.toLocaleString("vi-VN")}đ
                  </strong>
                </div>
                <div className="flex justify-between items-center text-[#786C65]">
                  <span>Nội dung chuyển khoản:</span>
                  <div className="flex items-center gap-1.5">
                    <strong className="font-mono font-bold text-[#2D2522] bg-white px-2 py-0.5 rounded border border-[#EFE6DC]">
                      {sePaySession.description}
                    </strong>
                    <button
                      type="button"
                      onClick={() => handleCopy(sePaySession.description, "des")}
                      className="p-1 rounded hover:bg-white text-[#786C65]"
                      title="Sao chép nội dung"
                    >
                      {copiedField === "des" ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
                    </button>
                  </div>
                </div>
              </div>

              <div className="p-3.5 rounded-2xl bg-blue-50 border border-blue-200 text-blue-900 text-xs flex items-center justify-center gap-2.5 font-bold">
                <span className="w-2.5 h-2.5 rounded-full bg-blue-500 animate-ping shrink-0" />
                <span>Đang tự động lắng nghe giao dịch TPBank qua SePay...</span>
              </div>
            </div>
          )}

          {/* Confirmed Payment Receipt Card */}
          {paymentMeta.isPaid && order.paymentMethod === "SEPAY" && (
            <div className="bg-emerald-50 p-5 rounded-3xl border border-emerald-200 space-y-2 text-xs text-emerald-950">
              <div className="flex items-center gap-2 font-black text-sm text-emerald-800">
                <ShieldCheck className="w-5 h-5 text-emerald-600" />
                <span>Đã Xác Thực Thanh Toán SePay (TPBank) Thành Công</span>
              </div>
              {order.paidAt && (
                <div className="flex justify-between">
                  <span className="text-emerald-700">Thời điểm thanh toán:</span>
                  <span className="font-mono">{new Date(order.paidAt).toLocaleString("vi-VN")}</span>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Modal Hủy Đơn */}
      {showCancelModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full border border-[#EFE6DC] shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-2 border-b">
              <div className="flex items-center gap-2 text-red-600 font-bold">
                <XCircle className="w-5 h-5" />
                <h3 className="text-base text-[#2D2522]">Hủy Đơn Hàng #{order.orderCode}</h3>
              </div>
              <button
                onClick={() => setShowCancelModal(false)}
                className="text-gray-400 hover:text-gray-600 font-bold text-xs"
              >
                [Đóng]
              </button>
            </div>

            <p className="text-xs text-[#786C65]">
              Đơn hàng chưa bước vào giai đoạn pha chế. Vui lòng chọn lý do nếu bạn muốn hủy đơn:
            </p>

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
                    name="modalCancelReason"
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
                  placeholder="Nhập lý do của bạn..."
                  value={customReason}
                  onChange={(e) => setCustomReason(e.target.value)}
                  className="w-full p-3 rounded-xl border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
              )}
            </div>

            <div className="pt-2 flex items-center justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowCancelModal(false)}
                className="px-4 py-2.5 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#786C65]"
              >
                Không, giữ đơn
              </button>
              <button
                type="button"
                disabled={cancelLoading}
                onClick={handleConfirmCancel}
                className="px-5 py-2.5 rounded-xl bg-red-600 text-white text-xs font-bold hover:bg-red-700 shadow-sm disabled:opacity-50"
              >
                {cancelLoading ? "Đang hủy..." : "Xác nhận Hủy Đơn"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Box Chat với Quản Lý */}
      {showChatModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-lg w-full border border-[#EFE6DC] shadow-2xl flex flex-col h-[520px] overflow-hidden">
            <div className="p-4 border-b border-[#EFE6DC] flex items-center justify-between bg-[#FAF7F2]">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-full bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs">
                  QL
                </div>
                <div>
                  <h3 className="font-extrabold text-sm text-[#2D2522]">Quản Lý Đơn Hàng MilkyVerse</h3>
                  <p className="text-[11px] text-[#786C65]">Hỗ trợ trực tiếp cho đơn #{order.orderCode}</p>
                </div>
              </div>
              <button
                onClick={() => setShowChatModal(false)}
                className="text-gray-400 hover:text-gray-600 text-xs font-bold"
              >
                Đóng [X]
              </button>
            </div>

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

            <form onSubmit={handleSendChat} className="p-3 border-t border-[#EFE6DC] bg-white flex items-center gap-2">
              <input
                type="text"
                placeholder="Nhắn tin cho Quản lý đơn hàng..."
                value={chatInput}
                onChange={(e) => setChatInput(e.target.value)}
                className="flex-1 px-4 py-2.5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <button
                type="submit"
                className="p-2.5 rounded-2xl bg-[#6F4E37] text-white hover:bg-[#4A3324] transition-colors shadow-sm"
              >
                Gửi
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
