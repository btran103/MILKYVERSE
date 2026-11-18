import React from "react";
import Link from "next/link";
import {
  Sparkles,
  Leaf,
  Star,
  Coffee,
  Plus,
  Zap,
  MessageSquare,
  ThumbsUp,
  CheckCircle2,
  Lock,
  LogIn,
} from "lucide-react";
import { Product } from "@/types";
import { CommentItem } from "./types";

interface ProductDetailModalProps {
  selectedProduct: Product | null;
  onClose: () => void;
  user: any;
  comments: CommentItem[];
  onQuickAdd: (product: Product) => void;
  onQuickBuyNow: (product: Product) => void;
  dishReviewText: string;
  setDishReviewText: (text: string) => void;
  dishReviewRating: number;
  setDishReviewRating: (rating: number) => void;
  dishReviewSuccess: boolean;
  onDishReviewSubmit: (e: React.FormEvent) => void;
}

export default function ProductDetailModal({
  selectedProduct,
  onClose,
  user,
  comments,
  onQuickAdd,
  onQuickBuyNow,
  dishReviewText,
  setDishReviewText,
  dishReviewRating,
  setDishReviewRating,
  dishReviewSuccess,
  onDishReviewSubmit,
}: ProductDetailModalProps) {
  if (!selectedProduct) return null;

  const prodId = selectedProduct.Id ?? selectedProduct.id;
  const prodName = selectedProduct.Name ?? selectedProduct.name ?? "Trà sữa Signature";
  const prodPrice = selectedProduct.BasePrice ?? selectedProduct.basePrice ?? 0;
  const prodImg = selectedProduct.ImageUrl || selectedProduct.imageUrl || "/logo.png";
  const prodDesc = selectedProduct.Description || selectedProduct.description || "";

  return (
    <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-3xl max-w-2xl w-full border border-[#EFE6DC] shadow-2xl p-6 sm:p-8 space-y-6 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between pb-3 border-b">
          <div className="flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-amber-500" />
            <h3 className="font-black text-lg text-[#2D2522]">
              Chi Tiết Món Signature Hôm Nay
            </h3>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 font-bold text-xs"
          >
            Đóng [X]
          </button>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 items-start">
          <div className="space-y-4">
            <div className="relative aspect-square rounded-2xl overflow-hidden bg-[#FAF7F2] border border-[#EFE6DC]">
              <img
                src={prodImg}
                alt={prodName}
                className="w-full h-full object-cover"
              />
              <span className="absolute top-3 left-3 bg-[#6F4E37] text-white text-[10px] font-bold px-2.5 py-0.5 rounded-full shadow-md">
                Signature Món Hot
              </span>
            </div>

            {/* Danh sách nguyên liệu tuyển chọn */}
            <div className="p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs space-y-2">
              <div className="flex items-center gap-2 font-bold text-[#2D2522]">
                <Leaf className="w-4 h-4 text-emerald-600" />
                <span>Danh Sách Nguyên Liệu Tuyển Chọn:</span>
              </div>
              <ul className="space-y-1 text-[11px] text-[#786C65] list-disc list-inside">
                <li>Lá trà Ô Long Mộc Châu ủ lạnh 4 tiếng giữ trọn hương</li>
                <li>100% Sữa tươi thanh trùng tiệt trùng chuẩn VietGAP</li>
                <li>Trân châu hoàng kim dẻo mềm nấu đường đen mía nguyên chất</li>
                <li>Nước lọc RO tinh khiết qua 5 tầng tiệt trùng</li>
              </ul>
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <h4 className="text-xl font-black text-[#2D2522]">{prodName}</h4>
              <div className="flex items-center gap-1.5 text-xs font-bold text-amber-500 mt-1">
                <Star className="w-4 h-4 fill-amber-500" />
                <span>4.9 / 5.0 (Đánh giá cao nhất từ khách hàng)</span>
              </div>
            </div>

            <p className="text-xs text-[#786C65] leading-relaxed">{prodDesc}</p>

            {/* Cẩm nang mức độ đường uống */}
            <div className="p-3.5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs space-y-1.5">
              <div className="flex items-center gap-2 font-bold text-[#6F4E37]">
                <Coffee className="w-4 h-4" />
                <span>Mức Độ Đường Uống Khuyên Dùng:</span>
              </div>
              <div className="grid grid-cols-2 gap-1.5 text-[11px] text-[#786C65]">
                <span className="bg-white p-1.5 rounded-lg border">0% - Vị mộc mạc</span>
                <span className="bg-white p-1.5 rounded-lg border">30% - Thanh nhẹ</span>
                <span className="bg-[#6F4E37] text-white p-1.5 rounded-lg font-bold">50% - Chuẩn vị (Ngon nhất)</span>
                <span className="bg-white p-1.5 rounded-lg border">70% - Ngọt dịu</span>
              </div>
            </div>

            <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 pt-2 border-t border-[#FAF7F2]">
              <span className="text-2xl font-black text-[#6F4E37]">
                {prodPrice.toLocaleString("vi-VN")}đ
              </span>

              <div className="flex items-center gap-2">
                {/* Nút Thêm Vào Giỏ */}
                <button
                  onClick={() => {
                    onQuickAdd(selectedProduct);
                    onClose();
                  }}
                  className="px-4 py-2.5 rounded-xl border border-[#6F4E37] text-[#6F4E37] hover:bg-[#6F4E37] hover:text-white font-bold text-xs shadow-sm transition-all flex items-center gap-1.5"
                >
                  <Plus className="w-4 h-4" />
                  <span>Thêm Vào Giỏ</span>
                </button>

                {/* Nút Mua Ngay kế bên nút thêm vào giỏ */}
                <button
                  onClick={() => {
                    onQuickBuyNow(selectedProduct);
                    onClose();
                  }}
                  className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-[#6F4E37] via-[#8B5A2B] to-[#C5A059] text-white font-bold text-xs hover:brightness-110 shadow-md transition-all flex items-center gap-1.5"
                >
                  <Zap className="w-4 h-4 text-amber-300 fill-amber-300" />
                  <span>Mua Ngay</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Bình luận tại món đó với xác thực phân quyền */}
        <div className="pt-4 border-t border-[#FAF7F2] space-y-4">
          <div className="flex items-center justify-between">
            <h5 className="font-extrabold text-xs text-[#2D2522] uppercase tracking-wider flex items-center gap-1.5">
              <MessageSquare className="w-4 h-4 text-[#6F4E37]" />
              <span>Bình Luận Đánh Giá Tại Món Này</span>
            </h5>
            <span className="text-[11px] text-[#786C65]">Đánh giá chân thật từ khách hàng</span>
          </div>

          {/* Form gửi bình luận cho món - Kiểm tra phân quyền đăng nhập */}
          {user ? (
            <form onSubmit={onDishReviewSubmit} className="p-3.5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-2.5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-xs font-bold text-[#2D2522]">
                  Đánh giá của bạn ({user.fullName}):
                </span>
                <div className="flex items-center gap-1">
                  {[1, 2, 3, 4, 5].map((st) => (
                    <button
                      key={st}
                      type="button"
                      onClick={() => setDishReviewRating(st)}
                      className="p-0.5 hover:scale-125 transition-transform"
                    >
                      <Star className={`w-4 h-4 ${st <= dishReviewRating ? "text-amber-500 fill-amber-500" : "text-gray-300"}`} />
                    </button>
                  ))}
                </div>
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="text"
                  required
                  placeholder="Bạn thấy món này ngon hay dở? Nhận xét hương vị..."
                  value={dishReviewText}
                  onChange={(e) => setDishReviewText(e.target.value)}
                  className="flex-1 px-3 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
                <button
                  type="submit"
                  className="px-4 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all shrink-0 flex items-center gap-1"
                >
                  <ThumbsUp className="w-3.5 h-3.5" />
                  <span>Đánh Giá</span>
                </button>
              </div>

              {dishReviewSuccess && (
                <p className="text-[11px] text-emerald-700 font-bold flex items-center gap-1">
                  <CheckCircle2 className="w-3.5 h-3.5" /> Đã gửi đánh giá thành công!
                </p>
              )}
            </form>
          ) : (
            <div className="p-3.5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-between gap-3">
              <div className="flex items-center gap-2 text-xs text-[#786C65]">
                <Lock className="w-4 h-4 text-amber-700 shrink-0" />
                <span>Xác thực phân quyền: Đăng nhập để bình luận & đánh giá món này.</span>
              </div>
              <Link
                href="/auth/login"
                className="px-3.5 py-1.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shrink-0 flex items-center gap-1"
              >
                <LogIn className="w-3.5 h-3.5" />
                <span>Đăng Nhập</span>
              </Link>
            </div>
          )}

          {/* Danh sách bình luận món */}
          <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
            {comments.map((cm) => (
              <div key={cm.id} className="p-3 rounded-xl bg-[#FAF7F2] text-xs space-y-1">
                <div className="flex items-center justify-between font-bold text-[#2D2522]">
                  <span className="flex items-center gap-1">
                    {cm.userName}
                    <span className="text-[10px] text-emerald-700 font-semibold bg-emerald-50 px-1 rounded border border-emerald-200">Đã thử món</span>
                  </span>
                  <div className="flex items-center gap-0.5 text-amber-500">
                    {Array.from({ length: cm.rating }).map((_, i) => (
                      <Star key={i} className="w-2.5 h-2.5 fill-amber-500" />
                    ))}
                  </div>
                </div>
                <p className="text-[#786C65] text-[11px]">{cm.content}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
