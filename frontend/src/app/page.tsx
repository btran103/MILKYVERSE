"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Category, Product } from "@/types";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
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
  MessageSquare,
  Send,
  Eye,
  Heart,
  ChevronRight,
  User,
  Zap,
  Lock,
  LogIn,
  Leaf,
  Coffee,
  CheckCircle2,
  ThumbsUp,
} from "lucide-react";

interface CommentItem {
  id: number;
  userName: string;
  avatarUrl?: string;
  rating: number;
  date: string;
  content: string;
  productId?: number;
}

const INITIAL_COMMENTS: CommentItem[] = [
  {
    id: 1,
    userName: "Trần Bảo Ngọc",
    avatarUrl: "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80",
    rating: 5,
    date: "Hôm nay",
    content: "Trà sữa nướng trân châu hoàng kim đỉnh chóp! Trân châu mềm dẻo thấm vị đường đen, vị trà đậm đà không bị ngọt gắt.",
  },
  {
    id: 2,
    userName: "Lê Minh Quân",
    avatarUrl: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80",
    rating: 5,
    date: "Hôm qua",
    content: "Mì cay 7 cấp độ ăn kèm trà dâu macchiato là combo chân ái luôn. Cửa hàng giao nhanh đúng 20 phút là có hàng nóng hổi.",
  },
  {
    id: 3,
    userName: "Hoàng Yến Nhi",
    avatarUrl: "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80",
    rating: 5,
    date: "2 ngày trước",
    content: "Món signature hôm nay ngon xuất sắc, lớp kem cheese béo mặn sánh mịn, uống kèm trân châu 3Q giòn sần sật cực mê!",
  },
  {
    id: 4,
    userName: "Đặng Tuấn Anh",
    avatarUrl: "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80",
    rating: 4,
    date: "3 ngày trước",
    content: "Cá viên chiên mẹt thập cẩm sốt Thái đậm đà, nước sốt chấm thần thánh ăn cuốn miệng dã man. 5 sao cho chất lượng phục vụ!",
  },
];

