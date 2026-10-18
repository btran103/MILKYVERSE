"use client";

import React, { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { api } from "@/lib/api";
import {
  Lock,
  Mail,
  User,
  Phone,
  MapPin,
  ArrowRight,
  AlertCircle,
  CheckCircle2,
  Eye,
  EyeOff,
  ArrowLeft,
  Check,
  ShieldCheck,
  RefreshCw,
} from "lucide-react";

export default function RegisterPage() {
  const router = useRouter();
  const { register } = useAuth();

  // Step 1 vs Step 2 (OTP Verification)
  const [step, setStep] = useState<1 | 2>(1);

  // Form Fields
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [address, setAddress] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  // Visibility toggles
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  // OTP Verification Code
  const [verificationCode, setVerificationCode] = useState("");
  const [receivedOtpCode, setReceivedOtpCode] = useState<string | null>(null);
  const [sentViaSmtp, setSentViaSmtp] = useState(false);
  const [isSendingOtp, setIsSendingOtp] = useState(false);
  const [otpSentMessage, setOtpSentMessage] = useState<string | null>(null);
  const [otpCountdown, setOtpCountdown] = useState(0);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Password criteria checks
  const hasMinLength = password.length >= 8;
  const hasUpperCase = /[A-Z]/.test(password);
  const hasLowerCase = /[a-z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasSpecialChar = /[^A-Za-z0-9]/.test(password);
  const isPasswordValid = hasMinLength && hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;

  const startCountdown = () => {
    setOtpCountdown(60);
    const interval = setInterval(() => {
      setOtpCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  };

  // Step 1 Submit: Validate & Send OTP -> Advance to Step 2
  const handleInitialSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!fullName.trim()) {
      setError("Vui lòng nhập họ và tên của bạn.");
      return;
    }

    if (!email.trim() || !email.includes("@")) {
      setError("Vui lòng nhập địa chỉ email hợp lệ (ví dụ: ten@gmail.com).");
      return;
    }

    const missingList: string[] = [];
    if (!hasMinLength) missingList.push("tối thiểu 8 ký tự");
    if (!hasUpperCase) missingList.push("chữ in HOA (A-Z)");
    if (!hasLowerCase) missingList.push("chữ thường (a-z)");
    if (!hasNumber) missingList.push("chữ số (0-9)");
    if (!hasSpecialChar) missingList.push("ký tự đặc biệt (@, #, $, %...)");

    if (missingList.length > 0) {
      setError(`Mật khẩu chưa đạt tiêu chuẩn: Còn thiếu ${missingList.join(", ")}.`);
      return;
    }

    if (password !== confirmPassword) {
      setError("Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập.");
      return;
    }

    // Call API to send OTP to the user's email
    setLoading(true);
    try {
      const res = await api.sendOtp(email.trim());
      if (res.code) {
        setReceivedOtpCode(res.code);
      }
      setSentViaSmtp(!!res.sentViaSmtp);
      setOtpSentMessage(res.message || `Mã OTP 6 số đã được chuẩn bị cho ${email.trim()}.`);
      startCountdown();
      setStep(2); // Advance to OTP verification screen
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Không thể gửi mã xác nhận đến email.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  // Step 2 Resend OTP
  const handleResendOtp = async () => {
    if (otpCountdown > 0 || isSendingOtp) return;
    setError(null);
    setIsSendingOtp(true);
    try {
      const res = await api.sendOtp(email.trim());
      if (res.code) {
        setReceivedOtpCode(res.code);
      }
      setSentViaSmtp(!!res.sentViaSmtp);
      setOtpSentMessage(res.message || `Đã gửi lại mã OTP mới tới ${email.trim()}.`);
      startCountdown();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Lỗi khi gửi lại mã OTP.";
      setError(msg);
    } finally {
      setIsSendingOtp(false);
    }
  };

  // Step 2 Submit: Verify OTP & Complete Registration
  const handleVerifyAndRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const cleanCode = verificationCode.trim();
    if (!cleanCode || cleanCode.length !== 6) {
      setError("Vui lòng nhập đầy đủ mã OTP 6 chữ số.");
      return;
    }

    setLoading(true);
    try {
      const res = await register({
        fullName: fullName.trim(),
        email: email.trim(),
        password,
        verificationCode: cleanCode,
        phoneNumber: phoneNumber.trim(),
        address: address.trim(),
      });

      if (res.success) {
        router.push("/");
      } else {
        setError(res.message || "Mã xác thực không chính xác hoặc đã hết hạn.");
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Đăng ký không thành công.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-md mx-auto px-4 py-16 space-y-6">
      {/* Header Banner */}
      <div className="text-center space-y-2">
        <div className="w-16 h-16 rounded-full overflow-hidden bg-white border border-[#EFE6DC] shadow-md flex items-center justify-center mx-auto p-1">
          {step === 1 ? (
            <Image
              src="/logo.png"
              alt="MilkyVerse Logo"
              width={64}
              height={64}
              className="w-full h-full object-cover rounded-full"
              priority
            />
          ) : (
            <ShieldCheck className="w-8 h-8 text-[#6F4E37]" />
          )}
        </div>
        <h1 className="text-2xl font-black text-[#2D2522]">
          {step === 1 ? "Tạo Tài Khoản Mới" : "Xác Thực Mã OTP"}
        </h1>
        <p className="text-xs text-[#786C65]">
          {step === 1
            ? "Đăng ký thành viên MilkyVerse và kích hoạt bảo mật tài khoản bằng OTP thật"
            : `Hệ thống đã gửi mã xác nhận 6 số đến: ${email}`}
        </p>
      </div>

      {/* Progress Pill Indicator */}
      <div className="flex items-center justify-center gap-2 text-xs">
        <div
          className={`flex items-center gap-1.5 px-3 py-1 rounded-full font-bold transition-all ${
            step === 1
              ? "bg-[#6F4E37] text-white shadow-xs"
              : "bg-[#EFE6DC] text-[#786C65]"
          }`}
        >
          <span>1. Thông tin</span>
        </div>
        <div className="w-4 h-0.5 bg-[#EFE6DC]" />
        <div
          className={`flex items-center gap-1.5 px-3 py-1 rounded-full font-bold transition-all ${
            step === 2
              ? "bg-[#6F4E37] text-white shadow-xs"
              : "bg-[#EFE6DC] text-[#786C65]"
          }`}
        >
          <span>2. Xác thực OTP</span>
        </div>
      </div>

      {error && (
        <div className="p-3.5 rounded-2xl bg-red-50 text-red-700 border border-red-200 text-xs flex items-center gap-2.5 animate-in fade-in">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {otpSentMessage && step === 2 && (
        <div className="p-3.5 rounded-2xl bg-emerald-50 text-emerald-800 border border-emerald-200 text-xs flex items-start gap-2.5 animate-in fade-in">
          <CheckCircle2 className="w-4 h-4 shrink-0 mt-0.5 text-emerald-600" />
          <span>{otpSentMessage}</span>
        </div>
      )}

      {/* BƯỚC 1: ĐIỀN THÔNG TIN TÀI KHOẢN */}
      {step === 1 && (
        <form onSubmit={handleInitialSubmit} noValidate className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1">Họ và tên *</label>
            <div className="relative">
              <input
                type="text"
                required
                placeholder="Ví dụ: Hoàng Mai"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <User className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
            </div>
          </div>

          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1">Email đăng ký (nhận OTP thật) *</label>
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
            <label className="text-xs font-bold text-[#2D2522] block mb-1">Số điện thoại</label>
            <div className="relative">
              <input
                type="tel"
                placeholder="0901234567"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <Phone className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
            </div>
          </div>

          {/* MẬT KHẨU CÓ ẨN / HIỆN & TIÊU CHUẨN BẢO MẬT */}
          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1">Mật khẩu *</label>
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                required
                placeholder="Ví dụ: Milky@2026"
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

            {/* Checklist Tiêu Chuẩn Mật Khẩu */}
            <div className="mt-2.5 p-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-1.5 text-[11px]">
              <span className="font-bold text-[#6F4E37] block">Yêu cầu bảo mật mật khẩu:</span>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-1 pt-1">
                <div className={`flex items-center gap-1.5 ${hasMinLength ? "text-emerald-700 font-semibold" : "text-[#786C65]"}`}>
                  <Check className={`w-3.5 h-3.5 ${hasMinLength ? "text-emerald-600" : "text-gray-300"}`} />
                  <span>Ít nhất 8 ký tự</span>
                </div>
                <div className={`flex items-center gap-1.5 ${hasUpperCase ? "text-emerald-700 font-semibold" : "text-[#786C65]"}`}>
                  <Check className={`w-3.5 h-3.5 ${hasUpperCase ? "text-emerald-600" : "text-gray-300"}`} />
                  <span>Có chữ in HOA (A-Z)</span>
                </div>
                <div className={`flex items-center gap-1.5 ${hasLowerCase ? "text-emerald-700 font-semibold" : "text-[#786C65]"}`}>
                  <Check className={`w-3.5 h-3.5 ${hasLowerCase ? "text-emerald-600" : "text-gray-300"}`} />
                  <span>Có chữ thường (a-z)</span>
                </div>
                <div className={`flex items-center gap-1.5 ${hasNumber ? "text-emerald-700 font-semibold" : "text-[#786C65]"}`}>
                  <Check className={`w-3.5 h-3.5 ${hasNumber ? "text-emerald-600" : "text-gray-300"}`} />
                  <span>Có chữ số (0-9)</span>
                </div>
                <div className={`flex items-center gap-1.5 sm:col-span-2 ${hasSpecialChar ? "text-emerald-700 font-semibold" : "text-[#786C65]"}`}>
                  <Check className={`w-3.5 h-3.5 ${hasSpecialChar ? "text-emerald-600" : "text-gray-300"}`} />
                  <span>Có ký tự đặc biệt (!@#$%^&*...)</span>
                </div>
              </div>
            </div>
          </div>

          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1">Xác nhận mật khẩu *</label>
            <div className="relative">
              <input
                type={showConfirmPassword ? "text" : "password"}
                required
                placeholder="Nhập lại chính xác mật khẩu"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="w-full pl-10 pr-10 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <Lock className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                className="absolute right-3.5 top-3 text-[#786C65] hover:text-[#2D2522] transition-colors"
                aria-label={showConfirmPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              >
                {showConfirmPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1">Địa chỉ giao hàng</label>
            <div className="relative">
              <input
                type="text"
                placeholder="Số nhà, tên đường, quận/huyện..."
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                className="w-full pl-10 pr-3 py-2.5 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <MapPin className="w-4 h-4 text-[#786C65] absolute left-3.5 top-3" />
            </div>
          </div>

          {error && (
            <div className="p-3.5 rounded-2xl bg-red-50 text-red-700 border border-red-200 text-xs flex items-center gap-2.5 animate-in fade-in">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <button
            type="submit"
            disabled={loading || !isPasswordValid}
            className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
          >
            <span>{loading ? "Đang gửi mã OTP..." : "Đăng Ký & Nhận Mã OTP"}</span>
            <ArrowRight className="w-4 h-4" />
          </button>

          <p className="text-center text-xs text-[#786C65] pt-2">
            Đã có tài khoản?{" "}
            <Link href="/auth/login" className="font-bold text-[#6F4E37] hover:underline">
              Đăng nhập ngay
            </Link>
          </p>
        </form>
      )}

      {/* BƯỚC 2: XÁC THỰC MÃ OTP 6 CHỮ SỐ */}
      {step === 2 && (
        <form onSubmit={handleVerifyAndRegister} className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-5 animate-in fade-in">
          <div className="text-center space-y-1">
            <h3 className="text-sm font-black text-[#2D2522]">Nhập mã OTP 6 chữ số</h3>
            <p className="text-xs text-[#786C65]">
              Mã xác nhận bảo mật được gửi đến email: <br />
              <strong className="text-[#6F4E37]">{email}</strong>
            </p>
          </div>

          {/* Hiển thị mã OTP hỗ trợ nếu chưa có SMTP hoặc để test nhanh */}
          {receivedOtpCode && (
            <div className="p-3.5 rounded-2xl bg-amber-50/80 border border-amber-200 text-xs space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-[#6F4E37] font-bold">Mã OTP của bạn:</span>
                <span className="font-mono text-base font-black text-[#6F4E37] tracking-widest bg-white px-2.5 py-0.5 rounded-lg border border-amber-300">
                  {receivedOtpCode}
                </span>
              </div>
              <div className="flex items-center justify-between pt-0.5">
                <p className="text-[10px] text-[#786C65]">
                  {sentViaSmtp
                    ? "Đã gửi đến email của bạn."
                    : "Mã xác thực được tạo từ hệ thống."}
                </p>
                <button
                  type="button"
                  onClick={() => setVerificationCode(receivedOtpCode)}
                  className="px-2.5 py-1 rounded-lg bg-[#6F4E37] text-white text-[10px] font-bold hover:bg-[#4A3324] transition-all"
                >
                  Tự động điền mã
                </button>
              </div>
            </div>
          )}

          <div>
            <label className="text-xs font-bold text-[#2D2522] block mb-1 text-center">
              Mã xác thực 6 số
            </label>
            <div className="relative max-w-xs mx-auto">
              <input
                type="text"
                required
                maxLength={6}
                autoFocus
                placeholder="••••••"
                value={verificationCode}
                onChange={(e) => setVerificationCode(e.target.value.replace(/\D/g, ""))}
                className="w-full py-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-center text-2xl font-mono tracking-[0.5em] font-black text-[#6F4E37] focus:outline-none focus:border-[#6F4E37]"
              />
            </div>
            <p className="text-[10px] text-center text-[#786C65] mt-1.5">
              Kiểm tra hộp thư đến hoặc mục Thư rác (Spam) trong email của bạn.
            </p>
          </div>

          <div className="flex flex-col gap-2 pt-2">
            <button
              type="submit"
              disabled={loading || verificationCode.length !== 6}
              className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2 disabled:opacity-50"
            >
              <span>{loading ? "Đang xác thực tài khoản..." : "Xác Nhận & Hoàn Tất Đăng Ký"}</span>
              <Check className="w-4 h-4" />
            </button>

            <div className="flex items-center justify-between text-xs pt-2">
              <button
                type="button"
                onClick={() => setStep(1)}
                className="text-[#786C65] hover:text-[#2D2522] flex items-center gap-1 font-semibold"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                <span>Sửa thông tin</span>
              </button>

              <button
                type="button"
                disabled={otpCountdown > 0 || isSendingOtp}
                onClick={handleResendOtp}
                className="text-[#6F4E37] hover:underline font-bold disabled:opacity-50 disabled:no-underline flex items-center gap-1"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${isSendingOtp ? "animate-spin" : ""}`} />
                <span>
                  {otpCountdown > 0 ? `Gửi lại mã (${otpCountdown}s)` : "Gửi lại mã OTP"}
                </span>
              </button>
            </div>
          </div>
        </form>
      )}
    </div>
  );
}
