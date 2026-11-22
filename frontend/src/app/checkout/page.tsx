"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import { api } from "@/lib/api";
import { OrderResponse } from "@/types";
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
  Clock,
  ShieldCheck,
  Ticket,
  Crown,
  Home,
  Building2,
  MapPin,
  CheckCircle2,
  Copy,
  QrCode,
  ExternalLink,
} from "lucide-react";
import { getSavedAddresses, SavedAddress } from "@/lib/addresses";
import { getLoyaltyOverview, saveLoyaltyPoints } from "@/lib/loyalty";
import { SePayQrResponse } from "@/types";

export default function CheckoutPage() {
  const router = useRouter();
  const { items, subtotal, clearCart } = useCart();
  const { user, isLoading } = useAuth();

  const [name, setName] = useState(user?.fullName || "");
  const [phone, setPhone] = useState(user?.phoneNumber || "");
  const [address, setAddress] = useState(user?.address || "");
  const [note, setNote] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("SEPAY");
  const [promoCode, setPromoCode] = useState<string>("");
  const [discountAmount, setDiscountAmount] = useState<number>(0);
  const [promoMsg, setPromoMsg] = useState<{ text: string; error?: boolean } | null>(null);

  // Saved Addresses & Loyalty
  const [savedAddresses, setSavedAddresses] = useState<SavedAddress[]>([]);
  const [selectedAddrId, setSelectedAddrId] = useState<string | null>(null);
  const [loyaltyPoints, setLoyaltyPoints] = useState<number>(0);
  const [usePoints, setUsePoints] = useState<boolean>(false);

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // SePay Payment Modal State
  const [showQrModal, setShowQrModal] = useState(false);
  const [createdOrder, setCreatedOrder] = useState<OrderResponse | null>(null);
  const [sePaySession, setSePaySession] = useState<SePayQrResponse | null>(null);
  const [qrCountdown, setQrCountdown] = useState(900); // 15 phút
  const [modalSuccess, setModalSuccess] = useState(false);
  const [copiedContent, setCopiedContent] = useState(false);

  // Modal Countdown Timer (15:00 -> 00:00)
  useEffect(() => {
    if (!showQrModal || modalSuccess) return;
    const t = setInterval(() => {
      setQrCountdown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(t);
  }, [showQrModal, modalSuccess]);

  // Real-time Polling every 2s while Modal is open: Tự động phát hiện khi SePay ghi nhận tiền thật vào TPBank
  useEffect(() => {
    if (!showQrModal || !createdOrder || modalSuccess) return;
    const poll = setInterval(async () => {
      try {
        const ord = await api.getOrderById(createdOrder.id);
        if (
          ord.paymentStatus === "Paid" ||
          ord.paymentStatus === "Success" ||
          ord.status === "Confirmed"
        ) {
          setModalSuccess(true);
          clearCart();
          sessionStorage.removeItem("milky_promo_code");
          confetti({
            particleCount: 160,
            spread: 90,
            origin: { y: 0.6 },
            colors: ["#2563EB", "#059669", "#6F4E37", "#F7D6D0"],
          });
          setTimeout(() => {
            router.push(`/orders/payment-success?orderId=${createdOrder.id}`);
          }, 1800);
        }
      } catch {}
    }, 2000);
    return () => clearInterval(poll);
  }, [showQrModal, createdOrder, modalSuccess, clearCart, router]);

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
    if (isLoading) return;

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
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="max-w-xl mx-auto px-4 py-28 text-center space-y-4">
        <div className="w-12 h-12 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-xs font-bold text-[#786C65]">Đang xác thực thông tin đăng nhập...</p>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="max-w-xl mx-auto px-4 py-28 text-center space-y-4">
        <div className="w-12 h-12 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-xs font-bold text-[#786C65]">Đang chuyển hướng đến trang đăng nhập...</p>
      </div>
    );
  }

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

      // SePay (VietQR / TPBank): Tạo mã VietQR TPBank chuẩn SePay với thời hạn 15 phút, hiện Modal đè lên trang checkout
      if (paymentMethod === "SEPAY") {
        const accountNumber = "00000107071";
        const bank = "TPBank";
        const accountName = "HA BAO TRAN";
        const description = `MILKY ${orderRes.orderCode}`;
        const qrUrl = `https://qr.sepay.vn/img?acc=${encodeURIComponent(accountNumber)}&bank=${encodeURIComponent(bank)}&amount=${Math.round(orderRes.totalAmount)}&des=${encodeURIComponent(description)}&template=compact`;

        let session: SePayQrResponse = {
          orderId: orderRes.id,
          orderCode: orderRes.orderCode,
          amount: orderRes.totalAmount,
          description,
          bank,
          accountNumber,
          accountName,
          qrUrl,
          paymentStatus: "Pending",
          expiredSeconds: 900,
        };

        try {
          const apiSession = await api.getSePayQr(orderRes.id);
          if (apiSession) session = apiSession;
        } catch (e) {
          console.warn("Lấy QR từ backend thất bại, dùng fallback trực tiếp:", e);
        }

        setCreatedOrder(orderRes);
        setSePaySession(session);
        setQrCountdown(session.expiredSeconds || 900);
        setShowQrModal(true);
        return;
      }

      // COD payment: Xác nhận đơn ngay với tiền mặt khi nhận hàng
      confetti({
        particleCount: 120,
        spread: 70,
        origin: { y: 0.6 },
        colors: ["#6F4E37", "#F7D6D0", "#C5A059", "#4E7D55"],
      });
      clearCart();
      sessionStorage.removeItem("milky_promo_code");
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

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => setPaymentMethod("SEPAY")}
                className={`p-4 rounded-2xl border text-left transition-all flex flex-col gap-2 ${
                  paymentMethod === "SEPAY"
                    ? "border-[#2563EB] bg-blue-50/50 shadow-sm ring-2 ring-[#2563EB]"
                    : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                }`}
              >
                <div className="flex items-center gap-2">
                  <QrCode className="w-5 h-5 text-[#2563EB] shrink-0" />
                  <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-[#2563EB] text-white">Khuyên dùng • Tiền thật TPBank</span>
                </div>
                <div>
                  <h4 className="font-bold text-xs text-[#2D2522]">Thanh toán SePay (VietQR TPBank)</h4>
                  <p className="text-[10px] text-[#786C65] mt-0.5">
                    Quét mã VietQR bằng mọi app ngân hàng. Tiền thật vào thẳng tài khoản TPBank, SePay tự động nổ đơn ngay lập tức!
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
                  <h4 className="font-bold text-xs text-[#2D2522]">Tiền mặt khi nhận hàng (COD)</h4>
                  <p className="text-[10px] text-[#786C65] mt-0.5">
                    Thanh toán trực tiếp cho shipper khi nhận trà sữa.
                  </p>
                </div>
              </button>
            </div>

            {paymentMethod === "MOMO" && (
              <div className="p-4 rounded-3xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-3 text-xs">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-[#A50064] font-bold">
                    <Sparkles className="w-4 h-4 text-[#A50064]" />
                    <span>Thanh Toán Trực Tuyến MoMo (Phiên hiệu lực 02:00):</span>
                  </div>
                  <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-red-50 text-red-700 font-mono font-bold text-xs border border-red-200">
                    <Clock className="w-3.5 h-3.5" />
                    <span>02:00</span>
                  </div>
                </div>

                <div className="flex flex-col sm:flex-row items-center gap-4 bg-white p-4 rounded-2xl border border-[#EFE6DC]">
                  <div className="w-24 h-24 bg-white p-1 rounded-2xl border border-[#A50064]/30 shrink-0 flex items-center justify-center overflow-hidden shadow-xs">
                    <img
                      src="/images/qr-momo.jpg"
                      alt="Mã QR MoMo Hà Bảo Trân"
                      className="w-full h-full object-cover rounded-xl"
                    />
                  </div>

                  <div className="space-y-1.5 text-[11px] w-full">
                    <div>
                      <span className="text-[#786C65] block text-[10px]">Cổng thanh toán tích hợp:</span>
                      <strong className="text-[#2D2522]">
                        MoMo Payment Gateway (Chữ ký HMAC SHA256 &amp; IPN Webhook Real-Time)
                      </strong>
                    </div>
                    <div>
                      <span className="text-[#786C65] block text-[10px]">Thời gian hiệu lực bắt buộc:</span>
                      <strong className="text-red-600 font-mono text-xs">02:00 (120 giây kể từ lúc tạo phiên trên Backend)</strong>
                    </div>
                    <div>
                      <span className="text-[#786C65] block text-[10px]">Số tiền thanh toán:</span>
                      <strong className="text-base font-black text-[#A50064]">
                        {finalTotal.toLocaleString("vi-VN")}đ
                      </strong>
                    </div>
                  </div>
                </div>

                <p className="text-[10px] text-[#786C65] italic leading-tight">
                  * Sau khi bấm <strong>Xác Nhận Đặt Hàng</strong>, hệ thống Backend sẽ khởi tạo phiên giao dịch MoMo duy nhất (`OrderId` & `RequestId`), lưu `PaymentExpiresAt` (120 giây) vào Database và bắt đầu đếm ngược thời gian thực `02:00`.
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
                  placeholder="Nhập mã ưu đãi của bạn..."
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
              className="w-full py-4 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50 cursor-pointer active:scale-98"
            >
              {loading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Đang xử lý đơn hàng...</span>
                </>
              ) : paymentMethod === "SEPAY" ? (
                <>
                  <QrCode className="w-4 h-4" />
                  <span>Thanh Toán Qua SePay (VietQR TPBank) — {finalTotal.toLocaleString("vi-VN")}đ</span>
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  <span>Xác Nhận Đặt Hàng (Tiền Mặt COD)</span>
                </>
              )}
            </button>
          </div>
        </div>
      </form>

      {/* Modal Popup: Thanh Toán SePay VietQR Nhận Tiền Thật TPBank */}
      {showQrModal && createdOrder && sePaySession && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4 overflow-y-auto animate-in fade-in duration-200">
          <div className="w-full max-w-md bg-white rounded-3xl p-6 sm:p-7 shadow-2xl border border-[#EFE6DC] space-y-4 text-center relative animate-in zoom-in-95 duration-200 my-auto">
            {/* Header */}
            <div className="flex items-center justify-between pb-3 border-b border-[#FAF7F2]">
              <div className="flex items-center gap-2 text-left">
                <div className="w-8 h-8 rounded-xl bg-[#2563EB] text-white flex items-center justify-center font-black text-xs shadow-xs">
                  S
                </div>
                <div>
                  <h3 className="font-extrabold text-[#2D2522] text-sm">
                    Thanh Toán SePay • VietQR TPBank
                  </h3>
                  <p className="text-[11px] text-[#786C65]">Mã đơn: #{createdOrder.orderCode}</p>
                </div>
              </div>

              {/* Countdown Pill (15 phút) */}
              <div className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-blue-50 text-blue-700 font-mono font-bold text-xs border border-blue-200">
                <Clock className="w-3.5 h-3.5 text-blue-600" />
                <span>
                  {Math.floor(qrCountdown / 60).toString().padStart(2, "0")}:
                  {(qrCountdown % 60).toString().padStart(2, "0")}
                </span>
              </div>
            </div>

            {modalSuccess ? (
              <div className="py-6 space-y-3 animate-in zoom-in-95 duration-200">
                <div className="w-16 h-16 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto border-2 border-emerald-200">
                  <CheckCircle2 className="w-10 h-10 animate-bounce" />
                </div>
                <h4 className="text-lg font-black text-emerald-700">
                  ĐÃ NHẬN TIỀN THẬT THÀNH CÔNG VÀO TPBANK!
                </h4>
                <p className="text-xs text-[#786C65] font-semibold">
                  SePay đã xác nhận thanh toán &amp; đơn hàng <strong>#{createdOrder.orderCode}</strong> thành công!
                </p>
                <p className="text-[11px] text-[#4E7D55] italic">
                  Đang chuyển bạn đến biên lai thanh toán...
                </p>
              </div>
            ) : (
              <>
                {/* Banner trạng thái: Chờ thanh toán */}
                <div className="p-2.5 rounded-2xl bg-amber-50 border border-amber-200 text-amber-900 text-xs text-left space-y-1">
                  <div className="flex items-center gap-1.5 font-bold text-amber-800">
                    <AlertCircle className="w-4 h-4 text-amber-600 shrink-0" />
                    <span>Trạng thái: CHỜ CHUYỂN KHOẢN (Hiệu lực 15 phút)</span>
                  </div>
                  <p className="text-[11px] text-amber-700 leading-normal">
                    Quét mã VietQR bằng app ngân hàng của bạn. Tiền vào TPBank là SePay <strong>tự động duyệt đơn ngay lập tức</strong>!
                  </p>
                </div>

                {/* Tấm ảnh mã VietQR TPBank chuẩn SePay */}
                <div className="space-y-1.5">
                  <div className="relative mx-auto max-w-[220px] bg-white p-2 rounded-2xl border-2 border-[#2563EB] shadow-md">
                    <img
                      src={sePaySession.qrUrl}
                      alt="Mã VietQR SePay TPBank"
                      className="w-full h-auto object-contain rounded-xl"
                    />
                  </div>
                  <p className="text-[11px] font-bold text-[#2563EB] flex items-center justify-center gap-1">
                    <Building2 className="w-3.5 h-3.5 text-purple-600" />
                    <span>TPBank • {sePaySession.accountName}</span>
                  </p>
                </div>

                {/* Thông tin chuyển khoản */}
                <div className="bg-[#FAF7F2] p-3 rounded-2xl border border-[#EFE6DC] text-left text-xs space-y-2">
                  <div className="flex justify-between items-center text-[#786C65]">
                    <span>Số tài khoản TPBank:</span>
                    <strong className="font-mono font-bold text-[#2D2522]">
                      {sePaySession.accountNumber}
                    </strong>
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
                        onClick={() => {
                          navigator.clipboard.writeText(sePaySession.description);
                          setCopiedContent(true);
                          setTimeout(() => setCopiedContent(false), 2000);
                        }}
                        className="p-1 rounded hover:bg-white text-[#786C65]"
                        title="Sao chép nội dung"
                      >
                        {copiedContent ? (
                          <Check className="w-3.5 h-3.5 text-emerald-600" />
                        ) : (
                          <Copy className="w-3.5 h-3.5" />
                        )}
                      </button>
                    </div>
                  </div>
                </div>

                {/* Radar Tự Động Lắng Nghe Tiền Về Thời Gian Thực */}
                <div className="p-3.5 rounded-2xl bg-blue-50 border border-blue-200 text-blue-900 text-xs flex items-center justify-center gap-2.5 font-bold">
                  <span className="w-2.5 h-2.5 rounded-full bg-blue-500 animate-ping shrink-0" />
                  <span>Đang tự động lắng nghe giao dịch TPBank qua SePay...</span>
                </div>

                <p className="text-[11px] text-[#786C65] leading-relaxed">
                  💡 Bạn chỉ cần quét mã và chuyển khoản. Sau khi tiền vào tài khoản TPBank, Webhook của SePay sẽ <strong>TỰ ĐỘNG XÁC NHẬN ĐƠN HÀNG</strong> ngay lập tức!
                </p>

                <div className="pt-1">
                  <button
                    type="button"
                    onClick={() => setShowQrModal(false)}
                    className="w-full py-2 text-xs text-[#786C65] hover:text-red-600 transition-colors cursor-pointer"
                  >
                    ✕ Hủy giao dịch (Quay lại giỏ hàng)
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
