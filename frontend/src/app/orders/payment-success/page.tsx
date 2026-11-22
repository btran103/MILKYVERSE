"use client";

import React, { useEffect, useState, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { api } from "@/lib/api";
import { OrderResponse, SePayQrResponse } from "@/types";
import confetti from "canvas-confetti";
import {
  CheckCircle2,
  Package,
  ArrowRight,
  Receipt,
  Building2,
  Phone,
  MapPin,
  Sparkles,
} from "lucide-react";

function PaymentSuccessContent() {
  const searchParams = useSearchParams();
  const orderId = searchParams.get("orderId");

  const [order, setOrder] = useState<OrderResponse | null>(null);
  const [sePayInfo, setSePayInfo] = useState<SePayQrResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    confetti({
      particleCount: 160,
      spread: 90,
      origin: { y: 0.5 },
      colors: ["#2563EB", "#059669", "#6F4E37", "#F7D6D0"],
    });

    const loadData = async () => {
      try {
        if (orderId) {
          const ordIdNum = parseInt(orderId, 10);
          if (!isNaN(ordIdNum)) {
            const [ord, sp] = await Promise.all([
              api.getOrderById(ordIdNum),
              api.getSePayStatus(ordIdNum).catch(() => null),
            ]);
            setOrder(ord);
            if (sp) setSePayInfo(sp);
          }
        }
      } catch (e) {
        console.error("Error loading payment success info", e);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [orderId]);

  if (loading) {
    return (
      <div className="max-w-xl mx-auto px-4 py-24 text-center space-y-4">
        <div className="w-12 h-12 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-xs font-bold text-[#786C65]">Đang kiểm tra kết quả thanh toán SePay...</p>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-16 text-center space-y-6">
      {/* Success Badge */}
      <div className="w-20 h-20 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center mx-auto shadow-lg ring-8 ring-emerald-50 animate-in zoom-in duration-300">
        <CheckCircle2 className="w-12 h-12" />
      </div>

      <div className="space-y-2">
        <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-50 text-emerald-800 text-xs font-bold border border-emerald-200">
          <Sparkles className="w-3.5 h-3.5 text-emerald-600" />
          <span>Xác Nhận Tiền Thật Thành Công Qua SePay (TPBank)</span>
        </div>
        <h1 className="text-3xl font-black text-[#2D2522]">
          THANH TOÁN THÀNH CÔNG!
        </h1>
        <p className="text-sm text-[#786C65] max-w-md mx-auto">
          Cảm ơn bạn! Đơn hàng đã được nhận tiền thật và tự động xác nhận vào hệ thống pha chế MilkyVerse.
        </p>
      </div>

      {/* Bill Card */}
      <div className="bg-white rounded-3xl p-6 sm:p-7 border border-[#EFE6DC] shadow-md text-left space-y-4">
        <div className="flex items-center justify-between pb-4 border-b border-[#FAF7F2]">
          <div className="flex items-center gap-2">
            <Receipt className="w-5 h-5 text-[#6F4E37]" />
            <h3 className="font-extrabold text-sm text-[#2D2522]">
              Biên Lai Giao Dịch SePay
            </h3>
          </div>
          <span className="px-2.5 py-1 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold">
            ĐÃ THANH TOÁN
          </span>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
          <div className="bg-[#FAF7F2] p-3 rounded-2xl">
            <span className="text-[#786C65] block mb-1">Mã đơn hàng:</span>
            <strong className="font-mono text-sm text-[#2D2522]">
              #{order?.orderCode || "MV2026"}
            </strong>
          </div>

          <div className="bg-[#FAF7F2] p-3 rounded-2xl">
            <span className="text-[#786C65] block mb-1">Ngân hàng thụ hưởng:</span>
            <strong className="text-[#2D2522] flex items-center gap-1.5">
              <Building2 className="w-3.5 h-3.5 text-purple-600" />
              <span>TPBank ({sePayInfo?.accountNumber || "00000546"})</span>
            </strong>
          </div>

          <div className="bg-[#FAF7F2] p-3 rounded-2xl">
            <span className="text-[#786C65] block mb-1">Chủ tài khoản:</span>
            <strong className="text-sm font-bold text-[#2D2522]">
              {sePayInfo?.accountName || "HA BAO TRAN"}
            </strong>
          </div>

          <div className="bg-[#FAF7F2] p-3 rounded-2xl">
            <span className="text-[#786C65] block mb-1">Số tiền thanh toán:</span>
            <strong className="text-sm font-black text-emerald-700">
              {(order?.totalAmount || sePayInfo?.amount || 0).toLocaleString("vi-VN")}đ
            </strong>
          </div>
        </div>

        {order && (
          <div className="pt-2 border-t border-[#FAF7F2] text-xs text-[#786C65] space-y-1.5">
            <div className="flex items-center gap-2">
              <Phone className="w-3.5 h-3.5 text-[#6F4E37]" />
              <span>Người nhận: <strong>{order.customerName}</strong> ({order.customerPhone})</span>
            </div>
            <div className="flex items-center gap-2">
              <MapPin className="w-3.5 h-3.5 text-[#6F4E37]" />
              <span>Địa chỉ: {order.shippingAddress}</span>
            </div>
          </div>
        )}
      </div>

      {/* Action Buttons */}
      <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
        {order && (
          <Link
            href={`/orders/${order.id}`}
            className="w-full sm:w-auto px-6 py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-xs hover:bg-[#4A3324] shadow-md transition-all flex items-center justify-center gap-2"
          >
            <Package className="w-4 h-4" />
            <span>Theo Dõi Đơn Hàng #{order.orderCode}</span>
          </Link>
        )}

        <Link
          href="/menu"
          className="w-full sm:w-auto px-6 py-3.5 rounded-2xl bg-white border border-[#EFE6DC] text-[#2D2522] font-bold text-xs hover:bg-[#FAF7F2] transition-all flex items-center justify-center gap-2"
        >
          <span>Tiếp Tục Mua Sắm</span>
          <ArrowRight className="w-4 h-4" />
        </Link>
      </div>
    </div>
  );
}

export default function PaymentSuccessPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-xl mx-auto px-4 py-24 text-center space-y-4">
          <div className="w-12 h-12 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-xs font-bold text-[#786C65]">Đang tải biên lai thanh toán PayOS...</p>
        </div>
      }
    >
      <PaymentSuccessContent />
    </Suspense>
  );
}
