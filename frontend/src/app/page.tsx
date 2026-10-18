"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { Category, Product } from "@/types";
import { useCart } from "@/context/CartContext";
import {
  Sparkles,
  CupSoda,
  Bot,
  Flame,
  ArrowRight,
  ShieldCheck,
  Truck,
  Plus,
  Check,
  Star,
} from "lucide-react";

export default function HomePage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [bestSellers, setBestSellers] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [addedId, setAddedId] = useState<number | null>(null);
  const { addItem } = useCart();

  useEffect(() => {
    async function fetchData() {
      try {
        const [catData, prodData] = await Promise.all([
          api.getCategories().catch(() => []),
          api.getProducts().catch(() => []),
        ]);
        const catList = Array.isArray(catData) ? catData : [];
        const prodList = Array.isArray(prodData) ? prodData : [];
        setCategories(catList);
        setBestSellers(prodList.filter((p) => p && Boolean(p.isBestSeller)).slice(0, 4));
      } catch (err) {
        console.error("Lỗi khi tải dữ liệu trang chủ:", err);
        setCategories([]);
        setBestSellers([]);
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  const handleQuickAdd = (product: Product) => {
    if (!product) return;
    const prodId = product.Id ?? product.id;
    const prodName = product.Name ?? product.name ?? "Trà sữa MilkyVerse";
    const prodPrice = product.BasePrice ?? product.basePrice ?? 0;
    const prodImage = product.ImageUrl ?? product.imageUrl ?? "/logo.png";

    const success = addItem({
      productId: prodId,
      name: prodName,
      imageUrl: prodImage,
      size: "M",
      sugarLevel: "50%",
      iceLevel: "70%",
      unitPrice: prodPrice,
      quantity: 1,
      totalPrice: prodPrice,
      isCustomDIY: false,
      toppings: [],
    });

    if (success) {
      setAddedId(prodId);
      setTimeout(() => setAddedId(null), 1500);
    }
  };

  return (
    <div className="space-y-16 pb-16">
      {/* 1. Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-b from-[#F7D6D0]/30 via-[#FAF7F2] to-[#FAF7F2] py-16 md:py-24 border-b border-[#EFE6DC]">
        {/* Celestial background ambient circles */}
        <div className="absolute top-10 left-1/4 w-72 h-72 bg-[#F7D6D0]/40 rounded-full blur-3xl -z-10 animate-subtle" />
        <div className="absolute bottom-10 right-1/4 w-96 h-96 bg-[#C5A059]/15 rounded-full blur-3xl -z-10" />

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            {/* Left Content */}
            <div className="lg:col-span-7 space-y-6 text-center lg:text-left">
              <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-white border border-[#EFE6DC] shadow-sm text-xs font-bold text-[#6F4E37]">
                <img src="/logo.png" alt="MilkyVerse" className="w-5 h-5 object-contain rounded-full" />
                <span>Vũ Trụ Trà Sữa Đột Phá MilkyVerse 2026</span>
              </div>

              <h1 className="text-4xl sm:text-5xl lg:text-6xl font-black text-[#2D2522] tracking-tight leading-[1.15]">
                Khám Phá Vũ Trụ <br />
                <span className="bg-gradient-to-r from-[#6F4E37] via-[#8B5A2B] to-[#D4A373] bg-clip-text text-transparent">
                  MILKYVERSE
                </span>{" "}
                Tích Hợp AI
              </h1>

              <p className="text-base sm:text-lg text-[#786C65] max-w-2xl leading-relaxed mx-auto lg:mx-0">
                Tự do sáng tạo ly trà sữa chuẩn gu độc bản cùng <strong>DIY Drink Studio</strong> hoặc để{" "}
                <strong>AI Barista</strong> thấu hiểu tâm trạng và khẩu vị của bạn để gợi ý công thức hoàn mỹ nhất.
              </p>

              {/* Action CTA Buttons */}
              <div className="flex flex-wrap items-center justify-center lg:justify-start gap-4 pt-2">
                <Link
                  href="/diy"
                  className="flex items-center gap-2 px-6 py-3.5 rounded-2xl bg-[#6F4E37] text-white font-bold text-base hover:bg-[#4A3324] hover:shadow-lg hover:shadow-[#6F4E37]/25 transition-all transform hover:-translate-y-0.5"
                >
                  <CupSoda className="w-5 h-5 text-[#F7D6D0]" />
                  <span>DIY Drink Studio (Tự Phối)</span>
                </Link>

                <Link
                  href="/ai-barista"
                  className="flex items-center gap-2 px-6 py-3.5 rounded-2xl bg-white border border-[#6F4E37] text-[#6F4E37] font-bold text-base hover:bg-[#FAF7F2] transition-all shadow-sm"
                >
                  <Bot className="w-5 h-5 text-[#6F4E37]" />
                  <span>Hỏi AI Barista</span>
                </Link>

                <Link
                  href="/menu"
                  className="flex items-center gap-1.5 px-4 py-3.5 text-sm font-bold text-[#786C65] hover:text-[#6F4E37] transition-colors"
                >
                  <span>Xem menu</span>
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>

              {/* Trust Badges */}
              <div className="pt-6 border-t border-[#EFE6DC] grid grid-cols-3 gap-4 text-left">
                <div className="flex items-center gap-2.5">
                  <ShieldCheck className="w-5 h-5 text-[#4E7D55] shrink-0" />
                  <span className="text-xs text-[#2D2522] font-semibold">
                    100% Sữa tươi Đà Lạt
                  </span>
                </div>
                <div className="flex items-center gap-2.5">
                  <Truck className="w-5 h-5 text-[#6F4E37] shrink-0" />
                  <span className="text-xs text-[#2D2522] font-semibold">
                    Giao hỏa tốc 30 phút
                  </span>
                </div>
                <div className="flex items-center gap-2.5">
                  <Star className="w-5 h-5 text-[#C5A059] fill-[#C5A059] shrink-0" />
                  <span className="text-xs text-[#2D2522] font-semibold">
                    4.9/5 ⭐ (10k+ đánh giá)
                  </span>
                </div>
              </div>
            </div>

            {/* Right Featured Visual */}
            <div className="lg:col-span-5 relative flex justify-center">
              <div className="relative w-72 sm:w-80 md:w-96 aspect-[3/4] rounded-3xl overflow-hidden shadow-2xl border-4 border-white">
                <img
                  src="https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80"
                  alt="MilkyVerse Signature Milk Tea"
                  className="w-full h-full object-cover transform hover:scale-105 transition-transform duration-700"
                />
                <div className="absolute top-4 right-4 w-12 h-12 rounded-full bg-white/90 backdrop-blur-md p-1 border border-white/80 shadow-lg flex items-center justify-center overflow-hidden">
                  <img src="/logo.png" alt="MilkyVerse" className="w-full h-full object-cover rounded-full" />
                </div>
                <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent flex flex-col justify-end p-6 text-white">
                  <span className="text-xs font-bold uppercase tracking-wider bg-[#6F4E37] px-2.5 py-1 rounded-md w-fit mb-2">
                    Signature Món Bán Chạy
                  </span>
                  <h3 className="text-xl font-extrabold">Milkyverse Signature</h3>
                  <p className="text-xs text-white/80 mt-1">
                    Cốt hồng trà Ceylon ủ lạnh 12h kết hợp trân châu hoàng kim
                  </p>
                  <p className="text-lg font-bold text-[#F7D6D0] mt-2">45.000đ</p>
                </div>
              </div>

              {/* Floating AI recommendation pill */}
              <div className="absolute -bottom-6 -left-4 sm:left-4 bg-white/95 backdrop-blur-md p-4 rounded-2xl shadow-xl border border-[#EFE6DC] flex items-center gap-3 animate-bounce-subtle">
                <div className="w-10 h-10 rounded-xl bg-[#6F4E37] flex items-center justify-center text-white">
                  <Bot className="w-5 h-5" />
                </div>
                <div className="text-xs">
                  <p className="font-bold text-[#2D2522]">AI Barista gợi ý:</p>
                  <p className="text-[#786C65]">&ldquo;50% đường + Trân châu 3Q giòn&rdquo;</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 2. DIY Drink Studio Callout Banner */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="relative rounded-3xl bg-gradient-to-r from-[#4A3324] via-[#6F4E37] to-[#8B5A2B] text-white p-8 sm:p-12 overflow-hidden shadow-xl">
          <div className="absolute -right-10 -bottom-10 w-80 h-80 bg-white/5 rounded-full blur-2xl pointer-events-none" />
          <div className="grid grid-cols-1 md:grid-cols-12 gap-8 items-center relative z-10">
            <div className="md:col-span-8 space-y-4">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-[#F7D6D0] text-[#6F4E37] text-xs font-bold">
                <Sparkles className="w-3.5 h-3.5" />
                Tính Năng Độc Quyền Của MILKYVERSE
              </div>
              <h2 className="text-3xl sm:text-4xl font-extrabold tracking-tight">
                DIY Drink Studio — Tự Phối Trà Sữa Theo Gu
              </h2>
              <p className="text-white/85 text-sm sm:text-base leading-relaxed">
                Bạn là nghệ nhân pha chế cho chính mình! Chọn loại cốt trà yêu thích (Hồng trà, Ô long rang khói, Lục trà lài), kết hợp sữa thanh trùng hoặc sữa yến mạch thuần chay, tùy chỉnh đường đá và thêm topping thỏa thích với bản xem trước trực quan.
              </p>
              <div className="pt-2">
                <Link
                  href="/diy"
                  className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-white text-[#6F4E37] font-bold text-sm hover:bg-[#F7D6D0] transition-colors shadow-md"
                >
                  <CupSoda className="w-4 h-4 text-[#6F4E37]" />
                  <span>Trải Nghiệm Ngay Bây Giờ</span>
                  <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            </div>
            <div className="md:col-span-4 flex justify-center">
              <div className="w-44 h-56 rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 p-4 flex flex-col items-center justify-center text-center space-y-3">
                <CupSoda className="w-16 h-16 text-[#F7D6D0]" />
                <span className="text-xs font-semibold text-white/90">
                  Xem trước ly trà sữa với hiệu ứng phân tầng trực quan
                </span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 3. Best Sellers Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div className="flex flex-col sm:flex-row items-start sm:items-end justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
              <Flame className="w-4 h-4 text-red-500 fill-red-500" />
              <span>Được Yêu Thích Nhất</span>
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-[#2D2522] mt-1">
              Top Đồ Uống Bán Chạy Ngân Hà
            </h2>
          </div>
          <Link
            href="/menu"
            className="flex items-center gap-1.5 text-sm font-bold text-[#6F4E37] hover:underline"
          >
            <span>Xem tất cả thực đơn</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {[1, 2, 3, 4].map((n) => (
              <div key={n} className="h-80 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
            ))}
          </div>
        ) : bestSellers.length === 0 ? (
          <div className="p-8 text-center text-[#786C65] bg-white rounded-3xl border border-[#EFE6DC]">
            Đang cập nhật danh sách món bán chạy hôm nay.
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {bestSellers.map((product) => {
              if (!product) return null;
              const id = product.Id ?? product.id;
              const name = product.Name ?? product.name ?? "Trà sữa MilkyVerse";
              const price = product.BasePrice ?? product.basePrice ?? 0;
              const img = (product.ImageUrl ?? product.imageUrl) || "/logo.png";
              const desc = product.Description ?? product.description ?? "";
              const slug = product.Slug ?? product.slug ?? `product-${id}`;

              return (
                <div
                  key={id}
                  className="group bg-white rounded-3xl border border-[#EFE6DC] overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 flex flex-col"
                >
                  {/* Image container */}
                  <div className="relative aspect-square overflow-hidden bg-[#FAF7F2]">
                    <img
                      src={img}
                      alt={name}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                    />
                    <span className="absolute top-3 left-3 bg-[#6F4E37] text-white text-[11px] font-bold px-2.5 py-1 rounded-full shadow-md flex items-center gap-1">
                      <Flame className="w-3 h-3 text-red-400 fill-red-400" />
                      Best Seller
                    </span>
                  </div>

                  {/* Body */}
                  <div className="p-5 flex-1 flex flex-col justify-between space-y-4">
                    <div>
                      <Link href={`/menu/${slug}`}>
                        <h3 className="font-extrabold text-[#2D2522] text-base group-hover:text-[#6F4E37] transition-colors line-clamp-1">
                          {name}
                        </h3>
                      </Link>
                      <p className="text-xs text-[#786C65] line-clamp-2 mt-1 leading-relaxed">
                        {desc}
                      </p>
                    </div>

                    <div className="pt-2 border-t border-[#FAF7F2] flex items-center justify-between">
                      <span className="text-base font-black text-[#6F4E37]">
                        {price.toLocaleString("vi-VN")}đ
                      </span>

                      <button
                        onClick={() => handleQuickAdd(product)}
                        className={`p-2.5 rounded-xl font-bold text-xs flex items-center gap-1 transition-all shadow-sm ${
                          addedId === id
                            ? "bg-[#4E7D55] text-white"
                            : "bg-[#F7D6D0] text-[#6F4E37] hover:bg-[#6F4E37] hover:text-white"
                        }`}
                        title="Thêm nhanh vào giỏ"
                      >
                        {addedId === id ? (
                          <>
                            <Check className="w-4 h-4" />
                            <span>Đã thêm</span>
                          </>
                        ) : (
                          <>
                            <Plus className="w-4 h-4" />
                            <span>Thêm</span>
                          </>
                        )}
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>

      {/* 4. Categories Grid */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div>
          <h2 className="text-2xl sm:text-3xl font-extrabold text-[#2D2522]">
            Danh Mục Trà Sữa Đa Dạng
          </h2>
          <p className="text-sm text-[#786C65] mt-1">
            Mỗi bộ sưu tập mang một phong cách hương vị riêng biệt được ủ thủ công mỗi ngày.
          </p>
        </div>

        {categories.length === 0 ? (
          <div className="p-8 text-center text-[#786C65] bg-white rounded-3xl border border-[#EFE6DC]">
            Đang tải danh mục thực đơn...
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {categories.slice(0, 8).map((cat) => {
              if (!cat) return null;
              const id = cat.Id ?? cat.id;
              const name = cat.Name ?? cat.name ?? "Danh mục";
              const desc = cat.Description ?? cat.description ?? "";
              const img = (cat.ImageUrl ?? cat.imageUrl) || "/logo.png";

              return (
                <Link
                  key={id}
                  href={`/menu?category=${id}`}
                  className="group relative rounded-3xl overflow-hidden aspect-[4/3] border border-[#EFE6DC] shadow-sm hover:shadow-xl transition-all"
                >
                  <img
                    src={img}
                    alt={name}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent flex flex-col justify-end p-5 text-white">
                    <h3 className="font-bold text-lg leading-tight group-hover:text-[#F7D6D0] transition-colors">
                      {name}
                    </h3>
                    <p className="text-xs text-white/80 line-clamp-1 mt-1">
                      {desc}
                    </p>
                  </div>
                </Link>
              );
            })}
          </div>
        )}

        {categories.length > 8 && (
          <div className="text-center pt-2">
            <Link
              href="/menu"
              className="inline-flex items-center gap-2 px-6 py-3 rounded-2xl bg-white border border-[#EFE6DC] text-sm font-bold text-[#6F4E37] hover:bg-[#FAF7F2] shadow-sm transition-all"
            >
              <span>Xem toàn bộ 23 danh mục món</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        )}
      </section>

      {/* 5. Promotional Vouchers Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="bg-[#FAF7F2] border-2 border-dashed border-[#6F4E37]/30 rounded-3xl p-6 sm:p-8">
          <div className="flex flex-col md:flex-row items-center justify-between gap-6">
            <div className="space-y-1 text-center md:text-left">
              <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
                Ưu Đãi Đặc Quyền Hôm Nay
              </span>
              <h3 className="text-xl sm:text-2xl font-extrabold text-[#2D2522]">
                Nhập Mã <span className="text-[#6F4E37] underline decoration-[#C5A059]">MILKYNEW</span> Giảm 20%
              </h3>
              <p className="text-xs text-[#786C65]">
                Áp dụng cho mọi khách hàng mới khi đặt hàng qua website. Miễn phí vận chuyển với mã <strong>FREESHIP</strong> đơn từ 99k!
              </p>
            </div>
            <Link
              href="/menu"
              className="px-6 py-3 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all shrink-0"
            >
              Áp Dụng Mã Ngay
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
