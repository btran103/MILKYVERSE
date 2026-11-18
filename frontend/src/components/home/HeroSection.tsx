"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { Product } from "@/types";
import {
  CupSoda,
  Bot,
  ArrowRight,
  ShieldCheck,
  Truck,
  Star,
  ChevronLeft,
  ChevronRight,
  Sparkles,
} from "lucide-react";

  interface HeroSlideItem {
    id: number | string;
    name: string;
    slug: string;
    description: string;
    price: number;
    imageUrl: string;
    badge: string;
    href: string;
  }

  const FALLBACK_SLIDES: HeroSlideItem[] = [
    {
      id: "sig-1",
      name: "Trà Sữa Trân Châu Đường Đen",
      slug: "tra-sua-tran-chau-duong-den",
      description: "Sữa tươi Đà Lạt thanh trùng kết hợp trân châu đường đen nấu chậm 2 giờ thơm lừng",
      price: 49000,
      imageUrl: "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80",
      badge: "SIGNATURE MÓN BÁN CHẠY #1",
      href: "/menu?type=DRINK",
    },
    {
      id: "sig-2",
      name: "Matcha Uji Kem Phô Mai",
      slug: "matcha-uji-kem-pho-mai",
      description: "Bột Matcha Uji Nhật Bản thượng hạng hòa quyện lớp kem phô mai mặn béo tan chảy",
      price: 55000,
      imageUrl: "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=800&auto=format&fit=crop&q=80",
      badge: "BEST SELLER MATCHA",
      href: "/menu?type=DRINK",
    },
    {
      id: "sig-3",
      name: "Trà Trái Cây Nhiệt Đới",
      slug: "tra-trai-cay-nhiet-doi",
      description: "Cốt trà lài ủ lạnh kết hợp cam vàng, dâu tây, chanh dây tươi mát giải nhiệt tức thì",
      price: 45000,
      imageUrl: "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=800&auto=format&fit=crop&q=80",
      badge: "THANH MÁT MỖI NGÀY",
      href: "/menu?type=DRINK",
    },
    {
      id: "sig-4",
      name: "Combo Trà Sữa & Ăn Vặt Giòn",
      slug: "combo-an-uong",
      description: "Sự kết hợp hoàn hảo giữa trà sữa béo ngậy và đồ ăn vặt nóng giòn siêu tiết kiệm",
      price: 79000,
      imageUrl: "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=800&auto=format&fit=crop&q=80",
      badge: "COMBO TIẾT KIỆM",
      href: "/menu?type=COMBO",
    },
    {
      id: "sig-5",
      name: "Cà Phê Muối Kem Béo",
      slug: "ca-phe-muoi-kem-beo",
      description: "Cà phê phin đậm đà phủ lớp kem muối biển mịn màng đánh thức mọi giác quan",
      price: 42000,
      imageUrl: "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80",
      badge: "TRENDING 2026",
      href: "/menu?type=DRINK",
    },
  ];

  const RELIABLE_FALLBACK_IMG =
    "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=800&auto=format&fit=crop&q=80";

  interface HeroSectionProps {
    featuredProducts?: Product[];
  }

  export default function HeroSection({ featuredProducts = [] }: HeroSectionProps) {
    const [currentIndex, setCurrentIndex] = useState(0);
    const [isPaused, setIsPaused] = useState(false);

    // Chuyển đổi sản phẩm từ Database sang Slide hiển thị (ưu tiên ảnh thật từ DB, có fallback an toàn)
    const slides: HeroSlideItem[] =
      featuredProducts && featuredProducts.length > 0
        ? featuredProducts.slice(0, 5).map((p, idx) => {
          const id = p.Id ?? p.id ?? idx;
          const name = p.Name ?? p.name ?? "MilkyVerse Signature";
          const slug = p.Slug ?? p.slug ?? "";
          const desc =
            p.Description ??
            p.description ??
            "Hương vị độc bản được pha chế tỉ mỉ từ nguyên liệu tuyển chọn MilkyVerse";
          const price = p.BasePrice ?? p.basePrice ?? 45000;
          const rawImg = p.ImageUrl ?? p.imageUrl ?? "";
          const img =
            rawImg && !rawImg.includes("photo-1558857563")
              ? rawImg
              : FALLBACK_SLIDES[idx % FALLBACK_SLIDES.length].imageUrl;
          const catName = p.CategoryName ?? p.categoryName;

          return {
            id,
            name,
            slug,
            description: desc,
            price,
            imageUrl: img,
            badge: catName ? `SIGNATURE • ${catName.toUpperCase()}` : `MÓN NỔI BẬT #${idx + 1}`,
            href: slug ? `/menu/${slug}` : "/menu",
          };
        })
        : FALLBACK_SLIDES;

    const totalSlides = slides.length;
    const safeIndex = currentIndex >= totalSlides ? 0 : currentIndex;
    const activeSlide = slides[safeIndex];

    const handlePrev = (e: React.MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setCurrentIndex((prev) => (prev - 1 + totalSlides) % totalSlides);
    };

    const handleNext = (e: React.MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setCurrentIndex((prev) => (prev + 1) % totalSlides);
    };

    // Tự động chạy qua lại các món sau mỗi 4.5 giây (dừng khi rê chuột vào)
    useEffect(() => {
      if (isPaused || totalSlides <= 1) return;
      const timer = setInterval(() => {
        setCurrentIndex((prev) => (prev + 1) % totalSlides);
      }, 4500);
      return () => clearInterval(timer);
    }, [isPaused, totalSlides]);

    return (
      <section className="relative overflow-hidden bg-gradient-to-b from-[#F7D6D0]/30 via-[#FAF7F2] to-[#FAF7F2] py-16 md:py-24 border-b border-[#EFE6DC]">
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
                <strong>AI Barista</strong> thấu hiểu tâm trạng và khẩu vị của bạn để gợi ý công thức hoàn hảo nhất.
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
                    4.9/5 ★ (10k+ đánh giá)
                  </span>
                </div>
              </div>
            </div>

                {/* Right Featured Interactive Carousel Card */}
                <div
                  className="lg:col-span-5 relative flex flex-col items-center"
                  onMouseEnter={() => setIsPaused(true)}
                  onMouseLeave={() => setIsPaused(false)}
                >
                  <div className="relative flex items-center justify-center w-full max-w-[420px]">
                    {/* Nút Lùi Trang (Previous) */}
                    <button
                      type="button"
                      onClick={handlePrev}
                      aria-label="Món trước"
                      className="absolute left-0 sm:-left-4 z-20 w-10 h-10 rounded-full bg-white/95 hover:bg-[#6F4E37] text-[#6F4E37] hover:text-white border border-[#EFE6DC] shadow-lg flex items-center justify-center transition-all hover:scale-110 active:scale-95"
                    >
                      <ChevronLeft className="w-5 h-5" />
                    </button>

                    {/* Thẻ Sản Phẩm Nổi Bật (Bấm vào để đi tới chi tiết món) */}
                    <Link
                      href={activeSlide.href}
                      className="group relative block w-72 sm:w-80 md:w-[340px] aspect-[3/4] rounded-3xl overflow-hidden shadow-2xl border-4 border-white cursor-pointer transition-all duration-300 hover:shadow-[#6F4E37]/25"
                    >
                      <img
                        key={activeSlide.id}
                        src={activeSlide.imageUrl}
                        alt={activeSlide.name}
                        onError={(e) => {
                          const target = e.currentTarget;
                          if (target.src !== RELIABLE_FALLBACK_IMG) {
                            target.src = RELIABLE_FALLBACK_IMG;
                          }
                        }}
                        className="w-full h-full object-cover transform group-hover:scale-105 transition-transform duration-700"
                      />

                      {/* Số trang góc trên bên trái */}
                      <div className="absolute top-4 left-4 z-10 px-3 py-1 rounded-full bg-black/50 backdrop-blur-md text-white text-xs font-bold flex items-center gap-1.5 border border-white/20">
                        <Sparkles className="w-3.5 h-3.5 text-[#F7D6D0]" />
                        <span>
                          {safeIndex + 1} / {totalSlides}
                        </span>
                      </div>

                      {/* Logo tròn góc trên bên phải */}
                      <div className="absolute top-4 right-4 z-10 w-11 h-11 rounded-full bg-white/95 backdrop-blur-md p-1 border border-white/80 shadow-lg flex items-center justify-center overflow-hidden">
                        <img src="/logo.png" alt="MilkyVerse" className="w-full h-full object-cover rounded-full" />
                      </div>

                      {/* Thông tin món & Nút Xem Chi Tiết */}
                      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/25 to-transparent flex flex-col justify-end p-6 text-white transition-all">
                        <span className="text-[10px] font-bold uppercase tracking-wider bg-[#6F4E37] px-2.5 py-1 rounded-md w-fit mb-2 shadow-sm">
                          {activeSlide.badge}
                        </span>
                        <h3 className="text-xl font-extrabold leading-snug group-hover:text-[#F7D6D0] transition-colors line-clamp-1">
                          {activeSlide.name}
                        </h3>
                        <p className="text-xs text-white/85 mt-1 line-clamp-2 leading-relaxed">
                          {activeSlide.description}
                        </p>
                        <div className="flex items-center justify-between mt-3 pt-2 border-t border-white/20">
                          <p className="text-lg font-black text-[#F7D6D0]">
                            {activeSlide.price.toLocaleString("vi-VN")}đ
                          </p>
                          <span className="inline-flex items-center gap-1 text-xs font-bold bg-white/20 group-hover:bg-[#6F4E37] backdrop-blur-sm px-3 py-1.5 rounded-xl transition-colors">
                            <span>Xem chi tiết</span>
                            <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                          </span>
                        </div>
                      </div>
                    </Link>

                    {/* Nút Tới Trang (Next) */}
                    <button
                      type="button"
                      onClick={handleNext}
                      aria-label="Món tiếp theo"
                      className="absolute right-0 sm:-right-4 z-20 w-10 h-10 rounded-full bg-white/95 hover:bg-[#6F4E37] text-[#6F4E37] hover:text-white border border-[#EFE6DC] shadow-lg flex items-center justify-center transition-all hover:scale-110 active:scale-95"
                    >
                      <ChevronRight className="w-5 h-5" />
                    </button>
                  </div>

                  {/* Thanh Phân Trang (Pagination Dots bấm chọn trực tiếp) */}
                  <div className="flex items-center justify-center gap-2 mt-5 bg-white/80 backdrop-blur-sm px-4 py-2 rounded-full border border-[#EFE6DC] shadow-sm">
                    {slides.map((slide, idx) => {
                      const isActive = idx === safeIndex;
                      return (
                        <button
                          key={slide.id}
                          type="button"
                          onClick={() => setCurrentIndex(idx)}
                          aria-label={`Chuyển đến trang ${idx + 1}: ${slide.name}`}
                          className={`h-2.5 rounded-full transition-all duration-300 ${isActive
                              ? "w-8 bg-[#6F4E37]"
                              : "w-2.5 bg-[#D8C7B5] hover:bg-[#9C7A5E]"
                            }`}
                        />
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
          </section>
          );
}
