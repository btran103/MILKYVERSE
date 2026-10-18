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
} from "lucide-react";

export default function AIBaristaPage() {
  const { addItem } = useCart();

  const [messages, setMessages] = useState<AIChatMessage[]>([
    {
      id: "welcome",
      role: "assistant",
      content:
        "Xin chào bạn! Mình là AI Barista của ngân hà MilkyVerse 🌟. Mình ở đây để giúp bạn tìm thấy thức uống hoàn hảo nhất hôm nay. Bạn đang cảm thấy thế nào, hay đang tìm kiếm hương vị ngọt ngào, đậm vị trà hay thanh mát trái cây?",
      isAiModelLive: false,
      timestamp: "2026-10-16T12:00:00Z",
    },
  ]);

  const [inputMessage, setInputMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState<number | undefined>(undefined);
  const [quickAddedId, setQuickAddedId] = useState<number | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, loading]);

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
      const res = await api.sendAIChat(textToSend.trim(), sessionId);
      setSessionId(res.sessionId);

      const assistantMsg: AIChatMessage = {
        id: "a_" + Date.now(),
        role: "assistant",
        content: res.reply,
        suggestedDrinks: (res.suggestedDrinks as unknown as SuggestedDrink[]) || [],
        isAiModelLive: res.isAiModelLive,
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
    "Trà sữa ít ngọt, béo nhẹ cho người ăn kiêng",
    "Món trà hoa quả nhiệt đới thanh mát giải khát",
    "Trà ô long đậm vị trà, hậu ngọt sâu",
    "Thức uống béo ngậy với lớp kem Cheese Foam",
  ];

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 space-y-6">
      {/* Header Banner */}
      <div className="bg-gradient-to-r from-[#FAF7F2] via-white to-[#FAF7F2] p-6 rounded-3xl border border-[#EFE6DC] shadow-sm flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-2xl bg-[#6F4E37] flex items-center justify-center text-white shadow-md">
            <Bot className="w-6 h-6 text-[#F7D6D0]" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-black text-[#2D2522]">AI Barista MilkyVerse</h1>
              <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#4E7D55] text-white">
                Sẵn sàng tư vấn
              </span>
            </div>
            <p className="text-xs text-[#786C65]">
              Chuyên gia tư vấn khẩu vị trà sữa thế hệ mới • Kết nối thời gian thực với thực đơn
            </p>
          </div>
        </div>

        <Link
          href="/diy"
          className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[#FAF7F2] border border-[#6F4E37] text-xs font-bold text-[#6F4E37] hover:bg-[#F7D6D0]/30 transition-all shrink-0"
        >
          <CupSoda className="w-3.5 h-3.5" />
          <span>Chuyển sang DIY Studio</span>
        </Link>
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
                {msg.role === "assistant" && msg.isAiModelLive !== undefined && (
                  <div className="mb-2 text-[10px] font-bold text-[#786C65] flex items-center gap-1">
                    <Sparkles className="w-3 h-3 text-[#C5A059]" />
                    <span>
                      {msg.isAiModelLive ? "Live Gemini AI Model" : "Local Sommelier Barista Engine"}
                    </span>
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
