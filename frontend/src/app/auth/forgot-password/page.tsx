"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import {
  Lock,
  Mail,
  ArrowRight,
  ArrowLeft,
  AlertCircle,
  CheckCircle2,
  KeyRound,
  ShieldCheck,
  Eye,
  EyeOff,
  RefreshCw,
} from "lucide-react";

export default function ForgotPasswordPage() {
  const router = useRouter();

  const [step, setStep] = useState<1 | 2>(1);
  const [email, setEmail] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [resendCooldown, setResendCooldown] = useState(0);

  useEffect(() => {
    if (resendCooldown <= 0) return;
    const timer = setInterval(() => {
      setResendCooldown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, [resendCooldown]);

  const handleSendOtp = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!email.trim() || !email.includes("@")) {
      setError("Vui lòng nhập địa chỉ email hợp lệ.");
      return;
    }

    setLoading(true);
    try {
      const res = await api.sendOtp(email.trim());
      setSuccessMsg(res.message || `Đã gửi mã xác nhận 6 số đến ${email}`);
      setStep(2);
      setResendCooldown(60);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Không thể gửi mã xác nhận. Vui lòng thử lại.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!otpCode.trim() || otpCode.trim().length < 6) {
      setError("Vui lòng nhập đầy đủ mã OTP 6 số.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Mật khẩu xác nhận không trùng khớp.");
      return;
    }

    setLoading(true);
    try {
      const res = await api.resetPassword({
        email: email.trim(),
        code: otpCode.trim(),
        newPassword: newPassword,
      });

      setSuccessMsg(res.message || "Đặt lại mật khẩu thành công! Đang chuyển đến trang đăng nhập...");
      setTimeout(() => {
        router.push("/auth/login");
      }, 2500);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Đặt lại mật khẩu thất bại.";
      setError(msg);
    } finally {
      setLoading(false);
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
            Khôi Phục Mật Khẩu
          </h1>
          <p className="text-xs text-[#786C65]">
            {step === 1
              ? "Nhập email đã đăng ký để nhận mã xác thực OTP lấy lại mật khẩu"
              : `Nhập mã OTP 6 số đã gửi tới ${email} và mật khẩu mới`}
          </p>
        </div>

        {/* Thông báo Lỗi / Thành công */}
        {error && (
          <div className="p-3.5 rounded-2xl bg-red-50 text-red-700 border border-red-200 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {successMsg && (
          <div className="p-3.5 rounded-2xl bg-emerald-50 text-emerald-800 border border-emerald-200 text-xs flex items-center gap-2">
            <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
            <span>{successMsg}</span>
          </div>
        )}

        {/* Step Indicator */}
        <div className="flex items-center justify-center gap-2 text-xs font-bold text-[#786C65]">
          <span
            className={`w-6 h-6 rounded-full flex items-center justify-center text-[11px] ${
              step === 1 ? "bg-[#6F4E37] text-white" : "bg-emerald-600 text-white"
            }`}
          >
            1
          </span>
          <span className={step === 1 ? "text-[#2D2522]" : "text-emerald-700"}>Nhập Email</span>
          <span className="w-6 h-0.5 bg-[#EFE6DC]" />
          <span
            className={`w-6 h-6 rounded-full flex items-center justify-center text-[11px] ${
              step === 2 ? "bg-[#6F4E37] text-white" : "bg-[#EFE6DC] text-[#786C65]"
            }`}
          >
            2
          </span>
          <span className={step === 2 ? "text-[#2D2522]" : "text-[#786C65]"}>Đổi Mật Khẩu</span>
        </div>

        {/* Form */}
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          {step === 1 ? (
            <form onSubmit={handleSendOtp} className="space-y-4">
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">
                  Email đăng ký
                </label>
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

              <button
                type="submit"
                disabled={loading}
                className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
              >
                <span>{loading ? "Đang gửi mã..." : "Gửi Mã Xác Thực OTP"}</span>
                <ArrowRight className="w-4 h-4" />
              </button>
            </form>
          ) : (
            <form onSubmit={handleResetPassword} className="space-y-4">
              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className="text-xs font-bold text-[#2D2522]">
                    Mã xác thực OTP (6 chữ số)
                  </label>
                  <button
                    type="button"
                    disabled={resendCooldown > 0 || loading}
                    onClick={() => handleSendOtp()}
                    className="text-[11px] font-semibold text-[#6F4E37] hover:underline disabled:text-[#A89F91]"
                  >
                    {resendCooldown > 0 ? `Gửi lại sau ${resendCooldown}s` : "Gửi lại mã"}
                  </button>
                </div>
                <div className="relative">
                  <input
                    type="text"
                    required
                    maxLength={6}
                    placeholder="123456"
                    value={otpCode}
                    onChange={(e) => setOtpCode(e.target.value)}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs font-mono tracking-widest text-[#2D2522] focus:outline-none focus:border-[#6F4E37]"
                  />
                  <KeyRound className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                </div>
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">
                  Mật khẩu mới (tối thiểu 8 ký tự, có chữ hoa, thường, số, ký tự đặc biệt)
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? "text" : "password"}
                    required
                    placeholder="••••••••"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="w-full pl-10 pr-10 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                  />
                  <Lock className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3.5 top-3 text-[#786C65] hover:text-[#2D2522] transition-colors"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">
                  Xác nhận mật khẩu mới
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? "text" : "password"}
                    required
                    placeholder="••••••••"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                  />
                  <ShieldCheck className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setStep(1)}
                  className="px-4 py-3 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#786C65] hover:bg-[#FAF7F2]"
                >
                  Quay lại
                </button>
                <button
                  type="submit"
                  disabled={loading}
                  className="flex-1 py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  <span>{loading ? "Đang xử lý..." : "Xác Nhận Đổi Mật Khẩu"}</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </form>
          )}

          <div className="border-t border-[#EFE6DC] pt-4 text-center">
            <Link
              href="/auth/login"
              className="inline-flex items-center gap-1.5 text-xs font-bold text-[#6F4E37] hover:underline"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              <span>Quay lại Đăng nhập</span>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
