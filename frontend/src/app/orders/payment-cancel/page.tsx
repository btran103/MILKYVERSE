"use client";

import React, { Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { XCircle, ArrowLeft, RotateCcw } from "lucide-react";

function PaymentCancelContent() {
  const searchParams = useSearchParams();
  const orderId = searchParams.get("orderId");

  return (
    <div className="max-w-md mx-auto px-4 py-20 text-center space-y-6">
      {/* Alert Icon */}
      <div className="w-20 h-20 rounded-full bg-red-100 text-red-600 flex items-center justify-center mx-auto shadow-md ring-8 ring-red-50">
        <XCircle className="w-12 h-12" />
      </div>

      <div className="space-y-2">
        <h1 className="text-2xl font-black text-[#2D2522]">
          THANH TOÁN ĐÃ BỊ HỦY
        </h1>
        <p className="text-xs text-[#786C65] leading-relaxed">
          Giao dịch thanh toán PayOS đã bị hủy hoặc chưa hoàn tất. Bạn có thể thử thanh toán lại hoặc chọn phương thức thanh toán tiền mặt (COD).
        </p>
      </div>

      <div className="flex flex-col gap-2.5 pt-2">
        {orderId ? (
          <Link
            href={`/orders/${orderId}`}
            className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2"
          >
            <RotateCcw className="w-4 h-4" />
            <span>Thử Thanh Toán Lại Đơn #{orderId}</span>
          </Link>
        ) : (
          <Link
            href="/cart"
            className="w-full py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2"
          >
            <RotateCcw className="w-4 h-4" />
            <span>Quay Lại Giỏ Hàng</span>
          </Link>
        )}

        <Link
          href="/menu"
          className="w-full py-3 rounded-2xl bg-white border border-[#EFE6DC] text-[#2D2522] font-bold text-xs hover:bg-[#FAF7F2] transition-all flex items-center justify-center gap-2"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Về Trang Thực Đơn</span>
        </Link>
      </div>
    </div>
  );
}

export default function PaymentCancelPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-xl mx-auto px-4 py-24 text-center space-y-4">
          <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-xs font-bold text-[#786C65]">Đang tải...</p>
        </div>
      }
    >
      <PaymentCancelContent />
    </Suspense>
  );
}
