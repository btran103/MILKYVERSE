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
  Clock,
  ShieldCheck,
  Ticket,
  RefreshCw,
  Crown,
  Home,
  Building2,
  GraduationCap,
  MapPin,
} from "lucide-react";
import { getSavedAddresses, SavedAddress } from "@/lib/addresses";
import { getLoyaltyOverview, saveLoyaltyPoints } from "@/lib/loyalty";

export default function CheckoutPage() {
  const router = useRouter();
  const { items, subtotal, clearCart } = useCart();
  const { user } = useAuth();

  const [name, setName] = useState(user?.fullName || "");
  const [phone, setPhone] = useState(user?.phoneNumber || "");
  const [address, setAddress] = useState(user?.address || "");
  const [note, setNote] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("VIETQR");
  const [promoCode, setPromoCode] = useState<string>("");
  const [discountAmount, setDiscountAmount] = useState<number>(0);
  const [promoMsg, setPromoMsg] = useState<{ text: string; error?: boolean } | null>(null);

  // Saved Addresses & Loyalty
  const [savedAddresses, setSavedAddresses] = useState<SavedAddress[]>([]);
  const [selectedAddrId, setSelectedAddrId] = useState<string | null>(null);
  const [loyaltyPoints, setLoyaltyPoints] = useState<number>(0);
  const [usePoints, setUsePoints] = useState<boolean>(false);

  const [timeLeft, setTimeLeft] = useState(120); // 2 minutes transfer timer
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // 2-minute countdown for online transfer (VIETQR or MOMO/ZaloPay)
  useEffect(() => {
    if (paymentMethod === "COD") return;
    const interval = setInterval(() => {
      setTimeLeft((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(interval);
  }, [paymentMethod]);

  const handleApplyPromoCode = async (codeToApply: string) => {
    if (!codeToApply.trim()) return;
    try {
      const res = await api.validatePromo(codeToApply.trim().toUpperCase(), subtotal);
      if (res.isValid) {
        setDiscountAmount(res.discountAmount);
        setPromoCode(codeToApply.trim().toUpperCase());
        setPromoMsg({ text: res.message });
      } else {
        setDiscountAmount(0);
        setPromoMsg({ text: res.message || "Mã không hợp lệ", error: true });
      }
    } catch {
      setDiscountAmount(0);
      setPromoMsg({ text: "Lỗi kiểm tra mã giảm giá", error: true });
    }
  };

  useEffect(() => {
    if (!user) {
      router.push("/auth/login?redirect=/checkout");
      return;
    }

    // Load saved addresses and populate default
    const addrs = getSavedAddresses(user.id, user.address, user.fullName, user.phoneNumber);
    setSavedAddresses(addrs);
    const def = addrs.find((a) => a.isDefault) || addrs[0];
    if (def) {
      setSelectedAddrId(def.id);
      setName(def.recipientName);
      setPhone(def.phoneNumber);
      setAddress(def.addressDetail);
    } else {
      if (!name) setName(user.fullName || "");
      if (!phone) setPhone(user.phoneNumber || "");
      if (!address) setAddress(user.address || "");
    }

    // Load loyalty points
    const loy = getLoyaltyOverview(user.id);
    setLoyaltyPoints(loy.points);

    const savedPromo = sessionStorage.getItem("milky_promo_code");
    if (savedPromo) {
      setPromoCode(savedPromo);
      handleApplyPromoCode(savedPromo);
    }
  }, [user, router]);

  // Nếu người dùng có vai trò không được đặt món (Shipper, Thủ kho, Quản lý/Admin)
  if (user && user.role !== "Customer" && user.role !== "Staff") {
    return (
      <div className="max-w-xl mx-auto px-4 py-20 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-amber-50 border border-amber-200 text-amber-700 flex items-center justify-center mx-auto">
          <AlertCircle className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-bold text-[#2D2522]">Không có quyền đặt món</h2>
        <p className="text-xs text-[#786C65] leading-relaxed">
          Tài khoản với vai trò <strong>{user.role}</strong> không có quyền đặt món. Chỉ tài khoản <strong>Khách Hàng</strong> và <strong>Nhân Viên</strong> mới có quyền đặt món.
        </p>
        <div className="pt-2">
          <Link
            href={
              user.role === "DeliveryStaff"
                ? "/delivery"
                : user.role === "WarehouseStaff"
                ? "/warehouse"
                : "/admin"
            }
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors"
          >
            Quay lại trang làm việc
          </Link>
        </div>
      </div>
    );
  }

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

  const maxPointsCanUse = Math.min(loyaltyPoints, Math.floor(Math.max(0, subtotal - discountAmount) / 1000));
  const pointsDiscount = usePoints ? maxPointsCanUse * 1000 : 0;
  const shippingFee = subtotal >= 200000 || (discountAmount + pointsDiscount) >= subtotal ? 0 : 15000;
  const finalTotal = Math.max(0, subtotal - discountAmount - pointsDiscount + shippingFee);

  const formatTimer = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  };

  const vietQrUrl = `https://img.vietqr.io/image/MB-0358899888-compact.png?amount=${finalTotal}&addInfo=MILKYVERSE%20${encodeURIComponent(
    phone || "KHACH"
  )}&accountName=MILKYVERSE%20STORE`;

  const handleSubmitOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (user && user.role !== "Customer" && user.role !== "Staff") {
      setErrorMessage(`Tài khoản vai trò ${user.role} không có quyền đặt món. Chỉ Khách Hàng và Nhân Viên mới được phép đặt món.`);
      return;
    }

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

      // Deduct used loyalty points & add new earned points (1 pt per 10k subtotal)
      if (user) {
        const ptsUsed = usePoints ? maxPointsCanUse : 0;
        const ptsEarned = Math.floor(subtotal / 10000);
        saveLoyaltyPoints(user.id, Math.max(0, loyaltyPoints - ptsUsed + ptsEarned));
      }

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

      {/* Multi-role Verification Banner */}
      <div className="p-4 sm:p-5 rounded-3xl bg-amber-50 border border-amber-200/80 space-y-3">
        <div className="flex items-center gap-2 text-amber-950 font-extrabold text-xs uppercase tracking-wider">
          <ShieldCheck className="w-4 h-4 text-[#C5A059]" />
          <span>Quy Trình Xác Thực Đa Vai Trò (Multi-Role Verification)</span>
        </div>
        <p className="text-[11px] text-amber-800 leading-relaxed">
          Đơn hàng được phân định vai trò độc lập & minh bạch: Khách hàng đặt món &gt; Nhân viên quầy xác nhận & đối soát &gt; Bếp trưởng nấu chuẩn công thức &gt; Quản lý/Admin giám sát &gt; Shipper giao tận tay &gt; Khách hàng nghiệm thu.
        </p>
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-2 text-center text-[10px]">
          <div className="bg-white p-2.5 rounded-xl border border-amber-200 shadow-2xs">
            <span className="font-black text-[#6F4E37] block">1. Khách Hàng</span>
            <span className="text-[#786C65] mt-0.5 block">Tạo đơn & CK trong 2 phút</span>
          </div>
          <div className="bg-white p-2.5 rounded-xl border border-amber-200 shadow-2xs">
            <span className="font-black text-[#6F4E37] block">2. NV Quầy</span>
            <span className="text-[#786C65] mt-0.5 block">Xác thực thanh toán</span>
          </div>
          <div className="bg-white p-2.5 rounded-xl border border-amber-200 shadow-2xs">
            <span className="font-black text-[#6F4E37] block">3. NV Bếp</span>
            <span className="text-[#786C65] mt-0.5 block">Pha chế & trừ kho FEFO</span>
          </div>
          <div className="bg-white p-2.5 rounded-xl border border-amber-200 shadow-2xs">
            <span className="font-black text-[#6F4E37] block">4. Shipper</span>
            <span className="text-[#786C65] mt-0.5 block">Giao hàng tận nơi</span>
          </div>
          <div className="bg-white p-2.5 rounded-xl border border-amber-200 shadow-2xs">
            <span className="font-black text-emerald-700 block">5. Hoàn Tất</span>
            <span className="text-[#786C65] mt-0.5 block">Khách nhận & đánh giá ⭐</span>
          </div>
        </div>
      </div>

      <form onSubmit={handleSubmitOrder} className="grid grid-cols-1 lg:grid-cols-12 gap-8">
        {/* Left: Shipping Details & Payment */}
        <div className="lg:col-span-7 space-y-6">
          {/* 1. Recipient Information */}
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider flex items-center gap-2">
              <Truck className="w-4 h-4 text-[#6F4E37]" />
              <span>1. Thông Tin Nhận Hàng</span>
            </h3>

            {/* Quick Saved Address Pills */}
            {savedAddresses.length > 0 && (
              <div className="space-y-1.5 pb-2 border-b border-[#FAF7F2]">
                <label className="font-bold text-[#786C65] text-[11px] block">
                  Chọn nhanh từ Sổ địa chỉ của bạn:
                </label>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {savedAddresses.map((a) => (
                    <button
                      key={a.id}
                      type="button"
                      onClick={() => {
                        setSelectedAddrId(a.id);
                        setName(a.recipientName);
                        setPhone(a.phoneNumber);
                        setAddress(a.addressDetail);
                      }}
                      className={`p-2.5 rounded-xl border text-left transition-all ${
                        selectedAddrId === a.id
                          ? "border-[#6F4E37] bg-[#FAF7F2] ring-1 ring-[#6F4E37]"
                          : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                      }`}
                    >
                      <div className="flex items-center justify-between text-[11px] font-bold">
                        <span className="flex items-center gap-1 text-[#6F4E37]">
                          {a.label === "Nhà riêng" ? <Home className="w-3 h-3" /> : a.label === "Văn phòng" ? <Building2 className="w-3 h-3" /> : <MapPin className="w-3 h-3" />}
                          {a.label}
                        </span>
                        {a.isDefault && (
                          <span className="text-[9px] bg-emerald-50 text-emerald-700 px-1.5 py-0.2 rounded-full">
                            Mặc định
                          </span>
                        )}
                      </div>
                      <p className="text-[11px] font-bold text-[#2D2522] mt-0.5 truncate">{a.recipientName} • {a.phoneNumber}</p>
                      <p className="text-[10px] text-[#786C65] truncate">{a.addressDetail}</p>
                    </button>
                  ))}
                </div>
              </div>
            )}

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
                <Smartphone className="w-5 h-5 text-[#0068FF] shrink-0" />
                <div>
                  <h4 className="font-bold text-xs text-[#2D2522]">ZaloPay / Ví Đa Năng</h4>
                  <p className="text-[10px] text-[#786C65] mt-0.5">
                    QR nhận tiền từ ZaloPay, MoMo &amp; 50+ ứng dụng ngân hàng.
                  </p>
                </div>
              </button>
            </div>

            {(paymentMethod === "VIETQR" || paymentMethod === "MOMO") && (
              <div className="p-4 rounded-3xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-3 text-xs">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-[#6F4E37] font-bold">
                    <Sparkles className="w-4 h-4 text-[#C5A059]" />
                    <span>
                      {paymentMethod === "VIETQR"
                        ? "Mã QR TPBank VietQR Chính Chủ (Xác thực Ngrok):"
                        : "Mã QR ZaloPay / Đa Ngân Hàng Chính Chủ (Xác thực Ngrok):"}
                    </span>
                  </div>
                  <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-red-50 text-red-700 font-mono font-bold text-xs border border-red-200">
                    <Clock className="w-3.5 h-3.5" />
                    <span>{formatTimer(timeLeft)}</span>
                    <button
                      type="button"
                      onClick={() => setTimeLeft(120)}
                      title="Làm mới thời gian"
                      className="ml-1 p-0.5 hover:opacity-70"
                    >
                      <RefreshCw className="w-3 h-3" />
                    </button>
                  </div>
                </div>

                <div className="flex flex-col sm:flex-row items-center gap-4 bg-white p-4 rounded-2xl border border-[#EFE6DC]">
                  <div className="w-48 h-56 bg-white p-1.5 rounded-2xl border-2 border-[#6F4E37]/30 shrink-0 shadow-sm flex items-center justify-center overflow-hidden">
                    <img
                      src={paymentMethod === "VIETQR" ? "/images/qr-tpbank.png" : "/images/qr-zalopay.png"}
                      alt={paymentMethod === "VIETQR" ? "TPBank VietQR HA BAO TRAN" : "ZaloPay QR HA BAO TRAN"}
                      className="w-full h-full object-contain rounded-xl"
                    />
                  </div>

                  <div className="space-y-2 text-[11px] w-full">
                    <div>
                      <span className="text-[#786C65] block text-[10px]">
                        {paymentMethod === "VIETQR" ? "Ngân hàng thụ hưởng:" : "Cổng thanh toán:"}
                      </span>
                      <strong className="text-[#2D2522]">
                        {paymentMethod === "VIETQR"
                          ? "TPBank (Ngân Hàng Tiên Phong - VietQR Napas247)"
                          : "ZaloPay / VietQR Đa Năng (Hỗ trợ 50+ Ngân hàng & Ví)"}
                      </strong>
                    </div>
                    <div>
                      <span className="text-[#786C65] block text-[10px]">Số tài khoản nhận:</span>
                      <strong className="text-[#6F4E37] font-mono text-sm tracking-wider">0000 0107 071</strong>
                    </div>
                    <div>
                      <span className="text-[#786C65] block text-[10px]">Chủ tài khoản xác thực:</span>
                      <strong className="text-[#2D2522] text-sm">HA BAO TRAN</strong>
                    </div>
                    <div>
                      <span className="text-[#786C65] block text-[10px]">Số tiền cần thanh toán:</span>
                      <strong className="text-base font-black text-[#6F4E37]">
                        {finalTotal.toLocaleString("vi-VN")}đ
                      </strong>
                    </div>
                  </div>
                </div>

                <p className="text-[10px] text-[#786C65] italic leading-tight">
                  * Sau khi bấm <strong>Xác Nhận Đặt Hàng</strong>, hệ thống sẽ chuyển sang cổng <strong>Đếm Ngược Xác Thực Quyền Thanh Toán Online qua Ngrok Webhook</strong>.
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

            {/* Voucher input block */}
            <div className="pt-2 border-t border-[#EFE6DC] space-y-2">
              <label className="text-xs font-bold text-[#786C65] flex items-center gap-1.5">
                <Ticket className="w-3.5 h-3.5 text-[#6F4E37]" />
                <span>Mã Ưu Đãi (Voucher)</span>
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="MILKYNEW, #MK2095F54..."
                  value={promoCode}
                  onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
                  className="flex-1 px-3 py-2 text-xs uppercase font-bold rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
                <button
                  type="button"
                  onClick={() => handleApplyPromoCode(promoCode)}
                  className="px-3.5 py-2 bg-[#6F4E37] text-white text-xs font-bold rounded-xl hover:bg-[#4A3324] transition-colors"
                >
                  Áp dụng
                </button>
              </div>

              {/* Quick voucher chips */}
              <div className="flex flex-wrap items-center gap-1.5 pt-0.5">
                <button
                  type="button"
                  onClick={() => {
                    setPromoCode("#MK2095F54");
                    handleApplyPromoCode("#MK2095F54");
                  }}
                  className="text-[10px] font-bold px-2 py-0.5 rounded-lg bg-[#FAF7F2] border border-[#EFE6DC] text-[#6F4E37] hover:border-[#6F4E37]"
                >
                  #MK2095F54 (-25k)
                </button>
                <button
                  type="button"
                  onClick={() => {
                    const hex = Math.floor(1000000 + Math.random() * 9000000).toString(16).toUpperCase();
                    const newCode = `#MK${hex}`;
                    setPromoCode(newCode);
                    handleApplyPromoCode(newCode);
                  }}
                  className="text-[10px] font-bold px-2 py-0.5 rounded-lg bg-amber-50 border border-amber-200 text-amber-800 hover:bg-amber-100"
                >
                  🎲 Tạo mã may mắn
                </button>
              </div>

              {promoMsg && (
                <div
                  className={`text-xs p-2 rounded-xl flex items-center gap-1.5 ${
                    promoMsg.error
                      ? "bg-red-50 text-red-700 border border-red-200"
                      : "bg-emerald-50 text-emerald-700 border border-emerald-200"
                  }`}
                >
                  {promoMsg.error ? (
                    <AlertCircle className="w-3.5 h-3.5 shrink-0" />
                  ) : (
                    <Check className="w-3.5 h-3.5 shrink-0" />
                  )}
                  <span>{promoMsg.text}</span>
                </div>
              )}
            </div>

            {/* Loyalty Points Redemption block */}
            {loyaltyPoints > 0 && maxPointsCanUse > 0 && (
              <div className="pt-2 border-t border-[#EFE6DC] space-y-2">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-bold text-[#786C65] flex items-center gap-1.5">
                    <Crown className="w-3.5 h-3.5 text-amber-500" />
                    <span>Điểm Thưởng Hội Viên</span>
                  </label>
                  <span className="text-[11px] font-bold text-amber-800 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">
                    Có {loyaltyPoints} điểm
                  </span>
                </div>

                <label className="flex items-center justify-between p-3 rounded-xl border border-amber-200 bg-amber-50/60 cursor-pointer hover:bg-amber-100/50 transition-colors">
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={usePoints}
                      onChange={(e) => setUsePoints(e.target.checked)}
                      className="w-4 h-4 rounded text-[#6F4E37] focus:ring-[#6F4E37]"
                    />
                    <span className="text-xs font-bold text-[#2D2522]">
                      Dùng {maxPointsCanUse} điểm giảm giá
                    </span>
                  </div>
                  <span className="text-xs font-black text-emerald-700">
                    -{(maxPointsCanUse * 1000).toLocaleString("vi-VN")}đ
                  </span>
                </label>
              </div>
            )}

            <div className="space-y-2 text-xs text-[#786C65] pt-2 border-t border-[#EFE6DC]">
              <div className="flex justify-between">
                <span>Tạm tính:</span>
                <span className="font-bold text-[#2D2522]">{subtotal.toLocaleString("vi-VN")}đ</span>
              </div>
              {discountAmount > 0 && (
                <div className="flex justify-between text-[#4E7D55] font-semibold">
                  <span>Mã ưu đãi ({promoCode}):</span>
                  <span>-{discountAmount.toLocaleString("vi-VN")}đ</span>
                </div>
              )}
              {pointsDiscount > 0 && (
                <div className="flex justify-between text-amber-700 font-semibold">
                  <span>Điểm thưởng ({maxPointsCanUse} điểm):</span>
                  <span>-{pointsDiscount.toLocaleString("vi-VN")}đ</span>
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
                {finalTotal.toLocaleString("vi-VN")}đ
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
