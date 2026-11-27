"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { api } from "@/lib/api";
import {
  AIChatMessage,
  AIDrinkCustomizationCard,
  AIRecipeCard,
  AIStatusResponse,
  SuggestedDrink,
} from "@/types";
import { useCart } from "@/context/CartContext";
import {
  AIThinkingIndicator,
  ChatMessage,
  SuggestedPrompts,
} from "@/components/ai/AgentCards";
import {
  Bot,
  Sparkles,
  Send,
  X,
  ExternalLink,
  ChevronDown,
  Minimize2,
} from "lucide-react";

const WIDGET_PROMPTS = [
  "💸 Có 40k uống trà sữa ít ngọt có trân châu",
  "🍵 Thêm 2 ly trà sữa matcha (1 ít đường, 1 bình thường)",
  "🧪 Tự pha Ô long sữa yến mạch Size L 30% đường",
  "🍯 Đang bị ho / đau họng uống gì ấm?",
  "🛒 Xem giỏ hàng của tôi",
  "📦 Kiểm tra đơn hàng của tôi",
];

export const AIBaristaWidget: React.FC = () => {
  const pathname = usePathname();
  const { items: cartItems, addItem, syncCartFromAgent } = useCart();

  const [isOpen, setIsOpen] = useState(false);
  const [aiStatus, setAiStatus] = useState<AIStatusResponse | null>(null);
  const [messages, setMessages] = useState<AIChatMessage[]>([
    {
      id: "welcome",
      role: "assistant",
      content:
        "Xin chào! Mình là **MILKY AI BARISTA** ✨ — AI Agent của MilkyVerse. Mình có thể tìm đồ uống theo ngân sách, tự phối công thức DIY, thêm món vào giỏ hàng hoặc kiểm tra đơn giúp bạn ngay tại đây!",
      isAiModelLive: true,
      modelName: "MILKY AI AGENT",
      timestamp: new Date().toISOString(),
    },
  ]);

  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<number | undefined>(undefined);
  const [quickAddedId, setQuickAddedId] = useState<number | null>(null);
  const [showTooltip, setShowTooltip] = useState(true);
  const [toastText, setToastText] = useState<string | null>(null);
  const [prompts, setPrompts] = useState<string[]>(WIDGET_PROMPTS);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const isDedicatedPage = pathname === "/ai-barista";

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
      setShowTooltip(false);
      // Clean up any legacy client-side API key from localStorage
      if (typeof window !== "undefined") {
        localStorage.removeItem("milky_ai_api_key");
        localStorage.removeItem("milky_ai_provider");
      }
      api
        .getAIStatus()
        .then((st) => setAiStatus(st))
        .catch(() => {});
    }
  }, [messages, loading, isOpen]);

  const handleSend = async (messageText?: string) => {
    const textToSend = messageText || inputMessage;
    if (!textToSend.trim() || loading) return;

    const userMsg: AIChatMessage = {
      id: "u_" + Date.now(),
      role: "user",
      content: textToSend.trim(),
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMsg]);
    setInputMessage("");
    setLoading(true);

    try {
      const res = await api.sendAIChat(textToSend.trim(), sessionId, cartItems);
      setSessionId(res.sessionId);

      if (res.cartAction && Array.isArray(res.cartAction.currentCart)) {
        syncCartFromAgent(res.cartAction.currentCart);
      }

      if (res.actions && res.actions.length > 0) {
        const addedNames: string[] = [];
        for (const act of res.actions) {
          if (act.type === "ADD_TO_CART" && act.payload) {
            const p = act.payload;
            addedNames.push(`${p.quantity || 1}x ${p.name}`);
            if (!res.cartAction) {
              addItem(
                {
                  productId: p.productId,
                  name: p.name,
                  imageUrl: p.imageUrl,
                  itemType: p.itemType || "DRINK",
                  size: p.size || "M",
                  sugarLevel: p.sugarLevel ?? "50%",
                  iceLevel: p.iceLevel ?? "70%",
                  unitPrice: Number(p.unitPrice) || 0,
                  quantity: Number(p.quantity) || 1,
                  totalPrice: Number(p.totalPrice) || Number(p.unitPrice) || 0,
                  isCustomDIY: Boolean(p.isCustomDIY),
                  customNotes: p.customNotes || "",
                  toppings: Array.isArray(p.toppings) ? p.toppings : [],
                },
                true
              );
            }
          } else if (act.type === "APPLY_COUPON" && act.payload?.code) {
            if (typeof window !== "undefined") {
              localStorage.setItem("milky_pending_promo", act.payload.code);
            }
            setToastText(`🎟️ Đã áp mã ${act.payload.code}`);
            setTimeout(() => setToastText(null), 3000);
          }
        }
        if (addedNames.length > 0) {
          setToastText(`✅ Đã thêm ${addedNames.join(", ")} vào giỏ!`);
          setTimeout(() => setToastText(null), 3000);
        }
      }

      if (res.suggestedPrompts && res.suggestedPrompts.length > 0) {
        setPrompts(res.suggestedPrompts);
      }

      const assistantMsg: AIChatMessage = {
        id: "a_" + Date.now(),
        role: "assistant",
        content: res.reply,
        suggestedDrinks: res.suggestedDrinks || [],
        actions: res.actions || [],
        recipes: res.recipes || [],
        customizations: res.customizations || [],
        cartAction: res.cartAction,
        orders: res.orders || [],
        payment: res.payment,
        inventoryReport: res.inventoryReport,
        adminAnalytics: res.adminAnalytics,
        executedTools: res.executedTools || [],
        isAiModelLive: res.isAiModelLive,
        modelName: res.modelName,
        timestamp: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, assistantMsg]);
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : "Đã có lỗi kết nối trợ lý AI.";
      setMessages((prev) => [
        ...prev,
        {
          id: "err_" + Date.now(),
          role: "assistant",
          content: `Milky tạm thời gặp trục trặc: ${errMsg}. Bạn thử lại sau chút nha!`,
          timestamp: new Date().toISOString(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleQuickAddDrink = (drink: SuggestedDrink) => {
    const id = drink.productId || 1;
    const isFood = drink.productType === "FOOD";
    const success = addItem(
      {
        productId: id,
        name: drink.name,
        imageUrl: drink.imageUrl,
        itemType: (drink.productType as "DRINK" | "FOOD" | "COMBO") || "DRINK",
        size: isFood ? "Standard" : "M",
        sugarLevel: isFood ? "" : drink.sugarRecommend || "50%",
        iceLevel: isFood ? "" : drink.iceRecommend || "70%",
        unitPrice: drink.estimatedPrice,
        quantity: 1,
        totalPrice: drink.estimatedPrice,
        isCustomDIY: false,
        customNotes: `Được Milky AI gợi ý (${drink.sugarRecommend || "50%"}, ${drink.iceRecommend || "70%"})`,
        toppings: [],
      },
      true
    );

    if (success) {
      setQuickAddedId(id);
      setTimeout(() => setQuickAddedId(null), 2000);
    }
  };

  const handleAddRecipe = (recipe: AIRecipeCard) => {
    addItem(
      {
        name: recipe.recipeName,
        imageUrl: "/images/products/tra-o-long-kem-cheese.png",
        itemType: "DRINK",
        size: recipe.size || "M",
        sugarLevel: recipe.sugarLevel || "50%",
        iceLevel: recipe.iceLevel || "70%",
        unitPrice: recipe.unitPrice,
        quantity: 1,
        totalPrice: recipe.unitPrice,
        isCustomDIY: true,
        customNotes: `DIY AI: ${recipe.teaBaseName} x ${recipe.milkName}`,
        toppings: (recipe.toppings || []).map((t) => ({
          toppingId: t.id || 1,
          name: t.name,
          price: t.price,
          quantity: 1,
        })),
      },
      true
    );
  };

  const handleAddCustom = (custom: AIDrinkCustomizationCard) => {
    addItem(
      {
        productId: custom.productId,
        name: custom.productName,
        imageUrl: custom.imageUrl || "/images/products/tra-sua-tran-chau-den.png",
        itemType: "DRINK",
        size: custom.size || "M",
        sugarLevel: custom.sugarLevel || "50%",
        iceLevel: custom.iceLevel || "70%",
        unitPrice: custom.unitPrice,
        quantity: custom.quantity || 1,
        totalPrice: custom.totalPrice,
        isCustomDIY: custom.isCustomDIY,
        customNotes: custom.breakdown?.join(" | "),
        toppings: (custom.selectedToppings || []).map((t) => ({
          toppingId: t.id || 1,
          name: t.name,
          price: t.price,
          quantity: 1,
        })),
      },
      true
    );
  };

  if (isDedicatedPage) return null;

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col items-end">
      {!isOpen && showTooltip && (
        <div className="mb-3 px-3.5 py-2 bg-[#30251F] text-[#FFF9F3] text-xs font-medium rounded-2xl shadow-xl flex items-center gap-2 border border-[#9B6247]/40 animate-bounce">
          <Sparkles className="w-3.5 h-3.5 text-[#9B6247]" />
          <span>Hỏi Milky AI Agent đặt món & phối trà nhé!</span>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setShowTooltip(false);
            }}
            className="text-gray-400 hover:text-white"
          >
            <X className="w-3 h-3" />
          </button>
        </div>
      )}

      {isOpen && (
        <div className="w-[94vw] sm:w-[440px] h-[590px] max-h-[84vh] bg-white rounded-3xl border border-[#9B6247]/30 shadow-2xl flex flex-col overflow-hidden mb-3 animate-in fade-in slide-in-from-bottom-5 duration-200">
          {/* Header */}
          <div className="bg-[#30251F] text-[#FFF9F3] px-4 py-3.5 flex items-center justify-between shrink-0 shadow-sm">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-[#9B6247] flex items-center justify-center text-[#FFF9F3] relative">
                <Bot className="w-5 h-5" />
                <span className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full bg-emerald-400 ring-2 ring-[#30251F]" />
              </div>
              <div>
                <div className="flex items-center gap-1.5">
                  <h3 className="font-extrabold text-sm text-[#FFF9F3]">MILKY AI BARISTA</h3>
                  <span className="text-[9px] font-bold px-1.5 py-0.5 rounded bg-emerald-500/30 text-emerald-100 border border-emerald-300/30">
                    {aiStatus?.isConfigured ? aiStatus.activeModel : "Agent Live"}
                  </span>
                </div>
                <p className="text-[11px] text-[#FFF9F3]/75">
                  AI Agent • Tư vấn • DIY • Giỏ hàng • Đơn hàng
                </p>
              </div>
            </div>

            <div className="flex items-center gap-1">
              <Link
                href="/ai-barista"
                title="Mở toàn màn hình"
                onClick={() => setIsOpen(false)}
                className="p-1.5 hover:bg-white/10 rounded-lg text-white/80 hover:text-white transition-colors"
              >
                <ExternalLink className="w-4 h-4" />
              </Link>
              <button
                onClick={() => setIsOpen(false)}
                className="p-1.5 hover:bg-white/10 rounded-lg text-white/80 hover:text-white transition-colors"
              >
                <Minimize2 className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Messages Area */}
          {toastText && (
            <div className="px-3.5 py-2 bg-emerald-600 text-white text-xs font-bold flex items-center justify-between shrink-0">
              <span>{toastText}</span>
              <Link
                href="/cart"
                onClick={() => setIsOpen(false)}
                className="underline text-[11px] font-extrabold ml-2"
              >
                Xem giỏ
              </Link>
            </div>
          )}
          <div className="flex-1 p-3.5 overflow-y-auto space-y-4 bg-[#FFF9F3]/40 text-[#30251F]">
            {messages.map((msg) => (
              <ChatMessage
                key={msg.id}
                msg={msg}
                quickAddedId={quickAddedId}
                onAddDrinkToCart={handleQuickAddDrink}
                onAddRecipeToCart={handleAddRecipe}
                onAddCustomToCart={handleAddCustom}
                onLinkClick={() => setIsOpen(false)}
                compact={true}
              />
            ))}

            {loading && <AIThinkingIndicator label="Milky đang thực thi công cụ..." />}

            <div ref={messagesEndRef} />
          </div>

          {/* Quick Prompt Chips */}
          <SuggestedPrompts
            prompts={prompts}
            loading={loading}
            onSelectPrompt={(p) => handleSend(p)}
          />

          {/* Input Form */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
            className="p-2.5 bg-white border-t border-[#9B6247]/20 flex items-center gap-2 shrink-0"
          >
            <input
              type="text"
              placeholder="Nhắn Milky tìm món, thêm giỏ, tự pha DIY..."
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              disabled={loading}
              className="flex-1 px-3.5 py-2.5 rounded-xl bg-[#FFF9F3] border border-[#9B6247]/25 text-xs text-[#30251F] focus:outline-none focus:border-[#9B6247]"
            />
            <button
              type="submit"
              disabled={!inputMessage.trim() || loading}
              className="w-9 h-9 rounded-xl bg-[#30251F] text-[#FFF9F3] flex items-center justify-center hover:bg-[#9B6247] disabled:opacity-40 transition-all shrink-0 cursor-pointer"
            >
              <Send className="w-4 h-4" />
            </button>
          </form>
        </div>
      )}

      {/* Floating Toggle Button */}
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        aria-label="Mở trợ lý AI Barista MilkyVerse"
        className={`w-14 h-14 rounded-full shadow-2xl flex items-center justify-center transition-all duration-300 border-2 border-[#FFF9F3] cursor-pointer ${
          isOpen
            ? "bg-[#30251F] text-[#FFF9F3] rotate-90"
            : "bg-gradient-to-tr from-[#30251F] to-[#9B6247] text-[#FFF9F3] hover:scale-105"
        }`}
      >
        {isOpen ? (
          <ChevronDown className="w-6 h-6" />
        ) : (
          <div className="relative">
            <Bot className="w-7 h-7 text-[#FFF9F3]" />
            <span className="absolute -top-1 -right-1 w-3 h-3 rounded-full bg-emerald-400 ring-2 ring-[#30251F] animate-pulse" />
          </div>
        )}
      </button>
    </div>
  );
};
