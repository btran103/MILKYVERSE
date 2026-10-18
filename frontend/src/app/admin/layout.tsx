"use client";

import React, { useEffect } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import {
  LayoutDashboard,
  CupSoda,
  Boxes,
  ShoppingBag,
  Users,
  ShieldCheck,
  ArrowLeft,
  Truck,
  ChefHat,
} from "lucide-react";

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const { user, isManager, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && (!user || !isManager)) {
      router.push("/auth/login?redirect=/admin");
    }
  }, [user, isManager, isLoading, router]);

  if (isLoading || !user || !isManager) {
    return (
      <div className="max-w-md mx-auto py-20 px-4 text-center space-y-4">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-sm font-bold text-[#786C65]">Đang kiểm tra quyền Quản lý / Quản trị viên...</p>
      </div>
    );
  }

  const navItems = [
    { label: "Tổng Quan (Dashboard)", href: "/admin", icon: LayoutDashboard },
    { label: "Nhân Sự & Phân Quyền", href: "/admin/staff", icon: Users },
    { label: "Quản Lý Sản Phẩm & Món", href: "/admin/products", icon: CupSoda },
    { label: "Quản Lý Đơn Hàng", href: "/admin/orders", icon: ShoppingBag },
    { label: "Bếp & Quầy POS", href: "/staff", icon: ChefHat },
    { label: "Kho Hàng & FEFO", href: "/warehouse", icon: Boxes },
    { label: "Đội Giao Hàng (Shipper)", href: "/delivery", icon: Truck },
  ];

  return (
    <div className="min-h-screen bg-[#FAF7F2] flex flex-col md:flex-row">
      {/* Sidebar */}
      <aside className="w-full md:w-64 bg-white border-r border-[#EFE6DC] p-6 space-y-8 shrink-0">
        <div className="space-y-2">
          <Link href="/" className="inline-flex items-center gap-1.5 text-xs text-[#786C65] hover:text-[#6F4E37]">
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Về trang chủ bán hàng</span>
          </Link>
          <div className="flex items-center gap-2.5 pt-1">
            <div className="w-8 h-8 rounded-xl bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs">
              <ShieldCheck className="w-4 h-4 text-[#C5A059]" />
            </div>
            <div>
              <h2 className="font-black text-sm text-[#2D2522]">MILKY PORTAL</h2>
              <p className="text-[10px] text-[#786C65] font-semibold">{user.role}: {user.fullName}</p>
            </div>
          </div>
        </div>

        {/* Nav Links */}
        <nav className="space-y-1.5">
          {navItems.map((item) => {
            const isActive = pathname === item.href;
            const Icon = item.icon;

            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-xs font-bold transition-all ${
                  isActive
                    ? "bg-[#6F4E37] text-white shadow-sm"
                    : "text-[#2D2522] hover:bg-[#FAF7F2] hover:text-[#6F4E37]"
                }`}
              >
                <Icon className="w-4 h-4 shrink-0" />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
      </aside>

      {/* Main Admin Content */}
      <main className="flex-1 p-6 sm:p-10 overflow-y-auto">
        {children}
      </main>
    </div>
  );
}
