"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { DashboardSummary } from "@/types";
import {
  DollarSign,
  ShoppingBag,
  Clock,
  Users,
  AlertTriangle,
  ArrowRight,
} from "lucide-react";

export default function AdminDashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      try {
        const data = await api.getDashboard();
        setSummary(data);
      } catch (err) {
        console.error("Lỗi khi tải dashboard:", err);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-white rounded-xl w-48 animate-pulse" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-32 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
          ))}
        </div>
      </div>
    );
  }

  const revenue = summary?.totalRevenue || 0;
  const ordersCount = summary?.totalOrders || 0;
  const processingCount = summary?.processingOrders || 0;
  const customersCount = summary?.totalCustomers || 0;
  const lowStock = summary?.lowStockIngredients || [];
  const recentOrders = summary?.recentOrders || [];
  const topProducts = summary?.topProducts || [];

  return (
    <div className="space-y-8">
      {/* Title */}
      <div>
        <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
          Tổng Quan Hoạt Động (Dashboard)
        </h1>
        <p className="text-xs text-[#786C65] mt-1">
          Số liệu thời gian thực được tính toán chuẩn xác từ database MilkyVerse
        </p>
      </div>

      {/* KPI Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Revenue */}
        <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-[#786C65] uppercase">Doanh Thu Đã Thu</span>
            <div className="w-9 h-9 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center">
              <DollarSign className="w-5 h-5" />
            </div>
          </div>
          <div>
            <h3 className="text-2xl font-black text-[#2D2522]">
              {revenue.toLocaleString("vi-VN")}đ
            </h3>
            <p className="text-[11px] text-[#4E7D55] font-semibold mt-0.5">
              Chỉ tính các đơn hoàn tất/đã thanh toán
            </p>
          </div>
        </div>

        {/* Total Orders */}
        <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-[#786C65] uppercase">Tổng Đơn Hàng</span>
            <div className="w-9 h-9 rounded-xl bg-[#F7D6D0] text-[#6F4E37] flex items-center justify-center">
              <ShoppingBag className="w-5 h-5" />
            </div>
          </div>
          <div>
            <h3 className="text-2xl font-black text-[#2D2522]">{ordersCount}</h3>
            <p className="text-[11px] text-[#786C65] mt-0.5">Toàn bộ lịch sử đặt hàng</p>
          </div>
        </div>

        {/* Processing Orders */}
        <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-[#786C65] uppercase">Đơn Đang Xử Lý</span>
            <div className="w-9 h-9 rounded-xl bg-amber-100 text-amber-700 flex items-center justify-center">
              <Clock className="w-5 h-5" />
            </div>
          </div>
          <div>
            <h3 className="text-2xl font-black text-[#2D2522]">{processingCount}</h3>
            <p className="text-[11px] text-amber-700 font-semibold mt-0.5">Cần pha chế & giao</p>
          </div>
        </div>

        {/* Total Customers */}
        <div className="bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-[#786C65] uppercase">Khách Hàng</span>
            <div className="w-9 h-9 rounded-xl bg-blue-100 text-blue-700 flex items-center justify-center">
              <Users className="w-5 h-5" />
            </div>
          </div>
          <div>
            <h3 className="text-2xl font-black text-[#2D2522]">{customersCount}</h3>
            <p className="text-[11px] text-[#786C65] mt-0.5">Tài khoản thành viên</p>
          </div>
        </div>
      </div>

      {/* Low Stock Warning Banner if any */}
      {lowStock.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 p-5 rounded-3xl flex items-start justify-between gap-4">
          <div className="flex items-start gap-3">
            <AlertTriangle className="w-5 h-5 text-amber-700 shrink-0 mt-0.5" />
            <div>
              <h4 className="font-extrabold text-sm text-amber-900">
                Cảnh Báo Tồn Kho: Có {lowStock.length} nguyên liệu chạm ngưỡng tối thiểu!
              </h4>
              <p className="text-xs text-amber-800 mt-1">
                Các nguyên liệu cần nhập thêm ngay:{" "}
                <strong>{lowStock.map((i) => `${i.name} (còn ${i.currentStock}${i.unit})`).join(", ")}</strong>
              </p>
            </div>
          </div>
          <Link
            href="/admin/inventory"
            className="px-4 py-2 bg-amber-700 text-white rounded-xl text-xs font-bold shrink-0 hover:bg-amber-800 transition-colors"
          >
            Nhập kho ngay
          </Link>
        </div>
      )}

      {/* Two Columns: Recent Orders & Top Products */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        {/* Recent Orders */}
        <div className="lg:col-span-8 bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider">
              Đơn Hàng Gần Đây
            </h3>
            <Link
              href="/admin/orders"
              className="text-xs font-bold text-[#6F4E37] hover:underline flex items-center gap-1"
            >
              <span>Xem tất cả</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-[#FAF7F2] text-[#786C65] font-bold">
                  <th className="pb-3">Mã đơn</th>
                  <th className="pb-3">Khách hàng</th>
                  <th className="pb-3">Tổng tiền</th>
                  <th className="pb-3">Trạng thái</th>
                  <th className="pb-3">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#FAF7F2]">
                {recentOrders.map((o) => (
                  <tr key={o.id} className="hover:bg-[#FAF7F2] transition-colors">
                    <td className="py-3 font-bold text-[#2D2522]">#{o.orderCode}</td>
                    <td className="py-3">{o.customerName}</td>
                    <td className="py-3 font-black text-[#6F4E37]">
                      {o.totalAmount.toLocaleString("vi-VN")}đ
                    </td>
                    <td className="py-3">
                      <span
                        className={`px-2 py-0.5 rounded-full font-bold text-[10px] ${
                          o.status === "Completed"
                            ? "bg-emerald-100 text-emerald-800"
                            : "bg-amber-100 text-amber-800"
                        }`}
                      >
                        {o.status}
                      </span>
                    </td>
                    <td className="py-3">
                      <Link
                        href={`/orders/${o.id}`}
                        className="text-[#6F4E37] font-bold hover:underline"
                      >
                        Xem
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Top Products */}
        <div className="lg:col-span-4 bg-white p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-extrabold text-[#2D2522] text-sm uppercase tracking-wider">
              Món Bán Chạy
            </h3>
            <Link
              href="/admin/products"
              className="text-xs font-bold text-[#6F4E37] hover:underline"
            >
              Quản lý
            </Link>
          </div>

          <div className="space-y-3">
            {topProducts.map((p) => {
              const id = p.Id ?? p.id;
              const name = p.Name ?? p.name;
              const price = p.BasePrice ?? p.basePrice;
              const img = p.ImageUrl ?? p.imageUrl;

              return (
                <div key={id} className="flex items-center gap-3">
                  <div className="w-12 h-12 rounded-xl overflow-hidden bg-[#FAF7F2] shrink-0 border border-[#EFE6DC]">
                    <img src={img} alt={name} className="w-full h-full object-cover" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="font-bold text-xs text-[#2D2522] truncate">{name}</h4>
                    <span className="text-xs font-black text-[#6F4E37]">
                      {price.toLocaleString("vi-VN")}đ
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
