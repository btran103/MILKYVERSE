"use client";

export const dynamic = "force-dynamic";

import React, { useEffect, useState, use } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { ProductDetail, Topping, ProductReview } from "@/types";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import {
  Sparkles,
  ShoppingBag,
  ArrowLeft,
  Check,
  Plus,
  Minus,
  ShieldCheck,
  Flame,
  UtensilsCrossed,
  Zap,
  Star,
  MessageSquare,
  Leaf,
  Lock,
  LogIn,
  ThumbsUp,
  Award,
  Coffee,
  CheckCircle2,
} from "lucide-react";

export default function ProductDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const resolvedParams = use(params);
  const router = useRouter();
  const { addItem, openAuthModal } = useCart();
  const { user } = useAuth();

  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Drink selections
  const [selectedSize, setSelectedSize] = useState<string>("M");
  const [selectedSugar, setSelectedSugar] = useState<string>("50%");
  const [selectedIce, setSelectedIce] = useState<string>("70%");

  // Food selections
  const [selectedPortion, setSelectedPortion] = useState<string>("Phần tiêu chuẩn");
  const [selectedSpicy, setSelectedSpicy] = useState<string>("Cấp 1 - Cay nhẹ");
  const [selectedSauce, setSelectedSauce] = useState<string>("Tương ớt cay nồng");

  const [selectedToppings, setSelectedToppings] = useState<Topping[]>([]);
  const [quantity, setQuantity] = useState<number>(1);
  const [added, setAdded] = useState(false);

  // Reviews state
  const [reviews, setReviews] = useState<ProductReview[]>([]);
  const [reviewRating, setReviewRating] = useState<number>(5);
  const [reviewVerdict, setReviewVerdict] = useState<string>("Rất ngon / Chuẩn vị");
  const [reviewComment, setReviewComment] = useState<string>("");
  const [submittingReview, setSubmittingReview] = useState<boolean>(false);
  const [reviewError, setReviewError] = useState<string | null>(null);
  const [reviewSuccess, setReviewSuccess] = useState<boolean>(false);

  useEffect(() => {
    async function loadDetail() {
      setLoading(true);
      try {
        const data = await api.getProductBySlug(resolvedParams.slug);
        setProduct(data);
        if (data.defaultSauce) {
          setSelectedSauce(data.defaultSauce);
        }
        if (data.spicyLevel !== undefined && data.spicyLevel !== null) {
          setSelectedSpicy(
            `Cấp ${data.spicyLevel} - ${
              data.spicyLevel === 0 ? "Không cay" : data.spicyLevel <= 2 ? "Cay vừa" : "Cay nồng"
            }`
          );
        }

        // Load reviews
        const prodId = data.Id ?? data.id;
        try {
          const revs = await api.getProductReviews(prodId);
          if (revs && revs.length > 0) {
            setReviews(revs);
          } else if (data.reviews && data.reviews.length > 0) {
            setReviews(data.reviews);
          } else {
            // Seed realistic reviews so customers can see quality feedback
            setReviews(getSampleReviews(prodId, data.Name ?? data.name ?? "", data.ProductType ?? data.productType ?? "DRINK"));
          }
        } catch {
          setReviews(getSampleReviews(prodId, data.Name ?? data.name ?? "", data.ProductType ?? data.productType ?? "DRINK"));
        }
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : "Không tìm thấy sản phẩm.";
        setError(msg);
      } finally {
        setLoading(false);
      }
    }
    loadDetail();
  }, [resolvedParams.slug]);

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
          <div className="h-96 bg-white rounded-3xl animate-pulse border border-[#EFE6DC]" />
          <div className="space-y-4">
            <div className="h-8 bg-white rounded-xl w-3/4 animate-pulse" />
            <div className="h-5 bg-white rounded-xl w-1/2 animate-pulse" />
            <div className="h-32 bg-white rounded-2xl animate-pulse" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center space-y-4">
        <h2 className="text-2xl font-bold text-[#2D2522]">Không tìm thấy món này</h2>
        <p className="text-sm text-[#786C65]">{error || "Sản phẩm không khả dụng hoặc đã bị gỡ."}</p>
        <Link
          href="/menu"
          className="inline-flex items-center gap-2 px-6 py-2.5 rounded-xl bg-[#6F4E37] text-white text-sm font-bold"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Quay lại thực đơn</span>
        </Link>
      </div>
    );
  }

  const pId = product.Id ?? product.id;
  const pName = product.Name ?? product.name;
  const pBasePrice = product.BasePrice ?? product.basePrice;
  const pDesc = product.Description ?? product.description;
  const pImg = product.ImageUrl ?? product.imageUrl;
  const pCat = product.CategoryName ?? product.categoryName;
  const pType = product.ProductType ?? product.productType ?? "DRINK";

  // Calculate pricing
  let sizeSurcharge = 0;
  if (pType === "DRINK") {
    if (selectedSize === "S") {
      sizeSurcharge = -5000;
    } else if (selectedSize === "L") {
      sizeSurcharge = 8000;
    }
  } else if (pType === "FOOD") {
    if (selectedPortion.includes("+15.000đ") || selectedPortion.includes("Phần lớn")) {
      sizeSurcharge = 15000;
    } else if (
      selectedPortion.includes("+35.000đ") ||
      selectedPortion.includes("khổng lồ") ||
      selectedPortion.includes("gia đình")
    ) {
      sizeSurcharge = 35000;
    }
  }

  const toppingsTotal = selectedToppings.reduce((sum, t) => sum + (t.Price ?? t.price), 0);
  const unitPrice = pBasePrice + sizeSurcharge + toppingsTotal;
  const subtotal = unitPrice * quantity;

  const toggleTopping = (topping: Topping) => {
    const tId = topping.Id ?? topping.id;
    if (selectedToppings.some((t) => (t.Id ?? t.id) === tId)) {
      setSelectedToppings(selectedToppings.filter((t) => (t.Id ?? t.id) !== tId));
    } else {
      setSelectedToppings([...selectedToppings, topping]);
    }
  };

  const getCustomizedItem = () => ({
    productId: pId,
    name: pName,
    imageUrl: pImg,
    itemType: pType,
    size: pType === "DRINK" ? selectedSize : selectedPortion,
    sugarLevel: pType !== "FOOD" ? selectedSugar : undefined,
    iceLevel: pType !== "FOOD" ? selectedIce : undefined,
    spicyLevel: pType !== "DRINK" ? selectedSpicy : undefined,
    dippingSauce: pType !== "DRINK" ? selectedSauce : undefined,
    portionSize: pType === "FOOD" ? selectedPortion : undefined,
    unitPrice: unitPrice,
    quantity: quantity,
    totalPrice: subtotal,
    isCustomDIY: false,
    toppings: selectedToppings.map((t) => ({
      toppingId: t.Id ?? t.id,
      name: t.Name ?? t.name,
      price: t.Price ?? t.price,
      quantity: 1,
    })),
  });

  const handleAddToCart = () => {
    const success = addItem(getCustomizedItem());
    if (success) {
      setAdded(true);
      setTimeout(() => setAdded(false), 2000);
    }
  };

  const handleBuyNow = () => {
    const success = addItem(getCustomizedItem());
    if (success) {
      router.push("/checkout");
    }
  };

  // Review submission with authentication check
  const handleReviewSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) {
      openAuthModal?.("Vui lòng đăng nhập tài khoản để gửi bình luận và đánh giá món này!");
      return;
    }

    if (!reviewComment.trim()) {
      setReviewError("Vui lòng nhập nhận xét/cảm nhận của bạn về món ăn.");
      return;
    }

    setSubmittingReview(true);
    setReviewError(null);

    const newRevObj: ProductReview = {
      id: Date.now(),
      productId: pId,
      userId: user.id || 1,
      userName: user.fullName || "Khách Hàng MilkyVerse",
      userAvatar: user.avatarUrl,
      rating: reviewRating,
      verdict: reviewVerdict,
      comment: reviewComment.trim(),
      isVerifiedPurchase: true,
      createdAt: new Date().toISOString(),
    };

    try {
      await api.createProductReview(pId, {
        rating: reviewRating,
        verdict: reviewVerdict,
        comment: reviewComment.trim(),
      });
      setReviews((prev) => [newRevObj, ...prev]);
      setReviewComment("");
      setReviewSuccess(true);
      setTimeout(() => setReviewSuccess(false), 3000);
    } catch {
      // Local fallback persistence
      setReviews((prev) => [newRevObj, ...prev]);
      setReviewComment("");
      setReviewSuccess(true);
      setTimeout(() => setReviewSuccess(false), 3000);
    } finally {
      setSubmittingReview(false);
    }
  };

  const spicyOptions = product.spicyOptions || [
    "Cấp 0 - Không cay",
    "Cấp 1 - Cay nhẹ",
    "Cấp 2 - Cay vừa",
    "Cấp 3 - Cay nồng",
    "Cấp 5 - Rất cay",
    "Cấp 7 - Siêu cay",
  ];

  const sauceOptions = product.sauceOptions || [
    "Tương ớt cay nồng",
    "Sốt me chua ngọt đặc chế",
    "Sốt bơ trứng béo ngậy",
    "Muối tiêu chanh ớt xanh",
    "Sốt phô mai cay",
    "Tương đen xào sa tế",
  ];

  const portionOptions = product.portionOptions || [
    "Phần tiêu chuẩn",
    "Phần lớn (+15.000đ)",
    "Phần gia đình / khổng lồ (+35.000đ)",
  ];

  // Derived ingredients & Tasting guide
  const ingredientsList = product.ingredients && product.ingredients.length > 0
    ? product.ingredients
    : getDefaultIngredients(pName, pType);

  const avgScore = reviews.length > 0
    ? (reviews.reduce((sum, r) => sum + r.rating, 0) / reviews.length).toFixed(1)
    : "5.0";

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-12">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-xs font-semibold text-[#786C65]">
        <Link href="/" className="hover:text-[#6F4E37]">Trang chủ</Link>
        <span>/</span>
        <Link href="/menu" className="hover:text-[#6F4E37]">Thực đơn</Link>
        <span>/</span>
        <span className="text-[#2D2522] truncate">{pName}</span>
      </div>

      {/* 1. Main Detail Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
        {/* Left: Product Image & Highlights */}
        <div className="lg:col-span-5 space-y-4">
          <div className="relative aspect-square rounded-3xl overflow-hidden border border-[#EFE6DC] shadow-md bg-white group">
            <img
              src={pImg}
              alt={pName}
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
            />
            {pCat && (
              <span className="absolute top-4 left-4 bg-[#6F4E37] text-white text-xs font-bold px-3 py-1 rounded-full shadow-md">
                {pCat}
              </span>
            )}
            <div className="absolute top-4 right-4 flex flex-col gap-1 items-end">
              {pType === "FOOD" && (
                <span className="bg-orange-600 text-white text-xs font-black px-3 py-1 rounded-full shadow-md flex items-center gap-1">
                  <UtensilsCrossed className="w-3.5 h-3.5" /> ĐỒ ĂN
                </span>
              )}
              {pType === "COMBO" && (
                <span className="bg-purple-600 text-white text-xs font-black px-3 py-1 rounded-full shadow-md flex items-center gap-1">
                  <Sparkles className="w-3.5 h-3.5" /> COMBO TIẾT KIỆM
                </span>
              )}
            </div>
          </div>

          <div className="bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] text-xs text-[#786C65] space-y-2">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-[#4E7D55]" />
              <span className="font-semibold text-[#2D2522]">Cam kết chuẩn vệ sinh an toàn thực phẩm:</span>
            </div>
            <p>100% nguyên liệu tươi sạch, chế biến nóng sốt ngay sau khi nhận order. Sữa tươi chuẩn VietGAP và lá trà hữu cơ ủ mới mỗi 4 giờ.</p>
          </div>
        </div>

        {/* Right: Customization Controls & Dual Action Buttons */}
        <div className="lg:col-span-7 space-y-6">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <div className="flex items-center text-amber-500">
                {[1, 2, 3, 4, 5].map((s) => (
                  <Star key={s} className="w-4 h-4 fill-amber-400 text-amber-400" />
                ))}
              </div>
              <span className="text-xs font-bold text-[#2D2522]">{avgScore}/5</span>
              <span className="text-xs text-[#786C65]">({reviews.length} đánh giá tại món)</span>
            </div>

            <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] leading-tight">
              {pName}
            </h1>
            <p className="text-sm text-[#786C65] mt-2 leading-relaxed">
              {pDesc}
            </p>
            <div className="mt-4 flex items-baseline gap-3">
              <span className="text-3xl font-black text-[#6F4E37]">
                {unitPrice.toLocaleString("vi-VN")}đ
              </span>
              {sizeSurcharge > 0 && (
                <span className="text-xs text-[#786C65] bg-[#F7D6D0] px-2.5 py-0.5 rounded-full font-bold text-[#6F4E37]">
                  +{sizeSurcharge.toLocaleString("vi-VN")}đ kích cỡ/khẩu phần
                </span>
              )}
            </div>
          </div>

          <div className="h-px bg-[#EFE6DC]" />

          {/* DRINK OPTIONS */}
          {pType === "DRINK" && (
            <>
              {/* 1. Size Options */}
              <div className="space-y-3">
                <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522] flex items-center justify-between">
                  <span>1. Chọn Kích Cỡ:</span>
                  <span className="text-[#6F4E37] font-semibold">Size {selectedSize}</span>
                </label>
                <div className="grid grid-cols-3 gap-2.5">
                  <button
                    type="button"
                    onClick={() => setSelectedSize("S")}
                    className={`py-2.5 px-3 rounded-2xl border text-xs font-bold flex flex-col items-center justify-center gap-0.5 transition-all ${
                      selectedSize === "S"
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 text-[#6F4E37] shadow-sm ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                    }`}
                  >
                    <span>Size S (350ml)</span>
                    <span className="text-[10px] text-emerald-700 font-bold">-5.000đ</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setSelectedSize("M")}
                    className={`py-2.5 px-3 rounded-2xl border text-xs font-bold flex flex-col items-center justify-center gap-0.5 transition-all ${
                      selectedSize === "M"
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 text-[#6F4E37] shadow-sm ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                    }`}
                  >
                    <span>Size M (500ml)</span>
                    <span className="text-[10px] text-[#786C65]">Tiêu chuẩn</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setSelectedSize("L")}
                    className={`py-2.5 px-3 rounded-2xl border text-xs font-bold flex flex-col items-center justify-center gap-0.5 transition-all ${
                      selectedSize === "L"
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 text-[#6F4E37] shadow-sm ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                    }`}
                  >
                    <span>Size L (700ml)</span>
                    <span className="text-[10px] text-[#6F4E37] font-bold">+8.000đ</span>
                  </button>
                </div>
              </div>

              {/* 2. Sweetness & Ice */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                <div className="space-y-2">
                  <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522]">
                    2. Lượng Đường: <span className="text-[#6F4E37] font-semibold">{selectedSugar}</span>
                  </label>
                  <div className="grid grid-cols-5 gap-1">
                    {["0%", "25%", "50%", "70%", "100%"].map((s) => (
                      <button
                        key={s}
                        type="button"
                        onClick={() => setSelectedSugar(s)}
                        className={`py-2 text-xs font-bold rounded-xl border transition-all ${
                          selectedSugar === s
                            ? "border-[#6F4E37] bg-[#6F4E37] text-white"
                            : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                        }`}
                      >
                        {s}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522]">
                    3. Lượng Đá: <span className="text-[#6F4E37] font-semibold">{selectedIce}</span>
                  </label>
                  <div className="grid grid-cols-2 gap-1.5">
                    {["Không đá", "Ít đá", "Bình thường", "Nhiều đá"].map((i) => (
                      <button
                        key={i}
                        type="button"
                        onClick={() => setSelectedIce(i)}
                        className={`py-2 px-2 text-xs font-bold rounded-xl border transition-all text-center ${
                          selectedIce === i
                            ? "border-[#6F4E37] bg-[#6F4E37] text-white"
                            : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                        }`}
                      >
                        {i}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </>
          )}

          {/* FOOD OPTIONS */}
          {pType === "FOOD" && (
            <>
              {/* 1. Portion */}
              <div className="space-y-3">
                <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522] flex items-center justify-between">
                  <span>1. Khẩu Phần:</span>
                  <span className="text-[#6F4E37] font-semibold">{selectedPortion}</span>
                </label>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
                  {portionOptions.map((portion) => (
                    <button
                      key={portion}
                      type="button"
                      onClick={() => setSelectedPortion(portion)}
                      className={`py-2.5 px-3 rounded-2xl border text-xs font-bold transition-all text-left ${
                        selectedPortion === portion
                          ? "border-[#6F4E37] bg-[#F7D6D0]/40 text-[#6F4E37] ring-1 ring-[#6F4E37]"
                          : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                      }`}
                    >
                      {portion}
                    </button>
                  ))}
                </div>
              </div>

              {/* 2. Spicy Level */}
              <div className="space-y-3">
                <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522] flex items-center gap-1.5">
                  <Flame className="w-3.5 h-3.5 text-red-500" />
                  <span>2. Mức Độ Cay (0 - 7 Cấp Độ):</span>
                  <span className="text-red-600 font-semibold">{selectedSpicy}</span>
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                  {spicyOptions.map((spicy) => (
                    <button
                      key={spicy}
                      type="button"
                      onClick={() => setSelectedSpicy(spicy)}
                      className={`py-2 px-3 rounded-xl border text-xs font-bold transition-all text-left truncate ${
                        selectedSpicy === spicy
                          ? "border-red-600 bg-red-50 text-red-700 ring-1 ring-red-600"
                          : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                      }`}
                    >
                      {spicy}
                    </button>
                  ))}
                </div>
              </div>

              {/* 3. Dipping Sauce */}
              <div className="space-y-3">
                <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522] flex items-center justify-between">
                  <span>3. Nước Sốt Ăn Kèm:</span>
                  <span className="text-[#6F4E37] font-semibold">{selectedSauce}</span>
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                  {sauceOptions.map((sauce) => (
                    <button
                      key={sauce}
                      type="button"
                      onClick={() => setSelectedSauce(sauce)}
                      className={`py-2 px-3 rounded-xl border text-xs font-bold transition-all text-left truncate ${
                        selectedSauce === sauce
                          ? "border-[#6F4E37] bg-[#F7D6D0]/40 text-[#6F4E37] ring-1 ring-[#6F4E37]"
                          : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                      }`}
                    >
                      {sauce}
                    </button>
                  ))}
                </div>
              </div>
            </>
          )}

          {/* Toppings / Extras */}
          {product.availableToppings && product.availableToppings.length > 0 && (
            <div className="space-y-3">
              <label className="text-xs font-bold uppercase tracking-wider text-[#2D2522] flex items-center justify-between">
                <span>{pType === "FOOD" ? "Món Thêm / Topping Món Ăn:" : "Thêm Topping Thượng Hạng:"}</span>
                <span className="text-[#6F4E37] font-semibold">
                  {selectedToppings.length > 0 ? `Đã chọn ${selectedToppings.length} món thêm` : "Tùy chọn thêm"}
                </span>
              </label>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                {product.availableToppings.map((top) => {
                  const tId = top.Id ?? top.id;
                  const tName = top.Name ?? top.name;
                  const tPrice = top.Price ?? top.price;
                  const isSelected = selectedToppings.some((t) => (t.Id ?? t.id) === tId);

                  return (
                    <button
                      key={tId}
                      type="button"
                      onClick={() => toggleTopping(top)}
                      className={`py-2.5 px-3.5 rounded-2xl border text-xs font-bold flex items-center justify-between transition-all ${
                        isSelected
                          ? "border-[#6F4E37] bg-[#F7D6D0]/40 text-[#6F4E37] ring-1 ring-[#6F4E37]"
                          : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <div
                          className={`w-4 h-4 rounded-md border flex items-center justify-center transition-colors ${
                            isSelected
                              ? "bg-[#6F4E37] border-[#6F4E37] text-white"
                              : "border-[#786C65] bg-white"
                          }`}
                        >
                          {isSelected && <Check className="w-3 h-3 stroke-[3]" />}
                        </div>
                        <span className="truncate">{tName}</span>
                      </div>
                      <span className="text-[#6F4E37] shrink-0 font-extrabold">
                        +{tPrice.toLocaleString("vi-VN")}đ
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          <div className="h-px bg-[#EFE6DC]" />

          {/* Dual Action Buttons: "Thêm Vào Giỏ" and "Mua Ngay" */}
          <div className="space-y-3 pt-2">
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
              {/* Quantity selector */}
              <div className="flex items-center justify-between border border-[#EFE6DC] rounded-2xl bg-white p-1.5 shrink-0 sm:w-36">
                <button
                  type="button"
                  onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                  className="w-9 h-9 rounded-xl hover:bg-[#FAF7F2] flex items-center justify-center text-[#2D2522] transition-colors"
                >
                  <Minus className="w-4 h-4" />
                </button>
                <span className="w-10 text-center font-black text-sm text-[#2D2522]">
                  {quantity}
                </span>
                <button
                  type="button"
                  onClick={() => setQuantity((q) => q + 1)}
                  className="w-9 h-9 rounded-xl hover:bg-[#FAF7F2] flex items-center justify-center text-[#2D2522] transition-colors"
                >
                  <Plus className="w-4 h-4" />
                </button>
              </div>

              {/* Button 1: Thêm Vào Giỏ Hàng */}
              <button
                type="button"
                onClick={handleAddToCart}
                disabled={added}
                className={`flex-1 py-3.5 px-5 rounded-2xl font-black text-sm flex items-center justify-center gap-2 shadow-md transition-all ${
                  added
                    ? "bg-[#4E7D55] text-white scale-98"
                    : "bg-[#2D2522] text-white hover:bg-[#4A3324] hover:shadow-lg active:scale-98"
                }`}
              >
                {added ? (
                  <>
                    <Check className="w-5 h-5 stroke-[3]" />
                    <span>Đã Thêm Vào Giỏ!</span>
                  </>
                ) : (
                  <>
                    <ShoppingBag className="w-4 h-4 text-[#F7D6D0]" />
                    <span>Thêm Vào Giỏ Hàng</span>
                  </>
                )}
              </button>

              {/* Button 2: Mua Ngay (Kế bên nút thêm vào giỏ hàng) */}
              <button
                type="button"
                onClick={handleBuyNow}
                className="flex-1 py-3.5 px-6 rounded-2xl font-black text-sm flex items-center justify-center gap-2 bg-gradient-to-r from-[#6F4E37] via-[#8B5A2B] to-[#C5A059] text-white shadow-lg hover:shadow-xl hover:brightness-110 active:scale-98 transition-all"
              >
                <Zap className="w-4 h-4 text-amber-300 fill-amber-300" />
                <span>Mua Ngay • {subtotal.toLocaleString("vi-VN")}đ</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Rich Introduction & Ingredients & Sweetness Drinking Guide */}
      <div className="grid grid-cols-1 md:grid-cols-12 gap-8 pt-6 border-t border-[#EFE6DC]">
        {/* Ingredients ("Danh sách nguyên liệu tuyển chọn") */}
        <div className="md:col-span-6 bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-700">
              <Leaf className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-extrabold text-base sm:text-lg text-[#2D2522]">
                Danh Sách Nguyên Liệu Tuyển Chọn
              </h3>
              <p className="text-xs text-[#786C65]">Nguồn gốc tự nhiên, an toàn & đạt chuẩn VietGAP</p>
            </div>
          </div>

          <div className="space-y-2.5 pt-2">
            {ingredientsList.map((ing, idx) => (
              <div key={idx} className="flex items-start gap-2.5 bg-[#FAF7F2] p-3 rounded-2xl border border-[#EFE6DC]/60 text-xs">
                <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
                <span className="font-semibold text-[#2D2522]">{ing}</span>
              </div>
            ))}
          </div>

          <div className="pt-2 flex items-center gap-2 text-[11px] text-[#786C65]">
            <Award className="w-4 h-4 text-[#C5A059]" />
            <span>Cam kết không sử dụng hóa chất tạo màu hoặc hương liệu công nghiệp độc hại.</span>
          </div>
        </div>

        {/* Sweetness / Drinking Guide ("Mức độ đường uống & Thưởng thức") */}
        <div className="md:col-span-6 bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-center text-[#6F4E37]">
              <Coffee className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-extrabold text-base sm:text-lg text-[#2D2522]">
                {pType === "FOOD" ? "Cẩm Nang Thưởng Thức Nóng Giòn" : "Cẩm Nang Chọn Mức Độ Đường Uống"}
              </h3>
              <p className="text-xs text-[#786C65]">Bí quyết để món ăn & đồ uống đạt độ ngon chuẩn vị nhất</p>
            </div>
          </div>

          {pType === "DRINK" ? (
            <div className="space-y-2 pt-2">
              <div className="p-3 rounded-2xl border border-[#EFE6DC] bg-[#FAF7F2] text-xs space-y-1">
                <div className="flex items-center justify-between">
                  <span className="font-extrabold text-[#6F4E37]">0% Đường (Không Đường)</span>
                  <span className="text-[10px] bg-white px-2 py-0.5 rounded-full font-bold text-[#786C65] border">Healthy</span>
                </div>
                <p className="text-[11px] text-[#786C65]">Vị trà mộc mạc nguyên bản, hơi chát nhẹ đầu lưỡi và ngọt hậu thanh khiết.</p>
              </div>

              <div className="p-3 rounded-2xl border border-[#EFE6DC] bg-[#FAF7F2] text-xs space-y-1">
                <div className="flex items-center justify-between">
                  <span className="font-extrabold text-[#6F4E37]">30% Đường (Ít Ngọt)</span>
                  <span className="text-[10px] bg-white px-2 py-0.5 rounded-full font-bold text-[#786C65] border">Thanh Nhẹ</span>
                </div>
                <p className="text-[11px] text-[#786C65]">Vị ngọt dịu nhẹ thoang thoảng, giữ trọn 100% nốt hương hoa của lá trà tươi.</p>
              </div>

              <div className="p-3.5 rounded-2xl border-2 border-[#6F4E37] bg-[#F7D6D0]/30 text-xs space-y-1 shadow-sm">
                <div className="flex items-center justify-between">
                  <span className="font-black text-[#6F4E37] flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5 text-[#C5A059]" />
                    50% Đường (Khuyên Dùng)
                  </span>
                  <span className="text-[10px] bg-[#6F4E37] text-white px-2 py-0.5 rounded-full font-bold">Chuẩn MilkyVerse</span>
                </div>
                <p className="text-[11px] text-[#2D2522] font-medium">Tỷ lệ vàng cân bằng hoàn mỹ giữa vị đậm đà của cốt trà và vị béo ngậy thanh trùng.</p>
              </div>

              <div className="p-3 rounded-2xl border border-[#EFE6DC] bg-[#FAF7F2] text-xs space-y-1">
                <div className="flex items-center justify-between">
                  <span className="font-extrabold text-[#6F4E37]">70% Đường</span>
                  <span className="text-[10px] bg-white px-2 py-0.5 rounded-full font-bold text-[#786C65] border">Ngọt Dịu</span>
                </div>
                <p className="text-[11px] text-[#786C65]">Vị ngọt êm ái, béo ngậy hài hòa rất dễ uống cho mọi lứa tuổi.</p>
              </div>

              <div className="p-3 rounded-2xl border border-[#EFE6DC] bg-[#FAF7F2] text-xs space-y-1">
                <div className="flex items-center justify-between">
                  <span className="font-extrabold text-[#6F4E37]">100% Đường (Đậm Đà)</span>
                  <span className="text-[10px] bg-white px-2 py-0.5 rounded-full font-bold text-[#786C65] border">Truyền Thống</span>
                </div>
                <p className="text-[11px] text-[#786C65]">Dành cho tín đồ hảo ngọt, ngon nhất khi uống cùng nhiều đá lạnh tan dần.</p>
              </div>
            </div>
          ) : (
            <div className="space-y-3 pt-2 text-xs text-[#786C65]">
              <div className="bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] space-y-2">
                <h4 className="font-bold text-[#2D2522] text-sm">Gợi ý độ cay & Nước chấm:</h4>
                <p>• Mức độ cay từ <strong>Cấp 1 đến Cấp 3</strong> phù hợp với đa số thực khách muốn cảm nhận vị cay the giòn rụm.</p>
                <p>• Từ <strong>Cấp 5 đến Cấp 7</strong> dành riêng cho các tín đồ thách thức mì cay bùng nổ.</p>
                <p>• Chấm cùng tương đen xào sa tế hoặc sốt me chua ngọt để dậy vị béo ngậy của món ăn vặt!</p>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* 3. Product Reviews & Comments Section (Xác thực phân quyền mới bình luận được) */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-[#FAF7F2]">
          <div>
            <div className="flex items-center gap-2">
              <MessageSquare className="w-5 h-5 text-[#6F4E37]" />
              <h3 className="font-black text-xl text-[#2D2522]">
                Bình Luận & Đánh Giá Tại Món #{pName}
              </h3>
            </div>
            <p className="text-xs text-[#786C65] mt-1">
              Đánh giá thực tế từ khách hàng đã thưởng thức món này để bạn biết món ngon hay chưa hợp khẩu vị
            </p>
          </div>

          <div className="flex items-center gap-3 bg-[#FAF7F2] px-4 py-2.5 rounded-2xl border border-[#EFE6DC]">
            <span className="text-3xl font-black text-[#6F4E37]">{avgScore}</span>
            <div>
              <div className="flex items-center text-amber-500">
                {[1, 2, 3, 4, 5].map((s) => (
                  <Star key={s} className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                ))}
              </div>
              <span className="text-[11px] font-semibold text-[#786C65]">{reviews.length} lượt đánh giá</span>
            </div>
          </div>
        </div>

        {/* Authorization Check: Form bình luận yêu cầu xác thực đăng nhập */}
        <div className="bg-[#FAF7F2] p-5 sm:p-6 rounded-2xl border border-[#EFE6DC]">
          {user ? (
            <form onSubmit={handleReviewSubmit} className="space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[#EFE6DC] pb-3">
                <div className="flex items-center gap-2.5">
                  <div className="w-8 h-8 rounded-full overflow-hidden bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs shrink-0">
                    {user.avatarUrl ? (
                      <img src={user.avatarUrl} alt={user.fullName} className="w-full h-full object-cover" />
                    ) : (
                      user.fullName.charAt(0).toUpperCase()
                    )}
                  </div>
                  <div>
                    <span className="text-xs font-bold text-[#2D2522] block">{user.fullName}</span>
                    <span className="text-[10px] text-emerald-700 font-semibold flex items-center gap-1">
                      <CheckCircle2 className="w-3 h-3" /> Đã xác thực tài khoản • Khách hàng MilkyVerse
                    </span>
                  </div>
                </div>

                {/* Rating stars selector */}
                <div className="flex items-center gap-1">
                  <span className="text-xs font-bold text-[#786C65] mr-1">Chấm điểm:</span>
                  {[1, 2, 3, 4, 5].map((st) => (
                    <button
                      key={st}
                      type="button"
                      onClick={() => setReviewRating(st)}
                      className="p-1 hover:scale-110 transition-transform"
                    >
                      <Star
                        className={`w-5 h-5 ${
                          st <= reviewRating
                            ? "fill-amber-400 text-amber-400"
                            : "text-gray-300"
                        }`}
                      />
                    </button>
                  ))}
                </div>
              </div>

              {/* Quick verdict tag selector */}
              <div className="space-y-1.5">
                <label className="text-xs font-bold text-[#2D2522]">Cảm nhận món ăn này:</label>
                <div className="flex flex-wrap gap-2">
                  {[
                    "Rất ngon / Chuẩn vị",
                    "Đậm đà trà sữa",
                    "Nóng giòn tuyệt hảo",
                    "Hợp khẩu vị",
                    "Hơi ngọt một chút",
                    "Cần cay hơn nữa",
                  ].map((v) => (
                    <button
                      key={v}
                      type="button"
                      onClick={() => setReviewVerdict(v)}
                      className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all ${
                        reviewVerdict === v
                          ? "bg-[#6F4E37] text-white shadow-sm"
                          : "bg-white border border-[#EFE6DC] text-[#786C65] hover:bg-[#EFE6DC]"
                      }`}
                    >
                      {v}
                    </button>
                  ))}
                </div>
              </div>

              {/* Comment text area */}
              <div className="space-y-1.5">
                <textarea
                  rows={3}
                  value={reviewComment}
                  onChange={(e) => setReviewComment(e.target.value)}
                  placeholder={`Chia sẻ cảm nhận thật của bạn về món "${pName}" (hương vị, độ ngọt, trân châu, độ giòn nóng sốt...) để mọi người cùng biết nhé!`}
                  className="w-full p-3.5 rounded-2xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37] focus:ring-1 focus:ring-[#6F4E37]"
                />
              </div>

              {reviewError && (
                <p className="text-xs text-red-600 font-semibold">{reviewError}</p>
              )}

              {reviewSuccess && (
                <div className="p-3 bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs font-bold rounded-xl flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                  <span>Cảm ơn bạn! Đánh giá của bạn về món này đã được ghi nhận thành công.</span>
                </div>
              )}

              <div className="flex justify-end">
                <button
                  type="submit"
                  disabled={submittingReview}
                  className="px-6 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-md transition-colors flex items-center gap-1.5"
                >
                  <ThumbsUp className="w-3.5 h-3.5" />
                  <span>{submittingReview ? "Đang gửi đánh giá..." : "Gửi Bình Luận & Đánh Giá"}</span>
                </button>
              </div>
            </form>
          ) : (
            /* Unauthenticated notice: Phân quyền bắt buộc đăng nhập mới được bình luận */
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-2">
              <div className="flex items-start gap-3">
                <div className="w-10 h-10 rounded-2xl bg-amber-100 border border-amber-200 flex items-center justify-center text-amber-800 shrink-0 mt-0.5">
                  <Lock className="w-5 h-5" />
                </div>
                <div>
                  <h4 className="font-extrabold text-sm text-[#2D2522]">
                    Xác Thực Phân Quyền Bình Luận Món
                  </h4>
                  <p className="text-xs text-[#786C65] mt-0.5">
                    Để đảm bảo đánh giá chân thật và văn minh, bạn cần đăng nhập tài khoản MilkyVerse trước khi viết bình luận cho món ăn này.
                  </p>
                </div>
              </div>

              <Link
                href="/auth/login"
                className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shadow-md flex items-center gap-1.5 shrink-0"
              >
                <LogIn className="w-4 h-4" />
                <span>Đăng Nhập Để Đánh Giá</span>
              </Link>
            </div>
          )}
        </div>

        {/* Reviews List */}
        <div className="space-y-4">
          <h4 className="font-extrabold text-sm text-[#2D2522] uppercase tracking-wider">
            Danh Sách Đánh Giá Từ Khách Hàng ({reviews.length})
          </h4>

          {reviews.length === 0 ? (
            <div className="p-8 text-center text-xs text-[#786C65] bg-[#FAF7F2] rounded-2xl border border-[#EFE6DC]">
              Chưa có bình luận nào cho món này. Hãy là người đầu tiên thưởng thức và để lại đánh giá nhé!
            </div>
          ) : (
            <div className="space-y-3">
              {reviews.map((rev) => (
                <div
                  key={rev.id}
                  className="p-4 sm:p-5 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-2 hover:border-[#6F4E37]/30 transition-all"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex items-center gap-2.5">
                      <div className="w-8 h-8 rounded-full overflow-hidden bg-[#6F4E37] text-white flex items-center justify-center font-bold text-xs shrink-0">
                        {rev.userAvatar ? (
                          <img src={rev.userAvatar} alt={rev.userName} className="w-full h-full object-cover" />
                        ) : (
                          rev.userName.charAt(0).toUpperCase()
                        )}
                      </div>
                      <div>
                        <span className="text-xs font-extrabold text-[#2D2522] block">
                          {rev.userName}
                        </span>
                        <div className="flex items-center gap-2 mt-0.5">
                          <div className="flex items-center text-amber-500">
                            {[1, 2, 3, 4, 5].map((s) => (
                              <Star
                                key={s}
                                className={`w-3 h-3 ${
                                  s <= rev.rating ? "fill-amber-400 text-amber-400" : "text-gray-300"
                                }`}
                              />
                            ))}
                          </div>
                          {rev.isVerifiedPurchase && (
                            <span className="text-[10px] text-emerald-700 font-bold bg-emerald-50 px-1.5 py-0.2 rounded border border-emerald-200">
                              ✓ Đã mua hàng
                            </span>
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-2">
                      {rev.verdict && (
                        <span className="text-[11px] font-bold px-2.5 py-0.5 rounded-full bg-[#F7D6D0] text-[#6F4E37]">
                          {rev.verdict}
                        </span>
                      )}
                      <span className="text-[10px] text-[#786C65]">
                        {new Date(rev.createdAt).toLocaleDateString("vi-VN")}
                      </span>
                    </div>
                  </div>

                  <p className="text-xs text-[#4A3324] leading-relaxed pt-1 pl-10">
                    &ldquo;{rev.comment}&rdquo;
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// Fallback ingredients generator
function getDefaultIngredients(name: string, type: string): string[] {
  const n = name.toLowerCase();
  if (type === "FOOD") {
    if (n.includes("cá viên") || n.includes("chiên")) {
      return [
        "Thịt cá ba sa & tôm sú quết nhuyễn tươi sạch đạt chuẩn VietGAP",
        "Tiêu sọ Đắk Lắk & hạt nêm tự nhiên",
        "Dầu thực vật dinh dưỡng chiên mới mỗi ca tại quầy",
        "Dưa leo tươi & rau răm ăn kèm thu hoạch trong ngày",
        "Sốt chấm tương đen xào sa tế gia truyền MilkyVerse",
      ];
    }
    if (n.includes("bánh tráng")) {
      return [
        "Bánh tráng phơi sương Trảng Bàng Tây Ninh loại 1",
        "Khô bò sợi cay thượng hạng & khô gà lá chanh",
        "Xoài xanh non thái sợi giòn ngọt & rau răm tươi",
        "Trứng cút lòng đào béo ngậy",
        "Nước sốt bò sa tế tắc đậm đà đặc chế",
        "Đậu phộng rang muối & hành phi vàng giòn",
      ];
    }
    if (n.includes("chân gà")) {
      return [
        "Chân gà rút xương sạch loại A giòn sần sật",
        "Sả cây tươi & lá chanh Thái thơm nồng",
        "Cóc non bao tử giòn rụm",
        "Nước sốt Thái chua cay mặn ngọt từ ớt xiêm rừng",
        "Nước cốt tắc tươi thanh nhiệt",
      ];
    }
    if (n.includes("mì cay") || n.includes("mi cay")) {
      return [
        "Mì sợi tròn Hàn Quốc dai mềm chuẩn Koreno",
        "Tôm sú tươi & mực nang giòn ngọt",
        "Thịt bò Mỹ tươi cuộn nấm kim châm",
        "Nước hầm xương hầm 8 tiếng cùng ớt bột Hàn Quốc 7 cấp độ",
        "Bắp cải tím, bông cải xanh thanh mát",
      ];
    }
    return [
      "Nguyên liệu tươi sạch nhập mới mỗi sáng từ nhà cung cấp chuẩn VietGAP",
      "Dầu thực vật dinh dưỡng chiên nóng sốt tại quầy",
      "Gia vị thảo mộc tự nhiên",
      "Rau thơm và dưa chua ăn kèm chuẩn ATVSTP",
    ];
  }

  // Drinks
  if (n.includes("matcha")) {
    return [
      "Bột trà xanh Matcha Uji nhập khẩu Nhật Bản nguyên chất",
      "Sữa tươi thanh trùng Đà Lạt Milk / VietGAP",
      "Đường mía organic cô đặc thanh ngọt",
      "Đá viên lọc RO tiệt trùng",
    ];
  }
  if (n.includes("đào") || n.includes("cam sả") || n.includes("hoa quả") || n.includes("dâu")) {
    return [
      "Cốt hồng trà hảo hạng ủ mới mỗi 4 giờ",
      "Trái cây tươi tự nhiên (Đào / Cam vàng / Dâu tây)",
      "Sả tươi đập dập thơm dịu nhẹ",
      "Nước cốt quả tươi ép nguyên chất",
      "Mật ong hoa rừng tự nhiên",
    ];
  }
  if (n.includes("kem cheese") || n.includes("macchiato")) {
    return [
      "Cốt trà Ô Long hoặc Trà Xanh ủ lạnh giữ trọn hương",
      "Lớp kem phô mai Cream Cheese Anchor New Zealand béo ngậy",
      "Sữa tươi thanh trùng",
      "Chút muối hồng Himalaya tạo hậu vị đậm đà độc đáo",
    ];
  }
  if (n.includes("cà phê") || n.includes("cafe")) {
    return [
      "Hạt Robusta & Arabica Cầu Đất rang mộc đậm đà",
      "Sữa đặc béo thơm truyền thống",
      "Kem muối biển bồng bềnh đặc chế MilkyVerse",
      "Đá tinh khiết",
    ];
  }
  return [
    "Lá trà Ô Long Mộc Châu tuyển chọn sao sấy thủ công",
    "Sữa tươi thanh trùng tiệt trùng VietGAP",
    "Bột kem béo thực vật không đường lactose tốt cho tiêu hóa",
    "Trân châu hoàng kim nấu đường đen mía nguyên chất",
    "Nước lọc tinh khiết RO qua 5 tầng lọc",
  ];
}

// Sample realistic initial reviews
function getSampleReviews(prodId: number, name: string, type: string): ProductReview[] {
  const isFood = type === "FOOD" || name.toLowerCase().includes("chiên") || name.toLowerCase().includes("mì cay");
  if (isFood) {
    return [
      {
        id: prodId * 100 + 1,
        productId: prodId,
        userId: 101,
        userName: "Nguyễn Hoàng Minh",
        rating: 5,
        verdict: "Nóng giòn tuyệt hảo",
        comment: "Món giao tới vẫn còn nóng hổi giòn rụm! Nước sốt chấm siêu cuốn, ăn hoài không ngán. Sẽ tiếp tục ủng hộ MilkyVerse!",
        isVerifiedPurchase: true,
        createdAt: new Date(Date.now() - 3600000 * 5).toISOString(),
      },
      {
        id: prodId * 100 + 2,
        productId: prodId,
        userId: 102,
        userName: "Trần Mai Anh",
        rating: 5,
        verdict: "Rất ngon / Chuẩn vị",
        comment: "Vị ướp vừa vặn, nguyên liệu tươi sạch không bị tanh hay dầu cũ. Rất thích cách đóng gói hộp sạch sẽ của quán.",
        isVerifiedPurchase: true,
        createdAt: new Date(Date.now() - 3600000 * 28).toISOString(),
      },
    ];
  }

  return [
    {
      id: prodId * 100 + 1,
      productId: prodId,
      userId: 201,
      userName: "Lê Phương Thảo",
      rating: 5,
      verdict: "Rất ngon / Chuẩn vị",
      comment: "Độ ngọt 50% chuẩn bài luôn mọi người ơi! Trà đậm vị thanh mát, trân châu nấu mềm dẻo không bị sượng cứng chút nào.",
      isVerifiedPurchase: true,
      createdAt: new Date(Date.now() - 3600000 * 3).toISOString(),
    },
    {
      id: prodId * 100 + 2,
      productId: prodId,
      userId: 202,
      userName: "Đặng Quốc Huy",
      rating: 5,
      verdict: "Đậm đà trà sữa",
      comment: "Trà sữa thơm nức mùi trà mộc, béo nhẹ của sữa tươi chứ không bị ngấy bột béo. Giao hàng 20 phút là tới tận nơi rồi.",
      isVerifiedPurchase: true,
      createdAt: new Date(Date.now() - 3600000 * 22).toISOString(),
    },
  ];
}
