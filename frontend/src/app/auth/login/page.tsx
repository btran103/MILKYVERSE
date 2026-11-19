"use client";

import React, { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Lock, Mail, ArrowRight, AlertCircle, Eye, EyeOff } from "lucide-react";

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
      const redirectUrl =
        typeof window !== "undefined"
          ? new URLSearchParams(window.location.search).get("redirect")
          : null;

      if (redirectUrl) {
        router.push(redirectUrl);
      } else {
        router.push("/");
      }
    } else {
      const msg =
        res.message?.includes("Failed to fetch") || res.message?.includes("kết nối")
          ? "Không thể kết nối máy chủ Backend. Vui lòng kiểm tra lại kết nối mạng."
          : res.message || "Email hoặc mật khẩu không chính xác.";
      setError(msg);
    }
  };

  return (
    <div className="min-h-[75vh] flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md space-y-6">
        {/* Header */}
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
          <h1 className="text-2xl font-black text-[#2D2522]">
            Đăng Nhập MilkyVerse
          </h1>
          <p className="text-xs text-[#786C65]">
            Chào mừng bạn quay trở lại với thế giới trà sữa MilkyVerse
          </p>
        </div>

        {/* Error message */}
        {error && (
          <div className="p-3.5 rounded-2xl bg-red-50 text-red-700 border border-red-200 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {/* Login Form */}
        <form
          onSubmit={handleSubmit}
          className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4"
        >
          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1">
              Email
            </label>
            <div className="relative">
              <input
                type="email"
                required
                autoComplete="username"
                placeholder="ten@vidu.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <Mail className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="text-xs font-bold text-[#2D2522]">
                Mật khẩu
              </label>
              <Link
                href="/auth/forgot-password"
                className="text-[11px] font-semibold text-[#6F4E37] hover:underline"
              >
                Quên mật khẩu?
              </Link>
            </div>
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                required
                autoComplete="current-password"
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
            <span>{loading ? "Đang đăng nhập..." : "Đăng Nhập"}</span>
            <ArrowRight className="w-4 h-4" />
          </button>

          <p className="text-center text-xs text-[#786C65] pt-2">
            Chưa có tài khoản?{" "}
            <Link href="/auth/register" className="font-bold text-[#6F4E37] hover:underline">
              Đăng ký ngay
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
