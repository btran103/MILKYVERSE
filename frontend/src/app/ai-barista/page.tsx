"use client";

import React, { useState, useRef, useEffect, useCallback } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import {
  AIChatMessage,
  AIConversationSummary,
  AIDrinkCustomizationCard,
  AIRecipeCard,
  AIStatusResponse,
  FavoriteRecipe,
  SuggestedDrink,
} from "@/types";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";
import {
  AIThinkingIndicator,
  ChatMessage,
  ConversationHistory,
  SuggestedPrompts,
} from "@/components/ai/AgentCards";
import {
  Bot,
  Send,
  CupSoda,
  Activity,
  CheckCircle2,
  Wrench,
  Bookmark,
  Trash2,
} from "lucide-react";

const DEFAULT_PROMPTS = [
  "💸 Tôi có 40.000đ, muốn uống trà sữa ít ngọt, có trân châu",
  "🍵 Thêm cho tôi 2 ly trà sữa matcha, 1 ly ít đường và 1 ly bình thường",
  "🧪 Tự pha 1 ly Ô long sữa yến mạch Size L 30% đường thêm trân châu hoàng kim",
  "🍯 Đang bị ho / đau họng thì nên uống món gì ấm?",
  "🌙 Món uống buổi tối không lo mất ngủ (0% caffeine)",
  "🛒 Xem giỏ hàng hiện tại của tôi",
  "📦 Kiểm tra trạng thái đơn hàng gần nhất của tôi",
];

