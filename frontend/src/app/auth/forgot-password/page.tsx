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
  Clock,
  Check,
} from "lucide-react";

export default function ForgotPasswordPage() {
  const router = useRouter();

  // Quy trình 3 bước chặt chẽ: 1. Nhập Email -> 2. Nhận & Xác thực OTP (5 phút) -> 3. Đặt mật khẩu mới
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [email, setEmail] = useState("");
  const [otpCode, setOtpCode] = useState("");
  const [receivedDevOtp, setReceivedDevOtp] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [resendCooldown, setResendCooldown] = useState(0);
  const [otpExpirySecs, setOtpExpirySecs] = useState(300); // 5 phút hiệu lực OTP

  // Password complexity checklist
  const hasMinLength = newPassword.length >= 8;
  const hasUpperCase = /[A-Z]/.test(newPassword);
  const hasLowerCase = /[a-z]/.test(newPassword);
  const hasNumber = /[0-9]/.test(newPassword);
  const hasSpecialChar = /[^A-Za-z0-9]/.test(newPassword);
  const isPasswordValid = hasMinLength && hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;

  useEffect(() => {
    if (resendCooldown <= 0) return;
    const timer = setInterval(() => {
      setResendCooldown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, [resendCooldown]);

  useEffect(() => {
    if (step !== 2 || otpExpirySecs <= 0) return;
    const timer = setInterval(() => {
      setOtpExpirySecs((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, [step, otpExpirySecs]);

  const formatTimer = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  };

  // BƯỚC 1: Gửi mã OTP 6 chữ số đến Email đã đăng ký
  const handleSendOtp = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!email.trim() || !email.includes("@")) {
      setError("Vui lòng nhập địa chỉ email đã đăng ký hợp lệ.");
      return;
    }

    setLoading(true);
    try {
      const res = await api.sendOtp(email.trim(), "forgot-password");
      setReceivedDevOtp(null);
      setSuccessMsg(`Đã gửi mã xác thực OTP 6 chữ số đến hộp thư ${email.trim()}. Vui lòng mở Gmail để lấy mã.`);
      setOtpExpirySecs(res.expiresInSeconds || 300);
      setResendCooldown(res.resendCooldownSeconds || 60);
      setStep(2);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Không thể gửi mã xác thực. Vui lòng thử lại.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  // BƯỚC 2: Xác thực mã OTP 6 chữ số trước khi cho phép nhập mật khẩu mới
  const handleVerifyOtpStep = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    const cleanCode = otpCode.trim();
    if (cleanCode.length !== 6) {
      setError("Vui lòng nhập đúng mã OTP gồm 6 chữ số.");
      return;
    }

    if (otpExpirySecs <= 0) {
      setError("Mã OTP đã hết hạn (quá 5 phút). Vui lòng bấm 'Gửi lại mã OTP'.");
      return;
    }

    setLoading(true);
    try {
      await api.verifyOtp(email.trim(), cleanCode, "forgot-password");
      setSuccessMsg("Xác thực mã OTP thành công! Vui lòng thiết lập mật khẩu mới cho tài khoản của bạn.");
      setStep(3);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Mã OTP không chính xác hoặc đã hết hạn.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  // BƯỚC 3: Đặt mật khẩu mới & Vô hiệu hóa OTP cũ trong Database
  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccessMsg(null);

    if (!isPasswordValid) {
      setError("Mật khẩu mới chưa đáp ứng đủ 5 tiêu chuẩn bảo mật.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setError("Mật khẩu xác nhận không trùng khớp với mật khẩu mới.");
      return;
    }

    setLoading(true);
    try {
      await api.resetPassword({
        email: email.trim(),
        code: otpCode.trim(),
        newPassword,
        confirmPassword,
      });

      setSuccessMsg("Đặt lại mật khẩu thành công! Mã OTP cũ đã được vô hiệu hóa. Đang chuyển về trang Đăng nhập...");
      setTimeout(() => {
        router.push("/auth/login");
      }, 2200);
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
            Khôi Phục Mật Khẩu Qua OTP
          </h1>
          <p className="text-xs text-[#786C65]">
            {step === 1 && "Bước 1: Nhập email đã đăng ký để nhận mã OTP 6 chữ số qua Gmail SMTP"}
            {step === 2 && `Bước 2: Xác thực mã OTP 6 số đã gửi tới ${email} (hiệu lực 5 phút)`}
            {step === 3 && "Bước 3: Thiết lập mật khẩu mới an toàn cho tài khoản của bạn"}
          </p>
        </div>

        {/* 3-Step Progress Stepper */}
        <div className="flex items-center justify-center gap-2 text-xs font-bold text-[#786C65]">
          <span
            className={`px-2.5 py-1 rounded-full text-[11px] ${
              step === 1 ? "bg-[#6F4E37] text-white" : "bg-emerald-600 text-white"
            }`}
          >
            1. Email
          </span>
          <span className="w-4 h-0.5 bg-[#EFE6DC]" />
          <span
            className={`px-2.5 py-1 rounded-full text-[11px] ${
              step === 2
                ? "bg-[#6F4E37] text-white"
                : step > 2
                ? "bg-emerald-600 text-white"
                : "bg-[#EFE6DC] text-[#786C65]"
            }`}
          >
            2. Xác thực OTP
          </span>
          <span className="w-4 h-0.5 bg-[#EFE6DC]" />
          <span
            className={`px-2.5 py-1 rounded-full text-[11px] ${
              step === 3 ? "bg-[#6F4E37] text-white" : "bg-[#EFE6DC] text-[#786C65]"
            }`}
          >
            3. Mật khẩu mới
          </span>
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

        {/* Form Container */}
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          {step === 1 && (
            <form onSubmit={handleSendOtp} className="space-y-4">
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">
                  Email tài khoản đã đăng ký *
                </label>
                <div className="relative">
                  <input
                    type="email"
                    required
                    placeholder="ten@gmail.com"
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
                <span>{loading ? "Đang gửi mã OTP qua Gmail..." : "Nhận Mã Xác Thực OTP (6 Số)"}</span>
                <ArrowRight className="w-4 h-4" />
              </button>
            </form>
          )}

          {step === 2 && (
            <form onSubmit={handleVerifyOtpStep} className="space-y-4">
              <div className="flex items-center justify-between p-3 rounded-2xl bg-amber-50 border border-amber-200 text-xs">
                <span className="font-bold text-amber-950 flex items-center gap-1.5">
                  <Clock className="w-4 h-4 text-[#6F4E37]" />
                  <span>Hiệu lực mã OTP (5 phút):</span>
                </span>
                <span className="font-mono font-black text-red-600 bg-white px-2.5 py-0.5 rounded-lg border border-red-200">
                  {formatTimer(otpExpirySecs)}
                </span>
              </div>

              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className="text-xs font-bold text-[#2D2522]">
                    Nhập mã OTP 6 chữ số từ hộp thư Gmail *
                  </label>
                  <button
                    type="button"
                    disabled={resendCooldown > 0 || loading}
                    onClick={() => handleSendOtp()}
                    className="text-[11px] font-bold text-[#6F4E37] hover:underline disabled:text-[#A89F91] flex items-center gap-1"
                  >
                    <RefreshCw className="w-3 h-3" />
                    <span>{resendCooldown > 0 ? `Gửi lại sau ${resendCooldown}s` : "Gửi lại mã OTP"}</span>
                  </button>
                </div>
                <div className="relative">
                  <input
                    type="text"
                    required
                    maxLength={6}
                    placeholder="••••••"
                    value={otpCode}
                    onChange={(e) => setOtpCode(e.target.value.replace(/\D/g, ""))}
                    className="w-full py-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-center text-2xl font-mono tracking-[0.5em] font-black text-[#6F4E37] focus:outline-none focus:border-[#6F4E37]"
                  />
                  <KeyRound className="w-4 h-4 text-[#786C65] absolute left-4 top-4" />
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setStep(1)}
                  className="px-4 py-3.5 rounded-2xl border border-[#EFE6DC] text-xs font-bold text-[#786C65] hover:bg-[#FAF7F2]"
                >
                  Đổi Email
                </button>
                <button
                  type="submit"
                  disabled={loading || otpCode.length !== 6}
                  className="flex-1 py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  <span>{loading ? "Đang kiểm tra OTP..." : "Xác Thực Mã OTP"}</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </form>
          )}

          {step === 3 && (
            <form onSubmit={handleResetPassword} className="space-y-4">
              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">
                  Mật khẩu mới *
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? "text" : "password"}
                    required
                    placeholder="Nhập mật khẩu mới..."
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="w-full pl-10 pr-10 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                  />
                  <Lock className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3.5 top-3 text-[#786C65] hover:text-[#2D2522]"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>

                {/* Checklist Tiêu Chuẩn Mật Khẩu */}
                <div className="mt-2.5 p-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-1 text-[11px]">
                  <span className="font-bold text-[#6F4E37] block">Tiêu chuẩn bảo mật mật khẩu:</span>
                  <div className="grid grid-cols-2 gap-1 pt-1">
                    <span className={`flex items-center gap-1 ${hasMinLength ? "text-emerald-700 font-bold" : "text-[#786C65]"}`}>
                      <Check className="w-3 h-3" /> Tối thiểu 8 ký tự
                    </span>
                    <span className={`flex items-center gap-1 ${hasUpperCase ? "text-emerald-700 font-bold" : "text-[#786C65]"}`}>
                      <Check className="w-3 h-3" /> Chữ in HOA (A-Z)
                    </span>
                    <span className={`flex items-center gap-1 ${hasLowerCase ? "text-emerald-700 font-bold" : "text-[#786C65]"}`}>
                      <Check className="w-3 h-3" /> Chữ thường (a-z)
                    </span>
                    <span className={`flex items-center gap-1 ${hasNumber ? "text-emerald-700 font-bold" : "text-[#786C65]"}`}>
                      <Check className="w-3 h-3" /> Chữ số (0-9)
                    </span>
                    <span className={`flex items-center gap-1 col-span-2 ${hasSpecialChar ? "text-emerald-700 font-bold" : "text-[#786C65]"}`}>
                      <Check className="w-3 h-3" /> Ký tự đặc biệt (!@#$%^&*...)
                    </span>
                  </div>
                </div>
              </div>

              <div>
                <label className="text-xs font-bold text-[#2D2522] block mb-1">
                  Xác nhận mật khẩu mới *
                </label>
                <div className="relative">
                  <input
                    type={showPassword ? "text" : "password"}
                    required
                    placeholder="Nhập lại mật khẩu mới..."
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                  />
                  <ShieldCheck className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
                </div>
              </div>

              <button
                type="submit"
                disabled={loading || !isPasswordValid}
                className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
              >
                <span>{loading ? "Đang lưu mật khẩu mới..." : "Cập Nhật Mật Khẩu & Vô Hiệu Hóa OTP"}</span>
                <Check className="w-4 h-4" />
              </button>
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
