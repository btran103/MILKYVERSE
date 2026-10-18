"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import { api } from "@/lib/api";
import confetti from "canvas-confetti";
import {
  CreditCard,
  Banknote,
  Truck,
  Check,
  ArrowLeft,
  AlertCircle,
  Loader2,
  Sparkles,
  QrCode,
  Smartphone,
} from "lucide-react";

export default function CheckoutPage() {
  const router = useRouter();
  const { items, subtotal, clearCart } = useCart();
  const { user } = useAuth();

  const [name, setName] = useState(user?.fullName || "");
  const [phone, setPhone] = useState(user?.phoneNumber || "");
  const [address, setAddress] = useState(user?.address || "");
  const [note, setNote] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("VIETQR");
  const [promoCode, setPromoCode] = useState<string | null>(null);

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!user) {
      router.push("/auth/login?redirect=/checkout");
      return;
    }

    if (!name) setName(user.fullName || "");
    if (!phone) setPhone(user.phoneNumber || "");
    if (!address) setAddress(user.address || "");

    const savedPromo = sessionStorage.getItem("milky_promo_code");
    if (savedPromo) {
      setPromoCode(savedPromo);
    }
  }, [user, router]);

  if (items.length === 0) {
    return (
      <div className="max-w-xl mx-auto px-4 py-20 text-center space-y-4">
        <h2 className="text-xl font-bold text-[#2D2522]">Không có sản phẩm nào để thanh toán</h2>
        <Link
          href="/menu"
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Quay lại thực đơn</span>
        </Link>
      </div>
    );
  }

  const shippingFee = subtotal >= 200000 ? 0 : 15000;

  const handleSubmitOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (!name.trim() || !phone.trim() || !address.trim()) {
      setErrorMessage("Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ nhận hàng.");
      return;
    }

    setLoading(true);

    try {
      const orderPayload = {
        customerName: name.trim(),
        customerPhone: phone.trim(),
        shippingAddress: address.trim(),
        note: note.trim(),
        paymentMethod: paymentMethod,
        promotionCode: promoCode || undefined,
        items: items.map((item) => ({
          productId: item.productId,
          productName: item.name,
          itemType: item.itemType || "DRINK",
          size: item.size || "Standard",
          sugarLevel: item.sugarLevel || "100%",
          iceLevel: item.iceLevel || "100%",
          spicyLevel: item.spicyLevel || "Không cay",
          dippingSauce: item.dippingSauce || "Tương ớt",
          portionSize: item.portionSize || "Tiêu chuẩn",
          quantity: item.quantity,
          isCustomDIY: item.isCustomDIY,
          customNotes: item.customNotes,
          toppings: (item.toppings || []).map((t) => ({
            toppingId: t.toppingId,
            quantity: t.quantity,
          })),
        })),
      };

      const orderRes = await api.createOrder(orderPayload);

      // Celebrate with confetti!
      confetti({
        particleCount: 120,
        spread: 70,
        origin: { y: 0.6 },
        colors: ["#6F4E37", "#F7D6D0", "#C5A059", "#4E7D55"],
      });

      // Clear cart
      clearCart();
      sessionStorage.removeItem("milky_promo_code");

      // Redirect to order details
      router.push(`/orders/${orderRes.id}`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Đã xảy ra lỗi khi tạo đơn hàng.";
      setErrorMessage(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Title */}
      <div>
        <Link
          href="/cart"
          className="inline-flex items-center gap-1.5 text-xs font-bold text-[#6F4E37] mb-2 hover:underline"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Quay lại giỏ hàng</span>
        </Link>
        <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
          Xác Nhận & Thanh Toán Đơn Hàng
        </h1>
      </div>

      {errorMessage && (
        <div className="bg-red-50 text-red-700 border border-red-200 p-4 rounded-2xl text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      <form onSubmit={handleSubmitOrder} className="grid grid-cols-1 lg:grid-cols-12 gap-8">
        {/* Left: Shipping Details & Payment */}
        <div className="lg:col-span-7 space-y-6">
          {/* 1. Recipient Information */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider flex items-center gap-2">
              <Truck className="w-4 h-4 text-[#6F4E37]" />
              <span>1. Thông Tin Nhận Hàng</span>
            </h3>

            <div className="space-y-3 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">
                  Họ và tên người nhận *
                </label>
                <input
                  type="text"
                  required
                  placeholder="Ví dụ: Nguyễn Văn A"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">
                  Số điện thoại liên hệ *
                </label>
                <input
                  type="tel"
                  required
                  placeholder="Ví dụ: 0901234567"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">
                  Địa chỉ giao hàng chi tiết *
                </label>
                <input
                  type="text"
                  required
                  placeholder="Số nhà, tên đường, phường/xã, quận/huyện..."
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">
                  Ghi chú cho shipper & pha chế
                </label>
                <textarea
                  rows={2}
                  placeholder="Giao giờ hành chính, đá để riêng, v.v..."
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  className="w-full px-3.5 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>
            </div>
          </div>

          {/* 2. Payment Method */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider flex items-center gap-2">
              <CreditCard className="w-4 h-4 text-[#6F4E37]" />
              <span>2. Phương Thức Thanh Toán</span>
            </h3>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <button
                type="button"
                onClick={() => setPaymentMethod("VIETQR")}
                className={`p-4 rounded-2xl border text-left transition-all flex flex-col gap-2 ${
                  paymentMethod === "VIETQR"
                    ? "border-[#6F4E37] bg-[#F7D6D0]/30 shadow-sm ring-1 ring-[#6F4E37]"
                    : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                }`}
              >
                <div className="flex items-center gap-2">
                  <QrCode className="w-5 h-5 text-[#6F4E37] shrink-0" />
                  <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-[#4E7D55] text-white">Khuyên dùng</span>
                </div>
                <div>
                  <h4 className="font-bold text-xs text-[#2D2522]">Chuyển khoản VietQR</h4>
                  <p className="text-[10px] text-[#786C65] mt-0.5">
                    Quét mã Napas247 tự động điền số tiền từ mọi app ngân hàng.
                  </p>
                </div>
              </button>

              <button
                type="button"
                onClick={() => setPaymentMethod("COD")}
                className={`p-4 rounded-2xl border text-left transition-all flex flex-col gap-2 ${
                  paymentMethod === "COD"
                    ? "border-[#6F4E37] bg-[#F7D6D0]/30 shadow-sm ring-1 ring-[#6F4E37]"
                    : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                }`}
              >
                <Banknote className="w-5 h-5 text-[#6F4E37] shrink-0" />
                <div>
                  <h4 className="font-bold text-xs text-[#2D2522]">Tiền mặt (COD)</h4>
                  <p className="text-[10px] text-[#786C65] mt-0.5">
                    Thanh toán trực tiếp cho shipper khi nhận trà sữa.
                  </p>
                </div>
              </button>

              <button
                type="button"
                onClick={() => setPaymentMethod("MOMO")}
                className={`p-4 rounded-2xl border text-left transition-all flex flex-col gap-2 ${
                  paymentMethod === "MOMO"
                    ? "border-[#6F4E37] bg-[#F7D6D0]/30 shadow-sm ring-1 ring-[#6F4E37]"
                    : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                }`}
              >
                <Smartphone className="w-5 h-5 text-[#A50064] shrink-0" />
                <div>
                  <h4 className="font-bold text-xs text-[#2D2522]">Ví MoMo</h4>
                  <p className="text-[10px] text-[#786C65] mt-0.5">
                    Thanh toán nhanh qua ví điện tử MoMo.
                  </p>
                </div>
              </button>
            </div>

            {paymentMethod === "VIETQR" && (
              <div className="p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-2 text-xs">
                <div className="flex items-center gap-2 text-[#6F4E37] font-bold">
                  <Sparkles className="w-4 h-4 text-[#C5A059]" />
                  <span>Thông tin chuyển khoản Ngân hàng (Napas247):</span>
                </div>
                <div className="grid grid-cols-2 gap-2 text-[11px] pt-1">
                  <div>
                    <span className="text-[#786C65] block">Ngân hàng:</span>
                    <strong className="text-[#2D2522]">MB Bank (Ngân Hàng Quân Đội)</strong>
                  </div>
                  <div>
                    <span className="text-[#786C65] block">Số tài khoản:</span>
                    <strong className="text-[#6F4E37] font-mono text-xs">0358899888</strong>
                  </div>
                  <div>
                    <span className="text-[#786C65] block">Chủ tài khoản:</span>
                    <strong className="text-[#2D2522]">MILKYVERSE STORE</strong>
                  </div>
                  <div>
                    <span className="text-[#786C65] block">Cần thanh toán:</span>
                    <strong className="text-[#6F4E37]">{(subtotal + shippingFee).toLocaleString("vi-VN")}đ</strong>
                  </div>
                </div>
                <p className="text-[10px] text-[#786C65] italic pt-1">
                  * Ngay sau khi bấm &ldquo;Xác Nhận Đặt Hàng&rdquo;, mã QR VietQR chuẩn Napas247 chứa đúng số tiền và mã đơn hàng sẽ được hiển thị ngay để bạn quét mã thanh toán tức thì.
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Right: Order Snapshot */}
        <div className="lg:col-span-5 space-y-6">
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h3 className="font-extrabold text-[#2D2522] text-sm">
              Đơn hàng của bạn ({items.length} món)
            </h3>

            <div className="space-y-3 max-h-64 overflow-y-auto pr-1">
              {items.map((item) => (
                <div key={item.id} className="flex justify-between items-start text-xs border-b border-[#FAF7F2] pb-2">
                  <div>
                    <p className="font-bold text-[#2D2522]">{item.name} x{item.quantity}</p>
                    <p className="text-[11px] text-[#786C65]">Size {item.size} • {item.sugarLevel} đường • {item.iceLevel} đá</p>
                    {item.toppings?.length > 0 && (
                      <p className="text-[10px] text-[#6F4E37]">+{item.toppings.map((t) => t.name).join(", ")}</p>
                    )}
                  </div>
                  <span className="font-black text-[#6F4E37]">
                    {item.totalPrice.toLocaleString("vi-VN")}đ
                  </span>
                </div>
              ))}
            </div>

            <div className="space-y-2 text-xs text-[#786C65] pt-2 border-t border-[#EFE6DC]">
              <div className="flex justify-between">
                <span>Tạm tính:</span>
                <span className="font-bold text-[#2D2522]">{subtotal.toLocaleString("vi-VN")}đ</span>
              </div>
              {promoCode && (
                <div className="flex justify-between text-[#4E7D55] font-semibold">
                  <span>Mã ưu đãi ({promoCode}):</span>
                  <span>Đã áp dụng</span>
                </div>
              )}
              <div className="flex justify-between">
                <span>Phí vận chuyển:</span>
                <span className="font-bold text-[#2D2522]">{shippingFee === 0 ? "Miễn phí" : `${shippingFee.toLocaleString("vi-VN")}đ`}</span>
              </div>
            </div>

            <div className="pt-2 border-t border-[#EFE6DC] flex justify-between items-baseline">
              <span className="font-bold text-sm text-[#2D2522]">Tổng thanh toán:</span>
              <span className="text-2xl font-black text-[#6F4E37]">
                {(subtotal + shippingFee).toLocaleString("vi-VN")}đ
              </span>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-4 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {loading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Đang xử lý đơn hàng...</span>
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  <span>Xác Nhận Đặt Hàng</span>
                </>
              )}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}