export default function AIBaristaPage() {
  const { items: cartItems, addItem, syncCartFromAgent } = useCart();
  const { user } = useAuth();

  const createWelcomeMessage = useCallback(
    (): AIChatMessage => ({
      id: "welcome",
      role: "assistant",
      content:
        "Xin chào bạn! Mình là **MILKY AI BARISTA** — AI Agent của ngân hà **MilkyVerse** 🌟.\n\nMình có khả năng sử dụng **Function Calling** để trực tiếp tìm kiếm thực đơn 213 món, lọc theo ngân sách & khẩu vị, thiết kế công thức DIY Drink Studio, kiểm tra mã giảm giá, thêm món vào giỏ hàng và tra cứu trạng thái đơn hàng của bạn!",
      isAiModelLive: true,
      modelName: "MILKY AI BARISTA AGENT",
      timestamp: new Date().toISOString(),
    }),
    []
  );

  const [messages, setMessages] = useState<AIChatMessage[]>([createWelcomeMessage()]);
  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<number | undefined>(undefined);
  const [quickAddedId, setQuickAddedId] = useState<number | null>(null);
  const [suggestedPrompts, setSuggestedPrompts] = useState<string[]>(DEFAULT_PROMPTS);

  // Conversation History & Favorite Recipes
  const [conversations, setConversations] = useState<AIConversationSummary[]>([]);
  const [favoriteRecipes, setFavoriteRecipes] = useState<FavoriteRecipe[]>([]);
  const [toastMsg, setToastMsg] = useState<string | null>(null);
  const [aiStatus, setAiStatus] = useState<AIStatusResponse | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, loading]);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 3000);
  };

  const loadConversationsAndFavorites = useCallback(async () => {
    try {
      const [convs, favs] = await Promise.all([
        api.getAIConversations().catch(() => []),
        api.getFavoriteRecipes(sessionId).catch(() => []),
      ]);
      if (Array.isArray(convs)) setConversations(convs);
      if (Array.isArray(favs)) setFavoriteRecipes(favs);
    } catch {
      // Ignore if guest
    }
  }, [sessionId]);

  useEffect(() => {
    // Clean up any legacy client-side API key from localStorage
    if (typeof window !== "undefined") {
      localStorage.removeItem("milky_ai_api_key");
      localStorage.removeItem("milky_ai_provider");
    }

    api
      .getAIStatus()
      .then((st) => setAiStatus(st))
      .catch(() => {});

    loadConversationsAndFavorites();
  }, [user, loadConversationsAndFavorites]);

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

      // Synchronize Frontend CartContext if Shopping Agent mutated the cart or returned client Actions
      if (res.cartAction && Array.isArray(res.cartAction.currentCart)) {
        syncCartFromAgent(res.cartAction.currentCart);
      }

      if (res.actions && res.actions.length > 0) {
        const addedNames: string[] = [];
        for (const act of res.actions) {
          if (act.type === "ADD_TO_CART" && act.payload) {
            const p = act.payload;
            addedNames.push(`${p.quantity || 1}x ${p.name}`);
            // If cartAction didn't already sync the whole cart, apply ADD_TO_CART directly to CartContext
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
            showToast(`🎟️ Đã lưu mã giảm giá ${act.payload.code} (-${Number(act.payload.discountAmount || 0).toLocaleString("vi-VN")}đ)!`);
          }
        }
        if (addedNames.length > 0) {
          showToast(`✅ Đã thêm ${addedNames.join(", ")} vào giỏ!`);
        }
      }

      if (res.suggestedPrompts && res.suggestedPrompts.length > 0) {
        setSuggestedPrompts(res.suggestedPrompts);
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
        suggestedPrompts: res.suggestedPrompts || [],
        isAiModelLive: res.isAiModelLive,
        modelName: res.modelName,
        apiError: res.apiError,
        timestamp: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, assistantMsg]);
      loadConversationsAndFavorites();
    } catch (err: unknown) {
      const errMsg =
        err instanceof Error ? err.message : "Đã có lỗi xảy ra khi kết nối trợ lý AI Agent.";
      setMessages((prev) => [
        ...prev,
        {
          id: "err_" + Date.now(),
          role: "assistant",
          content: `Xin lỗi bạn, AI Agent gặp trục trặc tạm thời: ${errMsg}. Bạn vui lòng thử lại nhé!`,
          timestamp: new Date().toISOString(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectSession = async (targetSessionId: number) => {
    try {
      setLoading(true);
      const detail = await api.getAIConversationDetail(targetSessionId);
      setSessionId(detail.id);
      const loadedMsgs: AIChatMessage[] = detail.messages.map((m) => ({
        id: `db_${m.id}`,
        role: m.senderRole.toLowerCase() === "user" ? "user" : "assistant",
        content: m.content,
        suggestedDrinks: m.agentPayload?.suggestedDrinks || m.suggestedDrinks || [],
        recipes: m.agentPayload?.recipes || [],
        customizations: m.agentPayload?.customizations || [],
        cartAction: m.agentPayload?.cartAction,
        orders: m.agentPayload?.orders || [],
        payment: m.agentPayload?.payment,
        inventoryReport: m.agentPayload?.inventoryReport,
        adminAnalytics: m.agentPayload?.adminAnalytics,
        executedTools: m.agentPayload?.executedTools || [],
        isAiModelLive: m.agentPayload?.isAiModelLive ?? true,
        modelName: m.agentPayload?.modelName || "MILKY AI BARISTA AGENT",
        timestamp: m.createdAt,
      }));
      setMessages(loadedMsgs.length > 0 ? loadedMsgs : [createWelcomeMessage()]);
    } catch (err: unknown) {
      showToast(err instanceof Error ? err.message : "Không thể tải hội thoại.");
    } finally {
      setLoading(false);
    }
  };

  const handleNewConversation = () => {
    setSessionId(undefined);
    setMessages([createWelcomeMessage()]);
  };

  const handleDeleteSession = async (targetSessionId: number) => {
    try {
      await api.deleteAIConversation(targetSessionId);
      setConversations((prev) => prev.filter((c) => c.id !== targetSessionId));
      if (sessionId === targetSessionId) {
        handleNewConversation();
      }
      showToast("Đã xóa cuộc hội thoại.");
    } catch (err: unknown) {
      showToast(err instanceof Error ? err.message : "Không thể xóa hội thoại.");
    }
  };

  const handleAddDrinkToCart = (drink: SuggestedDrink) => {
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
        customNotes: `Được Milky AI Barista tư vấn (${drink.sugarRecommend} đường, ${drink.iceRecommend} đá)`,
        toppings: [],
      },
      true
    );

    if (success) {
      setQuickAddedId(id);
      showToast(`Đã thêm "${drink.name}" vào giỏ hàng!`);
      setTimeout(() => setQuickAddedId(null), 2000);
    }
  };

  const handleAddRecipeToCart = (recipe: AIRecipeCard) => {
    const success = addItem(
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
        customNotes: `DIY AI Recipe: ${recipe.teaBaseName} x ${recipe.milkName}`,
        toppings: (recipe.toppings || []).map((t) => ({
          toppingId: t.id || 1,
          name: t.name,
          price: t.price,
          quantity: 1,
        })),
      },
      true
    );

    if (success) {
      showToast(`Đã thêm công thức "${recipe.recipeName}" vào giỏ hàng!`);
    }
  };

  const handleAddCustomToCart = (custom: AIDrinkCustomizationCard) => {
    const success = addItem(
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

    if (success) {
      showToast(`Đã thêm ${custom.quantity}x "${custom.productName}" vào giỏ hàng!`);
    }
  };

  const handleSaveFavoriteRecipe = async (recipe: AIRecipeCard) => {
    try {
      const saved = await api.saveFavoriteRecipe(
        {
          recipeName: recipe.recipeName,
          teaBaseId: recipe.teaBaseId,
          teaBaseName: recipe.teaBaseName,
          milkId: recipe.milkId,
          milkName: recipe.milkName,
          size: recipe.size,
          sugarLevel: recipe.sugarLevel,
          iceLevel: recipe.iceLevel,
          toppingIds: (recipe.toppings || []).map((t) => t.id),
          toppingNames: (recipe.toppings || []).map((t) => t.name),
          unitPrice: recipe.unitPrice,
          notes: recipe.description,
          isReferenceFormula: recipe.isReferenceFormula,
        },
        sessionId
      );
      setFavoriteRecipes((prev) => [saved, ...prev]);
      showToast(`Đã lưu "${recipe.recipeName}" vào sổ tay công thức yêu thích!`);
    } catch (err: unknown) {
      showToast(err instanceof Error ? err.message : "Không thể lưu công thức.");
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 space-y-6">
      {toastMsg && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 px-4 py-2.5 rounded-2xl bg-[#30251F] text-[#FFF9F3] text-xs font-bold shadow-xl border border-[#9B6247] flex items-center gap-2 animate-in fade-in slide-in-from-bottom-3">
          <CheckCircle2 className="w-4 h-4 text-emerald-400" />
          <span>{toastMsg}</span>
        </div>
      )}

      {/* Header Banner — Premium Modern Café (#FFF9F3, #9B6247, #30251F) */}
      <div className="bg-gradient-to-r from-[#FFF9F3] via-white to-[#FFF9F3] p-6 rounded-3xl border border-[#9B6247]/25 shadow-sm space-y-4">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3.5">
            <div className="w-12 h-12 rounded-2xl bg-[#30251F] flex items-center justify-center text-[#FFF9F3] shadow-md border border-[#9B6247]">
              <Bot className="w-6 h-6 text-[#9B6247]" />
            </div>
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-xl font-black text-[#30251F]">
                  MILKY AI BARISTA — AI Agent
                </h1>
                <span className="text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-emerald-600 text-white flex items-center gap-1">
                  <Activity className="w-3 h-3" />
                  {aiStatus?.isConfigured
                    ? `Gemini Function Calling (${aiStatus.activeModel})`
                    : "Local Agent Engine"}
                </span>
                <span className="text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-[#FFF9F3] border border-[#9B6247]/30 text-[#9B6247] flex items-center gap-1">
                  <Wrench className="w-3 h-3" />
                  <span>Tool Calling Enabled</span>
                </span>
              </div>
              <p className="text-xs text-[#30251F]/70 mt-0.5">
                Tích hợp trực tiếp Database ({aiStatus?.activeMenuCount ?? 213} món &{" "}
                {aiStatus?.activeToppingCount ?? 15} topping) • Tư vấn • DIY Studio • Giỏ hàng •
                Tra cứu đơn hàng
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <Link
              href="/diy"
              className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-[#FFF9F3] border border-[#9B6247] text-xs font-bold text-[#9B6247] hover:bg-[#9B6247] hover:text-[#FFF9F3] transition-all"
            >
              <CupSoda className="w-3.5 h-3.5" />
              <span>DIY Drink Studio</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Main Agent Workspace Grid: Sidebar (Conversation History & Saved Recipes) + Chat Panel */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Sidebar: Conversation Memory & Favorite DIY Recipes */}
        <div className="lg:col-span-3 space-y-4">
          <div className="h-[340px]">
            <ConversationHistory
              conversations={conversations}
              activeSessionId={sessionId}
              onSelectSession={handleSelectSession}
              onNewConversation={handleNewConversation}
              onDeleteSession={handleDeleteSession}
            />
          </div>

          {/* Saved Favorite DIY Recipes */}
          <div className="bg-white rounded-3xl border border-[#9B6247]/20 shadow-sm p-4 space-y-2.5">
            <div className="flex items-center gap-1.5 text-xs font-extrabold text-[#30251F] pb-1.5 border-b border-[#FFF9F3]">
              <Bookmark className="w-4 h-4 text-[#9B6247]" />
              <span>Công Thức DIY Đã Lưu ({favoriteRecipes.length})</span>
            </div>
            <div className="max-h-44 overflow-y-auto space-y-2 pr-1">
              {favoriteRecipes.length === 0 ? (
                <p className="text-[11px] text-[#30251F]/60 py-3 text-center">
                  Bảo Milky &ldquo;Tự pha & lưu công thức&rdquo; để lưu công thức yêu thích của
                  bạn!
                </p>
              ) : (
                favoriteRecipes.map((fav) => (
                  <div
                    key={fav.id}
                    className="p-2.5 rounded-2xl bg-[#FFF9F3] border border-[#9B6247]/20 text-[11px] space-y-1"
                  >
                    <div className="flex items-center justify-between gap-1">
                      <span className="font-bold text-[#30251F] truncate">{fav.recipeName}</span>
                      <button
                        type="button"
                        onClick={async () => {
                          await api.deleteFavoriteRecipe(fav.id);
                          setFavoriteRecipes((p) => p.filter((x) => x.id !== fav.id));
                        }}
                        className="text-gray-400 hover:text-red-600"
                      >
                        <Trash2 className="w-3 h-3" />
                      </button>
                    </div>
                    <div className="text-[10px] text-[#30251F]/70">
                      Size {fav.size} • {fav.sugarLevel} đường •{" "}
                      <strong className="text-[#9B6247]">
                        {fav.unitPrice.toLocaleString("vi-VN")}đ
                      </strong>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right Main Chat Area */}
        <div className="lg:col-span-9 bg-white rounded-3xl border border-[#9B6247]/25 shadow-sm h-[620px] flex flex-col overflow-hidden">
          <div className="flex-1 p-5 sm:p-6 overflow-y-auto space-y-5">
            {messages.map((msg) => (
              <ChatMessage
                key={msg.id}
                msg={msg}
                quickAddedId={quickAddedId}
                onAddDrinkToCart={handleAddDrinkToCart}
                onAddRecipeToCart={handleAddRecipeToCart}
                onAddCustomToCart={handleAddCustomToCart}
                onSaveFavoriteRecipe={handleSaveFavoriteRecipe}
              />
            ))}

            {loading && <AIThinkingIndicator />}

            <div ref={messagesEndRef} />
          </div>

          {/* Quick Prompt Suggestions */}
          <SuggestedPrompts
            prompts={suggestedPrompts}
            loading={loading}
            onSelectPrompt={(p) => handleSend(p)}
          />

          {/* Message Input Bar */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
            className="p-4 bg-white border-t border-[#9B6247]/20 flex gap-3"
          >
            <input
              type="text"
              placeholder="Ví dụ: 'Tôi có 40.000đ muốn uống trà sữa ít ngọt có trân châu' hoặc 'Thêm 2 ly matcha, 1 ít đường 1 bình thường'..."
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              disabled={loading}
              className="flex-1 px-4 py-3 rounded-2xl bg-[#FFF9F3] border border-[#9B6247]/25 text-xs sm:text-sm text-[#30251F] focus:outline-none focus:border-[#9B6247]"
            />
            <button
              type="submit"
              disabled={!inputMessage.trim() || loading}
              className="px-6 py-3 rounded-2xl bg-[#30251F] text-[#FFF9F3] font-bold text-xs sm:text-sm hover:bg-[#9B6247] disabled:opacity-40 transition-all flex items-center gap-2 shrink-0 cursor-pointer"
            >
              <span>Gửi</span>
              <Send className="w-4 h-4" />
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
