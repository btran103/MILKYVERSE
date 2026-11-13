"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
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
  Camera,
  ArrowLeft,
  Shield,
  Smile,
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

  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [address, setAddress] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<{ text: string; error?: boolean } | null>(null);

  useEffect(() => {
    if (!isLoading && !user) {
      router.push("/auth/login?redirect=/profile");
    } else if (user) {
      setFullName(user.fullName || "");
      setPhoneNumber(user.phoneNumber || "");
      setAddress(user.address || "");
      setAvatarUrl(user.avatarUrl || "");
    }
  }, [user, isLoading, router]);

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

  if (isLoading || !user) {
    return (
      <div className="max-w-3xl mx-auto px-4 py-20 text-center">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-sm text-[#786C65]">Đang tải thông tin tài khoản...</p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* 1. Header Navigation */}
      <div className="flex items-center justify-between pb-4 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-[#6F4E37] uppercase tracking-wider">
            <Sparkles className="w-3.5 h-3.5" />
            <span>Tài Khoản Khách Hàng</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
            Hồ Sơ Cá Nhân & Avatar Đại Diện
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

      <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-12 gap-8">
        {/* Left Column: Avatar Selector */}
        <div className="md:col-span-4 space-y-6">
          <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm text-center space-y-4">
            <div className="relative w-28 h-28 mx-auto rounded-full overflow-hidden border-4 border-[#F7D6D0] shadow-md bg-[#FAF7F2] flex items-center justify-center">
              {avatarUrl ? (
                <img
                  src={avatarUrl}
                  alt={fullName}
                  className="w-full h-full object-cover"
                  onError={() => setAvatarUrl("")}
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center bg-[#6F4E37] text-white font-black text-3xl">
                  {fullName ? fullName.charAt(0).toUpperCase() : "M"}
                </div>
              )}
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
                Chọn Avatar Yêu Thích:
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
                  Hoặc dán URL ảnh đại diện tùy ý:
                </label>
                <input
                  type="url"
                  placeholder="https://example.com/avatar.jpg"
                  value={avatarUrl}
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
