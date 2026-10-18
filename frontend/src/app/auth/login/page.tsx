"use client";

import React, { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Sparkles, Lock, Mail, ArrowRight, AlertCircle, Eye, EyeOff } from "lucide-react";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [showPassword, setShowPassword] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    const res = await login(email.trim(), password);
    setLoading(false);

    if (res.success) {
      router.push("/");
    } else {
      const msg = res.message?.includes("Failed to fetch") || res.message?.includes("kết nối")
        ? "Không thể kết nối tới máy chủ Backend (http://localhost:5000). Vui lòng kiểm tra Backend đã chạy chưa."
        : (res.message || "Email hoặc mật khẩu không chính xác.");
      setError(msg);
    }
  };

  const handleFillDemo = (demoEmail: string, demoPass: string) => {
    setEmail(demoEmail);
    setPassword(demoPass);
  };

  return (
    <div className="max-w-md mx-auto px-4 py-16 space-y-6">
      <div className="text-center space-y-2">
        <div className="w-16 h-16 rounded-full overflow-hidden bg-white border border-[#EFE6DC] shadow-md flex items-center justify-center mx-auto p-1">
          <Image
            src="/logo.png"
            alt="MilkyVerse Logo"
            width={64}
            height={64}
            className="w-full h-full object-cover rounded-full"
            priority
          />
        </div>
        <h1 className="text-2xl font-black text-[#2D2522]">Đăng Nhập MilkyVerse</h1>
        <p className="text-xs text-[#786C65]">
          Truy cập tài khoản để nhận điểm thưởng thành viên và lưu công thức yêu thích
        </p>
      </div>

      {error && (
        <div className="p-3 rounded-2xl bg-red-50 text-red-700 border border-red-200 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Form đăng nhập */}
      <form onSubmit={handleSubmit} className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
        <div>
          <label className="text-xs font-bold text-[#2D2522] block mb-1">Email</label>
          <div className="relative">
            <input
              type="email"
              required
              placeholder="ten@vidu.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
            />
            <Mail className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
          </div>
        </div>

        <div>
          <label className="text-xs font-bold text-[#2D2522] block mb-1">Mật khẩu</label>
          <div className="relative">
            <input
              type={showPassword ? "text" : "password"}
              required
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full pl-10 pr-10 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
            />
            <Lock className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3.5 top-3 text-[#786C65] hover:text-[#2D2522] transition-colors"
              aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
            >
              {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
          </div>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
        >
          <span>{loading ? "Đang xác thực..." : "Đăng Nhập"}</span>
          <ArrowRight className="w-4 h-4" />
        </button>

        <p className="text-center text-xs text-[#786C65] pt-2">
          Chưa có tài khoản?{" "}
          <Link href="/auth/register" className="font-bold text-[#6F4E37] hover:underline">
            Đăng ký ngay
          </Link>
        </p>
      </form>

      {/* Tài khoản mẫu test nhanh */}
      <div className="bg-white/80 backdrop-blur-sm p-4 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3">
        <p className="text-xs font-bold text-[#4A3324] flex items-center gap-1.5">
          <Sparkles className="w-3.5 h-3.5 text-[#C5A059]" />
          <span>Tài khoản mẫu thử nghiệm (Click để điền nhanh):</span>
        </p>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
          <button
            type="button"
            onClick={() => handleFillDemo("admin@milkyverse.com", "Admin@123")}
            className="p-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 border border-[#EFE6DC] text-left transition-colors"
          >
            <p className="text-xs font-bold text-purple-800">👑 Admin</p>
            <p className="text-[10px] text-[#786C65] truncate">admin@milkyverse.com</p>
          </button>

          <button
            type="button"
            onClick={() => handleFillDemo("manager@milkyverse.com", "Manager@123")}
            className="p-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 border border-[#EFE6DC] text-left transition-colors"
          >
            <p className="text-xs font-bold text-indigo-800">🛡️ Quản Lý</p>
            <p className="text-[10px] text-[#786C65] truncate">manager@milkyverse.com</p>
          </button>

          <button
            type="button"
            onClick={() => handleFillDemo("staff@milkyverse.com", "Staff@123")}
            className="p-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 border border-[#EFE6DC] text-left transition-colors"
          >
            <p className="text-xs font-bold text-orange-800">🧑‍🍳 Nhân Viên</p>
            <p className="text-[10px] text-[#786C65] truncate">staff@milkyverse.com</p>
          </button>

          <button
            type="button"
            onClick={() => handleFillDemo("shipper@milkyverse.com", "Shipper@123")}
            className="p-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 border border-[#EFE6DC] text-left transition-colors"
          >
            <p className="text-xs font-bold text-emerald-800">🛵 Shipper</p>
            <p className="text-[10px] text-[#786C65] truncate">shipper@milkyverse.com</p>
          </button>

          <button
            type="button"
            onClick={() => handleFillDemo("warehouse@milkyverse.com", "Warehouse@123")}
            className="p-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 border border-[#EFE6DC] text-left transition-colors"
          >
            <p className="text-xs font-bold text-blue-800">📦 Thủ Kho</p>
            <p className="text-[10px] text-[#786C65] truncate">warehouse@milkyverse.com</p>
          </button>

          <button
            type="button"
            onClick={() => handleFillDemo("khachhang@milkyverse.com", "Customer@123")}
            className="p-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 border border-[#EFE6DC] text-left transition-colors"
          >
            <p className="text-xs font-bold text-[#6F4E37]">☕ Khách Hàng</p>
            <p className="text-[10px] text-[#786C65] truncate">khachhang@milkyverse.com</p>
          </button>
        </div>
      </div>
    </div>
  );
}
