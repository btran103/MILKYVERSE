"use client";

import React, { useEffect, useState, Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { api } from "@/lib/api";
import { Category, Product } from "@/types";
import { useCart } from "@/context/CartContext";
import {
  Search,
  Sparkles,
  Flame,
  Plus,
  Check,
  SlidersHorizontal,
  CupSoda,
  UtensilsCrossed,
  Layers,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

type MainTabType = "ALL" | "DRINK" | "FOOD" | "COMBO";

function MenuContent() {
  const searchParams = useSearchParams();
  const initialCategory = searchParams.get("category");
  const initialType = searchParams.get("type") as MainTabType | null;

  const [categories, setCategories] = useState<Category[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);

  // Filter states
  const [activeTab, setActiveTab] = useState<MainTabType>(initialType || "ALL");
  const [selectedCatId, setSelectedCatId] = useState<number | null>(
    initialCategory ? parseInt(initialCategory, 10) : null
  );
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState("default");
  const [addedId, setAddedId] = useState<number | null>(null);

  // Pagination (4 items per page)
  const ITEMS_PER_PAGE = 4;
  const [currentPage, setCurrentPage] = useState(1);

  const { addItem } = useCart();

  useEffect(() => {
    async function loadData() {
      setLoading(true);
      try {
        const [cats, prods] = await Promise.all([
          api.getCategories(),
          api.getProducts({
            categoryId: selectedCatId || undefined,
            productType: activeTab !== "ALL" ? activeTab : undefined,
            search: searchQuery || undefined,
            sortBy: sortBy !== "default" ? sortBy : undefined,
          }),
        ]);
        setCategories(cats || []);
        setProducts(prods || []);
        setCurrentPage(1);
      } catch (err) {
        console.error("Lỗi khi tải thực đơn:", err);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [activeTab, selectedCatId, sortBy, searchQuery]);

  const handleSearchSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const prods = await api.getProducts({
        categoryId: selectedCatId || undefined,
        productType: activeTab !== "ALL" ? activeTab : undefined,
        search: searchQuery || undefined,
        sortBy: sortBy !== "default" ? sortBy : undefined,
      });
      setProducts(prods || []);
      setCurrentPage(1);
    } finally {
      setLoading(false);
    }
  };

  const handleTabChange = (tab: MainTabType) => {
    setActiveTab(tab);
    setSelectedCatId(null); // Reset sub-category filter when switching main tab
    setCurrentPage(1);
  };

  const totalItems = products.length;
  const totalPages = Math.ceil(totalItems / ITEMS_PER_PAGE) || 1;
  const validPage = Math.min(Math.max(1, currentPage), totalPages);
  const startIndex = (validPage - 1) * ITEMS_PER_PAGE;
  const endIndex = Math.min(startIndex + ITEMS_PER_PAGE, totalItems);
  const paginatedProducts = products.slice(startIndex, endIndex);

  const handlePageChange = (newPage: number) => {
    if (newPage < 1 || newPage > totalPages) return;
    setCurrentPage(newPage);
    const gridElem = document.getElementById("menu-grid-header");
    if (gridElem) {
      gridElem.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  };

  const handleQuickAdd = (product: Product) => {
    const id = product.Id ?? product.id;
    const name = product.Name ?? product.name;
    const price = product.BasePrice ?? product.basePrice;
    const img = product.ImageUrl ?? product.imageUrl;
    const type = product.ProductType ?? product.productType ?? "DRINK";

    const success = addItem({
      productId: id,
      name: name,
      imageUrl: img,
      itemType: type,
      size: type === "DRINK" ? "M" : "Standard",
      sugarLevel: type === "DRINK" ? "50%" : undefined,
      iceLevel: type === "DRINK" ? "70%" : undefined,
      spicyLevel: type === "FOOD" ? (product.spicyLevel ? `Cấp ${product.spicyLevel}` : "Không cay") : undefined,
      dippingSauce: product.defaultSauce || (type === "FOOD" ? "Tương ớt" : undefined),
      portionSize: product.portionDescription || "Phần tiêu chuẩn",
      unitPrice: price,
      quantity: 1,
      totalPrice: price,
      isCustomDIY: false,
      toppings: [],
    });

    if (success) {
      setAddedId(id);
      setTimeout(() => setAddedId(null), 1500);
    }
  };

  // Filter categories according to active main tab
  const visibleCategories = categories.filter((c) => {
    const cType = c.CategoryType ?? c.categoryType ?? "DRINK";
    if (activeTab === "ALL") return true;
    return cType === activeTab;
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* 1. Header Banner & Search */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 pb-6 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
            <Sparkles className="w-3.5 h-3.5 text-[#C5A059]" />
            <span>Thực Đơn MilkyVerse Toàn Diện</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-black text-[#2D2522] mt-1">
            Trà Sữa Thơm Béo & Ăn Vặt Nóng Giòn
          </h1>
          <p className="text-sm text-[#786C65] mt-1">
            Hơn 40 món ăn vặt đường phố, mì cay, gà sốt Thái và trà sữa pha chế tươi mỗi ngày
          </p>
        </div>

        {/* Search form */}
        <form
          onSubmit={handleSearchSubmit}
          className="relative w-full md:w-80 flex items-center shrink-0"
        >
          <input
            type="text"
            placeholder="Tìm trà sữa, cá viên, chân gà, mì cay..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-16 py-2.5 rounded-2xl bg-white border border-[#EFE6DC] text-sm focus:outline-none focus:border-[#6F4E37] focus:ring-1 focus:ring-[#6F4E37] shadow-sm"
          />
          <Search className="w-4 h-4 text-[#786C65] absolute left-3.5" />
          <button
            type="submit"
            className="absolute right-2 px-3 py-1 bg-[#6F4E37] text-white text-xs font-bold rounded-xl hover:bg-[#4A3324] transition-colors"
          >
            Tìm
          </button>
        </form>
      </div>

      {/* 2. Main High-Level Category Tabs */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <button
          onClick={() => handleTabChange("ALL")}
          className={`py-3.5 px-4 rounded-2xl font-bold text-sm flex items-center justify-center gap-2 transition-all shadow-sm ${
            activeTab === "ALL"
              ? "bg-[#6F4E37] text-white shadow-md ring-2 ring-[#6F4E37]/30"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          <Layers className="w-4 h-4" />
          <span>Tất Cả ({categories.reduce((acc, c) => acc + (c.ProductCount ?? c.productCount ?? 0), 0)})</span>
        </button>

        <button
          onClick={() => handleTabChange("DRINK")}
          className={`py-3.5 px-4 rounded-2xl font-bold text-sm flex items-center justify-center gap-2 transition-all shadow-sm ${
            activeTab === "DRINK"
              ? "bg-[#6F4E37] text-white shadow-md ring-2 ring-[#6F4E37]/30"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          <CupSoda className="w-4 h-4 text-[#F7D6D0]" />
          <span>Trà Sữa & Đồ Uống</span>
        </button>

        <button
          onClick={() => handleTabChange("FOOD")}
          className={`py-3.5 px-4 rounded-2xl font-bold text-sm flex items-center justify-center gap-2 transition-all shadow-sm ${
            activeTab === "FOOD"
              ? "bg-[#6F4E37] text-white shadow-md ring-2 ring-[#6F4E37]/30"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          <UtensilsCrossed className="w-4 h-4 text-[#C5A059]" />
          <span>Đồ Ăn & Ăn Vặt (10 Loại)</span>
        </button>

        <button
          onClick={() => handleTabChange("COMBO")}
          className={`py-3.5 px-4 rounded-2xl font-bold text-sm flex items-center justify-center gap-2 transition-all shadow-sm ${
            activeTab === "COMBO"
              ? "bg-[#6F4E37] text-white shadow-md ring-2 ring-[#6F4E37]/30"
              : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
          }`}
        >
          <Sparkles className="w-4 h-4 text-amber-300" />
          <span>Combo Ăn + Uống</span>
        </button>
      </div>

      {/* 3. Sub-Category Pills & Sort selector */}
      <div id="menu-grid-header" className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 scroll-mt-24">
        {/* Sub-Category filter tabs */}
        <div className="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
          <button
            onClick={() => {
              setSelectedCatId(null);
              setCurrentPage(1);
            }}
            className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all shadow-sm ${
              selectedCatId === null
                ? "bg-[#2D2522] text-white"
                : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
            }`}
          >
            Tất cả danh mục ({products.length})
          </button>

          {visibleCategories.map((c) => {
            const id = c.Id ?? c.id;
            const name = c.Name ?? c.name;
            const isSelected = selectedCatId === id;

            return (
              <button
                key={id}
                onClick={() => {
                  setSelectedCatId(id);
                  setCurrentPage(1);
                }}
                className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all shadow-sm ${
                  isSelected
                    ? "bg-[#2D2522] text-white"
                    : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
                }`}
              >
                {name}
              </button>
            );
          })}
        </div>

        {/* Sort selector & Pagination badge */}
        <div className="flex items-center gap-3 self-end sm:self-auto shrink-0">
          {totalItems > 0 && (
            <span className="text-[11px] font-bold text-[#6F4E37] bg-[#FAF7F2] border border-[#EFE6DC] px-2.5 py-1 rounded-xl">
              Trang {validPage}/{totalPages}
            </span>
          )}
          <div className="flex items-center gap-2">
            <SlidersHorizontal className="w-4 h-4 text-[#786C65]" />
            <span className="text-xs font-semibold text-[#786C65]">Sắp xếp:</span>
            <select
              value={sortBy}
              onChange={(e) => {
                setSortBy(e.target.value);
                setCurrentPage(1);
              }}
              className="bg-white border border-[#EFE6DC] text-xs font-semibold text-[#2D2522] rounded-xl px-3 py-1.5 focus:outline-none focus:border-[#6F4E37]"
            >
              <option value="default">Nổi bật nhất</option>
              <option value="price_asc">Giá tăng dần</option>
              <option value="price_desc">Giá giảm dần</option>
              <option value="newest">Món mới nhất</option>
            </select>
          </div>
        </div>
      </div>

      {/* 4. Product List Grid */}
      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
            <div
              key={i}
              className="h-96 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]"
            />
          ))}
        </div>
      ) : products.length === 0 ? (
        <div className="bg-white rounded-3xl p-12 text-center border border-[#EFE6DC] space-y-4">
          <div className="w-16 h-16 rounded-full bg-[#FAF7F2] flex items-center justify-center mx-auto text-[#6F4E37]">
            <Search className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-[#2D2522]">
            Không tìm thấy món nào phù hợp
          </h3>
          <p className="text-xs text-[#786C65] max-w-sm mx-auto">
            Vui lòng thử tìm kiếm với từ khóa khác hoặc chọn xem danh mục khác.
          </p>
          <button
            onClick={() => {
              setActiveTab("ALL");
              setSelectedCatId(null);
              setSearchQuery("");
            }}
            className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors"
          >
            Xem toàn bộ thực đơn
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {paginatedProducts.map((p) => {
            const id = p.Id ?? p.id;
            const name = p.Name ?? p.name;
            const slug = p.Slug ?? p.slug;
            const price = p.BasePrice ?? p.basePrice;
            const desc = p.Description ?? p.description;
            const img = p.ImageUrl ?? p.imageUrl;
            const isBest = p.IsBestSeller ?? p.isBestSeller;
            const isNew = p.IsNew ?? p.isNew;
            const catName = p.CategoryName ?? p.categoryName;
            const type = p.ProductType ?? p.productType ?? "DRINK";
            const isAdded = addedId === id;

            return (
              <div
                key={id}
                className="group bg-white rounded-3xl p-4 border border-[#EFE6DC] shadow-sm hover:shadow-xl hover:border-[#6F4E37]/30 transition-all duration-300 flex flex-col justify-between"
              >
                <div>
                  {/* Image container */}
                  <div className="relative aspect-square rounded-2xl overflow-hidden bg-[#FAF7F2] mb-3">
                    <img
                      src={img}
                      alt={name}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                    />

                    {/* Type Badge */}
                    <div className="absolute top-2.5 left-2.5 flex flex-col gap-1 items-start">
                      {type === "FOOD" && (
                        <span className="text-[10px] font-black px-2.5 py-1 rounded-full bg-orange-600 text-white shadow-sm flex items-center gap-1">
                          <UtensilsCrossed className="w-3 h-3" /> ĐỒ ĂN
                        </span>
                      )}
                      {type === "COMBO" && (
                        <span className="text-[10px] font-black px-2.5 py-1 rounded-full bg-purple-600 text-white shadow-sm flex items-center gap-1">
                          <Sparkles className="w-3 h-3" /> COMBO TIẾT KIỆM
                        </span>
                      )}
                      {type === "DRINK" && (
                        <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#6F4E37] text-white shadow-sm">
                          TRÀ SỮA
                        </span>
                      )}

                      {/* Spicy indicator */}
                      {(p.SpicyLevel ?? p.spicyLevel ?? 0) > 0 && (
                        <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-red-600 text-white shadow-sm flex items-center gap-0.5">
                          <Flame className="w-2.5 h-2.5" /> Cay cấp {p.SpicyLevel ?? p.spicyLevel}
                        </span>
                      )}
                    </div>

                    {/* Best Seller or New */}
                    <div className="absolute top-2.5 right-2.5 flex flex-col gap-1 items-end">
                      {isBest && (
                        <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-[#C5A059] text-white shadow-sm flex items-center gap-1">
                          <Flame className="w-2.5 h-2.5" /> Bán Chạy
                        </span>
                      )}
                      {isNew && (
                        <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-[#4E7D55] text-white shadow-sm">
                          Mới
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Category name & Product Title */}
                  <div className="space-y-1">
                    <span className="text-[11px] font-semibold text-[#786C65] block uppercase tracking-wider">
                      {catName}
                    </span>
                    <Link
                      href={`/menu/${slug}`}
                      className="font-extrabold text-[#2D2522] text-sm group-hover:text-[#6F4E37] transition-colors line-clamp-1 block"
                      title={name}
                    >
                      {name}
                    </Link>
                    <p className="text-xs text-[#786C65] line-clamp-2 leading-relaxed">
                      {desc}
                    </p>

                    {/* Portion or Sauce hint for food */}
                    {(p.PortionDescription ?? p.portionDescription) && (
                      <p className="text-[11px] text-[#6F4E37] font-medium pt-0.5">
                        Khẩu phần: {p.PortionDescription ?? p.portionDescription}
                      </p>
                    )}
                  </div>
                </div>

                {/* Price & Action button */}
                <div className="pt-4 mt-3 border-t border-[#FAF7F2] flex items-center justify-between">
                  <div>
                    <span className="text-base font-black text-[#6F4E37]">
                      {price.toLocaleString("vi-VN")}đ
                    </span>
                  </div>

                  <div className="flex items-center gap-1.5">
                    <Link
                      href={`/menu/${slug}`}
                      className="px-2.5 py-1.5 rounded-xl border border-[#EFE6DC] text-[11px] font-bold text-[#6F4E37] hover:bg-[#FAF7F2] transition-colors"
                      title="Tùy chọn chi tiết"
                    >
                      Tùy chọn
                    </Link>

                    <button
                      onClick={() => handleQuickAdd(p)}
                      disabled={isAdded}
                      className={`p-2 rounded-xl font-bold text-xs flex items-center justify-center transition-all ${
                        isAdded
                          ? "bg-[#4E7D55] text-white scale-95"
                          : "bg-[#6F4E37] text-white hover:bg-[#4A3324] shadow-sm active:scale-95"
                      }`}
                      title="Thêm nhanh vào giỏ"
                    >
                      {isAdded ? (
                        <Check className="w-4 h-4" />
                      ) : (
                        <Plus className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* 5. Pagination Controls (4 món / trang) */}
      {!loading && totalPages > 1 && (
        <div className="pt-8 border-t border-[#EFE6DC] flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="text-xs font-semibold text-[#786C65]">
            Đang hiển thị món <span className="font-extrabold text-[#2D2522]">{startIndex + 1} - {endIndex}</span> trong tổng số <span className="font-extrabold text-[#6F4E37]">{totalItems} món</span> (4 món / trang)
          </div>

          <div className="flex items-center gap-1.5">
            {/* Prev Button */}
            <button
              onClick={() => handlePageChange(validPage - 1)}
              disabled={validPage === 1}
              className={`px-3 py-2 rounded-xl text-xs font-bold flex items-center gap-1 transition-all ${
                validPage === 1
                  ? "bg-[#FAF7F2] text-gray-300 border border-[#EFE6DC] cursor-not-allowed"
                  : "bg-white text-[#2D2522] border border-[#EFE6DC] hover:bg-[#6F4E37] hover:text-white hover:border-[#6F4E37] shadow-xs active:scale-95"
              }`}
              title="Trang trước"
            >
              <ChevronLeft className="w-4 h-4" />
              <span className="hidden sm:inline">Trước</span>
            </button>

            {/* Page Number Buttons with Smart Windowing */}
            {Array.from({ length: totalPages }, (_, idx) => idx + 1).map((pNum) => {
              const isNearby = Math.abs(pNum - validPage) <= 2;
              const isFirstOrLast = pNum === 1 || pNum === totalPages;
              if (!isNearby && !isFirstOrLast) {
                if (pNum === 2 || pNum === totalPages - 1) {
                  return (
                    <span key={pNum} className="px-1 text-xs text-[#786C65]">
                      ...
                    </span>
                  );
                }
                return null;
              }

              const isActive = pNum === validPage;
              return (
                <button
                  key={pNum}
                  onClick={() => handlePageChange(pNum)}
                  className={`w-9 h-9 rounded-xl text-xs font-black transition-all ${
                    isActive
                      ? "bg-[#6F4E37] text-white shadow-md scale-105"
                      : "bg-white text-[#2D2522] border border-[#EFE6DC] hover:bg-[#FAF7F2]"
                  }`}
                >
                  {pNum}
                </button>
              );
            })}

            {/* Next Button */}
            <button
              onClick={() => handlePageChange(validPage + 1)}
              disabled={validPage === totalPages}
              className={`px-3 py-2 rounded-xl text-xs font-bold flex items-center gap-1 transition-all ${
                validPage === totalPages
                  ? "bg-[#FAF7F2] text-gray-300 border border-[#EFE6DC] cursor-not-allowed"
                  : "bg-white text-[#2D2522] border border-[#EFE6DC] hover:bg-[#6F4E37] hover:text-white hover:border-[#6F4E37] shadow-xs active:scale-95"
              }`}
              title="Trang tiếp theo"
            >
              <span className="hidden sm:inline">Sau</span>
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default function MenuPage() {
  return (
    <Suspense
      fallback={
        <div className="max-w-7xl mx-auto px-4 py-20 text-center">
          <div className="w-12 h-12 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-xs text-[#786C65] mt-3 font-semibold">Đang tải thực đơn MilkyVerse...</p>
        </div>
      }
    >
      <MenuContent />
    </Suspense>
  );
}
