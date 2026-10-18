"use client";

export const dynamic = "force-dynamic";

import React, { useEffect, useState, use } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { ProductDetail, Topping } from "@/types";
import { useCart } from "@/context/CartContext";
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
} from "lucide-react";

export default function ProductDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const resolvedParams = use(params);
  const { addItem } = useCart();

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
          setSelectedSpicy(`Cấp ${data.spicyLevel} - ${data.spicyLevel === 0 ? "Không cay" : data.spicyLevel <= 2 ? "Cay vừa" : "Cay nồng"}`);
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
    } else if (selectedPortion.includes("+35.000đ") || selectedPortion.includes("khổng lồ") || selectedPortion.includes("gia đình")) {
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

  const handleAddToCart = () => {
    const success = addItem({
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

    if (success) {
      setAdded(true);
      setTimeout(() => setAdded(false), 2000);
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

      {/* Main detail grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
        {/* Left: Product Image */}
        <div className="lg:col-span-5 space-y-4">
          <div className="relative aspect-square rounded-3xl overflow-hidden border border-[#EFE6DC] shadow-md bg-white">
            <img
              src={pImg}
              alt={pName}
              className="w-full h-full object-cover"
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
                  <Sparkles className="w-3.5 h-3.5" /> COMBO
                </span>
              )}
            </div>
          </div>

          <div className="bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] text-xs text-[#786C65] space-y-2">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-[#4E7D55]" />
              <span className="font-semibold text-[#2D2522]">Cam kết chuẩn vệ sinh an toàn thực phẩm:</span>
            </div>
            <p>100% nguyên liệu tươi sạch, chế biến nóng sốt ngay sau khi nhận order. Dầu chiên thay mới hàng ngày.</p>
          </div>
        </div>

        {/* Right: Customization Controls */}
        <div className="lg:col-span-7 space-y-6">
          <div>
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
                      className={`py-2 px-3 rounded-xl border text-xs font-bold transition-all ${
                        selectedSpicy === spicy
                          ? "border-red-500 bg-red-50 text-red-700 ring-1 ring-red-500"
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
                  <span>3. Nước Chấm Đi Kèm:</span>
                  <span className="text-[#6F4E37] font-semibold">{selectedSauce}</span>
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                  {sauceOptions.map((sauce) => (
                    <button
                      key={sauce}
                      type="button"
                      onClick={() => setSelectedSauce(sauce)}
                      className={`py-2 px-3 rounded-xl border text-xs font-bold transition-all ${
                        selectedSauce === sauce
                          ? "border-[#6F4E37] bg-[#6F4E37] text-white"
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

          {/* COMBO OPTIONS */}
          {pType === "COMBO" && (
            <div className="space-y-4 p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC]">
              <div className="flex items-center gap-2 text-xs font-bold text-[#6F4E37]">
                <Sparkles className="w-4 h-4 text-purple-600" />
                <span>Tùy Chỉnh Trọn Gói Combo Ăn + Uống:</span>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="text-[11px] font-bold text-[#2D2522] block mb-1">
                    Độ Ngọt Trà Sữa:
                  </label>
                  <select
                    value={selectedSugar}
                    onChange={(e) => setSelectedSugar(e.target.value)}
                    className="w-full bg-white border border-[#EFE6DC] rounded-xl px-3 py-2 text-xs font-semibold text-[#2D2522]"
                  >
                    <option value="30%">30% Đường (Ít ngọt)</option>
                    <option value="50%">50% Đường (Vừa phải)</option>
                    <option value="70%">70% Đường</option>
                    <option value="100%">100% Đường chuẩn</option>
                  </select>
                </div>

                <div>
                  <label className="text-[11px] font-bold text-[#2D2522] block mb-1">
                    Mức Cay Món Ăn:
                  </label>
                  <select
                    value={selectedSpicy}
                    onChange={(e) => setSelectedSpicy(e.target.value)}
                    className="w-full bg-white border border-[#EFE6DC] rounded-xl px-3 py-2 text-xs font-semibold text-[#2D2522]"
                  >
                    {spicyOptions.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </div>
              </div>
            </div>
          )}

          {/* 4. Toppings / Extra Additions */}
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

          {/* 5. Quantity & Add to Cart button */}
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-4 pt-2">
            <div className="flex items-center justify-between border border-[#EFE6DC] rounded-2xl bg-white p-1.5 shrink-0">
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

            <button
              type="button"
              onClick={handleAddToCart}
              disabled={added}
              className={`flex-1 py-4 px-6 rounded-2xl font-black text-sm flex items-center justify-center gap-2 shadow-lg transition-all ${
                added
                  ? "bg-[#4E7D55] text-white scale-98"
                  : "bg-[#6F4E37] text-white hover:bg-[#4A3324] hover:shadow-xl active:scale-98"
              }`}
            >
              {added ? (
                <>
                  <Check className="w-5 h-5 stroke-[3]" />
                  <span>Đã Thêm Vào Giỏ Hàng!</span>
                </>
              ) : (
                <>
                  <ShoppingBag className="w-5 h-5" />
                  <span>
                    Thêm Vào Giỏ • {subtotal.toLocaleString("vi-VN")}đ
                  </span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