export default function HomePage() {
  const router = useRouter();
  const { user } = useAuth();
  const { addItem, openAuthModal } = useCart();

  const [categories, setCategories] = useState<Category[]>([]);
  const [signatureProducts, setSignatureProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [addedId, setAddedId] = useState<number | null>(null);

  // Modal Detail & Review State
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [comments, setComments] = useState<CommentItem[]>(INITIAL_COMMENTS);
  const [newCommentName, setNewCommentName] = useState("");
  const [newCommentText, setNewCommentText] = useState("");
  const [newCommentRating, setNewCommentRating] = useState(5);

  // Dish-specific review state
  const [dishReviewText, setDishReviewText] = useState("");
  const [dishReviewRating, setDishReviewRating] = useState(5);
  const [dishReviewVerdict, setDishReviewVerdict] = useState("Rất ngon / Chuẩn vị");
  const [dishReviewSuccess, setDishReviewSuccess] = useState(false);

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

        // 5 Món Signature mỗi ngày bất kì (Ưu tiên BestSeller hoặc lấy đúng 5 món đầu tiên/đặc sắc)
        const best = prodList.filter((p) => p && Boolean(p.isBestSeller));
        const finalFive = best.length >= 5 ? best.slice(0, 5) : prodList.slice(0, 5);
        setSignatureProducts(finalFive);
      } catch (err) {
        console.error("Lỗi khi tải dữ liệu trang chủ:", err);
        setCategories([]);
        setSignatureProducts([]);
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  const handleQuickAdd = (product: Product, e?: React.MouseEvent) => {
    if (e) e.stopPropagation();
    if (!product) return;
    const prodId = product.Id ?? product.id;
    const prodName = product.Name ?? product.name ?? "Trà sữa MilkyVerse";
    const prodPrice = product.BasePrice ?? product.basePrice ?? 0;
    const prodImage = product.ImageUrl ?? product.imageUrl ?? "/logo.png";
    const type = product.ProductType ?? product.productType ?? "DRINK";

    const success = addItem({
      productId: prodId,
      name: prodName,
      imageUrl: prodImage,
      itemType: type,
      size: type === "DRINK" ? "M" : "Standard",
      sugarLevel: type === "DRINK" ? "50%" : undefined,
      iceLevel: type === "DRINK" ? "70%" : undefined,
      spicyLevel: type === "FOOD" && product.spicyLevel ? `Cấp ${product.spicyLevel}` : undefined,
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

  const handleQuickBuyNow = (product: Product, e?: React.MouseEvent) => {
    if (e) e.stopPropagation();
    if (!product) return;
    const prodId = product.Id ?? product.id;
    const prodName = product.Name ?? product.name ?? "Trà sữa MilkyVerse";
    const prodPrice = product.BasePrice ?? product.basePrice ?? 0;
    const prodImage = product.ImageUrl ?? product.imageUrl ?? "/logo.png";
    const type = product.ProductType ?? product.productType ?? "DRINK";

    const success = addItem({
      productId: prodId,
      name: prodName,
      imageUrl: prodImage,
      itemType: type,
      size: type === "DRINK" ? "M" : "Standard",
      sugarLevel: type === "DRINK" ? "50%" : undefined,
      iceLevel: type === "DRINK" ? "70%" : undefined,
      spicyLevel: type === "FOOD" && product.spicyLevel ? `Cấp ${product.spicyLevel}` : undefined,
      unitPrice: prodPrice,
      quantity: 1,
      totalPrice: prodPrice,
      isCustomDIY: false,
      toppings: [],
    });

    if (success) {
      router.push("/checkout");
    }
  };

  const handleAddReview = (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) {
      openAuthModal?.("Vui lòng đăng nhập tài khoản để gửi bình luận & đánh giá!");
      return;
    }
    if (!newCommentText.trim()) return;

    const newRev: CommentItem = {
      id: Date.now(),
      userName: user.fullName || "Khách Hàng MilkyVerse",
      avatarUrl: user.avatarUrl || "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
      rating: newCommentRating,
      date: "Vừa xong",
      content: newCommentText.trim(),
      productId: selectedProduct ? (selectedProduct.Id ?? selectedProduct.id) : undefined,
    };

    setComments([newRev, ...comments]);
    setNewCommentText("");
  };

  const handleDishReviewSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) {
      openAuthModal?.("Vui lòng đăng nhập để bình luận cho món này!");
      return;
    }
    if (!dishReviewText.trim()) return;

    const newDishRev: CommentItem = {
      id: Date.now(),
      userName: user.fullName || "Khách Hàng MilkyVerse",
      avatarUrl: user.avatarUrl || "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
      rating: dishReviewRating,
      date: "Vừa xong",
      content: `${dishReviewVerdict ? `[${dishReviewVerdict}] ` : ""}${dishReviewText.trim()}`,
      productId: selectedProduct ? (selectedProduct.Id ?? selectedProduct.id) : undefined,
    };

    setComments([newDishRev, ...comments]);
    setDishReviewText("");
    setDishReviewSuccess(true);
    setTimeout(() => setDishReviewSuccess(false), 3000);
  };

  return (
    <div className="space-y-16 pb-16">
      {/* 1. Hero Section */}
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
            </div>
          </div>
        </div>
      </section>

      {/* 2. 5 MÓN SIGNATURE MỖI NGÀY (HÔM NAY) */}
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
        ) : signatureProducts.length === 0 ? (
          <div className="p-8 text-center text-[#786C65] bg-white rounded-3xl border border-[#EFE6DC]">
            Đang tải 5 món signature hôm nay...
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-5">
            {signatureProducts.map((product, idx) => {
              if (!product) return null;
              const id = product.Id ?? product.id;
              const name = product.Name ?? product.name ?? "Trà sữa Signature";
              const price = product.BasePrice ?? product.basePrice ?? 0;
              const img = (product.ImageUrl ?? product.imageUrl) || "/logo.png";
              const desc = product.Description ?? product.description ?? "";

              return (
                <div
                  key={id}
                  onClick={() => setSelectedProduct(product)}
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
                          onClick={(e) => handleQuickAdd(product, e)}
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
                          onClick={(e) => handleQuickBuyNow(product, e)}
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

      {/* 3. ĐÁNH GIÁ SỐ SAO CHO TOÀN BỘ MÓN & BÌNH LUẬN KHÁCH HÀNG */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div className="bg-white rounded-3xl p-6 sm:p-10 border border-[#EFE6DC] shadow-sm space-y-8">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 pb-6 border-b border-[#EFE6DC]">
            <div>
              <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
                <Star className="w-4 h-4 text-amber-500 fill-amber-500" />
                <span>Cộng Đồng Thưởng Thức MilkyVerse</span>
              </div>
              <h2 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
                Đánh Giá Số Sao & Bình Luận Toàn Bộ Món
              </h2>
              <p className="text-xs sm:text-sm text-[#786C65] mt-1">
                Nhận xét chân thực từ hàng ngàn tín đồ trà sữa & đồ ăn vặt mỗi ngày
              </p>
            </div>

            {/* Overall Rating Summary Box */}
            <div className="flex items-center gap-4 bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] shrink-0">
              <div className="text-center">
                <span className="text-3xl font-black text-[#2D2522] block">4.9</span>
                <div className="flex items-center justify-center gap-0.5 text-amber-500">
                  {[1, 2, 3, 4, 5].map((s) => (
                    <Star key={s} className="w-3.5 h-3.5 fill-amber-500" />
                  ))}
                </div>
              </div>
              <div className="text-xs text-[#786C65] border-l border-[#EFE6DC] pl-4">
                <p className="font-bold text-[#2D2522]">1,280+ Đánh Giá</p>
                <p>98% Khách Hài Lòng</p>
              </div>
            </div>
          </div>

          {/* Comments Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {comments.slice(0, 4).map((c) => (
              <div
                key={c.id}
                className="p-5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-3 flex flex-col justify-between"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-full overflow-hidden bg-white border border-[#EFE6DC] shrink-0">
                      {c.avatarUrl ? (
                        <img src={c.avatarUrl} alt={c.userName} className="w-full h-full object-cover" />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center bg-[#6F4E37] text-white font-bold text-xs">
                          {c.userName.charAt(0)}
                        </div>
                      )}
                    </div>
                    <div>
                      <h4 className="font-extrabold text-xs text-[#2D2522]">{c.userName}</h4>
                      <div className="flex items-center gap-1 text-amber-500 mt-0.5">
                        {Array.from({ length: c.rating }).map((_, i) => (
                          <Star key={i} className="w-3 h-3 fill-amber-500" />
                        ))}
                      </div>
                    </div>
                  </div>
                  <span className="text-[10px] text-[#786C65] font-semibold">{c.date}</span>
                </div>

                <p className="text-xs text-[#786C65] leading-relaxed italic">
                  &ldquo;{c.content}&rdquo;
                </p>
              </div>
            ))}
          </div>

          {/* Form Viết Đánh Giá & Chấm Sao (Có Xác Thực Phân Quyền) */}
          {user ? (
            <form
              onSubmit={handleAddReview}
              className="p-5 sm:p-6 rounded-2xl bg-gradient-to-r from-[#FAF7F2] to-white border border-[#EFE6DC] space-y-4"
            >
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div className="flex items-center gap-2.5">
                  <div className="w-8 h-8 rounded-full overflow-hidden bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs shrink-0">
                    {user.avatarUrl ? (
                      <img src={user.avatarUrl} alt={user.fullName} className="w-full h-full object-cover" />
                    ) : (
                      user.fullName.charAt(0)
                    )}
                  </div>
                  <div>
                    <h4 className="font-extrabold text-xs text-[#2D2522]">{user.fullName}</h4>
                    <span className="text-[10px] text-emerald-700 font-semibold flex items-center gap-1">
                      <CheckCircle2 className="w-3 h-3" /> Đã xác thực tài khoản • Khách hàng MilkyVerse
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <span className="text-xs font-semibold text-[#786C65]">Chấm điểm sao:</span>
                  <div className="flex items-center gap-1">
                    {[1, 2, 3, 4, 5].map((s) => (
                      <button
                        key={s}
                        type="button"
                        onClick={() => setNewCommentRating(s)}
                        className="p-1 hover:scale-125 transition-transform"
                      >
                        <Star
                          className={`w-4 h-4 ${
                            s <= newCommentRating
                              ? "text-amber-500 fill-amber-500"
                              : "text-gray-300"
                          }`}
                        />
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="text"
                  required
                  placeholder="Cảm nhận của bạn về hương vị trà sữa, độ ngọt béo, đồ ăn vặt..."
                  value={newCommentText}
                  onChange={(e) => setNewCommentText(e.target.value)}
                  className="flex-1 px-4 py-2.5 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
                <button
                  type="submit"
                  className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all flex items-center gap-1.5 shrink-0"
                >
                  <Send className="w-3.5 h-3.5" />
                  <span>Gửi</span>
                </button>
              </div>
            </form>
          ) : (
            <div className="p-5 sm:p-6 rounded-2xl bg-gradient-to-r from-[#FAF7F2] to-white border border-[#EFE6DC] flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
              <div className="flex items-start gap-3">
                <div className="w-10 h-10 rounded-2xl bg-amber-100 text-amber-800 flex items-center justify-center shrink-0">
                  <Lock className="w-5 h-5" />
                </div>
                <div>
                  <h4 className="font-extrabold text-sm text-[#2D2522]">Xác Thực Phân Quyền Bình Luận</h4>
                  <p className="text-xs text-[#786C65]">Vui lòng đăng nhập tài khoản MilkyVerse để viết đánh giá và chấm điểm sao.</p>
                </div>
              </div>
              <Link
                href="/auth/login"
                className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shadow-sm flex items-center gap-1.5 shrink-0"
              >
                <LogIn className="w-4 h-4" />
                <span>Đăng Nhập Để Bình Luận</span>
              </Link>
            </div>
          )}
        </div>
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
                Nhập Mã <span className="text-[#6F4E37] underline decoration-[#C5A059]">#MK2095F54</span> Giảm Ngay 25.000đ
              </h3>
              <p className="text-xs text-[#786C65]">
                Áp dụng cho mọi hóa đơn. Mã <strong>FREESHIP</strong> miễn phí vận chuyển cho đơn từ 99k!
              </p>
            </div>
            <Link
              href="/menu"
              className="px-6 py-3 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all shrink-0"
            >
              Đặt Món Ngay
            </Link>
          </div>
        </div>
      </section>

      {/* MODAL XEM CHI TIẾT TỪNG MÓN SIGNATURE BÊN TRONG */}
      {selectedProduct && (
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
                onClick={() => setSelectedProduct(null)}
                className="text-gray-400 hover:text-gray-600 font-bold text-xs"
              >
                Đóng [X]
              </button>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 items-start">
              <div className="space-y-4">
                <div className="relative aspect-square rounded-2xl overflow-hidden bg-[#FAF7F2] border border-[#EFE6DC]">
                  <img
                    src={selectedProduct.ImageUrl || selectedProduct.imageUrl || "/logo.png"}
                    alt={selectedProduct.Name || selectedProduct.name}
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
                  <h4 className="text-xl font-black text-[#2D2522]">
                    {selectedProduct.Name || selectedProduct.name}
                  </h4>
                  <div className="flex items-center gap-1.5 text-xs font-bold text-amber-500 mt-1">
                    <Star className="w-4 h-4 fill-amber-500" />
                    <span>4.9 / 5.0 (Đánh giá cao nhất từ khách hàng)</span>
                  </div>
                </div>

                <p className="text-xs text-[#786C65] leading-relaxed">
                  {selectedProduct.Description || selectedProduct.description}
                </p>

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
                    {(selectedProduct.BasePrice || selectedProduct.basePrice || 0).toLocaleString("vi-VN")}đ
                  </span>

                  <div className="flex items-center gap-2">
                    {/* Nút Thêm Vào Giỏ */}
                    <button
                      onClick={() => {
                        handleQuickAdd(selectedProduct);
                        setSelectedProduct(null);
                      }}
                      className="px-4 py-2.5 rounded-xl border border-[#6F4E37] text-[#6F4E37] hover:bg-[#6F4E37] hover:text-white font-bold text-xs shadow-sm transition-all flex items-center gap-1.5"
                    >
                      <Plus className="w-4 h-4" />
                      <span>Thêm Vào Giỏ</span>
                    </button>

                    {/* Nút Mua Ngay kế bên nút thêm vào giỏ */}
                    <button
                      onClick={() => {
                        handleQuickBuyNow(selectedProduct);
                        setSelectedProduct(null);
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
                <form onSubmit={handleDishReviewSubmit} className="p-3.5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-2.5">
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
                      placeholder={`Bạn thấy món này ngon hay dở? Nhận xét hương vị...`}
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
      )}
    </div>
  );
}
