"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { DIYCatalog, DIYQuoteResponse } from "@/types";
import { useCart } from "@/context/CartContext";
import {
  Sparkles,
  ShoppingBag,
  Check,
  Plus,
  Minus,
  RefreshCw,
} from "lucide-react";

export default function DIYDrinkStudioPage() {
  const { addItem } = useCart();

  const [catalog, setCatalog] = useState<DIYCatalog | null>(null);
  const [loadingCatalog, setLoadingCatalog] = useState(true);

  // DIY Selections
  const [selectedTea, setSelectedTea] = useState<string>("black_tea");
  const [selectedMilk, setSelectedMilk] = useState<string>("fresh_milk");
  const [selectedSize, setSelectedSize] = useState<string>("M");
  const [selectedSugar, setSelectedSugar] = useState<string>("50%");
  const [selectedIce, setSelectedIce] = useState<string>("70%");
  const [selectedToppingIds, setSelectedToppingIds] = useState<number[]>([]);
  const [quantity, setQuantity] = useState<number>(1);

  // Backend quote response
  const [quote, setQuote] = useState<DIYQuoteResponse | null>(null);
  const [quoting, setQuoting] = useState(false);
  const [added, setAdded] = useState(false);

  // Load catalog on mount
  useEffect(() => {
    async function loadCatalog() {
      try {
        const data = await api.getDIYCatalog();
        setCatalog(data);
        if (data?.toppings?.length) {
          // Pre-select first topping for nice preview
          setSelectedToppingIds([data.toppings[0].Id ?? data.toppings[0].id]);
        }
      } catch (err) {
        console.error("Lỗi khi tải danh mục DIY:", err);
      } finally {
        setLoadingCatalog(false);
      }
    }
    loadCatalog();
  }, []);

  // Request backend verification and quote whenever choices change
  useEffect(() => {
    if (!catalog) return;

    let isMounted = true;
    async function fetchQuote() {
      setQuoting(true);
      try {
        const quoteRes = await api.quoteDIYDrink({
          teaBaseId: selectedTea,
          milkId: selectedMilk,
          size: selectedSize,
          sugarLevel: selectedSugar,
          iceLevel: selectedIce,
          toppingIds: selectedToppingIds,
          quantity: quantity,
        });

        if (isMounted) {
          setQuote(quoteRes);
        }
      } catch (err) {
        console.error("Lỗi báo giá DIY:", err);
      } finally {
        if (isMounted) setQuoting(false);
      }
    }

    const timer = setTimeout(fetchQuote, 200);
    return () => {
      isMounted = false;
      clearTimeout(timer);
    };
  }, [selectedTea, selectedMilk, selectedSize, selectedSugar, selectedIce, selectedToppingIds, quantity, catalog]);

  const toggleTopping = (toppingId: number) => {
    if (selectedToppingIds.includes(toppingId)) {
      setSelectedToppingIds(selectedToppingIds.filter((id) => id !== toppingId));
    } else {
      setSelectedToppingIds([...selectedToppingIds, toppingId]);
    }
  };

  const handleAddToCart = () => {
    if (!quote || !quote.isValid) return;

    const currentTea = catalog?.teaBases.find((t) => t.id === selectedTea);
    const currentMilk = catalog?.milks.find((m) => m.id === selectedMilk);
    const chosenToppingObjects = (catalog?.toppings || []).filter((t) =>
      selectedToppingIds.includes(t.Id ?? t.id)
    );

    const drinkName = `DIY: ${currentTea?.name || "Trà"} x ${currentMilk?.name || "Sữa"}`;

    const success = addItem({
      name: drinkName,
      imageUrl: "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
      size: selectedSize,
      sugarLevel: selectedSugar,
      iceLevel: selectedIce,
      unitPrice: quote.unitPrice,
      quantity: quantity,
      totalPrice: quote.totalPrice,
      isCustomDIY: true,
      customNotes: `Đường: ${selectedSugar}, Đá: ${selectedIce}. ${quote.breakdown.join(" | ")}`,
      toppings: chosenToppingObjects.map((t) => ({
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

  // Get current tea color for visual cup
  const currentTeaObj = catalog?.teaBases.find((t) => t.id === selectedTea);
  const teaColor = currentTeaObj?.colorHex || "#8B4513";

  if (loadingCatalog) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center space-y-4">
        <RefreshCw className="w-8 h-8 text-[#6F4E37] animate-spin mx-auto" />
        <p className="text-sm font-bold text-[#786C65]">Đang chuẩn bị phòng thí nghiệm DIY Drink Studio...</p>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* Header */}
      <div className="text-center max-w-3xl mx-auto space-y-3">
        <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[#F7D6D0] text-[#6F4E37] text-xs font-bold">
          <Sparkles className="w-3.5 h-3.5" />
          <span>MILKYVERSE INTERACTIVE STUDIO</span>
        </div>
        <h1 className="text-3xl sm:text-4xl lg:text-5xl font-black text-[#2D2522]">
          DIY Drink Studio — Tự Thiết Kế Đồ Uống
        </h1>
        <p className="text-sm sm:text-base text-[#786C65]">
          Pha trộn nền trà, chọn loại sữa, cân đối lượng đường - đá và phối topping độc bản. Bản xem trước ly trà sữa tương tác và giá được backend xác thực tức thì.
        </p>
      </div>

      {/* Main Studio Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-10 items-start">
        {/* Left Column: Visual Cup Preview */}
        <div className="lg:col-span-5 sticky top-28 bg-white p-8 rounded-3xl border border-[#EFE6DC] shadow-lg flex flex-col items-center justify-center space-y-6">
          <div className="text-center">
            <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
              Bản Xem Trước Trực Quan
            </span>
            <h3 className="font-extrabold text-[#2D2522] text-lg mt-0.5">
              {quote?.recipeName || "Ly Trà Sữa Của Bạn"}
            </h3>
          </div>

          {/* Realistic SVG & CSS Simulated Cup */}
          <div className="relative w-48 h-72 flex items-center justify-center">
            {/* Straw */}
            <div className="absolute -top-6 right-16 w-3 h-20 bg-[#D4A373] rounded-full rotate-12 shadow-sm z-30" />

            {/* Cup Container */}
            <div
              className={`relative w-44 rounded-b-3xl border-4 border-white/80 shadow-2xl overflow-hidden backdrop-blur-sm transition-all duration-500 ${
                selectedSize === "L" ? "h-64" : "h-56"
              }`}
              style={{
                background: "linear-gradient(to bottom, rgba(255,255,255,0.4), rgba(255,255,255,0.1))",
                boxShadow: "0 20px 40px -15px rgba(111, 78, 55, 0.25)",
              }}
            >
              {/* Cup Rim */}
              <div className="absolute top-0 left-0 right-0 h-3 bg-white/60 border-b border-white/80 z-20" />

              {/* Liquid Layer (Tea + Milk Gradient) */}
              <div
                className="absolute inset-x-0 bottom-0 top-6 transition-all duration-700"
                style={{
                  background: `linear-gradient(180deg, #FFFFFF 0%, #FAF0E6 20%, ${teaColor} 80%)`,
                  opacity: 0.9,
                }}
              >
                {/* Cheese foam or milk swirl on top */}
                <div className="absolute top-0 inset-x-0 h-8 bg-white/70 rounded-b-full filter blur-[2px]" />
              </div>

              {/* Floating Ice Cubes */}
              {selectedIce !== "0%" && (
                <div className="absolute top-10 inset-x-0 flex justify-center gap-2 z-10 opacity-70">
                  <div className="w-6 h-6 bg-white/80 rounded-md rotate-12 shadow-inner border border-white" />
                  <div className="w-5 h-5 bg-white/80 rounded-md -rotate-6 shadow-inner border border-white" />
                  {selectedIce === "100%" && (
                    <div className="w-6 h-6 bg-white/80 rounded-md rotate-45 shadow-inner border border-white" />
                  )}
                </div>
              )}

              {/* Sinking Boba / Toppings at the bottom */}
              {selectedToppingIds.length > 0 && (
                <div className="absolute bottom-2 inset-x-2 flex flex-wrap gap-1 justify-center z-10 transition-all duration-500">
                  {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map((dot) => (
                    <div
                      key={dot}
                      className="w-3.5 h-3.5 rounded-full bg-[#1A110B] shadow-inner border border-[#3A2416] animate-pulse"
                      style={{ animationDelay: `${dot * 150}ms` }}
                    />
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Drink Summary Pill */}
          <div className="w-full bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] text-xs space-y-1.5">
            <div className="flex justify-between text-[#786C65]">
              <span>Kích cỡ:</span>
              <span className="font-bold text-[#2D2522]">Size {selectedSize}</span>
            </div>
            <div className="flex justify-between text-[#786C65]">
              <span>Độ ngọt & Đá:</span>
              <span className="font-bold text-[#2D2522]">{selectedSugar} đường • {selectedIce} đá</span>
            </div>
            <div className="flex justify-between text-[#786C65]">
              <span>Topping:</span>
              <span className="font-bold text-[#6F4E37]">
                {selectedToppingIds.length} loại đã chọn
              </span>
            </div>
          </div>

          {/* Price quote breakdown */}
          <div className="w-full pt-4 border-t border-[#EFE6DC] space-y-3">
            <div className="flex items-baseline justify-between">
              <div>
                <span className="text-xs text-[#786C65] block">Đơn giá tự phối:</span>
                <span className="text-2xl font-black text-[#6F4E37]">
                  {quote ? `${quote.unitPrice.toLocaleString("vi-VN")}đ` : "..."}
                </span>
              </div>

              {/* Quantity */}
              <div className="flex items-center border border-[#EFE6DC] rounded-xl bg-white p-1">
                <button
                  type="button"
                  onClick={() => setQuantity(Math.max(1, quantity - 1))}
                  className="w-8 h-8 rounded-lg hover:bg-[#FAF7F2] flex items-center justify-center text-[#2D2522]"
                >
                  <Minus className="w-3.5 h-3.5" />
                </button>
                <span className="w-8 text-center font-bold text-sm text-[#2D2522]">
                  {quantity}
                </span>
                <button
                  type="button"
                  onClick={() => setQuantity(quantity + 1)}
                  className="w-8 h-8 rounded-lg hover:bg-[#FAF7F2] flex items-center justify-center text-[#2D2522]"
                >
                  <Plus className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>

            <button
              type="button"
              onClick={handleAddToCart}
              disabled={!quote?.isValid || quoting}
              className={`w-full py-3.5 px-4 rounded-2xl font-bold text-sm flex items-center justify-center gap-2 transition-all shadow-md ${
                added
                  ? "bg-[#4E7D55] text-white"
                  : "bg-[#6F4E37] text-white hover:bg-[#4A3324]"
              }`}
            >
              {added ? (
                <>
                  <Check className="w-4 h-4" />
                  <span>Đã thêm ly DIY vào giỏ hàng!</span>
                </>
              ) : (
                <>
                  <ShoppingBag className="w-4 h-4 text-[#F7D6D0]" />
                  <span>
                    Thêm Vào Giỏ • {quote ? `${quote.totalPrice.toLocaleString("vi-VN")}đ` : "..."}
                  </span>
                </>
              )}
            </button>
          </div>
        </div>

        {/* Right Column: Customization Wizard Steps */}
        <div className="lg:col-span-7 space-y-8 bg-white p-6 sm:p-8 rounded-3xl border border-[#EFE6DC] shadow-sm">
          {/* STEP 1: TEA BASE */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37] flex items-center gap-1.5">
                <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">1</span>
                Chọn Cốt Trà Nền
              </span>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {(catalog?.teaBases || []).map((tea) => {
                const isSelected = selectedTea === tea.id;
                return (
                  <button
                    key={tea.id}
                    type="button"
                    onClick={() => setSelectedTea(tea.id)}
                    className={`p-4 rounded-2xl border text-left transition-all ${
                      isSelected
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 shadow-sm ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                    }`}
                  >
                    <div className="flex items-center justify-between font-bold text-sm text-[#2D2522]">
                      <span className="flex items-center gap-2">
                        <span
                          className="w-3.5 h-3.5 rounded-full inline-block border border-black/20"
                          style={{ backgroundColor: tea.colorHex }}
                        />
                        {tea.name}
                      </span>
                      <span className="text-[#6F4E37] text-xs">
                        +{tea.additionalPrice.toLocaleString("vi-VN")}đ
                      </span>
                    </div>
                    <p className="text-xs text-[#786C65] mt-1 line-clamp-2 leading-relaxed">
                      {tea.description}
                    </p>
                  </button>
                );
              })}
            </div>
          </div>

          {/* STEP 2: MILK / ADD-IN */}
          <div className="space-y-3 pt-4 border-t border-[#EFE6DC]">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37] flex items-center gap-1.5">
                <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">2</span>
                Chọn Loại Sữa / Thành Phần Bổ Sung
              </span>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {(catalog?.milks || []).map((milk) => {
                const isSelected = selectedMilk === milk.id;
                return (
                  <button
                    key={milk.id}
                    type="button"
                    onClick={() => setSelectedMilk(milk.id)}
                    className={`p-4 rounded-2xl border text-left transition-all ${
                      isSelected
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 shadow-sm ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                    }`}
                  >
                    <div className="flex items-center justify-between font-bold text-sm text-[#2D2522]">
                      <span>{milk.name}</span>
                      <span className="text-[#6F4E37] text-xs">
                        +{milk.additionalPrice.toLocaleString("vi-VN")}đ
                      </span>
                    </div>
                    <p className="text-xs text-[#786C65] mt-1 line-clamp-2 leading-relaxed">
                      {milk.description}
                    </p>
                  </button>
                );
              })}
            </div>
          </div>

          {/* STEP 3: SIZE */}
          <div className="space-y-3 pt-4 border-t border-[#EFE6DC]">
            <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37] flex items-center gap-1.5">
              <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">3</span>
              Chọn Kích Cỡ Ly
            </span>
            <div className="grid grid-cols-2 gap-3">
              {(catalog?.sizes || []).map((s) => {
                const isSelected = selectedSize === s.id;
                return (
                  <button
                    key={s.id}
                    type="button"
                    onClick={() => setSelectedSize(s.id)}
                    className={`p-3.5 rounded-2xl border text-left transition-all ${
                      isSelected
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 shadow-sm ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2]"
                    }`}
                  >
                    <div className="flex items-center justify-between font-bold text-sm text-[#2D2522]">
                      <span>{s.name}</span>
                      {s.additionalPrice > 0 && (
                        <span className="text-[#6F4E37] text-xs">
                          +{s.additionalPrice.toLocaleString("vi-VN")}đ
                        </span>
                      )}
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          {/* STEP 4 & 5: SUGAR & ICE */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 pt-4 border-t border-[#EFE6DC]">
            <div className="space-y-3">
              <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37] flex items-center gap-1.5">
                <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">4</span>
                Mức Đường
              </span>
              <div className="grid grid-cols-3 gap-2">
                {["0%", "30%", "50%", "70%", "100%"].map((sugar) => (
                  <button
                    key={sugar}
                    type="button"
                    onClick={() => setSelectedSugar(sugar)}
                    className={`py-2 rounded-xl text-xs font-bold border transition-all ${
                      selectedSugar === sugar
                        ? "border-[#6F4E37] bg-[#6F4E37] text-white shadow-sm"
                        : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                    }`}
                  >
                    {sugar}
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-3">
              <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37] flex items-center gap-1.5">
                <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">5</span>
                Mức Đá
              </span>
              <div className="grid grid-cols-3 gap-2">
                {["0%", "30%", "50%", "70%", "100%"].map((ice) => (
                  <button
                    key={ice}
                    type="button"
                    onClick={() => setSelectedIce(ice)}
                    className={`py-2 rounded-xl text-xs font-bold border transition-all ${
                      selectedIce === ice
                        ? "border-[#6F4E37] bg-[#6F4E37] text-white shadow-sm"
                        : "border-[#EFE6DC] bg-white text-[#2D2522] hover:bg-[#FAF7F2]"
                    }`}
                  >
                    {ice}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* STEP 6: TOPPINGS */}
          <div className="space-y-3 pt-4 border-t border-[#EFE6DC]">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37] flex items-center gap-1.5">
                <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">6</span>
                Chọn Topping Thượng Hạng
              </span>
              <span className="text-xs text-[#786C65]">Có thể chọn nhiều</span>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {(catalog?.toppings || []).map((t) => {
                const tId = t.Id ?? t.id;
                const tName = t.Name ?? t.name;
                const tPrice = t.Price ?? t.price;
                const isChecked = selectedToppingIds.includes(tId);

                return (
                  <button
                    key={tId}
                    type="button"
                    onClick={() => toggleTopping(tId)}
                    className={`p-3.5 rounded-2xl border text-left transition-all flex items-center justify-between ${
                      isChecked
                        ? "border-[#6F4E37] bg-[#F7D6D0]/30 text-[#6F4E37] ring-1 ring-[#6F4E37]"
                        : "border-[#EFE6DC] bg-white hover:bg-[#FAF7F2] text-[#2D2522]"
                    }`}
                  >
                    <div className="flex items-center gap-2.5">
                      <div
                        className={`w-4 h-4 rounded-md border flex items-center justify-center ${
                          isChecked ? "bg-[#6F4E37] border-[#6F4E37] text-white" : "border-[#786C65]"
                        }`}
                      >
                        {isChecked && <Check className="w-3 h-3" />}
                      </div>
                      <span className="text-xs font-bold">{tName}</span>
                    </div>
                    <span className="text-xs font-bold text-[#6F4E37]">
                      +{tPrice.toLocaleString("vi-VN")}đ
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
