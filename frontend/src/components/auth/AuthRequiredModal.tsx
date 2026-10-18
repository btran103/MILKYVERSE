"use client";

import React from "react";
import { useRouter } from "next/navigation";
import { Lock, UserPlus, LogIn, X, Gift } from "lucide-react";

interface AuthRequiredModalProps {
  isOpen: boolean;
  onClose: () => void;
  message?: string;
}

export const AuthRequiredModal: React.FC<AuthRequiredModalProps> = ({
  isOpen,
  onClose,
  message = "Bạn cần đăng nhập hoặc tạo tài khoản để thêm món vào giỏ hàng và đặt trà sữa MilkyVerse.",
}) => {
  const router = useRouter();

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white w-full max-w-md rounded-3xl border border-[#EFE6DC] shadow-2xl overflow-hidden p-6 relative">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 rounded-full text-[#786C65] hover:bg-[#FAF7F2] transition-colors"
          aria-label="Đóng"
        >
          <X className="w-4 h-4" />
        </button>

        <div className="text-center space-y-3 pt-2">
          <div className="w-14 h-14 rounded-2xl bg-[#F7D6D0] text-[#6F4E37] flex items-center justify-center mx-auto shadow-inner">
            <Lock className="w-7 h-7" />
          </div>

          <h3 className="text-xl font-black text-[#2D2522]">Yêu Cầu Đăng Nhập</h3>

          <p className="text-xs text-[#786C65] leading-relaxed px-4">
            {message}
          </p>

          <div className="bg-[#FAF7F2] p-3.5 rounded-2xl border border-[#EFE6DC] text-left text-xs space-y-2 mt-4">
            <div className="flex items-center gap-2 text-[#6F4E37] font-bold">
              <Gift className="w-4 h-4 text-[#C5A059] shrink-0" />
              <span>Đặc quyền tài khoản MilkyVerse:</span>
            </div>
            <ul className="text-[11px] text-[#786C65] space-y-1 list-disc list-inside">
              <li>Nhận ngay mã giảm 20% cho đơn hàng đầu tiên</li>
              <li>Lưu công thức DIY cá nhân hóa & nhận đề xuất từ AI</li>
              <li>Tích điểm thành viên và theo dõi vận chuyển trực tiếp</li>
            </ul>
          </div>

          <div className="grid grid-cols-2 gap-3 pt-3">
            <button
              onClick={() => {
                onClose();
                router.push("/auth/login");
              }}
              className="py-3 px-4 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-1.5"
            >
              <LogIn className="w-3.5 h-3.5" />
              <span>Đăng Nhập</span>
            </button>

            <button
              onClick={() => {
                onClose();
                router.push("/auth/register");
              }}
              className="py-3 px-4 rounded-xl bg-white border border-[#6F4E37] text-[#6F4E37] text-xs font-bold hover:bg-[#FAF7F2] transition-all flex items-center justify-center gap-1.5"
            >
              <UserPlus className="w-3.5 h-3.5" />
              <span>Đăng Ký</span>
            </button>
          </div>

          <button
            onClick={onClose}
            className="text-[11px] text-[#786C65] hover:underline pt-1 block mx-auto"
          >
            Tiếp tục xem thực đơn
          </button>
        </div>
      </div>
    </div>
  );
};
