"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { useCart } from "@/context/CartContext";
import { api } from "@/lib/api";
import { Product } from "@/types";
import { getLoyaltyOverview, TIERS } from "@/lib/loyalty";
import {
  getSavedAddresses,
  saveAddress,
  deleteAddress,
  setDefaultAddress,
  SavedAddress,
} from "@/lib/addresses";
import { getFavoriteIds, toggleFavorite, subscribeFavorites } from "@/lib/favorites";
import {
  User as UserIcon,
  Phone,
  MapPin,
  Mail,
  Lock,
  Save,
  CheckCircle,
  AlertCircle,
  Sparkles,
  ArrowLeft,
  Shield,
  Crown,
  Award,
  Plus,
  Trash2,
  Home,
  Building2,
  GraduationCap,
  Heart,
  ShoppingBag,
  Camera,
  ChevronRight,
} from "lucide-react";
import Link from "next/link";

const PRESET_AVATARS = [
  "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1492562080023-ab3db95bfbce?w=150&auto=format&fit=crop&q=80",
  "https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=150&auto=format&fit=crop&q=80",
];

export default function ProfilePage() {
  const router = useRouter();
  const { user, updateProfile, isLoading } = useAuth();
  const { addItem } = useCart();

  // Basic Profile Form State
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [address, setAddress] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<{ text: string; error?: boolean } | null>(null);
  const fileInputRef = React.useRef<HTMLInputElement | null>(null);

  const handleUploadFromComputer = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith("image/")) {
      setMsg({ text: "Vui lòng chọn tệp hình ảnh hợp lệ (.jpg, .png, .webp...).", error: true });
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      const img = new window.Image();
      img.onload = () => {
        const canvas = document.createElement("canvas");
        const size = 256;
        canvas.width = size;
        canvas.height = size;
        const ctx = canvas.getContext("2d");
        if (ctx) {
          const minSide = Math.min(img.width, img.height);
          const sx = (img.width - minSide) / 2;
          const sy = (img.height - minSide) / 2;
          ctx.drawImage(img, sx, sy, minSide, minSide, 0, 0, size, size);
          const compressedDataUrl = canvas.toDataURL("image/jpeg", 0.85);
          setAvatarUrl(compressedDataUrl);
          setMsg({
            text: `Đã gắn ảnh "${file.name}" từ máy tính! Bấm "Lưu Thay Đổi Hồ Sơ" bên dưới để lưu lại.`,
            error: false,
          });
        }
      };
      img.src = event.target?.result as string;
    };
    reader.readAsDataURL(file);
    e.target.value = "";
  };

  // Loyalty Points & Tier
  const [loyalty, setLoyalty] = useState<{
    points: number;
    currentTier: typeof TIERS.BRONZE;
    progress: number;
    pointsToNext: number;
  } | null>(null);

  // Multiple Addresses
  const [addresses, setAddresses] = useState<SavedAddress[]>([]);
  const [showAddAddressModal, setShowAddAddressModal] = useState(false);
  const [newLabel, setNewLabel] = useState<"Nhà riêng" | "Văn phòng" | "Trường học" | "Khác">("Nhà riêng");
  const [newRecipientName, setNewRecipientName] = useState("");
  const [newRecipientPhone, setNewRecipientPhone] = useState("");
  const [newAddressDetail, setNewAddressDetail] = useState("");
  const [newIsDefault, setNewIsDefault] = useState(false);

  // Favorites
  const [favProducts, setFavProducts] = useState<Product[]>([]);
  const [loadingFavs, setLoadingFavs] = useState(false);

  useEffect(() => {
    if (!isLoading && !user) {
      router.push("/auth/login?redirect=/profile");
    } else if (user) {
      setFullName(user.fullName || "");
      setPhoneNumber(user.phoneNumber || "");
      setAddress(user.address || "");
      setAvatarUrl(user.avatarUrl || "");

      // Load Loyalty Data
      const loy = getLoyaltyOverview(user.id);
      setLoyalty(loy);

      // Load Saved Addresses
      const addrs = getSavedAddresses(user.id, user.address, user.fullName, user.phoneNumber);
      setAddresses(addrs);
    }
  }, [user, isLoading, router]);

  // Load Favorite Products
  const loadFavorites = async () => {
    const ids = getFavoriteIds();
    if (ids.length === 0) {
      setFavProducts([]);
      return;
    }
    setLoadingFavs(true);
    try {
      const allProds = await api.getProducts();
      const filtered = allProds.filter((p) => ids.includes(p.Id ?? p.id));
      setFavProducts(filtered);
    } catch (e) {
      console.error("Lỗi khi tải món yêu thích:", e);
    } finally {
      setLoadingFavs(false);
    }
  };

  useEffect(() => {
    loadFavorites();
    return subscribeFavorites(() => loadFavorites());
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);

    if (password && password !== confirmPassword) {
      setMsg({ text: "Mật khẩu xác nhận không khớp.", error: true });
      return;
    }

    setSaving(true);
    const res = await updateProfile({
      fullName: fullName.trim(),
      phoneNumber: phoneNumber.trim(),
      address: address.trim(),
      avatarUrl: avatarUrl.trim() || undefined,
      password: password ? password : undefined,
    });
    setSaving(false);

    if (res.success) {
      setMsg({ text: "Cập nhật thông tin cá nhân & Avatar thành công!", error: false });
      setPassword("");
      setConfirmPassword("");
    } else {
      setMsg({ text: res.message || "Cập nhật thất bại.", error: true });
    }
  };

  const handleAddNewAddress = (e: React.FormEvent) => {
    e.preventDefault();
    if (!user || !newAddressDetail.trim()) return;

    const updated = saveAddress(user.id, {
      label: newLabel,
      recipientName: newRecipientName.trim() || user.fullName || "Khách Hàng",
      phoneNumber: newRecipientPhone.trim() || user.phoneNumber || "0908888777",
      addressDetail: newAddressDetail.trim(),
      isDefault: newIsDefault,
    });

    setAddresses(updated);
    setShowAddAddressModal(false);
    setNewAddressDetail("");
    setNewIsDefault(false);
  };

  const handleDeleteAddress = (id: string) => {
    if (!user) return;
    const updated = deleteAddress(user.id, id);
    setAddresses(updated);
  };

  const handleSetDefaultAddress = (id: string) => {
    if (!user) return;
    const updated = setDefaultAddress(user.id, id);
    setAddresses(updated);
    const chosen = updated.find((a) => a.id === id);
    if (chosen) {
      setAddress(chosen.addressDetail);
    }
  };

  const handleQuickAddFav = (product: Product) => {
    const id = product.Id ?? product.id;
    const name = product.Name ?? product.name;
    const price = product.BasePrice ?? product.basePrice;
    const img = product.ImageUrl ?? product.imageUrl;
    const type = product.ProductType ?? product.productType ?? "DRINK";

    const ok = addItem({
      productId: id,
      name: name,
      imageUrl: img,
      itemType: type,
      size: type === "DRINK" ? "M" : "Standard",
      sugarLevel: type === "DRINK" ? "50%" : undefined,
      iceLevel: type === "DRINK" ? "70%" : undefined,
      spicyLevel: type === "FOOD" ? (product.spicyLevel ? `Cấp ${product.spicyLevel}` : "Không cay") : undefined,
      dippingSauce: product.defaultSauce || (type === "FOOD" ? "Tương ớt" : undefined),
      portionSize: product.portionDescription || "Phần tiêu chuẩn",
      unitPrice: price,
      quantity: 1,
      totalPrice: price,
      isCustomDIY: false,
      toppings: [],
    });

    if (ok) {
      router.push("/cart");
    }
  };

  if (isLoading || !user) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-20 text-center">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-sm text-[#786C65]">Đang tải thông tin tài khoản...</p>
      </div>
    );
  }

  const currentTier = loyalty?.currentTier || TIERS.BRONZE;

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* 1. Header Navigation */}
      <div className="flex items-center justify-between pb-4 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-[#6F4E37] uppercase tracking-wider">
            <Sparkles className="w-3.5 h-3.5" />
            <span>Trung Tâm Khách Hàng MilkyVerse</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
            Hồ Sơ, Thẻ Hội Viên & Sổ Địa Chỉ
          </h1>
        </div>
        <Link
          href="/"
          className="px-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs font-bold text-[#2D2522] hover:bg-[#FAF7F2] transition-colors flex items-center gap-1.5"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          <span>Về Trang Chủ</span>
        </Link>
      </div>

      {msg && (
        <div
          className={`p-4 rounded-2xl text-xs flex items-center gap-2 ${
            msg.error
              ? "bg-red-50 text-red-700 border border-red-200"
              : "bg-emerald-50 text-emerald-700 border border-emerald-200"
          }`}
        >
          {msg.error ? <AlertCircle className="w-4 h-4 shrink-0" /> : <CheckCircle className="w-4 h-4 shrink-0" />}
          <span className="font-semibold">{msg.text}</span>
        </div>
      )}

      {/* 2. MILKYVERSE VIP MEMBERSHIP CARD */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Crown className="w-5 h-5 text-amber-500" />
            <h2 className="text-lg font-black text-[#2D2522]">Thẻ Hội Viên Milky Galaxy VIP</h2>
          </div>
          <span className="text-xs font-bold text-[#6F4E37] bg-amber-50 border border-amber-200 px-3 py-1 rounded-full">
            10.000đ = 1 Điểm Thưởng
          </span>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Virtual VIP Card */}
          <div className="lg:col-span-6 relative overflow-hidden rounded-3xl p-6 sm:p-8 bg-gradient-to-br from-[#2D2522] via-[#4A3324] to-[#6F4E37] text-white shadow-xl border border-amber-400/30 flex flex-col justify-between min-h-[220px]">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-[10px] uppercase font-bold tracking-widest text-amber-300/90">
                  MILKYVERSE LOYALTY CLUB
                </p>
                <h3 className="text-xl sm:text-2xl font-black mt-1 text-white">
                  {currentTier.name}
                </h3>
              </div>
              <div className="w-12 h-12 rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 flex items-center justify-center text-amber-300">
                <Crown className="w-6 h-6" />
              </div>
            </div>

            <div className="space-y-2 my-4">
              <div className="flex items-baseline justify-between text-xs">
                <span className="font-semibold text-white/80">Điểm tích lũy hiện có:</span>
                <span className="text-2xl font-black text-amber-300">
                  {loyalty?.points || 0} <span className="text-xs text-white/90">điểm</span>
                </span>
              </div>
              <div className="w-full bg-white/20 h-2 rounded-full overflow-hidden">
                <div
                  className="bg-amber-400 h-full rounded-full transition-all duration-700"
                  style={{ width: `${loyalty?.progress || 0}%` }}
                />
              </div>
              <p className="text-[11px] text-white/70 text-right">
                {currentTier.nextTierPoints
                  ? `Còn ${loyalty?.pointsToNext || 0} điểm nữa để thăng hạng ${
                      currentTier.tier === "BRONZE"
                        ? "Bạc"
                        : currentTier.tier === "SILVER"
                        ? "Vàng"
                        : "Kim Cương"
                    }`
                  : "Bạn đã đạt danh hiệu Hội viên Kim Cương cao cấp nhất!"}
              </p>
            </div>

            <div className="flex items-center justify-between text-xs pt-2 border-t border-white/15">
              <span className="font-mono tracking-wider text-white/80">
                MV-VIP-{(user.id * 8831).toString().padStart(8, "0")}
              </span>
              <span className="font-bold text-white/90">{user.fullName || "Khách Hàng VIP"}</span>
            </div>
          </div>

          {/* Tier Benefits */}
          <div className="lg:col-span-6 bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm flex flex-col justify-between space-y-4">
            <div>
              <div className="flex items-center gap-2 pb-3 border-b border-[#FAF7F2]">
                <Award className="w-4 h-4 text-[#6F4E37]" />
                <h3 className="text-sm font-black text-[#2D2522]">
                  Đặc Quyền Của Bạn ({currentTier.name})
                </h3>
              </div>
              <ul className="space-y-2.5 pt-3">
                {currentTier.benefits.map((b, idx) => (
                  <li key={idx} className="flex items-start gap-2 text-xs text-[#2D2522]">
                    <span className="w-1.5 h-1.5 rounded-full bg-[#6F4E37] mt-1.5 shrink-0" />
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div className="p-3 bg-amber-50 rounded-2xl border border-amber-200/80 text-[11px] text-amber-900 leading-relaxed">
              💡 <strong>Mẹo dùng điểm:</strong> Bạn có thể dùng điểm tích lũy để trừ tiền trực tiếp tại bước <strong>Thanh Toán (Checkout)</strong>. Cứ 1 điểm = 1.000đ giảm giá!
            </div>
          </div>
        </div>
      </div>

      {/* 3. SỔ ĐỊA CHỈ NHẬN HÀNG (MULTIPLE ADDRESSES) */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-[#FAF7F2]">
          <div>
            <h2 className="text-base font-bold text-[#2D2522] flex items-center gap-2">
              <MapPin className="w-4 h-4 text-[#6F4E37]" />
              <span>Sổ Địa Chỉ Giao Hàng ({addresses.length})</span>
            </h2>
            <p className="text-xs text-[#786C65] mt-0.5">
              Lưu sẵn nhiều địa chỉ (Nhà riêng, Văn phòng, Trường học) để chọn nhanh khi đặt hàng
            </p>
          </div>
          <button
            type="button"
            onClick={() => {
              setNewRecipientName(user.fullName || "");
              setNewRecipientPhone(user.phoneNumber || "");
              setShowAddAddressModal(true);
            }}
            className="px-4 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors flex items-center gap-1.5 shadow-sm"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Thêm Địa Chỉ Mới</span>
          </button>
        </div>

        {addresses.length === 0 ? (
          <div className="text-center py-6 text-xs text-[#786C65]">
            Chưa có địa chỉ nào được lưu. Bấm &quot;Thêm Địa Chỉ Mới&quot; để tạo ngay.
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {addresses.map((a) => (
              <div
                key={a.id}
                className={`p-4 rounded-2xl border transition-all space-y-3 flex flex-col justify-between ${
                  a.isDefault
                    ? "border-[#6F4E37] bg-[#FAF7F2]/60 shadow-sm"
                    : "border-[#EFE6DC] hover:border-[#6F4E37]/40"
                }`}
              >
                <div className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full bg-white border border-[#EFE6DC] text-[11px] font-bold text-[#6F4E37]">
                      {a.label === "Nhà riêng" ? (
                        <Home className="w-3 h-3" />
                      ) : a.label === "Văn phòng" ? (
                        <Building2 className="w-3 h-3" />
                      ) : a.label === "Trường học" ? (
                        <GraduationCap className="w-3 h-3" />
                      ) : (
                        <MapPin className="w-3 h-3" />
                      )}
                      <span>{a.label}</span>
                    </span>

                    {a.isDefault && (
                      <span className="text-[10px] font-bold text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-0.5 rounded-full">
                        Mặc Định
                      </span>
                    )}
                  </div>

                  <p className="text-xs font-bold text-[#2D2522]">
                    {a.recipientName} <span className="font-normal text-[#786C65]">({a.phoneNumber})</span>
                  </p>
                  <p className="text-xs text-[#2D2522] leading-relaxed">{a.addressDetail}</p>
                </div>

                <div className="flex items-center justify-between pt-2 border-t border-[#EFE6DC]/60 text-xs">
                  {!a.isDefault ? (
                    <button
                      type="button"
                      onClick={() => handleSetDefaultAddress(a.id)}
                      className="text-[11px] font-bold text-[#6F4E37] hover:underline"
                    >
                      Đặt làm mặc định
                    </button>
                  ) : (
                    <span className="text-[11px] text-[#786C65]">Đang dùng khi checkout</span>
                  )}

                  <button
                    type="button"
                    onClick={() => handleDeleteAddress(a.id)}
                    className="text-red-500 hover:text-red-700 p-1"
                    title="Xóa địa chỉ"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Modal Thêm Địa Chỉ Mới */}
      {showAddAddressModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full shadow-2xl space-y-4 animate-scale">
            <h3 className="text-lg font-black text-[#2D2522] flex items-center gap-2">
              <MapPin className="w-5 h-5 text-[#6F4E37]" />
              <span>Thêm Địa Chỉ Nhận Hàng</span>
            </h3>

            <form onSubmit={handleAddNewAddress} className="space-y-4">
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Loại địa chỉ</label>
                <div className="grid grid-cols-4 gap-2">
                  {(["Nhà riêng", "Văn phòng", "Trường học", "Khác"] as const).map((lbl) => (
                    <button
                      key={lbl}
                      type="button"
                      onClick={() => setNewLabel(lbl)}
                      className={`py-2 rounded-xl text-xs font-bold border transition-all ${
                        newLabel === lbl
                          ? "bg-[#6F4E37] text-white border-[#6F4E37]"
                          : "bg-[#FAF7F2] border-[#EFE6DC] text-[#2D2522] hover:bg-white"
                      }`}
                    >
                      {lbl}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Tên người nhận</label>
                <input
                  type="text"
                  required
                  value={newRecipientName}
                  onChange={(e) => setNewRecipientName(e.target.value)}
                  placeholder="Họ tên người nhận"
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Số điện thoại</label>
                <input
                  type="tel"
                  required
                  value={newRecipientPhone}
                  onChange={(e) => setNewRecipientPhone(e.target.value)}
                  placeholder="0901234567"
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Địa chỉ chi tiết</label>
                <textarea
                  rows={3}
                  required
                  value={newAddressDetail}
                  onChange={(e) => setNewAddressDetail(e.target.value)}
                  placeholder="Số nhà, tên tòa nhà, tên đường, phường, quận..."
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-[#2D2522]">
                <input
                  type="checkbox"
                  checked={newIsDefault}
                  onChange={(e) => setNewIsDefault(e.target.checked)}
                  className="w-4 h-4 rounded text-[#6F4E37] focus:ring-[#6F4E37]"
                />
                <span>Đặt làm địa chỉ nhận hàng mặc định</span>
              </label>

              <div className="flex items-center justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setShowAddAddressModal(false)}
                  className="px-4 py-2 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#786C65] hover:bg-[#FAF7F2]"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm"
                >
                  Lưu Địa Chỉ
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 4. MÓN ĂN YÊU THÍCH (FAVORITES) */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-6">
        <div className="flex items-center justify-between pb-4 border-b border-[#FAF7F2]">
          <div>
            <h2 className="text-base font-bold text-[#2D2522] flex items-center gap-2">
              <Heart className="w-4 h-4 text-rose-500 fill-current" />
              <span>Danh Sách Món Yêu Thích Của Tôi ({favProducts.length})</span>
            </h2>
            <p className="text-xs text-[#786C65] mt-0.5">
              Những thức uống & món ăn vặt bạn đã thả tim để đặt lại nhanh chóng
            </p>
          </div>
          <Link
            href="/menu?type=FAVORITES"
            className="text-xs font-bold text-[#6F4E37] hover:underline flex items-center gap-1"
          >
            <span>Xem trên thực đơn</span>
            <ChevronRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        {loadingFavs ? (
          <div className="text-center py-6 text-xs text-[#786C65]">Đang tải món yêu thích...</div>
        ) : favProducts.length === 0 ? (
          <div className="text-center py-8 space-y-2">
            <Heart className="w-8 h-8 text-[#EFE6DC] mx-auto" />
            <p className="text-xs text-[#786C65]">Bạn chưa thả tim món nào trong thực đơn.</p>
            <Link
              href="/menu"
              className="inline-flex items-center gap-1.5 text-xs font-bold text-[#6F4E37] hover:underline pt-1"
            >
              <span>Khám phá menu ngay</span>
              <ArrowLeft className="w-3.5 h-3.5 rotate-180" />
            </Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {favProducts.map((p) => {
              const id = p.Id ?? p.id;
              const name = p.Name ?? p.name;
              const price = p.BasePrice ?? p.basePrice;
              const img = p.ImageUrl ?? p.imageUrl;
              const slug = p.Slug ?? p.slug;

              return (
                <div
                  key={id}
                  className="flex items-center justify-between p-3.5 rounded-2xl border border-[#EFE6DC] hover:border-[#6F4E37]/30 transition-all bg-[#FAF7F2]/40"
                >
                  <div className="flex items-center gap-3 min-w-0 pr-2">
                    <img
                      src={img}
                      alt={name}
                      className="w-12 h-12 rounded-xl object-cover shrink-0 border border-[#EFE6DC]"
                    />
                    <div className="min-w-0">
                      <Link
                        href={`/menu/${slug}`}
                        className="text-xs font-bold text-[#2D2522] truncate block hover:text-[#6F4E37]"
                        title={name}
                      >
                        {name}
                      </Link>
                      <span className="text-[11px] font-black text-[#6F4E37]">
                        {price.toLocaleString("vi-VN")}đ
                      </span>
                    </div>
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      onClick={() => handleQuickAddFav(p)}
                      className="p-2 rounded-xl bg-[#6F4E37] text-white hover:bg-[#4A3324] shadow-sm transition-colors"
                      title="Thêm vào giỏ & Đặt ngay"
                    >
                      <ShoppingBag className="w-3.5 h-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => toggleFavorite(id)}
                      className="p-2 rounded-xl bg-white border border-rose-200 text-rose-500 hover:bg-rose-50 transition-colors"
                      title="Bỏ yêu thích"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* 5. CẬP NHẬT HỒ SƠ & MẬT KHẨU */}
      <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-12 gap-8">
        {/* Left Column: Avatar Selector */}
        <div className="md:col-span-4 space-y-6">
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm text-center space-y-4">
            <div
              onClick={() => fileInputRef.current?.click()}
              title="Nhấn để chọn ảnh đại diện từ máy tính"
              className="group relative w-28 h-28 mx-auto rounded-full overflow-hidden border-4 border-[#F7D6D0] shadow-md bg-[#FAF7F2] flex items-center justify-center cursor-pointer"
            >
              {avatarUrl ? (
                <img
                  src={avatarUrl}
                  alt={fullName}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform"
                  onError={() => setAvatarUrl("")}
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center bg-[#6F4E37] text-white font-black text-3xl">
                  {fullName ? fullName.charAt(0).toUpperCase() : "M"}
                </div>
              )}
              <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center text-white">
                <Camera className="w-6 h-6" />
              </div>
            </div>

            {/* Nút ngay dưới ảnh nhấn vô thay đổi ảnh từ máy tính lên */}
            <div className="pt-1">
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                onChange={handleUploadFromComputer}
                className="hidden"
              />
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                className="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all active:scale-95"
              >
                <Camera className="w-4 h-4 text-[#F7D6D0]" />
                <span>Thay Đổi Ảnh Từ Máy Tính</span>
              </button>
              <p className="text-[10px] text-[#786C65] mt-1">
                Hỗ trợ ảnh JPG, PNG, WEBP trực tiếp từ máy tính của bạn
              </p>
            </div>

            <div>
              <h2 className="font-extrabold text-[#2D2522] text-lg">{fullName || "Người Dùng"}</h2>
              <p className="text-xs text-[#786C65]">{user.email}</p>
              <div className="inline-block mt-2 px-3 py-1 rounded-full bg-[#FAF7F2] text-[#6F4E37] text-[11px] font-bold border border-[#EFE6DC]">
                Vai trò: {user.role}
              </div>
            </div>

            <div className="pt-2 border-t border-[#FAF7F2]">
              <label className="text-xs font-bold text-[#2D2522] block mb-2 text-left">
                Hoặc Chọn Avatar Mẫu Có Sẵn:
              </label>
              <div className="grid grid-cols-4 gap-2">
                {PRESET_AVATARS.map((url, i) => (
                  <button
                    key={i}
                    type="button"
                    onClick={() => setAvatarUrl(url)}
                    className={`relative aspect-square rounded-xl overflow-hidden border-2 transition-all ${
                      avatarUrl === url
                        ? "border-[#6F4E37] scale-105 shadow-md"
                        : "border-transparent opacity-75 hover:opacity-100"
                    }`}
                  >
                    <img src={url} alt={`Avatar ${i}`} className="w-full h-full object-cover" />
                  </button>
                ))}
              </div>

              <div className="mt-3 text-left">
                <label className="text-[11px] text-[#786C65] font-semibold block mb-1">
                  Hoặc dán đường dẫn ảnh (URL):
                </label>
                <input
                  type="text"
                  placeholder="https://example.com/avatar.jpg"
                  value={avatarUrl.startsWith("data:") ? "" : avatarUrl}
                  onChange={(e) => setAvatarUrl(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Right Column: Personal Information Form */}
        <div className="md:col-span-8 bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-6">
          <div className="border-b border-[#FAF7F2] pb-4">
            <h2 className="text-base font-bold text-[#2D2522]">Thông Tin Cá Nhân & Nhận Hàng</h2>
            <p className="text-xs text-[#786C65] mt-0.5">
              Địa chỉ và số điện thoại mặc định khi bạn đặt trà sữa & đồ ăn vặt
            </p>
          </div>

          <div className="space-y-4">
            <div>
              <label className="text-xs font-bold text-[#2D2522] block mb-1">Họ và tên</label>
              <div className="relative">
                <input
                  type="text"
                  required
                  placeholder="Nguyễn Văn A"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37] font-semibold"
                />
                <UserIcon className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Email (Cố định)</label>
                <div className="relative">
                  <input
                    type="email"
                    disabled
                    value={user.email}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-gray-100 border border-[#EFE6DC] text-xs text-gray-500 cursor-not-allowed"
                  />
                  <Mail className="w-4 h-4 text-gray-400 absolute left-3.5 top-3" />
                </div>
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">Số điện thoại</label>
                <div className="relative">
                  <input
                    type="tel"
                    placeholder="0901234567"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37] font-semibold"
                  />
                  <Phone className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                </div>
              </div>
            </div>

            <div>
              <label className="text-xs font-bold text-[#2D2522] block mb-1">Địa chỉ giao hàng mặc định</label>
              <div className="relative">
                <input
                  type="text"
                  placeholder="Số nhà, tên đường, phường, quận, TP.HCM"
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
                <MapPin className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
              </div>
            </div>
          </div>

          <div className="pt-4 border-t border-[#FAF7F2] space-y-4">
            <div>
              <h3 className="text-xs font-bold text-[#2D2522] flex items-center gap-1.5">
                <Shield className="w-3.5 h-3.5 text-[#6F4E37]" />
                <span>Đổi Mật Khẩu (Để trống nếu không muốn thay đổi)</span>
              </h3>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="text-xs font-medium text-[#786C65] block mb-1">Mật khẩu mới</label>
                <div className="relative">
                  <input
                    type="password"
                    placeholder="Tối thiểu 6 ký tự"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                  />
                  <Lock className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                </div>
              </div>

              <div>
                <label className="text-xs font-medium text-[#786C65] block mb-1">Xác nhận mật khẩu mới</label>
                <div className="relative">
                  <input
                    type="password"
                    placeholder="Nhập lại mật khẩu mới"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                  />
                  <Lock className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                </div>
              </div>
            </div>
          </div>

          <div className="pt-2">
            <button
              type="submit"
              disabled={saving}
              className="w-full sm:w-auto px-8 py-3 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
            >
              <Save className="w-4 h-4" />
              <span>{saving ? "Đang lưu..." : "Lưu Thay Đổi Hồ Sơ"}</span>
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}
