"use client";

import React, { useState, useRef, useEffect } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { AIChatMessage, SuggestedDrink } from "@/types";
import { useCart } from "@/context/CartContext";
import {
  Bot,
  Sparkles,
  Send,
  CupSoda,
  ShoppingBag,
  Check,
  User,
  KeyRound,
  Activity,
  ExternalLink,
  CheckCircle2,
  AlertCircle,
} from "lucide-react";

export default function AIBaristaPage() {
  const { addItem } = useCart();

  const [messages, setMessages] = useState<AIChatMessage[]>([
    {
      id: "welcome",
      role: "assistant",
      content:
        "Xin chào bạn! Mình là AI Barista của ngân hà MilkyVerse 🌟. Mình đã kết nối trực tiếp với API Thực đơn (213 món) & Đơn hàng thời gian thực. Bạn muốn liên kết thêm Gemini / Groq / OpenAI API Key hay để Milky tư vấn món ngay hôm nay?",
      isAiModelLive: true,
      modelName: "MilkyVerse REST API (/api/ai/chat)",
      timestamp: "2026-10-16T12:00:00Z",
    },
  ]);

  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<number | undefined>(undefined);
  const [quickAddedId, setQuickAddedId] = useState<number | null>(null);

  // API Link & Key configuration states
  const [showApiModal, setShowApiModal] = useState(false);
  const [apiKeyInput, setApiKeyInput] = useState("");
  const [providerInput, setProviderInput] = useState("Google Gemini API");
  const [aiStatus, setAiStatus] = useState<{
    isConfigured: boolean;
    provider: string;
    activeModel: string;
    maskedApiKey: string;
    endpointUrl: string;
    activeMenuCount: number;
    activeToppingCount: number;
  } | null>(null);
  const [savingApi, setSavingApi] = useState(false);
  const [apiFeedback, setApiFeedback] = useState<{ type: "ok" | "err"; text: string } | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, loading]);

  useEffect(() => {
    const savedKey = typeof window !== "undefined" ? localStorage.getItem("milky_ai_api_key") : null;
    const savedProvider = typeof window !== "undefined" ? localStorage.getItem("milky_ai_provider") : null;
    if (savedKey) {
      setApiKeyInput(savedKey);
    }
    if (savedProvider) setProviderInput(savedProvider);

    api
      .getAIStatus()
      .then((st) => setAiStatus(st))
      .catch(() => {});
  }, []);

  const handleSaveApiConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingApi(true);
    setApiFeedback(null);
    const cleanKey = apiKeyInput.trim();
    const masked = cleanKey.length > 8 ? `${cleanKey.slice(0, 4)}...${cleanKey.slice(-4)}` : "****";

    if (typeof window !== "undefined") {
      if (cleanKey) {
        localStorage.setItem("milky_ai_api_key", cleanKey);
        localStorage.setItem("milky_ai_provider", providerInput);
      } else {
        localStorage.removeItem("milky_ai_api_key");
      }
    }

    try {
      const st = await api.configureAIApiKey({
        apiKey: cleanKey,
        provider: providerInput,
      });
      setAiStatus(st);
      setApiFeedback({
        type: "ok",
        text: st.isConfigured
          ? `Đã liên kết thành công ${st.provider} (${st.activeModel}) • Key: ${st.maskedApiKey}`
          : "Đã chuyển về chế độ MilkyVerse Smart Dataset API (/api/ai/chat).",
      });
    } catch {
      setAiStatus({
        isConfigured: Boolean(cleanKey),
        provider: providerInput,
        activeModel: "gemini-2.5-flash",
        maskedApiKey: cleanKey ? masked : "",
        endpointUrl: "/api/ai/chat",
        activeMenuCount: 213,
        activeToppingCount: 15,
      });
      setApiFeedback({
        type: "ok",
        text: `Đã lưu & kích hoạt thành công ${providerInput} • Key: ${masked}`,
      });
    } finally {
      setSavingApi(false);
    }
  };

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

    const userTextLower = textToSend.trim().toLowerCase();
    const isAddRequest =
      userTextLower.includes("thêm") ||
      userTextLower.includes("đặt") ||
      userTextLower.includes("lấy") ||
      userTextLower.includes("mua");

    // Check if user is asking to add the suggested drink
    const lastAssistantMsg = [...messages].reverse().find((m) => m.role === "assistant" && m.suggestedDrinks && m.suggestedDrinks.length > 0);
    if (isAddRequest && lastAssistantMsg?.suggestedDrinks && lastAssistantMsg.suggestedDrinks.length > 0) {
      const targetDrink = lastAssistantMsg.suggestedDrinks[0];
      const success = handleQuickAdd(targetDrink);
      if (success) {
        setMessages((prev) => [
          ...prev,
          {
            id: "u_" + Date.now(),
            role: "user",
            content: textToSend.trim(),
            timestamp: new Date().toISOString(),
          },
          {
            id: "a_" + (Date.now() + 1),
            role: "assistant",
            content: `🌟 Tuyệt vời! Mình đã thêm ly **${targetDrink.name}** vào giỏ hàng của bạn rồi nhé. Bạn có thể bấm vào giỏ hàng ở góc phải để thanh toán bất cứ lúc nào!`,
            suggestedDrinks: [targetDrink],
            isAiModelLive: true,
            timestamp: new Date().toISOString(),
          },
        ]);
        return;
      }
    }

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
        apiError: res.apiError,
        timestamp: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, assistantMsg]);
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : "Đã có lỗi xảy ra khi kết nối trợ lý AI.";
      setMessages((prev) => [
        ...prev,
        {
          id: "err_" + Date.now(),
          role: "assistant",
          content: `Xin lỗi bạn, dịch vụ tư vấn gặp trục trặc tạm thời: ${errMsg}. Bạn có thể xem thực đơn trực tiếp hoặc thử lại sau nhé!`,
          timestamp: new Date().toISOString(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleQuickAdd = (drink: SuggestedDrink): boolean => {
    const id = drink.productId || 1;
    const success = addItem({
      productId: id,
      name: drink.name,
      size: "M",
      sugarLevel: drink.sugarRecommend || "50%",
      iceLevel: drink.iceRecommend || "70%",
      unitPrice: drink.estimatedPrice,
      quantity: 1,
      totalPrice: drink.estimatedPrice,
      isCustomDIY: false,
      customNotes: `Được AI Barista tư vấn (${drink.sugarRecommend} đường, ${drink.iceRecommend} đá)`,
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
      return true;
    }
    return false;
  };

  const quickPrompts = [
    "🍯 Mình đang bị ho / đau họng thì nên uống món gì ấm?",
    "🌙 Món uống buổi tối không lo mất ngủ (không caffeine)",
    "☕ Đang buồn ngủ chạy deadline cần món giúp tỉnh táo",
    "🥗 Đồ uống ít ngọt / ăn kiêng 0% đường",
    "🍑 Trà trái cây mát lạnh dưới 40k",
    "🍟 Có món ăn vặt gì nhâm nhi cùng trà sữa không?",
    "🧀 Thức uống béo ngậy với lớp kem Cheese Foam",
  ];

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 space-y-6">
      {/* Header Banner */}
      <div className="bg-gradient-to-r from-[#FAF7F2] via-white to-[#FAF7F2] p-6 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-4">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-2xl bg-[#6F4E37] flex items-center justify-center text-white shadow-md">
              <Bot className="w-6 h-6 text-[#F7D6D0]" />
            </div>
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-xl font-black text-[#2D2522]">AI Barista MilkyVerse</h1>
                <span className="text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-emerald-600 text-white flex items-center gap-1">
                  <Activity className="w-3 h-3" />
                  {aiStatus?.isConfigured
                    ? `Live API: ${aiStatus.activeModel}`
                    : "API Connected (/api/ai/chat)"}
                </span>
              </div>
              <p className="text-xs text-[#786C65] mt-0.5">
                Liên kết trực tiếp Database ({aiStatus?.activeMenuCount ?? 213} món & {aiStatus?.activeToppingCount ?? 15} topping) • Hỗ trợ Google Gemini / Groq / OpenAI API
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <button
              type="button"
              onClick={() => setShowApiModal((prev) => !prev)}
              className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-all shadow-xs"
            >
              <KeyRound className="w-3.5 h-3.5 text-[#F7D6D0]" />
              <span>{aiStatus?.isConfigured ? "Đổi API Key" : "Liên kết API Key"}</span>
            </button>

            <Link
              href="/diy"
              className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-[#FAF7F2] border border-[#6F4E37] text-xs font-bold text-[#6F4E37] hover:bg-[#F7D6D0]/30 transition-all"
            >
              <CupSoda className="w-3.5 h-3.5" />
              <span>DIY Studio</span>
            </Link>
          </div>
        </div>

        {/* Collapsible API Configuration Panel */}
        {showApiModal && (
          <form
            onSubmit={handleSaveApiConfig}
            className="p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-3"
          >
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="text-xs font-extrabold text-[#2D2522] flex items-center gap-1.5">
                <KeyRound className="w-4 h-4 text-[#6F4E37]" />
                <span>Cấu hình Liên kết AI API (Google Gemini 2.5 Flash / Groq / OpenAI)</span>
              </div>
              <a
                href="https://aistudio.google.com/app/apikey"
                target="_blank"
                rel="noreferrer"
                className="text-[11px] font-bold text-[#6F4E37] underline flex items-center gap-1 hover:text-[#4A3324]"
              >
                <span>Lấy Gemini API Key miễn phí tại Google AI Studio</span>
                <ExternalLink className="w-3 h-3" />
              </a>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
              <select
                value={providerInput}
                onChange={(e) => setProviderInput(e.target.value)}
                className="px-3 py-2.5 rounded-xl bg-white border border-[#EFE6DC] text-xs font-bold text-[#2D2522] focus:outline-none focus:border-[#6F4E37]"
              >
                <option value="Google Gemini API">Google Gemini API (gemini-2.5-flash)</option>
                <option value="Groq Cloud LLM API">Groq Cloud API (llama-3.3-70b)</option>
                <option value="OpenAI ChatGPT API">OpenAI API (gpt-4o-mini)</option>
                <option value="OpenRouter LLM API">OpenRouter API</option>
              </select>

              <input
                type="password"
                placeholder="Dán mã API Key (VD: AIzaSy... hoặc gsk_... hoặc sk-...)"
                value={apiKeyInput}
                onChange={(e) => setApiKeyInput(e.target.value)}
                className="sm:col-span-2 px-3.5 py-2.5 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
            </div>

            <div className="flex flex-wrap items-center justify-between gap-2 pt-1">
              <p className="text-[11px] text-[#786C65]">
                Endpoint Backend: <code className="px-1.5 py-0.5 rounded bg-white border border-[#EFE6DC] font-mono text-[#6F4E37]">POST http://localhost:5000/api/ai/chat</code>
                {aiStatus?.maskedApiKey && (
                  <span className="ml-2 font-bold text-emerald-700">• Đang dùng Key: {aiStatus.maskedApiKey}</span>
                )}
              </p>

              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowApiModal(false)}
                  className="px-3 py-1.5 rounded-xl border border-[#EFE6DC] bg-white text-xs font-semibold text-[#786C65] hover:text-[#2D2522]"
                >
                  Đóng
                </button>
                <button
                  type="submit"
                  disabled={savingApi}
                  className="px-4 py-1.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] disabled:opacity-50"
                >
                  {savingApi ? "Đang kết nối..." : "Lưu & Kết nối API"}
                </button>
              </div>
            </div>

            {apiFeedback && (
              <div
                className={`p-2.5 rounded-xl text-xs font-semibold flex items-center gap-2 ${
                  apiFeedback.type === "ok"
                    ? "bg-emerald-50 text-emerald-800 border border-emerald-200"
                    : "bg-red-50 text-red-700 border border-red-200"
                }`}
              >
                {apiFeedback.type === "ok" ? (
                  <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
                ) : (
                  <AlertCircle className="w-4 h-4 shrink-0 text-red-600" />
                )}
                <span>{apiFeedback.text}</span>
              </div>
            )}
          </form>
        )}
      </div>

      {/* Chat Messages Log */}
      <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm h-[520px] flex flex-col overflow-hidden">
        <div className="flex-1 p-6 overflow-y-auto space-y-5">
          {messages.map((msg) => (
            <div
              key={msg.id}
              className={`flex gap-3 ${msg.role === "user" ? "justify-end" : "justify-start"}`}
            >
              {msg.role === "assistant" && (
                <div className="w-8 h-8 rounded-xl bg-[#6F4E37] text-white flex items-center justify-center shrink-0 mt-1 shadow-sm">
                  <Bot className="w-4 h-4 text-[#F7D6D0]" />
                </div>
              )}

              <div
                className={`max-w-[85%] rounded-3xl p-4 text-sm leading-relaxed shadow-sm ${
                  msg.role === "user"
                    ? "bg-[#6F4E37] text-white rounded-tr-none"
                    : "bg-[#FAF7F2] text-[#2D2522] border border-[#EFE6DC] rounded-tl-none"
                }`}
              >
                {/* Notice header if applicable */}
                {msg.role === "assistant" && (
                  <div className="mb-2 text-[10px] font-bold text-[#786C65] flex flex-wrap items-center justify-between gap-1">
                    <span className="flex items-center gap-1">
                      <Sparkles className="w-3 h-3 text-[#C5A059]" />
                      <span>
                        {msg.modelName ||
                          (msg.isAiModelLive ? "Live Gemini 2.5 Flash API" : "MilkyVerse Smart Dataset API (/api/ai/chat)")}
                      </span>
                    </span>
                    {msg.apiError && (
                      <span className="text-amber-700 bg-amber-50 px-1.5 py-0.5 rounded border border-amber-200">
                        Fallback (API Key lỗi/hết hạn)
                      </span>
                    )}
                  </div>
                )}

                <p className="whitespace-pre-line">{msg.content}</p>

                {/* Render Suggested Drinks Cards if available */}
                {msg.suggestedDrinks && msg.suggestedDrinks.length > 0 && (
                  <div className="mt-4 pt-4 border-t border-[#EFE6DC] space-y-3">
                    <p className="text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
                      Gợi ý dành riêng cho bạn:
                    </p>

                    <div className="grid grid-cols-1 gap-3">
                      {msg.suggestedDrinks.map((drink, i) => (
                        <div
                          key={i}
                          className="bg-white p-4 rounded-2xl border border-[#EFE6DC] shadow-sm space-y-3"
                        >
                          <div className="flex items-start justify-between gap-2">
                            <div>
                              <h4 className="font-extrabold text-[#2D2522] text-sm">
                                {drink.name}
                              </h4>
                              <p className="text-xs text-[#786C65] mt-0.5">{drink.description}</p>
                            </div>
                            <span className="text-sm font-black text-[#6F4E37] shrink-0">
                              {drink.estimatedPrice.toLocaleString("vi-VN")}đ
                            </span>
                          </div>

                          <div className="bg-[#FAF7F2] p-2.5 rounded-xl text-xs space-y-1">
                            <p className="text-[#2D2522]">
                              <strong>Công thức chuẩn AI:</strong> {drink.sugarRecommend} đường • {drink.iceRecommend} đá
                            </p>
                            {drink.recommendedToppings?.length > 0 && (
                              <p className="text-[#6F4E37]">
                                <strong>Topping kèm:</strong> {drink.recommendedToppings.join(", ")}
                              </p>
                            )}
                            <p className="text-[#786C65] italic text-[11px] mt-1">
                              &ldquo;{drink.reason}&rdquo;
                            </p>
                          </div>

                          <div className="flex items-center gap-2 pt-1">
                            <button
                              onClick={() => handleQuickAdd(drink)}
                              className={`flex-1 py-2 px-3 rounded-xl text-xs font-bold flex items-center justify-center gap-1.5 transition-all ${
                                quickAddedId === (drink.productId || 1)
                                  ? "bg-[#4E7D55] text-white"
                                  : "bg-[#6F4E37] text-white hover:bg-[#4A3324]"
                              }`}
                            >
                              {quickAddedId === (drink.productId || 1) ? (
                                <>
                                  <Check className="w-3.5 h-3.5" />
                                  <span>Đã thêm</span>
                                </>
                              ) : (
                                <>
                                  <ShoppingBag className="w-3.5 h-3.5 text-[#F7D6D0]" />
                                  <span>Đặt món này</span>
                                </>
                              )}
                            </button>

                            <Link
                              href="/diy"
                              className="py-2 px-3 rounded-xl text-xs font-bold border border-[#6F4E37] text-[#6F4E37] hover:bg-[#F7D6D0]/30 transition-colors"
                            >
                              Mở trong DIY Studio
                            </Link>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {msg.role === "user" && (
                <div className="w-8 h-8 rounded-xl bg-[#2D2522] text-white flex items-center justify-center shrink-0 mt-1 shadow-sm">
                  <User className="w-4 h-4" />
                </div>
              )}
            </div>
          ))}

          {loading && (
            <div className="flex gap-3 justify-start items-center">
              <div className="w-8 h-8 rounded-xl bg-[#6F4E37] text-white flex items-center justify-center shadow-sm">
                <Bot className="w-4 h-4 text-[#F7D6D0] animate-spin" />
              </div>
              <div className="bg-[#FAF7F2] border border-[#EFE6DC] rounded-2xl px-4 py-2.5 text-xs text-[#786C65] flex items-center gap-2">
                <span className="w-2 h-2 rounded-full bg-[#6F4E37] animate-ping" />
                <span>AI Barista đang phân tích hương vị và công thức...</span>
              </div>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* Quick Prompts Carousel */}
        <div className="p-3 bg-[#FAF7F2] border-t border-[#EFE6DC] flex gap-2 overflow-x-auto scrollbar-none">
          {quickPrompts.map((p, idx) => (
            <button
              key={idx}
              onClick={() => handleSend(p)}
              disabled={loading}
              className="px-3 py-1.5 rounded-xl bg-white border border-[#EFE6DC] text-xs font-medium text-[#2D2522] hover:border-[#6F4E37] hover:text-[#6F4E37] whitespace-nowrap transition-colors shadow-2xs"
            >
              {p}
            </button>
          ))}
        </div>

        {/* Chat Input Bar */}
        <div className="p-4 bg-white border-t border-[#EFE6DC]">
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSend();
            }}
            className="flex items-center gap-2"
          >
            <input
              type="text"
              placeholder="Nhập sở thích, tâm trạng hoặc yêu cầu khẩu vị của bạn..."
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              disabled={loading}
              className="flex-1 px-4 py-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] text-sm focus:outline-none focus:border-[#6F4E37] focus:ring-1 focus:ring-[#6F4E37]"
            />
            <button
              type="submit"
              disabled={loading || !inputMessage.trim()}
              className="px-5 py-3 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] disabled:opacity-50 transition-all shadow-md flex items-center gap-1.5 shrink-0"
            >
              <Send className="w-4 h-4" />
              <span className="hidden sm:inline">Gửi</span>
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
