"use client";

import React, { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import { api } from "@/lib/api";
import {
  ShoppingBag,
  Trash2,
  Plus,
  Minus,
  ArrowRight,
  Ticket,
  Check,
  AlertCircle,
  CupSoda,
} from "lucide-react";

export default function CartPage() {
  const router = useRouter();
  const { items, updateQuantity, removeItem, clearCart, subtotal, openAuthModal } = useCart();
  const { user } = useAuth();

  const [promoCode, setPromoCode] = useState("");
  const [discountAmount, setDiscountAmount] = useState(0);
  const [promoMessage, setPromoMessage] = useState<{ text: string; success: boolean } | null>(null);
  const [applyingPromo, setApplyingPromo] = useState(false);

  const shippingFee = subtotal >= 200000 || discountAmount >= subtotal ? 0 : 15000;
  const finalTotal = Math.max(0, subtotal - discountAmount + shippingFee);

  const handleApplyPromo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!promoCode.trim()) return;

    setApplyingPromo(true);
    setPromoMessage(null);

    try {
      const res = await api.validatePromo(promoCode.trim().toUpperCase(), subtotal);
      if (res.isValid) {
        setDiscountAmount(res.discountAmount);
        setPromoMessage({ text: res.message, success: true });
      } else {
        setDiscountAmount(0);
        setPromoMessage({ text: res.message, success: false });
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Mã ưu đãi không hợp lệ.";
      setDiscountAmount(0);
      setPromoMessage({ text: msg, success: false });
    } finally {
      setApplyingPromo(false);
    }
  };

  const handleProceedToCheckout = () => {
    if (!user) {
      openAuthModal("Vui lòng đăng nhập hoặc đăng ký tài khoản để tiến hành đặt hàng!");
      return;
    }

    // Store promo code in sessionStorage for checkout
    if (discountAmount > 0 && promoCode) {
      sessionStorage.setItem("milky_promo_code", promoCode.toUpperCase());
    } else {
      sessionStorage.removeItem("milky_promo_code");
    }
    router.push("/checkout");
  };

  if (items.length === 0) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center space-y-6">
        <div className="w-20 h-20 rounded-full bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-center mx-auto text-[#6F4E37] shadow-inner">
          <ShoppingBag className="w-10 h-10" />
        </div>
        <div className="space-y-2">
          <h2 className="text-2xl font-black text-[#2D2522]">Giỏ hàng của bạn đang trống</h2>
          <p className="text-sm text-[#786C65]">
            Hãy dạo quanh ngân hà MilkyVerse và chọn ngay những ly trà sữa tươi thơm ngon nhất nhé!
          </p>
        </div>
        <div className="flex flex-wrap items-center justify-center gap-4 pt-2">
          <Link
            href="/menu"
            className="px-6 py-3 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all"
          >
            Khám phá thực đơn
          </Link>
          <Link
            href="/diy"
            className="px-6 py-3 rounded-2xl bg-white border border-[#6F4E37] text-[#6F4E37] font-bold text-sm hover:bg-[#FAF7F2] transition-all"
          >
            Tự pha chế trong DIY Studio
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Title */}
      <div className="flex items-center justify-between pb-6 border-b border-[#EFE6DC]">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
            Giỏ Hàng Của Bạn
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            {items.length} món đồ uống độc bản đang chờ được pha chế
          </p>
        </div>
        <button
          onClick={clearCart}
          className="text-xs text-red-600 hover:underline font-semibold"
        >
          Xóa toàn bộ giỏ hàng
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        {/* Left: Cart Items List */}
        <div className="lg:col-span-8 space-y-4">
          {items.map((item) => (
            <div
              key={item.id}
              className="bg-white rounded-3xl p-5 border border-[#EFE6DC] shadow-sm flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4"
            >
              {/* Product Info */}
              <div className="flex items-center gap-4">
                <div className="w-20 h-20 rounded-2xl overflow-hidden bg-[#FAF7F2] shrink-0 border border-[#EFE6DC]">
                  {item.imageUrl ? (
                    <img
                      src={item.imageUrl}
                      alt={item.name}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-[#6F4E37]">
                      <CupSoda className="w-8 h-8" />
                    </div>
                  )}
                </div>

                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <h3 className="font-extrabold text-[#2D2522] text-sm sm:text-base">
                      {item.name}
                    </h3>
                    {item.isCustomDIY && (
                      <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#F7D6D0] text-[#6F4E37]">
                        DIY
                      </span>
                    )}
                  </div>

                  {/* Specifications */}
                  {item.itemType === "FOOD" ? (
                    <div className="space-y-0.5">
                      <p className="text-xs text-[#786C65]">
                        {item.portionSize || "Phần tiêu chuẩn"}
                        {item.spicyLevel && ` • ${item.spicyLevel}`}
                        {item.dippingSauce && ` • Chấm: ${item.dippingSauce}`}
                      </p>
                    </div>
                  ) : item.itemType === "COMBO" ? (
                    <div className="space-y-0.5">
                      <p className="text-xs text-[#6F4E37] font-semibold">
                        {item.portionSize || "Combo ăn uống trọn gói"}
                      </p>
                      <p className="text-xs text-[#786C65]">
                        {item.spicyLevel ? `Mức cay: ${item.spicyLevel} • ` : ""}{item.sugarLevel || "100%"} đường • {item.iceLevel || "100%"} đá
                      </p>
                    </div>
                  ) : (
                    <p className="text-xs text-[#786C65]">
                      Size {item.size} • {item.sugarLevel} đường • {item.iceLevel} đá
                    </p>
                  )}

                  {/* Toppings summary */}
                  {item.toppings && item.toppings.length > 0 && (
                    <p className="text-[11px] text-[#6F4E37] font-semibold">
                      Topping / Thêm: {item.toppings.map((t) => t.name).join(", ")}
                    </p>
                  )}

                  <p className="text-sm font-black text-[#6F4E37] sm:hidden pt-1">
                    {item.totalPrice.toLocaleString("vi-VN")}đ
                  </p>
                </div>
              </div>

              {/* Quantity controller & actions */}
              <div className="flex items-center justify-between w-full sm:w-auto gap-6 pt-2 sm:pt-0 border-t sm:border-t-0 border-[#FAF7F2]">
                <div className="flex items-center border border-[#EFE6DC] rounded-xl bg-white p-1">
                  <button
                    onClick={() => updateQuantity(item.id, -1)}
                    className="w-7 h-7 rounded-lg hover:bg-[#FAF7F2] flex items-center justify-center text-[#2D2522]"
                  >
                    <Minus className="w-3 h-3" />
                  </button>
                  <span className="w-8 text-center font-bold text-xs text-[#2D2522]">
                    {item.quantity}
                  </span>
                  <button
                    onClick={() => updateQuantity(item.id, 1)}
                    className="w-7 h-7 rounded-lg hover:bg-[#FAF7F2] flex items-center justify-center text-[#2D2522]"
                  >
                    <Plus className="w-3 h-3" />
                  </button>
                </div>

                <div className="hidden sm:block text-right">
                  <span className="text-base font-black text-[#6F4E37] block">
                    {item.totalPrice.toLocaleString("vi-VN")}đ
                  </span>
                  <span className="text-[11px] text-[#786C65]">
                    {item.unitPrice.toLocaleString("vi-VN")}đ / ly
                  </span>
                </div>

                <button
                  onClick={() => removeItem(item.id)}
                  className="p-2 text-[#786C65] hover:text-red-600 transition-colors"
                  title="Xóa món"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Right: Summary & Checkout */}
        <div className="lg:col-span-4 space-y-6">
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-5">
            <h3 className="font-extrabold text-[#2D2522] text-base">
              Tóm Tắt Đơn Hàng
            </h3>

            {/* Voucher input form */}
            <form onSubmit={handleApplyPromo} className="space-y-2">
              <label className="text-xs font-bold text-[#786C65] flex items-center gap-1.5">
                <Ticket className="w-3.5 h-3.5 text-[#6F4E37]" />
                <span>Mã Ưu Đãi (Voucher)</span>
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="MILKYNEW, FREESHIP..."
                  value={promoCode}
                  onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
                  className="flex-1 px-3 py-2 text-xs uppercase font-bold rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
                <button
                  type="submit"
                  disabled={applyingPromo || !promoCode.trim()}
                  className="px-4 py-2 bg-[#6F4E37] text-white text-xs font-bold rounded-xl hover:bg-[#4A3324] disabled:opacity-50 transition-colors"
                >
                  Áp dụng
                </button>
              </div>

              {promoMessage && (
                <div
                  className={`text-xs p-2.5 rounded-xl flex items-center gap-2 ${
                    promoMessage.success
                      ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                      : "bg-red-50 text-red-700 border border-red-200"
                  }`}
                >
                  {promoMessage.success ? (
                    <Check className="w-3.5 h-3.5 shrink-0" />
                  ) : (
                    <AlertCircle className="w-3.5 h-3.5 shrink-0" />
                  )}
                  <span>{promoMessage.text}</span>
                </div>
              )}
            </form>

            <div className="h-px bg-[#FAF7F2]" />

            {/* Calculations */}
            <div className="space-y-2.5 text-xs text-[#786C65]">
              <div className="flex justify-between">
                <span>Tạm tính ({items.length} món):</span>
                <span className="font-bold text-[#2D2522]">
                  {subtotal.toLocaleString("vi-VN")}đ
                </span>
              </div>

              {discountAmount > 0 && (
                <div className="flex justify-between text-[#4E7D55] font-semibold">
                  <span>Giảm giá voucher:</span>
                  <span>-{discountAmount.toLocaleString("vi-VN")}đ</span>
                </div>
              )}

              <div className="flex justify-between">
                <span>Phí giao hàng:</span>
                <span className="font-bold text-[#2D2522]">
                  {shippingFee === 0 ? "Miễn phí" : `${shippingFee.toLocaleString("vi-VN")}đ`}
                </span>
              </div>

              {subtotal < 200000 && (
                <p className="text-[10px] text-[#C5A059] font-medium pt-1">
                  💡 Mẹo: Mua thêm {(200000 - subtotal).toLocaleString("vi-VN")}đ để được freeship toàn thành phố!
                </p>
              )}
            </div>

            <div className="h-px bg-[#EFE6DC]" />

            <div className="flex items-baseline justify-between">
              <span className="text-sm font-bold text-[#2D2522]">Tổng thanh toán:</span>
              <span className="text-2xl font-black text-[#6F4E37]">
                {finalTotal.toLocaleString("vi-VN")}đ
              </span>
            </div>

            <button
              onClick={handleProceedToCheckout}
              className="w-full py-4 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2"
            >
              <span>Tiến Hành Đặt Hàng</span>
              <ArrowRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
