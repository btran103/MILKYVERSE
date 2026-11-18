import React from "react";
import Link from "next/link";
import { Sparkles, ArrowRight, Flame, Eye, Star, Check, Plus, Zap } from "lucide-react";
import { Product } from "@/types";

interface SignatureSectionProps {
  products: Product[];
  loading: boolean;
  addedId: number | null;
  onSelectProduct: (product: Product) => void;
  onQuickAdd: (product: Product, e?: React.MouseEvent) => void;
  onQuickBuyNow: (product: Product, e?: React.MouseEvent) => void;
}

export default function SignatureSection({
  products,
  loading,
  addedId,
  onSelectProduct,
  onQuickAdd,
  onQuickBuyNow,
}: SignatureSectionProps) {
  return (
    <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
      <div className="flex flex-col sm:flex-row items-start sm:items-end justify-between gap-4 pb-2 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
            <Sparkles className="w-4 h-4 text-amber-500 fill-amber-500" />
            <span>Thực Đơn Đặc Biệt Hôm Nay</span>
          </div>
          <h2 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
            5 Món Signature Mỗi Ngày Bất Kỳ
          </h2>
          <p className="text-xs text-[#786C65] mt-1">
            Tuyển chọn 5 món đặc sắc trong ngày: Nhấn vào để xem chi tiết bên trong, bình luận và đánh giá số sao
          </p>
        </div>
        <Link
          href="/menu"
          className="flex items-center gap-1.5 text-xs font-bold text-[#6F4E37] hover:underline"
        >
          <span>Khám phá toàn bộ menu</span>
          <ArrowRight className="w-4 h-4" />
        </Link>
      </div>

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
          {[1, 2, 3, 4, 5].map((n) => (
            <div key={n} className="h-80 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
          ))}
        </div>
      ) : products.length === 0 ? (
        <div className="p-8 text-center text-[#786C65] bg-white rounded-3xl border border-[#EFE6DC]">
          Đang tải 5 món signature hôm nay...
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-5">
          {products.map((product, idx) => {
            if (!product) return null;
            const id = product.Id ?? product.id;
            const name = product.Name ?? product.name ?? "Trà sữa Signature";
            const price = product.BasePrice ?? product.basePrice ?? 0;
            const img = (product.ImageUrl ?? product.imageUrl) || "/logo.png";
            const desc = product.Description ?? product.description ?? "";

            return (
              <div
                key={id}
                onClick={() => onSelectProduct(product)}
                className="group bg-white rounded-3xl border border-[#EFE6DC] overflow-hidden shadow-sm hover:shadow-xl transition-all duration-300 flex flex-col cursor-pointer transform hover:-translate-y-1"
              >
                <div className="relative aspect-square overflow-hidden bg-[#FAF7F2]">
                  <img
                    src={img}
                    alt={name}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                  <div className="absolute top-2.5 left-2.5 flex flex-col gap-1">
                    <span className="bg-[#6F4E37] text-white text-[10px] font-bold px-2 py-0.5 rounded-full shadow-md flex items-center gap-1">
                      <Flame className="w-3 h-3 text-amber-300 fill-amber-300" />
                      Signature #{idx + 1}
                    </span>
                  </div>

                  <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                    <span className="px-3 py-1.5 rounded-xl bg-white/95 text-[#6F4E37] font-bold text-xs shadow-md flex items-center gap-1.5">
                      <Eye className="w-3.5 h-3.5" />
                      <span>Xem chi tiết</span>
                    </span>
                  </div>
                </div>

                <div className="p-4 flex-1 flex flex-col justify-between space-y-3">
                  <div>
                    <h3 className="font-extrabold text-[#2D2522] text-sm group-hover:text-[#6F4E37] transition-colors line-clamp-1">
                      {name}
                    </h3>
                    <p className="text-[11px] text-[#786C65] line-clamp-2 mt-1 leading-relaxed">
                      {desc}
                    </p>
                  </div>

                  <div className="flex items-center gap-1 text-[11px] font-bold text-amber-800">
                    <Star className="w-3.5 h-3.5 text-amber-500 fill-amber-500" />
                    <span>4.9 ★ (Được khen nhiều)</span>
                  </div>

                  <div className="pt-2 border-t border-[#FAF7F2] flex items-center justify-between gap-1.5">
                    <span className="text-sm font-black text-[#6F4E37]">
                      {price.toLocaleString("vi-VN")}đ
                    </span>

                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={(e) => onQuickAdd(product, e)}
                        className={`p-2 rounded-xl font-bold text-xs flex items-center gap-1 transition-all shadow-sm ${
                          addedId === id
                            ? "bg-[#4E7D55] text-white"
                            : "bg-[#F7D6D0] text-[#6F4E37] hover:bg-[#6F4E37] hover:text-white"
                        }`}
                        title="Thêm nhanh vào giỏ"
                      >
                        {addedId === id ? <Check className="w-3.5 h-3.5" /> : <Plus className="w-3.5 h-3.5" />}
                        <span className="text-[11px]">Thêm</span>
                      </button>

                      <button
                        onClick={(e) => onQuickBuyNow(product, e)}
                        className="px-2.5 py-1.5 rounded-xl bg-gradient-to-r from-[#6F4E37] to-[#8B5A2B] text-white text-[11px] font-bold hover:brightness-110 shadow-sm flex items-center gap-1"
                        title="Mua ngay lập tức"
                      >
                        <Zap className="w-3 h-3 text-amber-300 fill-amber-300" />
                        <span>Mua Ngay</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </section>
  );
}
