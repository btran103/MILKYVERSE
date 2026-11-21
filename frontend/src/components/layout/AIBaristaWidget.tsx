"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { api } from "@/lib/api";
import { AIChatMessage, SuggestedDrink } from "@/types";
import { useCart } from "@/context/CartContext";
import {
  Bot,
  Sparkles,
  Send,
  X,
  ShoppingBag,
  Check,
  ExternalLink,
  ChevronDown,
  Minimize2,
  KeyRound,
} from "lucide-react";

export const AIBaristaWidget: React.FC = () => {
  const pathname = usePathname();
  const { addItem } = useCart();

  const [isOpen, setIsOpen] = useState(false);
  const [showKeyBox, setShowKeyBox] = useState(false);
  const [apiKeyInput, setApiKeyInput] = useState("");
  const [aiStatus, setAiStatus] = useState<{
    isConfigured: boolean;
    provider: string;
    activeModel: string;
    maskedApiKey: string;
  } | null>(null);
  const [savingKey, setSavingKey] = useState(false);
  const [keyStatusMsg, setKeyStatusMsg] = useState<string | null>(null);
  const [messages, setMessages] = useState<AIChatMessage[]>([
    {
      id: "welcome",
      role: "assistant",
      content:
        "Xin chào! Mình là Milky ✨ - AI Barista thông minh của MilkyVerse. Bạn cần tìm món uống thanh nhiệt, đồ uống ít ngọt, không caffeine cho buổi tối hay muốn kiểm tra đơn hàng nè?",
      isAiModelLive: true,
      timestamp: new Date().toISOString(),
    },
  ]);

  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<number | undefined>(undefined);
  const [quickAddedId, setQuickAddedId] = useState<number | null>(null);
  const [showTooltip, setShowTooltip] = useState(true);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Hide widget if already on the dedicated full-page /ai-barista
  const isDedicatedPage = pathname === "/ai-barista";

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
      setShowTooltip(false);
      const savedKey = typeof window !== "undefined" ? localStorage.getItem("milky_ai_api_key") : null;
      if (savedKey) {
        setApiKeyInput(savedKey);
      }
      api
        .getAIStatus()
        .then((st) => setAiStatus(st))
        .catch(() => {});
    }
  }, [messages, loading, isOpen]);

  const handleSaveWidgetApiKey = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingKey(true);
    setKeyStatusMsg(null);
    const clean = apiKeyInput.trim();
    const masked = clean.length > 8 ? `${clean.slice(0, 4)}...${clean.slice(-4)}` : "****";

    if (typeof window !== "undefined") {
      if (clean) localStorage.setItem("milky_ai_api_key", clean);
      else localStorage.removeItem("milky_ai_api_key");
    }

    try {
      const st = await api.configureAIApiKey({ apiKey: clean });
      setAiStatus(st);
      setKeyStatusMsg(`✅ Đã lưu & kết nối API Key (${st.maskedApiKey || masked})!`);
    } catch {
      // Even if old backend process doesn't have /api/ai/config yet, localStorage is saved and passed on every /api/ai/chat request
      setAiStatus({
        isConfigured: Boolean(clean),
        provider: "Google Gemini API",
        activeModel: "gemini-2.5-flash",
        maskedApiKey: clean ? masked : "",
      });
      setKeyStatusMsg(`✅ Đã lưu API Key (${masked}) và kích hoạt liên kết!`);
    } finally {
      setSavingKey(false);
      setTimeout(() => {
        setShowKeyBox(false);
        setKeyStatusMsg(null);
      }, 1200);
    }
  };

  // Dataset-aligned quick prompt chips
  const datasetPrompts = [
    "🍯 Đang bị ho / đau họng uống gì?",
    "🌙 Uống tối không caffeine",
    "☕ Cần tỉnh táo chạy deadline",
    "🥗 Ít ngọt / Healthy 0% đường",
    "🍑 Trà mát lạnh dưới 40k",
    "🍟 Ăn vặt nhâm nhi cùng trà sữa",
  ];

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
      const savedKey = typeof window !== "undefined" ? localStorage.getItem("milky_ai_api_key") || undefined : undefined;
      const savedProvider = typeof window !== "undefined" ? localStorage.getItem("milky_ai_provider") || undefined : undefined;
      const res = await api.sendAIChat(textToSend.trim(), sessionId, savedKey, savedProvider);
      setSessionId(res.sessionId);

      const assistantMsg: AIChatMessage = {
        id: "a_" + Date.now(),
        role: "assistant",
        content: res.reply,
        suggestedDrinks: (res.suggestedDrinks as unknown as SuggestedDrink[]) || [],
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

  const handleQuickAdd = (drink: SuggestedDrink) => {
    const id = drink.productId || 1;
    const success = addItem({
      productId: id,
      name: drink.name,
      size: "M",
      sugarLevel: drink.sugarRecommend?.includes("%") ? drink.sugarRecommend : "50%",
      iceLevel: drink.iceRecommend?.includes("%") ? drink.iceRecommend : "70%",
      unitPrice: drink.estimatedPrice,
      quantity: 1,
      totalPrice: drink.estimatedPrice,
      isCustomDIY: false,
      customNotes: `Được AI Barista gợi ý (${drink.sugarRecommend || "50%"}, ${drink.iceRecommend || "70%"})`,
      toppings: (drink.recommendedToppings || []).map((tName, idx) => ({
        toppingId: idx + 1,
        name: tName,
        price: 10000,
        quantity: 1,
      })),
    });

    if (success) {
      setQuickAddedId(id);
      setTimeout(() => setQuickAddedId(null), 2000);
    }
  };

  // Helper to render markdown links as clickable elements
  const renderMessageContent = (content: string) => {
    // Regex matches [Link Text](url)
    const linkRegex = /\[([^\]]+)\]\(([^)]+)\)/g;
    const parts = [];
    let lastIndex = 0;
    let match;

    while ((match = linkRegex.exec(content)) !== null) {
      if (match.index > lastIndex) {
        parts.push(content.substring(lastIndex, match.index));
      }
      const label = match[1];
      const href = match[2];
      parts.push(
        <Link
          key={match.index}
          href={href}
          onClick={() => {
            if (href.startsWith("/")) setIsOpen(false);
          }}
          className="text-[#6F4E37] underline font-bold hover:text-[#4A3324] inline-flex items-center gap-0.5 mx-0.5"
        >
          <span>{label}</span>
          <ExternalLink className="w-3 h-3 inline" />
        </Link>
      );
      lastIndex = linkRegex.lastIndex;
    }

    if (lastIndex < content.length) {
      parts.push(content.substring(lastIndex));
    }

    return (
      <div className="whitespace-pre-line text-xs sm:text-sm leading-relaxed">
        {parts.map((p, idx) => (typeof p === "string" ? <span key={idx}>{p}</span> : p))}
      </div>
    );
  };

  if (isDedicatedPage) return null;

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col items-end">
      {/* Tooltip bubble when closed */}
      {!isOpen && showTooltip && (
        <div className="mb-3 px-3.5 py-2 bg-[#2D2522] text-white text-xs font-medium rounded-2xl shadow-xl flex items-center gap-2 border border-[#6F4E37]/30 animate-bounce">
          <Sparkles className="w-3.5 h-3.5 text-[#F7D6D0]" />
          <span>Hỏi Milky gợi ý đồ uống nhé!</span>
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

      {/* Floating Chat Window */}
      {isOpen && (
        <div className="w-[92vw] sm:w-[400px] h-[550px] max-h-[82vh] bg-white rounded-3xl border border-[#EFE6DC] shadow-2xl flex flex-col overflow-hidden mb-3 animate-in fade-in slide-in-from-bottom-5 duration-200">
          {/* Header */}
          <div className="bg-[#6F4E37] text-white px-4 py-3.5 flex items-center justify-between shrink-0 shadow-sm">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-white/10 border border-white/20 flex items-center justify-center text-[#F7D6D0] relative">
                <Bot className="w-5 h-5" />
                <span className="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full bg-emerald-400 ring-2 ring-[#6F4E37]" />
              </div>
              <div>
                <div className="flex items-center gap-1.5">
                  <h3 className="font-extrabold text-sm text-white">AI Barista Milky</h3>
                  <span className="text-[9px] font-bold px-1.5 py-0.2 rounded bg-emerald-500/30 text-emerald-100 border border-emerald-300/30">
                    {aiStatus?.isConfigured ? aiStatus.activeModel : "API Live"}
                  </span>
                </div>
                <p className="text-[11px] text-[#F7D6D0]/80">Kết nối API Thực đơn & Đơn hàng</p>
              </div>
            </div>

            <div className="flex items-center gap-1">
              <button
                onClick={() => setShowKeyBox((p) => !p)}
                title="Liên kết API Key (Gemini / Groq / OpenAI)"
                className={`p-1.5 rounded-lg transition-colors ${
                  showKeyBox || aiStatus?.isConfigured
                    ? "bg-white/20 text-[#F7D6D0]"
                    : "hover:bg-white/10 text-white/80 hover:text-white"
                }`}
              >
                <KeyRound className="w-4 h-4" />
              </button>
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

          {showKeyBox && (
            <form
              onSubmit={handleSaveWidgetApiKey}
              className="px-3.5 py-2.5 bg-[#FAF7F2] border-b border-[#EFE6DC] space-y-2 shrink-0"
            >
              <div className="flex items-center justify-between text-[11px] font-bold text-[#2D2522]">
                <span>🔑 Liên kết Gemini / Groq / OpenAI API Key</span>
                {aiStatus?.maskedApiKey && (
                  <span className="text-emerald-700">Đang dùng: {aiStatus.maskedApiKey}</span>
                )}
              </div>
              <div className="flex items-center gap-1.5">
                <input
                  type="password"
                  placeholder="Dán API Key (AIzaSy... / AQ... / gsk_... / sk-...)"
                  value={apiKeyInput}
                  onChange={(e) => setApiKeyInput(e.target.value)}
                  className="flex-1 px-2.5 py-1.5 rounded-lg bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
                />
                <button
                  type="submit"
                  disabled={savingKey}
                  className="px-3 py-1.5 rounded-lg bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] cursor-pointer active:scale-95 transition-all"
                >
                  {savingKey ? "Đang lưu..." : "Lưu"}
                </button>
              </div>
              {keyStatusMsg && (
                <div className="text-[11px] font-bold text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-lg px-2.5 py-1">
                  {keyStatusMsg}
                </div>
              )}
            </form>
          )}

          {/* Messages Area */}
          <div className="flex-1 p-3.5 overflow-y-auto space-y-3.5 bg-[#FAF7F2]/50 text-[#2D2522]">
            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex gap-2.5 ${msg.role === "user" ? "justify-end" : "justify-start"}`}
              >
                {msg.role === "assistant" && (
                  <div className="w-7 h-7 rounded-lg bg-[#6F4E37] text-[#F7D6D0] flex items-center justify-center shrink-0 mt-0.5 shadow-xs">
                    <Bot className="w-4 h-4" />
                  </div>
                )}

                <div
                  className={`max-w-[85%] rounded-2xl p-3 text-xs leading-relaxed shadow-xs ${
                    msg.role === "user"
                      ? "bg-[#6F4E37] text-white rounded-tr-none"
                      : "bg-white text-[#2D2522] border border-[#EFE6DC] rounded-tl-none"
                  }`}
                >
                  {renderMessageContent(msg.content)}

                  {/* Render Suggested Drinks Cards */}
                  {msg.suggestedDrinks && msg.suggestedDrinks.length > 0 && (
                    <div className="mt-2.5 pt-2.5 border-t border-[#EFE6DC] space-y-2">
                      <p className="text-[10px] font-extrabold uppercase tracking-wider text-[#6F4E37]">
                        Món gợi ý cho bạn:
                      </p>
                      {msg.suggestedDrinks.map((drink, i) => (
                        <div
                          key={i}
                          className="bg-[#FAF7F2] p-2.5 rounded-xl border border-[#EFE6DC] space-y-1.5"
                        >
                          <div className="flex items-start justify-between gap-2">
                            <div>
                              <h5 className="font-extrabold text-[#2D2522] text-xs">
                                {drink.name}
                              </h5>
                              <p className="text-[10px] text-[#786C65] line-clamp-1">{drink.description}</p>
                            </div>
                            <span className="text-xs font-black text-[#6F4E37] shrink-0">
                              {drink.estimatedPrice.toLocaleString("vi-VN")}đ
                            </span>
                          </div>

                          <div className="text-[10px] text-[#786C65] flex flex-wrap gap-1">
                            <span className="px-1.5 py-0.5 rounded bg-white border border-[#EFE6DC] font-medium">
                              Đường: {drink.sugarRecommend}
                            </span>
                            <span className="px-1.5 py-0.5 rounded bg-white border border-[#EFE6DC] font-medium">
                              Đá: {drink.iceRecommend}
                            </span>
                          </div>

                          <div className="flex items-center gap-1.5 pt-1">
                            <button
                              onClick={() => handleQuickAdd(drink)}
                              className={`flex-1 py-1.5 px-2 rounded-lg text-[11px] font-bold flex items-center justify-center gap-1 transition-all ${
                                quickAddedId === (drink.productId || 1)
                                  ? "bg-emerald-600 text-white"
                                  : "bg-[#6F4E37] text-white hover:bg-[#4A3324]"
                              }`}
                            >
                              {quickAddedId === (drink.productId || 1) ? (
                                <>
                                  <Check className="w-3 h-3" />
                                  <span>Đã thêm giỏ</span>
                                </>
                              ) : (
                                <>
                                  <ShoppingBag className="w-3 h-3 text-[#F7D6D0]" />
                                  <span>Thêm giỏ</span>
                                </>
                              )}
                            </button>

                            {drink.slug && drink.slug !== "diy" ? (
                              <Link
                                href={`/menu/${drink.slug}`}
                                onClick={() => setIsOpen(false)}
                                className="py-1.5 px-2 rounded-lg text-[11px] font-bold border border-[#6F4E37] text-[#6F4E37] hover:bg-[#F7D6D0]/30 transition-colors"
                              >
                                Xem món
                              </Link>
                            ) : (
                              <Link
                                href="/diy"
                                onClick={() => setIsOpen(false)}
                                className="py-1.5 px-2 rounded-lg text-[11px] font-bold border border-[#6F4E37] text-[#6F4E37] hover:bg-[#F7D6D0]/30 transition-colors"
                              >
                                Tự Pha
                              </Link>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            ))}

            {loading && (
              <div className="flex gap-2 items-center">
                <div className="w-7 h-7 rounded-lg bg-[#6F4E37] text-[#F7D6D0] flex items-center justify-center">
                  <Bot className="w-4 h-4 animate-spin" />
                </div>
                <div className="bg-white border border-[#EFE6DC] rounded-xl px-3 py-1.5 text-xs text-[#786C65] flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-[#6F4E37] animate-ping" />
                  <span>Milky đang tìm món...</span>
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Quick Prompts Carousel */}
          <div className="px-2.5 py-2 bg-white border-t border-[#EFE6DC] flex gap-1.5 overflow-x-auto scrollbar-none shrink-0">
            {datasetPrompts.map((p, idx) => (
              <button
                key={idx}
                onClick={() => handleSend(p)}
                disabled={loading}
                className="px-2.5 py-1 rounded-lg bg-[#FAF7F2] border border-[#EFE6DC] text-[11px] font-medium text-[#2D2522] hover:border-[#6F4E37] hover:text-[#6F4E37] whitespace-nowrap transition-colors"
              >
                {p}
              </button>
            ))}
          </div>

          {/* Input Bar */}
          <div className="p-2.5 bg-white border-t border-[#EFE6DC] shrink-0">
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleSend();
              }}
              className="flex items-center gap-1.5"
            >
              <input
                type="text"
                placeholder="Hỏi món, độ ngọt, hủy đơn..."
                value={inputMessage}
                onChange={(e) => setInputMessage(e.target.value)}
                disabled={loading}
                className="flex-1 px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <button
                type="submit"
                disabled={loading || !inputMessage.trim()}
                className="p-2 rounded-xl bg-[#6F4E37] text-white hover:bg-[#4A3324] disabled:opacity-50 transition-all shrink-0"
              >
                <Send className="w-4 h-4" />
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Floating Toggle Button */}
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        className="w-14 h-14 rounded-full bg-gradient-to-tr from-[#6F4E37] to-[#8C6246] text-white shadow-xl hover:shadow-2xl hover:scale-105 active:scale-95 transition-all flex items-center justify-center border-2 border-white relative group"
        aria-label="Mở AI Barista Milky"
      >
        {isOpen ? (
          <ChevronDown className="w-6 h-6" />
        ) : (
          <>
            <Bot className="w-7 h-7 text-[#F7D6D0] group-hover:rotate-12 transition-transform" />
            <span className="absolute top-1 right-1 w-3 h-3 rounded-full bg-emerald-400 ring-2 ring-white" />
          </>
        )}
      </button>
    </div>
  );
};
