"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { OrderResponse } from "@/types";
import { useAuth } from "@/context/AuthContext";
import {
  Package,
  ArrowRight,
  ExternalLink,
} from "lucide-react";

export default function OrdersListPage() {
  const { isLoading } = useAuth();
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadOrders() {
      try {
        const data = await api.getOrders();
        setOrders(data || []);
      } catch (err) {
        console.error("Lỗi khi tải đơn hàng:", err);
      } finally {
        setLoading(false);
      }
    }

    if (!isLoading) {
      loadOrders();
    }
  }, [isLoading]);

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      <div className="border-b border-[#EFE6DC] pb-6">
        <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
          Lịch Sử Đơn Hàng Của Bạn
        </h1>
        <p className="text-xs text-[#786C65] mt-1">
          Theo dõi hành trình và trạng thái các ly trà sữa MilkyVerse bạn đã đặt
        </p>
      </div>

      {loading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((n) => (
            <div key={n} className="h-28 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
          ))}
        </div>
      ) : orders.length === 0 ? (
        <div className="bg-white rounded-3xl p-12 text-center border border-[#EFE6DC] space-y-4">
          <div className="w-16 h-16 rounded-full bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-center mx-auto text-[#6F4E37]">
            <Package className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-[#2D2522]">Bạn chưa có đơn hàng nào</h3>
          <p className="text-xs text-[#786C65]">
            Hãy khám phá các hương vị signature hoặc tự phối ly trà sữa chuẩn gu trong DIY Studio!
          </p>
          <div className="pt-2">
            <Link
              href="/menu"
              className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold shadow-md hover:bg-[#4A3324] transition-colors"
            >
              <span>Đặt ly đầu tiên ngay</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {orders.map((o) => (
            <div
              key={o.id}
              className="bg-white rounded-3xl p-6 border border-[#EFE6DC] shadow-sm hover:shadow-md transition-shadow flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4"
            >
              <div className="space-y-1">
                <div className="flex items-center gap-3">
                  <span className="font-black text-[#2D2522] text-base">
                    #{o.orderCode}
                  </span>
                  <span
                    className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${
                      o.status === "Completed"
                        ? "bg-emerald-100 text-emerald-800"
                        : o.status === "Cancelled"
                        ? "bg-red-100 text-red-800"
                        : "bg-amber-100 text-amber-800"
                    }`}
                  >
                    {o.status}
                  </span>
                </div>
                <p className="text-xs text-[#786C65]">
                  {new Date(o.createdAt).toLocaleString("vi-VN")} • {o.items?.length || 0} món đồ uống
                </p>
                <p className="text-xs text-[#786C65] line-clamp-1">
                  Giao đến: {o.shippingAddress}
                </p>
              </div>

              <div className="flex items-center justify-between w-full sm:w-auto gap-6 pt-3 sm:pt-0 border-t sm:border-t-0 border-[#FAF7F2]">
                <div className="text-right">
                  <span className="text-lg font-black text-[#6F4E37] block">
                    {o.totalAmount.toLocaleString("vi-VN")}đ
                  </span>
                  <span className="text-[11px] text-[#786C65]">
                    {o.paymentMethod} ({o.paymentStatus === "Paid" ? "Đã TT" : "Chưa TT"})
                  </span>
                </div>

                <Link
                  href={`/orders/${o.id}`}
                  className="px-4 py-2 rounded-xl bg-[#FAF7F2] hover:bg-[#F7D6D0]/40 text-[#6F4E37] text-xs font-bold transition-colors flex items-center gap-1.5"
                >
                  <span>Chi tiết</span>
                  <ExternalLink className="w-3.5 h-3.5" />
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
