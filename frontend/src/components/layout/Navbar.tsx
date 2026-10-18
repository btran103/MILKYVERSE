"use client";

import React, { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { usePathname } from "next/navigation";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import {
  ShoppingBag,
  CupSoda,
  Bot,
  User as UserIcon,
  UserPlus,
  Menu as MenuIcon,
  X,
  ShieldAlert,
  LogOut,
  Clock,
  ChefHat,
  Boxes,
  Truck,
} from "lucide-react";

export const Navbar: React.FC = () => {
  const pathname = usePathname();
  const { cartCount } = useCart();
  const { user, logout, isManager, isStaff, isWarehouse, isDelivery } = useAuth();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);
  const [mounted, setMounted] = useState(false);

  React.useEffect(() => {
    setMounted(true);
  }, []);

  const navLinks = [
    { label: "Trang Chủ", href: "/" },
    { label: "Thực Đơn (Ăn & Uống)", href: "/menu" },
    {
      label: "DIY Studio",
      href: "/diy",
      badge: "Tự Phối",
      icon: CupSoda,
    },
    {
      label: "AI Barista",
      href: "/ai-barista",
      badge: "AI 2026",
      icon: Bot,
      highlight: true,
    },
    { label: "Tra Cứu Đơn", href: "/orders" },
  ];

  return (
    <header className="sticky top-0 z-50 bg-[#FAF7F2]/95 backdrop-blur-md border-b border-[#EFE6DC] transition-all">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-20">
          {/* 1. Brand Logo */}
          <Link href="/" className="flex items-center gap-3 group">
            <div className="relative w-12 h-12 rounded-full overflow-hidden bg-white border border-[#EFE6DC] shadow-md shadow-[#6F4E37]/15 group-hover:scale-105 transition-transform flex items-center justify-center p-0.5">
              <Image
                src="/logo.png"
                alt="MilkyVerse Logo"
                width={48}
                height={48}
                className="w-full h-full object-cover rounded-full"
                priority
              />
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <span className="font-extrabold text-2xl tracking-wider bg-gradient-to-r from-[#4A3324] via-[#6F4E37] to-[#8B5A2B] bg-clip-text text-transparent">
                  MILKYVERSE
                </span>
                <span className="text-[10px] uppercase font-bold tracking-widest px-1.5 py-0.5 rounded bg-[#F7D6D0] text-[#6F4E37]">
                  FOOD & DRINK
                </span>
              </div>
              <p className="text-[11px] text-[#786C65] tracking-wide font-medium">
                Vũ Trụ Trà Sữa Tươi & Đồ Ăn Vặt Nóng Giòn
              </p>
            </div>
          </Link>

          {/* 2. Desktop Navigation */}
          <nav className="hidden md:flex items-center gap-1 lg:gap-2">
            {navLinks.map((item) => {
              const isActive = pathname === item.href;
              const Icon = item.icon;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`relative flex items-center gap-2 px-3.5 py-2 rounded-xl text-sm font-semibold transition-all ${
                    isActive
                      ? "text-[#6F4E37] bg-[#F7D6D0]/40 shadow-sm"
                      : "text-[#2D2522] hover:text-[#6F4E37] hover:bg-[#FAF7F2]"
                  }`}
                >
                  {Icon && <Icon className={`w-4 h-4 ${item.highlight ? "text-[#6F4E37]" : ""}`} />}
                  <span>{item.label}</span>
                  {item.badge && (
                    <span
                      className={`text-[10px] font-bold px-1.5 py-0.5 rounded-full ${
                        item.highlight
                          ? "bg-[#6F4E37] text-white animate-subtle"
                          : "bg-[#F7D6D0] text-[#6F4E37]"
                      }`}
                    >
                      {item.badge}
                    </span>
                  )}
                </Link>
              );
            })}
          </nav>

          {/* 3. Actions: Cart & User & Quick Role Badges */}
          <div className="flex items-center gap-3">
            {/* Quick role shortcuts for logged-in staff */}
            {mounted && user && user.role !== "Customer" && (
              <div className="hidden lg:flex items-center gap-1 bg-[#FAF7F2] p-1 rounded-2xl border border-[#EFE6DC]">
                {isStaff && (
                  <Link
                    href="/staff"
                    className={`px-2.5 py-1 rounded-xl text-xs font-bold flex items-center gap-1 transition-colors ${
                      pathname === "/staff" ? "bg-[#6F4E37] text-white" : "text-[#6F4E37] hover:bg-white"
                    }`}
                    title="Bếp & Quầy POS"
                  >
                    <ChefHat className="w-3.5 h-3.5" />
                    <span>Quầy & Bếp</span>
                  </Link>
                )}
                {isWarehouse && (
                  <Link
                    href="/warehouse"
                    className={`px-2.5 py-1 rounded-xl text-xs font-bold flex items-center gap-1 transition-colors ${
                      pathname === "/warehouse" ? "bg-[#6F4E37] text-white" : "text-blue-700 hover:bg-white"
                    }`}
                    title="Kho Hàng FEFO"
                  >
                    <Boxes className="w-3.5 h-3.5" />
                    <span>Kho FEFO</span>
                  </Link>
                )}
                {isDelivery && (
                  <Link
                    href="/delivery"
                    className={`px-2.5 py-1 rounded-xl text-xs font-bold flex items-center gap-1 transition-colors ${
                      pathname === "/delivery" ? "bg-[#6F4E37] text-white" : "text-emerald-700 hover:bg-white"
                    }`}
                    title="Đội Giao Hàng Shipper"
                  >
                    <Truck className="w-3.5 h-3.5" />
                    <span>Giao Hàng</span>
                  </Link>
                )}
                {isManager && (
                  <Link
                    href="/admin"
                    className={`px-2.5 py-1 rounded-xl text-xs font-bold flex items-center gap-1 transition-colors ${
                      pathname.startsWith("/admin") ? "bg-[#6F4E37] text-white" : "text-purple-700 hover:bg-white"
                    }`}
                    title="Manager / Admin Portal"
                  >
                    <ShieldAlert className="w-3.5 h-3.5" />
                    <span>Quản Lý</span>
                  </Link>
                )}
              </div>
            )}

            {/* Cart Button */}
            <Link
              href="/cart"
              className="relative p-2.5 rounded-xl bg-white border border-[#EFE6DC] text-[#4A3324] hover:bg-[#FAF7F2] transition-colors shadow-sm flex items-center"
              title="Xem giỏ hàng"
            >
              <ShoppingBag className="w-5 h-5 text-[#6F4E37]" />
              {mounted && cartCount > 0 && (
                <span className="absolute -top-1.5 -right-1.5 bg-[#6F4E37] text-white text-xs font-bold w-5 h-5 rounded-full flex items-center justify-center shadow-md animate-scale">
                  {cartCount}
                </span>
              )}
            </Link>

            {/* Auth section */}
            {mounted && user ? (
              <div className="relative">
                <button
                  onClick={() => setUserDropdownOpen(!userDropdownOpen)}
                  className="flex items-center gap-2 px-3 py-2 rounded-xl bg-white border border-[#EFE6DC] text-sm font-medium text-[#2D2522] hover:bg-[#FAF7F2] transition-all shadow-sm"
                >
                  <div className="w-7 h-7 rounded-full bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs">
                    {user.fullName.charAt(0).toUpperCase()}
                  </div>
                  <span className="hidden sm:inline-block max-w-[100px] truncate">
                    {user.fullName}
                  </span>
                </button>

                {/* Dropdown Menu */}
                {userDropdownOpen && (
                  <div className="absolute right-0 mt-2 w-64 bg-white rounded-2xl shadow-xl border border-[#EFE6DC] py-2 z-50 text-sm">
                    <div className="px-4 py-2 border-b border-[#FAF7F2]">
                      <p className="font-bold text-[#2D2522] truncate">{user.fullName}</p>
                      <p className="text-xs text-[#786C65] truncate">{user.email}</p>
                      <span className="inline-block mt-1 text-[10px] font-bold px-2 py-0.5 rounded bg-[#FAF7F2] text-[#6F4E37]">
                        Vai trò: {user.role}
                      </span>
                    </div>

                    {isManager && (
                      <Link
                        href="/admin"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center gap-2 px-4 py-2 text-[#6F4E37] font-bold hover:bg-[#FAF7F2]"
                      >
                        <ShieldAlert className="w-4 h-4 text-[#C5A059]" />
                        <span>Trang Quản Lý (Manager)</span>
                      </Link>
                    )}

                    {isStaff && (
                      <Link
                        href="/staff"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center gap-2 px-4 py-2 text-[#2D2522] font-semibold hover:bg-[#FAF7F2]"
                      >
                        <ChefHat className="w-4 h-4 text-orange-600" />
                        <span>Quầy & Bếp Order POS</span>
                      </Link>
                    )}

                    {isWarehouse && (
                      <Link
                        href="/warehouse"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center gap-2 px-4 py-2 text-[#2D2522] font-semibold hover:bg-[#FAF7F2]"
                      >
                        <Boxes className="w-4 h-4 text-blue-600" />
                        <span>Kho Hàng & Lô FEFO</span>
                      </Link>
                    )}

                    {isDelivery && (
                      <Link
                        href="/delivery"
                        onClick={() => setUserDropdownOpen(false)}
                        className="flex items-center gap-2 px-4 py-2 text-[#2D2522] font-semibold hover:bg-[#FAF7F2]"
                      >
                        <Truck className="w-4 h-4 text-emerald-600" />
                        <span>Đội Giao Vận (Shipper)</span>
                      </Link>
                    )}

                    <Link
                      href="/orders"
                      onClick={() => setUserDropdownOpen(false)}
                      className="flex items-center gap-2 px-4 py-2 text-[#2D2522] hover:bg-[#FAF7F2]"
                    >
                      <Clock className="w-4 h-4 text-[#786C65]" />
                      <span>Lịch sử đơn hàng</span>
                    </Link>

                    <button
                      onClick={() => {
                        logout();
                        setUserDropdownOpen(false);
                      }}
                      className="w-full text-left flex items-center gap-2 px-4 py-2 text-red-600 hover:bg-red-50 border-t border-[#FAF7F2] mt-1 font-semibold"
                    >
                      <LogOut className="w-4 h-4" />
                      <span>Đăng xuất</span>
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <Link
                  href="/auth/register"
                  className="hidden sm:inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl border border-[#6F4E37]/30 text-[#6F4E37] font-semibold text-sm hover:bg-[#6F4E37]/10 transition-all"
                >
                  <UserPlus className="w-4 h-4" />
                  <span>Đăng Ký</span>
                </Link>
                <Link
                  href="/auth/login"
                  className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[#6F4E37] text-white font-semibold text-sm hover:bg-[#4A3324] transition-all shadow-md shadow-[#6F4E37]/20"
                >
                  <UserIcon className="w-4 h-4" />
                  <span>Đăng Nhập</span>
                </Link>
              </div>
            )}

            {/* Mobile menu trigger */}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="md:hidden p-2 text-[#2D2522] hover:text-[#6F4E37]"
            >
              {mobileMenuOpen ? <X className="w-6 h-6" /> : <MenuIcon className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile menu */}
        {mobileMenuOpen && (
          <div className="md:hidden py-4 border-t border-[#EFE6DC] space-y-2">
            {navLinks.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setMobileMenuOpen(false)}
                className="flex items-center justify-between px-3 py-2.5 rounded-xl text-base font-semibold text-[#2D2522] hover:bg-[#F7D6D0]/30"
              >
                <span className="flex items-center gap-2">
                  {item.icon && <item.icon className="w-5 h-5 text-[#6F4E37]" />}
                  {item.label}
                </span>
                {item.badge && (
                  <span className="text-xs font-bold px-2 py-0.5 rounded-full bg-[#6F4E37] text-white">
                    {item.badge}
                  </span>
                )}
              </Link>
            ))}

            {(!mounted || !user) && (
              <div className="pt-3 border-t border-[#EFE6DC] flex flex-col gap-2">
                <Link
                  href="/auth/login"
                  onClick={() => setMobileMenuOpen(false)}
                  className="w-full flex items-center justify-center gap-2 py-2.5 rounded-xl bg-[#6F4E37] text-white font-bold text-sm shadow-md shadow-[#6F4E37]/20"
                >
                  <UserIcon className="w-4 h-4" />
                  <span>Đăng Nhập</span>
                </Link>
                <Link
                  href="/auth/register"
                  onClick={() => setMobileMenuOpen(false)}
                  className="w-full flex items-center justify-center gap-2 py-2.5 rounded-xl border border-[#6F4E37]/40 text-[#6F4E37] font-bold text-sm hover:bg-[#6F4E37]/10"
                >
                  <UserPlus className="w-4 h-4" />
                  <span>Đăng Ký Tài Khoản</span>
                </Link>
              </div>
            )}

            {mounted && user && (
              <div className="pt-2 border-t border-[#EFE6DC] space-y-1">
                {isStaff && (
                  <Link
                    href="/staff"
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center gap-2 px-3 py-2 text-sm font-bold text-orange-800"
                  >
                    <ChefHat className="w-4 h-4" />
                    <span>Quầy & Bếp Order POS</span>
                  </Link>
                )}
                {isWarehouse && (
                  <Link
                    href="/warehouse"
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center gap-2 px-3 py-2 text-sm font-bold text-blue-800"
                  >
                    <Boxes className="w-4 h-4" />
                    <span>Kho Hàng & Lô FEFO</span>
                  </Link>
                )}
                {isDelivery && (
                  <Link
                    href="/delivery"
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center gap-2 px-3 py-2 text-sm font-bold text-emerald-800"
                  >
                    <Truck className="w-4 h-4" />
                    <span>Đội Giao Hàng (Shipper)</span>
                  </Link>
                )}
                {isManager && (
                  <Link
                    href="/admin"
                    onClick={() => setMobileMenuOpen(false)}
                    className="flex items-center gap-2 px-3 py-2 text-sm font-bold text-purple-800"
                  >
                    <ShieldAlert className="w-4 h-4" />
                    <span>Trang Quản Lý (Manager)</span>
                  </Link>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
};
