"use client";

import React from "react";
import Link from "next/link";
import {
  AIAdminAnalyticsCard,
  AICartActionCard,
  AIChatMessage,
  AIClientAction,
  AIConversationSummary,
  AIDrinkCustomizationCard,
  AIInventoryReportCard,
  AIOrderStatusCard,
  AIPaymentCard,
  AIRecipeCard,
  SuggestedDrink,
} from "@/types";
import { getDistinctOrderItemImage } from "@/lib/orderImageHelper";
import {
  Bot,
  User,
  Sparkles,
  ShoppingBag,
  Check,
  ExternalLink,
  CupSoda,
  Package,
  QrCode,
  TrendingUp,
  AlertTriangle,
  MessageSquarePlus,
  Trash2,
  Clock,
  Wrench,
  Bookmark,
  ArrowRight,
} from "lucide-react";

// ============================================================================
// 1. SAFE MARKDOWN RENDERER
// ============================================================================
export const SafeMarkdownText: React.FC<{ content: string; onLinkClick?: () => void }> = ({
  content,
  onLinkClick,
}) => {
  const lines = (content || "").split("\n");

  const renderInline = (line: string, lineIdx: number) => {
    // Parse links [label](href) and bold **text** and inline code `code`
    const tokenRegex = /(\[[^\]]+\]\([^)]+\)|\*\*[^*]+\*\*|`[^`]+`)/g;
    const parts = line.split(tokenRegex);

    return parts.map((part, idx) => {
      if (!part) return null;

      const linkMatch = part.match(/^\[([^\]]+)\]\(([^)]+)\)$/);
      if (linkMatch) {
        const label = linkMatch[1];
        const href = linkMatch[2];
        const isSafeHref = href.startsWith("/") || href.startsWith("http://") || href.startsWith("https://");
        if (!isSafeHref) return <span key={`${lineIdx}_${idx}`}>{label}</span>;

        return (
          <Link
            key={`${lineIdx}_${idx}`}
            href={href}
            onClick={onLinkClick}
            className="text-[#9B6247] underline font-bold hover:text-[#30251F] inline-flex items-center gap-0.5 mx-0.5"
          >
            <span>{label}</span>
            <ExternalLink className="w-3 h-3 inline" />
          </Link>
        );
      }

      if (part.startsWith("**") && part.endsWith("**") && part.length > 4) {
        return (
          <strong key={`${lineIdx}_${idx}`} className="font-extrabold text-[#30251F]">
            {part.slice(2, -2)}
          </strong>
        );
      }

      if (part.startsWith("`") && part.endsWith("`") && part.length > 2) {
        return (
          <code
            key={`${lineIdx}_${idx}`}
            className="px-1.5 py-0.5 rounded-md bg-[#FFF9F3] border border-[#9B6247]/25 text-[#9B6247] font-mono text-[11px] font-bold"
          >
            {part.slice(1, -1)}
          </code>
        );
      }

      return <span key={`${lineIdx}_${idx}`}>{part}</span>;
    });
  };

  return (
    <div className="space-y-1.5 text-xs sm:text-sm leading-relaxed">
      {lines.map((line, idx) => {
        if (!line.trim()) return <div key={idx} className="h-1" />;
        return <div key={idx}>{renderInline(line, idx)}</div>;
      })}
    </div>
  );
};

// ============================================================================
// 2. AI THINKING INDICATOR
// ============================================================================
export const AIThinkingIndicator: React.FC<{ label?: string }> = ({
  label = "Milky AI Agent đang phân tích & gọi công cụ hệ thống...",
}) => (
  <div className="flex gap-3 justify-start animate-in fade-in duration-200">
    <div className="w-8 h-8 rounded-xl bg-[#9B6247] text-[#FFF9F3] flex items-center justify-center shrink-0 shadow-sm">
      <Bot className="w-4 h-4 animate-pulse" />
    </div>
    <div className="p-3.5 rounded-2xl bg-[#FFF9F3] border border-[#9B6247]/20 flex items-center gap-2.5 text-xs font-semibold text-[#30251F] shadow-xs">
      <div className="flex gap-1">
        <span className="w-2 h-2 rounded-full bg-[#9B6247] animate-bounce" />
        <span className="w-2 h-2 rounded-full bg-[#9B6247] animate-bounce [animation-delay:150ms]" />
        <span className="w-2 h-2 rounded-full bg-[#9B6247] animate-bounce [animation-delay:300ms]" />
      </div>
      <span>{label}</span>
    </div>
  </div>
);

// ============================================================================
// 3. PRODUCT RECOMMENDATION CARD
// ============================================================================
export const ProductRecommendation: React.FC<{
  drink: SuggestedDrink;
  index?: number;
  quickAddedId: number | null;
  onAddToCart: (drink: SuggestedDrink) => void;
  compact?: boolean;
}> = ({ drink, index = 0, quickAddedId, onAddToCart, compact = false }) => {
  const prodImg = getDistinctOrderItemImage(
    {
      id: drink.productId || index + 1,
      productId: drink.productId,
      productName: drink.name,
      imageUrl: drink.imageUrl,
    },
    index
  );

  const isAdded = quickAddedId === (drink.productId || 1);

  return (
    <div className="p-3.5 rounded-2xl bg-white border border-[#9B6247]/20 shadow-xs hover:shadow-md hover:border-[#9B6247]/50 transition-all flex flex-col justify-between gap-3">
      <div className="flex gap-3">
        <div className="w-16 h-16 sm:w-20 sm:h-20 rounded-xl overflow-hidden bg-[#FFF9F3] border border-[#9B6247]/15 shrink-0 relative">
          <img
            src={prodImg}
            alt={drink.name}
            className="w-full h-full object-cover"
            onError={(e) => {
              const target = e.currentTarget;
              if (!target.src.includes("tra-sua-tran-chau-den")) {
                target.src = "/images/products/tra-sua-tran-chau-den.png";
              }
            }}
          />
          {drink.isInStock === false && (
            <span className="absolute inset-0 bg-black/60 text-white text-[10px] font-bold flex items-center justify-center">
              Tạm hết
            </span>
          )}
        </div>

        <div className="flex-1 min-w-0 space-y-1">
          <div className="flex justify-between items-start gap-1.5">
            <h4 className="font-extrabold text-[#30251F] text-xs sm:text-sm line-clamp-1">
              {drink.name}
            </h4>
            <span className="text-xs sm:text-sm font-black text-[#9B6247] shrink-0">
              {drink.estimatedPrice.toLocaleString("vi-VN")}đ
            </span>
          </div>

          <p className="text-[11px] text-[#30251F]/70 line-clamp-2">{drink.description}</p>

          <div className="flex flex-wrap gap-1 pt-0.5">
            <span className="text-[10px] bg-[#FFF9F3] text-[#9B6247] border border-[#9B6247]/20 px-2 py-0.5 rounded-md font-bold">
              Đường: {drink.sugarRecommend}
            </span>
            <span className="text-[10px] bg-[#FFF9F3] text-[#9B6247] border border-[#9B6247]/20 px-2 py-0.5 rounded-md font-bold">
              Đá: {drink.iceRecommend}
            </span>
          </div>
        </div>
      </div>

      {!compact && drink.reason && (
        <p className="text-[11px] italic text-[#30251F]/75 bg-[#FFF9F3] px-2.5 py-1.5 rounded-xl border border-[#9B6247]/15">
          💡 {drink.reason}
        </p>
      )}

      <div className="flex items-center justify-between gap-2 pt-2 border-t border-[#FFF9F3]">
        <Link
          href={drink.slug === "diy" ? "/diy" : `/menu/${drink.slug || ""}`}
          className="text-[11px] font-bold text-[#30251F] hover:text-[#9B6247] px-2.5 py-1.5 rounded-xl border border-[#9B6247]/25 bg-[#FFF9F3] transition-colors flex items-center gap-1"
        >
          <span>Xem chi tiết</span>
          <ExternalLink className="w-3 h-3" />
        </Link>

        <button
          type="button"
          onClick={() => onAddToCart(drink)}
          className={`flex-1 py-1.5 px-3 rounded-xl text-[11px] font-bold flex items-center justify-center gap-1.5 transition-all cursor-pointer ${
            isAdded
              ? "bg-emerald-600 text-white"
              : "bg-[#9B6247] text-[#FFF9F3] hover:bg-[#30251F]"
          }`}
        >
          {isAdded ? (
            <>
              <Check className="w-3.5 h-3.5" />
              <span>Đã thêm vào giỏ</span>
            </>
          ) : (
            <>
              <ShoppingBag className="w-3.5 h-3.5" />
              <span>Thêm vào giỏ</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
};

// ============================================================================
// 4. RECIPE RECOMMENDATION CARD (DIY Studio vs Reference Formula)
// ============================================================================
export const RecipeRecommendation: React.FC<{
  recipe: AIRecipeCard;
  onAddRecipeToCart: (recipe: AIRecipeCard) => void;
  onSaveFavorite?: (recipe: AIRecipeCard) => void;
}> = ({ recipe, onAddRecipeToCart, onSaveFavorite }) => (
  <div className="p-4 rounded-2xl bg-[#FFF9F3] border-2 border-[#9B6247]/30 shadow-xs space-y-3">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div className="flex items-center gap-2">
        <div className="w-8 h-8 rounded-xl bg-[#9B6247] text-[#FFF9F3] flex items-center justify-center">
          <CupSoda className="w-4 h-4" />
        </div>
        <div>
          <span
            className={`text-[10px] font-extrabold px-2 py-0.5 rounded-full ${
              recipe.isReferenceFormula
                ? "bg-amber-100 text-amber-800 border border-amber-300"
                : "bg-emerald-100 text-emerald-800 border border-emerald-300"
            }`}
          >
            {recipe.isReferenceFormula
              ? "📖 Công Thức AI Sáng Tạo Tham Khảo"
              : "🛍️ Cấu Hình DIY Thực Tế Có Thể Đặt Mua"}
          </span>
          <h4 className="font-black text-sm text-[#30251F] mt-0.5">{recipe.recipeName}</h4>
        </div>
      </div>

      <div className="text-right">
        <span className="text-[10px] text-[#30251F]/60 block">Giá chuẩn hệ thống</span>
        <span className="text-base font-black text-[#9B6247]">
          {recipe.unitPrice.toLocaleString("vi-VN")}đ
        </span>
      </div>
    </div>

    <p className="text-xs text-[#30251F]/80">{recipe.description}</p>

    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-[11px]">
      <div className="p-2 rounded-xl bg-white border border-[#9B6247]/15">
        <span className="text-[#30251F]/60 block">Cốt trà</span>
        <span className="font-bold text-[#30251F]">{recipe.teaBaseName}</span>
      </div>
      <div className="p-2 rounded-xl bg-white border border-[#9B6247]/15">
        <span className="text-[#30251F]/60 block">Nền sữa</span>
        <span className="font-bold text-[#30251F]">{recipe.milkName}</span>
      </div>
      <div className="p-2 rounded-xl bg-white border border-[#9B6247]/15">
        <span className="text-[#30251F]/60 block">Size & Đá</span>
        <span className="font-bold text-[#30251F]">
          Size {recipe.size} • {recipe.iceLevel} đá
        </span>
      </div>
      <div className="p-2 rounded-xl bg-white border border-[#9B6247]/15">
        <span className="text-[#30251F]/60 block">Độ ngọt</span>
        <span className="font-bold text-[#9B6247]">{recipe.sugarLevel} đường</span>
      </div>
    </div>

    {recipe.toppings?.length > 0 && (
      <div className="flex flex-wrap gap-1.5 items-center text-[11px]">
        <span className="font-bold text-[#30251F]">Topping phối kèm:</span>
        {recipe.toppings.map((t, i) => (
          <span
            key={i}
            className="px-2 py-0.5 rounded-lg bg-white border border-[#9B6247]/25 text-[#9B6247] font-semibold"
          >
            {t.name} (+{t.price.toLocaleString("vi-VN")}đ)
          </span>
        ))}
      </div>
    )}

    {recipe.brewingSteps?.length > 0 && (
      <div className="p-3 rounded-xl bg-white border border-[#9B6247]/15 space-y-1 text-[11px] text-[#30251F]/80">
        <div className="font-extrabold text-[#30251F] text-xs mb-1">Quy trình pha chế chuẩn:</div>
        {recipe.brewingSteps.map((step, idx) => (
          <div key={idx}>• {step}</div>
        ))}
      </div>
    )}

    <div className="flex flex-wrap items-center justify-between gap-2 pt-1">
      <div className="flex items-center gap-2">
        <Link
          href={recipe.diyLink || "/diy"}
          className="px-3 py-1.5 rounded-xl bg-white border border-[#9B6247]/30 text-xs font-bold text-[#30251F] hover:bg-[#9B6247]/10 transition-colors flex items-center gap-1"
        >
          <CupSoda className="w-3.5 h-3.5 text-[#9B6247]" />
          <span>Mở trong DIY Studio</span>
        </Link>

        {onSaveFavorite && (
          <button
            type="button"
            onClick={() => onSaveFavorite(recipe)}
            className="px-3 py-1.5 rounded-xl bg-white border border-[#9B6247]/30 text-xs font-bold text-[#9B6247] hover:bg-[#9B6247]/10 transition-colors flex items-center gap-1 cursor-pointer"
          >
            <Bookmark className="w-3.5 h-3.5" />
            <span>Lưu công thức</span>
          </button>
        )}
      </div>

      {recipe.isPurchasable && (
        <button
          type="button"
          onClick={() => onAddRecipeToCart(recipe)}
          className="px-4 py-1.5 rounded-xl bg-[#9B6247] text-[#FFF9F3] text-xs font-bold hover:bg-[#30251F] transition-colors flex items-center gap-1.5 cursor-pointer"
        >
          <ShoppingBag className="w-3.5 h-3.5" />
          <span>Đưa cấu hình vào giỏ ({recipe.unitPrice.toLocaleString("vi-VN")}đ)</span>
        </button>
      )}
    </div>
  </div>
);

// ============================================================================
// 5. DRINK CUSTOMIZATION CARD
// ============================================================================
export const DrinkCustomizationCard: React.FC<{
  custom: AIDrinkCustomizationCard;
  onAddCustomToCart: (custom: AIDrinkCustomizationCard) => void;
}> = ({ custom, onAddCustomToCart }) => (
  <div className="p-3.5 rounded-2xl bg-white border border-[#9B6247]/30 shadow-xs space-y-2.5">
    <div className="flex items-center justify-between gap-2">
      <div className="flex items-center gap-2">
        <span className="px-2 py-0.5 rounded-md bg-[#FFF9F3] border border-[#9B6247]/25 text-[#9B6247] text-[10px] font-extrabold">
          {custom.isCustomDIY ? "Cấu Hình DIY" : "Tùy Chỉnh Món"}
        </span>
        <h4 className="font-extrabold text-xs sm:text-sm text-[#30251F]">
          {custom.quantity}x {custom.productName}
        </h4>
      </div>
      <span className="font-black text-sm text-[#9B6247]">
        {custom.totalPrice.toLocaleString("vi-VN")}đ
      </span>
    </div>

    <div className="flex flex-wrap gap-1.5 text-[11px]">
      <span className="px-2 py-0.5 rounded-lg bg-[#FFF9F3] text-[#30251F] font-semibold">
        Size {custom.size}
      </span>
      <span className="px-2 py-0.5 rounded-lg bg-[#FFF9F3] text-[#30251F] font-semibold">
        {custom.sugarLevel} đường
      </span>
      <span className="px-2 py-0.5 rounded-lg bg-[#FFF9F3] text-[#30251F] font-semibold">
        {custom.iceLevel} đá
      </span>
      {custom.selectedToppings?.map((t, idx) => (
        <span
          key={idx}
          className="px-2 py-0.5 rounded-lg bg-[#9B6247]/10 text-[#9B6247] font-bold"
        >
          + {t.name}
        </span>
      ))}
    </div>

    {custom.breakdown?.length > 0 && (
      <div className="text-[11px] text-[#30251F]/70 bg-[#FFF9F3] p-2 rounded-xl">
        Chi tiết giá: {custom.breakdown.join(" • ")}
      </div>
    )}

    <div className="flex justify-end">
      <button
        type="button"
        onClick={() => onAddCustomToCart(custom)}
        className="px-3.5 py-1.5 rounded-xl bg-[#9B6247] text-[#FFF9F3] text-xs font-bold hover:bg-[#30251F] transition-colors flex items-center gap-1.5 cursor-pointer"
      >
        <ShoppingBag className="w-3.5 h-3.5" />
        <span>Thêm cấu hình này vào giỏ</span>
      </button>
    </div>
  </div>
);

// ============================================================================
// 6. CART ACTION CARD
// ============================================================================
export const CartActionCard: React.FC<{ cartAction: AICartActionCard }> = ({ cartAction }) => (
  <div className="p-4 rounded-2xl bg-emerald-50/70 border border-emerald-300 shadow-xs space-y-3">
    <div className="flex items-center justify-between gap-2">
      <div className="flex items-center gap-2">
        <div className="w-8 h-8 rounded-xl bg-emerald-600 text-white flex items-center justify-center">
          <ShoppingBag className="w-4 h-4" />
        </div>
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-emerald-700">
            Shopping Agent • Giỏ Hàng Đã Đồng Bộ
          </span>
          <h4 className="font-extrabold text-xs sm:text-sm text-[#30251F]">{cartAction.summary}</h4>
        </div>
      </div>

      <div className="text-right shrink-0">
        <span className="text-[10px] text-[#30251F]/60 block">
          Tổng ({cartAction.totalItemsCount} món)
        </span>
        <span className="text-sm font-black text-emerald-700">
          {cartAction.cartSubtotal.toLocaleString("vi-VN")}đ
        </span>
      </div>
    </div>

    {cartAction.currentCart?.length > 0 && (
      <div className="space-y-1.5 max-h-40 overflow-y-auto pr-1">
        {cartAction.currentCart.map((item, idx) => (
          <div
            key={item.id || idx}
            className="p-2 rounded-xl bg-white border border-emerald-200/80 flex items-center justify-between gap-2 text-xs"
          >
            <div className="min-w-0">
              <span className="font-bold text-[#30251F]">
                {item.quantity}x {item.name}
              </span>
              <span className="text-[11px] text-[#30251F]/65 block">
                Size {item.size}
                {item.sugarLevel ? ` • ${item.sugarLevel} đường` : ""}
                {item.iceLevel ? ` • ${item.iceLevel} đá` : ""}
                {item.toppings?.length
                  ? ` • +${item.toppings.map((t) => t.name).join(", ")}`
                  : ""}
              </span>
            </div>
            <span className="font-extrabold text-[#9B6247] shrink-0">
              {item.totalPrice.toLocaleString("vi-VN")}đ
            </span>
          </div>
        ))}
      </div>
    )}

    <div className="flex items-center justify-end gap-2 pt-1">
      <Link
        href="/cart"
        className="px-3 py-1.5 rounded-xl bg-white border border-emerald-300 text-xs font-bold text-emerald-800 hover:bg-emerald-100 transition-colors"
      >
        Xem giỏ hàng
      </Link>
      {cartAction.totalItemsCount > 0 && (
        <Link
          href="/checkout"
          className="px-3.5 py-1.5 rounded-xl bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 transition-colors flex items-center gap-1"
        >
          <span>Thanh toán ngay</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      )}
    </div>
  </div>
);

// ============================================================================
// 7. ORDER STATUS CARD & PAYMENT CARD
// ============================================================================
export const OrderStatusCard: React.FC<{
  order: AIOrderStatusCard;
  payment?: AIPaymentCard;
}> = ({ order, payment }) => (
  <div className="p-4 rounded-2xl bg-white border border-[#9B6247]/30 shadow-xs space-y-3">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div className="flex items-center gap-2">
        <div className="w-8 h-8 rounded-xl bg-[#30251F] text-[#FFF9F3] flex items-center justify-center">
          <Package className="w-4 h-4" />
        </div>
        <div>
          <div className="flex items-center gap-1.5">
            <span className="font-black text-xs sm:text-sm text-[#30251F]">
              Đơn #{order.orderCode}
            </span>
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#FFF9F3] border border-[#9B6247]/30 text-[#9B6247]">
              {order.status}
            </span>
          </div>
          <span className="text-[11px] text-[#30251F]/65">
            Thanh toán: <strong>{order.paymentMethod}</strong> ({order.paymentStatus})
          </span>
        </div>
      </div>

      <span className="text-sm font-black text-[#9B6247]">
        {order.totalAmount.toLocaleString("vi-VN")}đ
      </span>
    </div>

    {order.itemSummaries?.length > 0 && (
      <div className="p-2.5 rounded-xl bg-[#FFF9F3] border border-[#9B6247]/15 text-[11px] space-y-0.5 text-[#30251F]/80">
        {order.itemSummaries.map((s, idx) => (
          <div key={idx}>• {s}</div>
        ))}
      </div>
    )}

    {payment && payment.qrCodeUrl && (
      <div className="p-3 rounded-xl bg-blue-50/60 border border-blue-200 flex flex-col sm:flex-row items-center gap-3">
        <img
          src={payment.qrCodeUrl}
          alt="VietQR SePay"
          className="w-28 h-28 rounded-xl bg-white p-1.5 border border-blue-200 shrink-0 object-contain"
        />
        <div className="space-y-1 text-xs text-[#30251F]">
          <div className="font-extrabold text-blue-900 flex items-center gap-1">
            <QrCode className="w-3.5 h-3.5" />
            <span>Mã VietQR SePay ({payment.bankName})</span>
          </div>
          <p className="text-[11px] text-[#30251F]/80">{payment.instructions}</p>
          {payment.transferContent && (
            <p className="text-[11px]">
              Nội dung CK:{" "}
              <code className="px-1.5 py-0.5 rounded bg-white border border-blue-200 font-bold text-blue-900">
                {payment.transferContent}
              </code>
            </p>
          )}
        </div>
      </div>
    )}

    <div className="flex justify-end">
      <Link
        href={order.orderUrl || `/orders/${order.orderId}`}
        className="px-3.5 py-1.5 rounded-xl bg-[#30251F] text-[#FFF9F3] text-xs font-bold hover:bg-[#9B6247] transition-colors flex items-center gap-1.5"
      >
        <span>Theo dõi & Thanh toán đơn hàng</span>
        <ExternalLink className="w-3.5 h-3.5" />
      </Link>
    </div>
  </div>
);

// ============================================================================
// 8. INVENTORY & ADMIN ANALYTICS CARDS
// ============================================================================
export const InventoryReportCard: React.FC<{ report: AIInventoryReportCard }> = ({ report }) => (
  <div className="p-4 rounded-2xl bg-amber-50/70 border border-amber-300 space-y-2.5 text-xs">
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2 font-extrabold text-amber-950">
        <AlertTriangle className="w-4 h-4 text-amber-600" />
        <span>Inventory Agent • Báo Cáo Tồn Kho Nguyên Liệu</span>
      </div>
      <span className="px-2 py-0.5 rounded-full bg-amber-200/80 text-amber-900 font-bold text-[10px]">
        {report.lowStockCount} sắp hết / {report.totalIngredients} nguyên liệu
      </span>
    </div>
    <p className="text-[11px] text-amber-900">{report.summaryNote}</p>
    {report.highlightedIngredients?.length > 0 && (
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-1.5">
        {report.highlightedIngredients.slice(0, 6).map((ing) => (
          <div
            key={ing.id}
            className="p-2 rounded-xl bg-white border border-amber-200 flex justify-between items-center text-[11px]"
          >
            <span className="font-bold text-[#30251F] truncate">{ing.name}</span>
            <span className="font-extrabold text-amber-800 shrink-0">
              {ing.currentStock.toLocaleString("vi-VN")} {ing.unit}
            </span>
          </div>
        ))}
      </div>
    )}
  </div>
);

export const AdminAnalyticsCard: React.FC<{ analytics: AIAdminAnalyticsCard }> = ({
  analytics,
}) => (
  <div className="p-4 rounded-2xl bg-[#FFF9F3] border-2 border-[#9B6247]/40 space-y-3 text-xs">
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2 font-extrabold text-[#30251F]">
        <TrendingUp className="w-4 h-4 text-[#9B6247]" />
        <span>Admin AI Agent • Phân Tích Kinh Doanh ({analytics.periodLabel})</span>
      </div>
      <Link
        href="/admin"
        className="text-[11px] font-bold text-[#9B6247] underline hover:text-[#30251F]"
      >
        Mở Dashboard
      </Link>
    </div>

    <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
      <div className="p-2.5 rounded-xl bg-white border border-[#9B6247]/20">
        <span className="text-[10px] text-[#30251F]/60 block">Tổng doanh thu</span>
        <span className="font-black text-sm text-emerald-700">
          {analytics.totalRevenue.toLocaleString("vi-VN")}đ
        </span>
      </div>
      <div className="p-2.5 rounded-xl bg-white border border-[#9B6247]/20">
        <span className="text-[10px] text-[#30251F]/60 block">Tổng đơn hàng</span>
        <span className="font-black text-sm text-[#30251F]">{analytics.totalOrders} đơn</span>
      </div>
      <div className="p-2.5 rounded-xl bg-white border border-[#9B6247]/20">
        <span className="text-[10px] text-[#30251F]/60 block">Khách hàng</span>
        <span className="font-black text-sm text-[#9B6247]">{analytics.totalCustomers}</span>
      </div>
      <div className="p-2.5 rounded-xl bg-white border border-[#9B6247]/20">
        <span className="text-[10px] text-[#30251F]/60 block">Kho sắp hết</span>
        <span className="font-black text-sm text-amber-700">{analytics.lowStockCount} món</span>
      </div>
    </div>
  </div>
);

// ============================================================================
// 9. SUGGESTED PROMPTS
// ============================================================================
export const SuggestedPrompts: React.FC<{
  prompts: string[];
  loading: boolean;
  onSelectPrompt: (prompt: string) => void;
}> = ({ prompts, loading, onSelectPrompt }) => (
  <div className="px-4 py-2.5 bg-[#FFF9F3] border-t border-[#9B6247]/15 flex items-center gap-2 overflow-x-auto no-scrollbar">
    <Sparkles className="w-3.5 h-3.5 text-[#9B6247] shrink-0" />
    {prompts.map((qp, idx) => (
      <button
        key={idx}
        type="button"
        disabled={loading}
        onClick={() => onSelectPrompt(qp)}
        className="text-[11px] font-semibold bg-white border border-[#9B6247]/25 text-[#30251F] px-3 py-1.5 rounded-full whitespace-nowrap hover:border-[#9B6247] hover:bg-[#9B6247] hover:text-[#FFF9F3] transition-all shrink-0 disabled:opacity-50 cursor-pointer"
      >
        {qp}
      </button>
    ))}
  </div>
);

// ============================================================================
// 10. CONVERSATION HISTORY SIDEBAR
// ============================================================================
export const ConversationHistory: React.FC<{
  conversations: AIConversationSummary[];
  activeSessionId?: number;
  onSelectSession: (sessionId: number) => void;
  onNewConversation: () => void;
  onDeleteSession: (sessionId: number) => void;
}> = ({
  conversations,
  activeSessionId,
  onSelectSession,
  onNewConversation,
  onDeleteSession,
}) => (
  <div className="bg-white rounded-3xl border border-[#9B6247]/20 shadow-sm p-4 flex flex-col h-full space-y-3">
    <div className="flex items-center justify-between gap-2 pb-2 border-b border-[#FFF9F3]">
      <div className="flex items-center gap-1.5 text-xs font-extrabold text-[#30251F]">
        <Clock className="w-4 h-4 text-[#9B6247]" />
        <span>Lịch Sử Hội Thoại</span>
      </div>
      <button
        type="button"
        onClick={onNewConversation}
        className="px-2.5 py-1.5 rounded-xl bg-[#9B6247] text-[#FFF9F3] text-[11px] font-bold hover:bg-[#30251F] transition-colors flex items-center gap-1 cursor-pointer"
      >
        <MessageSquarePlus className="w-3.5 h-3.5" />
        <span>Mới</span>
      </button>
    </div>

    <div className="flex-1 overflow-y-auto space-y-2 pr-1">
      {conversations.length === 0 ? (
        <p className="text-[11px] text-[#30251F]/60 text-center py-6">
          Chưa có lịch sử lưu trữ. Các cuộc trò chuyện của bạn sẽ tự động lưu tại đây.
        </p>
      ) : (
        conversations.map((conv) => {
          const isActive = conv.id === activeSessionId;
          return (
            <div
              key={conv.id}
              onClick={() => onSelectSession(conv.id)}
              className={`group p-2.5 rounded-2xl border transition-all cursor-pointer flex items-start justify-between gap-2 ${
                isActive
                  ? "bg-[#FFF9F3] border-[#9B6247] shadow-2xs"
                  : "bg-white border-[#9B6247]/15 hover:bg-[#FFF9F3]/60"
              }`}
            >
              <div className="min-w-0 flex-1">
                <h5 className="font-bold text-xs text-[#30251F] truncate">{conv.sessionTitle}</h5>
                {conv.lastMessagePreview && (
                  <p className="text-[10px] text-[#30251F]/60 truncate mt-0.5">
                    {conv.lastMessagePreview}
                  </p>
                )}
              </div>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  onDeleteSession(conv.id);
                }}
                title="Xóa hội thoại"
                className="opacity-0 group-hover:opacity-100 p-1 rounded-lg text-gray-400 hover:text-red-600 hover:bg-red-50 transition-all"
              >
                <Trash2 className="w-3.5 h-3.5" />
              </button>
            </div>
          );
        })
      )}
    </div>
  </div>
);

// ============================================================================
// 11. CHAT MESSAGE BUBBLE (Wraps text + all interactive Agent Cards)
// ============================================================================
export const ChatMessage: React.FC<{
  msg: AIChatMessage;
  quickAddedId: number | null;
  onAddDrinkToCart: (drink: SuggestedDrink) => void;
  onAddRecipeToCart: (recipe: AIRecipeCard) => void;
  onAddCustomToCart: (custom: AIDrinkCustomizationCard) => void;
  onSaveFavoriteRecipe?: (recipe: AIRecipeCard) => void;
  onLinkClick?: () => void;
  compact?: boolean;
}> = ({
  msg,
  quickAddedId,
  onAddDrinkToCart,
  onAddRecipeToCart,
  onAddCustomToCart,
  onSaveFavoriteRecipe,
  onLinkClick,
  compact = false,
}) => (
  <div className={`flex gap-3 ${msg.role === "user" ? "justify-end" : "justify-start"}`}>
    {msg.role === "assistant" && (
      <div className="w-8 h-8 rounded-xl bg-[#9B6247] text-[#FFF9F3] flex items-center justify-center shrink-0 mt-1 shadow-sm">
        <Bot className="w-4 h-4" />
      </div>
    )}

    <div className={`max-w-[90%] sm:max-w-[82%] space-y-3`}>
      <div
        className={`p-4 rounded-2xl ${
          msg.role === "user"
            ? "bg-[#30251F] text-[#FFF9F3] rounded-tr-none shadow-xs"
            : "bg-[#FFF9F3] text-[#30251F] border border-[#9B6247]/25 rounded-tl-none shadow-2xs"
        }`}
      >
        {msg.role === "assistant" && (
          <div className="flex flex-wrap items-center gap-1.5 mb-2">
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-[#9B6247]/15 text-[#9B6247]">
              ⚡ {msg.modelName || "MILKY AI BARISTA AGENT"}
            </span>
            {msg.executedTools && msg.executedTools.length > 0 && (
              <span className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-800 flex items-center gap-1">
                <Wrench className="w-2.5 h-2.5" />
                <span>
                  Tools: {msg.executedTools.map((t) => t.toolName).join(" ➔ ")}
                </span>
              </span>
            )}
          </div>
        )}

        <SafeMarkdownText content={msg.content} onLinkClick={onLinkClick} />
      </div>

      {/* Client Actions Confirmation Cards (ADD_TO_CART, OPEN_DIY, APPLY_COUPON) */}
      {msg.actions && msg.actions.length > 0 && (
        <div className="space-y-2">
          {msg.actions.map((act: AIClientAction, idx: number) => {
            if (act.type === "ADD_TO_CART" && act.payload) {
              const p = act.payload;
              return (
                <div
                  key={`act_${idx}`}
                  className="p-3.5 rounded-2xl bg-emerald-50 border border-emerald-300 flex flex-wrap items-center justify-between gap-2 text-xs shadow-2xs"
                >
                  <div className="flex items-center gap-2">
                    <div className="w-7 h-7 rounded-xl bg-emerald-600 text-white flex items-center justify-center shrink-0">
                      <Check className="w-4 h-4" />
                    </div>
                    <div>
                      <span className="font-extrabold text-emerald-950 block">
                        Đã thêm {p.quantity || 1}x {p.name} vào giỏ
                      </span>
                      <span className="text-[11px] text-emerald-800">
                        Size {p.size || "M"}
                        {p.sugarLevel ? ` • ${p.sugarLevel} đường` : ""}
                        {p.iceLevel ? ` • ${p.iceLevel} đá` : ""} —{" "}
                        <strong>{Number(p.totalPrice || p.unitPrice || 0).toLocaleString("vi-VN")}đ</strong>
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <Link
                      href="/cart"
                      onClick={onLinkClick}
                      className="px-2.5 py-1.5 rounded-xl bg-white border border-emerald-300 text-emerald-800 font-bold text-[11px] hover:bg-emerald-100"
                    >
                      Giỏ hàng
                    </Link>
                    <Link
                      href="/checkout"
                      onClick={onLinkClick}
                      className="px-3 py-1.5 rounded-xl bg-emerald-600 text-white font-bold text-[11px] hover:bg-emerald-700 flex items-center gap-1"
                    >
                      <span>Thanh toán</span>
                      <ArrowRight className="w-3 h-3" />
                    </Link>
                  </div>
                </div>
              );
            }

            if (act.type === "OPEN_DIY" && act.payload) {
              const p = act.payload;
              return (
                <div
                  key={`act_${idx}`}
                  className="p-3.5 rounded-2xl bg-[#FFF9F3] border border-[#9B6247]/40 flex flex-wrap items-center justify-between gap-2 text-xs"
                >
                  <div className="flex items-center gap-2">
                    <CupSoda className="w-5 h-5 text-[#9B6247] shrink-0" />
                    <div>
                      <span className="font-extrabold text-[#30251F] block">
                        Cấu hình DIY: {p.recipeName || "Trà Sữa Tự Phối"}
                      </span>
                      <span className="text-[11px] text-[#30251F]/75">
                        Size {p.size || "M"} • {p.sugarLevel || "50%"} đường •{" "}
                        <strong className="text-[#9B6247]">
                          {Number(p.unitPrice || 0).toLocaleString("vi-VN")}đ
                        </strong>
                      </span>
                    </div>
                  </div>
                  <Link
                    href={p.diyUrl || "/diy"}
                    onClick={onLinkClick}
                    className="px-3 py-1.5 rounded-xl bg-[#9B6247] text-[#FFF9F3] font-bold text-[11px] hover:bg-[#30251F] flex items-center gap-1"
                  >
                    <span>Mở DIY Studio</span>
                    <ExternalLink className="w-3 h-3" />
                  </Link>
                </div>
              );
            }

            if (act.type === "APPLY_COUPON" && act.payload) {
              const p = act.payload;
              return (
                <div
                  key={`act_${idx}`}
                  className="p-3.5 rounded-2xl bg-amber-50 border border-amber-300 flex flex-wrap items-center justify-between gap-2 text-xs"
                >
                  <div>
                    <span className="font-extrabold text-amber-950 block">
                      🎟️ Mã giảm giá hợp lệ: {p.code} (Giảm{" "}
                      {Number(p.discountAmount || 0).toLocaleString("vi-VN")}đ)
                    </span>
                    <span className="text-[11px] text-amber-800">{p.message}</span>
                  </div>
                  <Link
                    href="/checkout"
                    onClick={onLinkClick}
                    className="px-3 py-1.5 rounded-xl bg-[#30251F] text-[#FFF9F3] font-bold text-[11px] hover:bg-[#9B6247] flex items-center gap-1"
                  >
                    <span>Dùng tại Checkout</span>
                    <ArrowRight className="w-3 h-3" />
                  </Link>
                </div>
              );
            }

            return null;
          })}
        </div>
      )}

      {/* Cart Action Card */}
      {msg.cartAction && <CartActionCard cartAction={msg.cartAction} />}

      {/* DIY Recipe Recommendation Cards */}
      {msg.recipes && msg.recipes.length > 0 && (
        <div className="space-y-2.5">
          {msg.recipes.map((rec, i) => (
            <RecipeRecommendation
              key={i}
              recipe={rec}
              onAddRecipeToCart={onAddRecipeToCart}
              onSaveFavorite={onSaveFavoriteRecipe}
            />
          ))}
        </div>
      )}

      {/* Drink Customization Cards */}
      {msg.customizations && msg.customizations.length > 0 && (
        <div className="space-y-2">
          {msg.customizations.map((c, i) => (
            <DrinkCustomizationCard
              key={i}
              custom={c}
              onAddCustomToCart={onAddCustomToCart}
            />
          ))}
        </div>
      )}

      {/* Order Status & Payment Cards */}
      {msg.orders && msg.orders.length > 0 && (
        <div className="space-y-2">
          {msg.orders.map((ord, i) => (
            <OrderStatusCard
              key={ord.orderId || i}
              order={ord}
              payment={i === 0 ? msg.payment : undefined}
            />
          ))}
        </div>
      )}

      {/* Inventory Report Card */}
      {msg.inventoryReport && <InventoryReportCard report={msg.inventoryReport} />}

      {/* Admin Analytics Card */}
      {msg.adminAnalytics && <AdminAnalyticsCard analytics={msg.adminAnalytics} />}

      {/* Product Recommendations Grid */}
      {msg.suggestedDrinks && msg.suggestedDrinks.length > 0 && (
        <div className={`grid grid-cols-1 ${compact ? "" : "sm:grid-cols-2"} gap-2.5`}>
          {msg.suggestedDrinks.map((drink, idx) => (
            <ProductRecommendation
              key={idx}
              drink={drink}
              index={idx}
              quickAddedId={quickAddedId}
              onAddToCart={onAddDrinkToCart}
              compact={compact}
            />
          ))}
        </div>
      )}
    </div>

    {msg.role === "user" && (
      <div className="w-8 h-8 rounded-xl bg-[#FFF9F3] border border-[#9B6247]/30 text-[#9B6247] flex items-center justify-center shrink-0 mt-1">
        <User className="w-4 h-4" />
      </div>
    )}
  </div>
);
