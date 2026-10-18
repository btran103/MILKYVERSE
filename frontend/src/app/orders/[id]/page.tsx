"use client";

export const dynamic = "force-dynamic";

import React, { useEffect, useState, use } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { OrderResponse } from "@/types";
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
  QrCode,
} from "lucide-react";

export default function OrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const resolvedParams = use(params);
  const [order, setOrder] = useState<OrderResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [notifiedTransfer, setNotifiedTransfer] = useState(false);

  const handleCopy = (text: string, field: string) => {
    navigator.clipboard.writeText(text);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  useEffect(() => {
    async function fetchOrder() {
      try {
        const data = await api.getOrderById(parseInt(resolvedParams.id, 10));
        setOrder(data);
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : "Không tìm thấy đơn hàng.";
        setError(msg);
      } finally {
        setLoading(false);
      }
    }
    fetchOrder();
  }, [resolvedParams.id]);

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

  // 5 Status Steps
  const steps = [
    { key: "Pending", label: "Chờ xác nhận", icon: Clock },
    { key: "Confirmed", label: "Đã xác nhận", icon: CheckCircle2 },
    { key: "Preparing", label: "Đang pha chế", icon: Package },
    { key: "Delivering", label: "Đang giao", icon: Truck },
    { key: "Completed", label: "Hoàn tất", icon: Check },
  ];

  const statusIndexMap: Record<string, number> = {
    Pending: 0,
    Confirmed: 1,
    Preparing: 2,
    Delivering: 3,
    Completed: 4,
  };

  const currentStepIndex = statusIndexMap[order.status] ?? 0;
  const isCancelled = order.status === "Cancelled";

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
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
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] flex items-center gap-3">
            <span>Mã Đơn: #{order.orderCode}</span>
            <span
              className={`text-xs font-bold px-2.5 py-1 rounded-full ${
                order.status === "Completed"
                  ? "bg-emerald-100 text-emerald-800"
                  : isCancelled
                  ? "bg-red-100 text-red-800"
                  : "bg-amber-100 text-amber-800"
              }`}
            >
              {order.status}
            </span>
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Đặt lúc: {new Date(order.createdAt).toLocaleString("vi-VN")}
          </p>
        </div>
      </div>

      {/* Status Progress Stepper */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm">
        {isCancelled ? (
          <div className="flex items-center gap-3 text-red-700 bg-red-50 p-4 rounded-2xl border border-red-200">
            <XCircle className="w-6 h-6 shrink-0" />
            <div>
              <h4 className="font-bold text-sm">Đơn hàng này đã bị hủy</h4>
              <p className="text-xs text-red-600">
                Nếu bạn cần hỗ trợ, vui lòng gọi hotline 1900 888 999 của MilkyVerse.
              </p>
            </div>
          </div>
        ) : (
          <div className="relative">
            {/* Step Line */}
            <div className="hidden sm:block absolute top-1/2 left-8 right-8 -translate-y-1/2 h-1 bg-[#FAF7F2] -z-0" />

            <div className="grid grid-cols-2 sm:grid-cols-5 gap-4 relative z-10">
              {steps.map((st, idx) => {
                const Icon = st.icon;
                const isPassed = idx <= currentStepIndex;
                const isCurrent = idx === currentStepIndex;

                return (
                  <div key={st.key} className="flex flex-col items-center text-center space-y-2">
                    <div
                      className={`w-12 h-12 rounded-2xl flex items-center justify-center font-bold text-sm shadow-md transition-all ${
                        isCurrent
                          ? "bg-[#6F4E37] text-white ring-4 ring-[#F7D6D0]"
                          : isPassed
                          ? "bg-[#4E7D55] text-white"
                          : "bg-[#FAF7F2] text-[#786C65] border border-[#EFE6DC]"
                      }`}
                    >
                      <Icon className="w-5 h-5" />
                    </div>
                    <span
                      className={`text-xs font-bold ${
                        isCurrent
                          ? "text-[#6F4E37]"
                          : isPassed
                          ? "text-[#2D2522]"
                          : "text-[#786C65]"
                      }`}
                    >
                      {st.label}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Details Grid */}
      <div className="grid grid-cols-1 md:grid-cols-12 gap-8 items-start">
        {/* Left: Items Snapshot */}
        <div className="md:col-span-7 bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider">
            Danh sách đồ uống đã lưu snapshot
          </h3>

          <div className="space-y-4">
            {order.items.map((item) => (
              <div
                key={item.id}
                className="p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-2"
              >
                <div className="flex justify-between items-start">
                  <div>
                    <h4 className="font-extrabold text-sm text-[#2D2522]">
                      {item.productName}
                    </h4>
                    <span className="text-xs text-[#786C65]">
                      Số lượng: {item.quantity} ly
                    </span>
                  </div>
                  <span className="font-black text-[#6F4E37] text-sm">
                    {item.totalPrice.toLocaleString("vi-VN")}đ
                  </span>
                </div>

                <div className="text-xs text-[#786C65] space-y-0.5">
                  <p>
                    <strong>Cấu hình:</strong> Size {item.size} • {item.sugarLevel} đường • {item.iceLevel} đá
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
            ))}
          </div>
        </div>

        {/* Right: Customer & Financial Recap */}
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

          {/* Payment & Total */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3 text-xs">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider mb-2">
              Thanh Toán
            </h3>
            <div className="flex justify-between text-[#786C65]">
              <span>Phương thức:</span>
              <span className="font-bold text-[#2D2522]">{order.paymentMethod}</span>
            </div>
            <div className="flex justify-between text-[#786C65]">
              <span>Trạng thái thanh toán:</span>
              <span
                className={`font-bold px-2 py-0.5 rounded-md ${
                  order.paymentStatus === "Paid"
                    ? "bg-emerald-100 text-emerald-800"
                    : "bg-amber-100 text-amber-800"
                }`}
              >
                {order.paymentStatus === "Paid" ? "Đã thanh toán" : "Chưa thanh toán"}
              </span>
            </div>
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

          {/* VietQR Bank Payment Card (Real money payment via Napas247) */}
          {order.paymentStatus !== "Paid" && order.paymentMethod !== "COD" && (
            <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4 text-center">
              <div className="flex items-center justify-between pb-2 border-b border-[#FAF7F2]">
                <div className="flex items-center gap-2 text-xs font-black text-[#2D2522]">
                  <QrCode className="w-4 h-4 text-[#6F4E37]" />
                  <span>Thanh Toán VietQR (Napas247)</span>
                </div>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800">
                  Chờ chuyển khoản
                </span>
              </div>

              <p className="text-xs text-[#786C65]">
                Quét mã QR bằng ứng dụng ngân hàng bất kỳ để chuyển khoản tiền thật tức thì (hệ thống tự động điền số tiền và mã đơn):
              </p>

              {/* Dynamic VietQR Image */}
              <div className="relative mx-auto w-56 h-56 bg-white p-2 rounded-2xl border-2 border-[#6F4E37] shadow-md flex items-center justify-center">
                <img
                  src={`https://img.vietqr.io/image/MB-0358899888-compact2.png?amount=${order.totalAmount}&addInfo=MILKY${order.orderCode}&accountName=MILKYVERSE%20STORE`}
                  alt="Mã VietQR thanh toán MilkyVerse"
                  className="w-full h-full object-contain"
                />
              </div>

              {/* Bank Transfer Details Table */}
              <div className="bg-[#FAF7F2] p-3.5 rounded-2xl border border-[#EFE6DC] text-left text-xs space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-[#786C65]">Ngân hàng:</span>
                  <span className="font-bold text-[#2D2522]">MB Bank (Quân Đội)</span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#786C65]">Số tài khoản:</span>
                  <div className="flex items-center gap-1.5">
                    <span className="font-mono font-black text-[#6F4E37]">0358899888</span>
                    <button
                      onClick={() => handleCopy("0358899888", "account")}
                      className="p-1 rounded-md hover:bg-white text-[#786C65]"
                      title="Sao chép số tài khoản"
                    >
                      {copiedField === "account" ? (
                        <Check className="w-3.5 h-3.5 text-emerald-600" />
                      ) : (
                        <Copy className="w-3.5 h-3.5" />
                      )}
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#786C65]">Chủ tài khoản:</span>
                  <span className="font-bold text-[#2D2522]">MILKYVERSE STORE</span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#786C65]">Số tiền:</span>
                  <span className="font-black text-[#6F4E37] text-sm">
                    {order.totalAmount.toLocaleString("vi-VN")}đ
                  </span>
                </div>

                <div className="flex items-center justify-between pt-1 border-t border-[#EFE6DC]">
                  <span className="text-[#786C65]">Nội dung CK:</span>
                  <div className="flex items-center gap-1.5">
                    <span className="font-mono font-bold text-[#2D2522]">MILKY{order.orderCode}</span>
                    <button
                      onClick={() => handleCopy(`MILKY${order.orderCode}`, "content")}
                      className="p-1 rounded-md hover:bg-white text-[#786C65]"
                      title="Sao chép nội dung chuyển khoản"
                    >
                      {copiedField === "content" ? (
                        <Check className="w-3.5 h-3.5 text-emerald-600" />
                      ) : (
                        <Copy className="w-3.5 h-3.5" />
                      )}
                    </button>
                  </div>
                </div>
              </div>

              {notifiedTransfer ? (
                <div className="p-3 rounded-2xl bg-emerald-50 text-emerald-800 border border-emerald-200 text-xs flex items-center justify-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                  <span>Đã ghi nhận! Nhân viên đang kiểm tra giao dịch và sẽ chuyển đơn sang pha chế ngay.</span>
                </div>
              ) : (
                <button
                  onClick={() => setNotifiedTransfer(true)}
                  className="w-full py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-1.5"
                >
                  <Check className="w-3.5 h-3.5" />
                  <span>Tôi đã chuyển khoản xong</span>
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
