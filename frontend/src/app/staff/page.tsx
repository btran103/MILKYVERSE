"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { OrderResponse } from "@/types";
import { useAuth } from "@/context/AuthContext";
import {
  ChefHat,
  CheckCircle,
  Search,
  Flame,
  UtensilsCrossed,
  CupSoda,
  Printer,
  Sparkles,
  Phone,
  BookOpen,
  ChevronLeft,
  ChevronRight,
  Clock,
} from "lucide-react";

interface RecipeInfo {
  title: string;
  category: string;
  yieldInfo: string;
  ingredients: string[];
  steps: { stepNumber: number; title: string; detail: string }[];
}

const SAMPLE_RECIPES: Record<string, RecipeInfo> = {
  pearl: {
    title: "Trân Châu Đen Đường Đen Nấu Mới",
    category: "Topping Signature",
    yieldInfo: "Cho 100g - 200g trân châu khô",
    ingredients: [
      "Trân châu đen khô: 200g",
      "Đường đen (hoặc đường nâu Hàn Quốc/đường thốt nốt): 100g – 150g",
      "Nước lọc: 1 lít (để luộc trân châu) + 150ml - 200ml (để nấu sốt đường đen)",
    ],
    steps: [
      {
        stepNumber: 1,
        title: "Luộc trân châu",
        detail:
          "1. Đun sôi 1 lít nước trong nồi lớn (lưu ý: cần dùng đủ lượng nước để trân châu không bị dính đáy).\n2. Khi nước sôi già, từ từ thả 200g trân châu khô vào nồi.\n3. Dùng muôi khuấy nhẹ theo một chiều để trân châu không bị dính vào đáy nồi.\n4. Khi trân châu nổi lên mặt nước, hãy giảm lửa vừa và nấu trong khoảng 20 - 30 phút (thỉnh thoảng mở nắp khuấy đều).",
      },
      {
        stepNumber: 2,
        title: "Ủ trân châu",
        detail:
          "1. Sau khi nấu đủ thời gian, tắt bếp, đậy kín nắp nồi và ủ trân châu thêm 15 - 30 phút. Đây là bước quan trọng giúp tâm hạt trân châu chín mềm hoàn toàn và không bị sượng.\n2. Vớt trân châu ra rổ, xả sơ qua với nước sạch hoặc nước ấm để giảm bớt lớp nhớt bên ngoài (hạn chế xả bằng nước đá lạnh để tránh làm trân châu bị cứng).",
      },
      {
        stepNumber: 3,
        title: "Nấu sốt đường đen và rim trân châu",
        detail:
          "1. Cho 100g đường đen và 150ml - 200ml nước vào một chiếc chảo hoặc nồi nhỏ.\n2. Đun với lửa nhỏ, khuấy đều cho đến khi đường tan hoàn toàn và nước đường bắt đầu sánh lại.\n3. Trút phần trân châu đã ráo nước vào chảo nước đường. Đun nhỏ lửa, đảo đều liên tục khoảng 5 - 10 phút để hạt trân châu thấm vị ngọt đậm đà và hỗn hợp sánh sệt lại thì tắt bếp.",
      },
    ],
  },
  spicynoodle: {
    title: "Mì Cay Hàn Quốc 7 Cấp Độ",
    category: "Món Ăn Nóng",
    yieldInfo: "Khẩu phần 1 tô tiêu chuẩn",
    ingredients: [
      "Mì sợi Hàn Quốc Nongshim: 1 gói (120g)",
      "Bò mỹ ba chỉ cuộn: 60g",
      "Tôm tươi & Mực tươi: mỗi loại 2 con",
      "Xúc xích & Cá viên: 1 cây xúc xích, 3 viên cá",
      "Nước dùng kim chi hầm xương: 550ml",
      "Bột ớt cấp độ ớt hiểm Hàn Quốc: theo yêu cầu order của khách (Cấp 1 đến Cấp 7)",
      "Nấm kim châm, bắp cải tím, súp lơ xanh",
    ],
    steps: [
      {
        stepNumber: 1,
        title: "Nấu sôi nước lèo kim chi",
        detail: "Cho 550ml nước dùng kim chi vào thố đất, đun sôi bùng trên bếp ga công nghiệp.",
      },
      {
        stepNumber: 2,
        title: "Thêm ớt cấp độ theo order",
        detail: "Cân chuẩn thìa ớt theo đúng cấp độ khách đặt (Cấp 1: 3g, Cấp 2: 6g, Cấp 3: 10g... Cấp 7: 25g). Khuấy tan.",
      },
      {
        stepNumber: 3,
        title: "Trụng mì & topping",
        detail: "Cho mì Nongshim, bò cuộn, tôm, cá viên, nấm kim châm vào thố nấu trong 3 phút 30 giây. Rắc hành boa-rô, bắc thố đất ra khay gỗ phục vụ nóng sôi sùng sục.",
      },
    ],
  },
  milktea: {
    title: "Cốt Trà Sữa Oolong Rang Khói Chuẩn Gu",
    category: "Trà Sữa Thủ Công",
    yieldInfo: "Pha 1 ly Size M/L",
    ingredients: [
      "Cốt trà Ô Long rang khói ủ lạnh: 120ml (Size M) / 160ml (Size L)",
      "Sữa tươi thanh trùng Dalat Milk: 60ml",
      "Bột sữa thực vật cao cấp: 25g",
      "Nước đường mía tự nhiên: 15ml (50% đường) / 25ml (100% đường)",
      "Đá viên tinh khiết: 150g",
    ],
    steps: [
      {
        stepNumber: 1,
        title: "Khuấy tan bột sữa",
        detail: "Cho cốt trà nóng ấm vào bình lắc shaker, thêm 25g bột sữa, khuấy tan đều bằng phới lồng.",
      },
      {
        stepNumber: 2,
        title: "Định lượng đường & đá",
        detail: "Thêm sữa tươi và nước đường đúng tỉ lệ order (30%, 50%, 70%, 100%). Thêm đá viên tới vạch 500ml.",
      },
      {
        stepNumber: 3,
        title: "Shake lạnh & rót ly",
        detail: "Lắc đều shaker 12-15 lần cho tạo bọt mịn và hòa quyện độ béo. Cho topping đáy ly rồi rót trà sữa lên trên, dập màng nắp.",
      },
    ],
  },
};

export default function StaffOrderBoardPage() {
  const router = useRouter();
  const { user, isStaff, isLoading } = useAuth();

  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<string>("ALL");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedTicket, setSelectedTicket] = useState<OrderResponse | null>(null);
  const [activeRecipe, setActiveRecipe] = useState<RecipeInfo | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  // Pagination for kitchen orders
  const ITEMS_PER_PAGE = 6;
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => {
    if (!isLoading && (!user || !isStaff)) {
      router.push("/auth/login?redirect=/staff");
    }
  }, [user, isStaff, isLoading, router]);

  const loadData = async () => {
    setLoading(true);
    try {
      const orderList = await api.getOrders();
      setOrders(orderList || []);
    } catch (err) {
      console.error("Lỗi khi tải đơn bếp:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user && isStaff) {
      loadData();
      const interval = setInterval(loadData, 10000);
      return () => clearInterval(interval);
    }
  }, [user, isStaff]);

  const handleUpdateStatus = async (orderId: number, status: string, note: string) => {
    setActionLoading(true);
    try {
      await api.updateOrderStatus(orderId, status, undefined, note);
      await loadData();
      if (selectedTicket && selectedTicket.id === orderId) {
        setSelectedTicket(null);
      }
    } catch (err) {
      console.error("Lỗi cập nhật trạng thái đơn:", err);
    } finally {
      setActionLoading(false);
    }
  };

  // Kitchen only cares about uncompleted food/drink prep orders
  const kitchenOrders = orders.filter((o) => {
    return o.status === "Pending" || o.status === "Confirmed" || o.status === "Preparing" || o.status === "ReadyForPickup";
  });

  const filteredOrders = kitchenOrders.filter((o) => {
    if (activeTab === "PENDING" && o.status !== "Pending") return false;
    if (activeTab === "CONFIRMED" && o.status !== "Confirmed") return false;
    if (activeTab === "PREPARING" && o.status !== "Preparing") return false;
    if (activeTab === "READY" && o.status !== "ReadyForPickup") return false;

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      const matchCode = o.orderCode?.toLowerCase().includes(q);
      const matchName = o.customerName?.toLowerCase().includes(q);
      const matchPhone = o.customerPhone?.includes(q);
      const matchItems = o.items.some((i) => i.productName.toLowerCase().includes(q));
      return matchCode || matchName || matchPhone || matchItems;
    }
    return true;
  });

  const totalPages = Math.ceil(filteredOrders.length / ITEMS_PER_PAGE) || 1;
  const paginatedOrders = filteredOrders.slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Pending":
        return <span className="bg-amber-100 text-amber-800 text-[11px] font-bold px-2.5 py-1 rounded-full">Chờ xác nhận</span>;
      case "Confirmed":
        return <span className="bg-blue-100 text-blue-800 text-[11px] font-bold px-2.5 py-1 rounded-full">Đã nhận đơn</span>;
      case "Preparing":
        return <span className="bg-orange-100 text-orange-800 text-[11px] font-bold px-2.5 py-1 rounded-full animate-pulse">Đang nấu / pha</span>;
      case "ReadyForPickup":
        return <span className="bg-purple-100 text-purple-800 text-[11px] font-bold px-2.5 py-1 rounded-full">Xong món (Bàn giao)</span>;
      default:
        return <span className="bg-gray-100 text-gray-800 text-[11px] font-bold px-2.5 py-1 rounded-full">{status}</span>;
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* 1. Header & Kitchen Role Separation Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
            <ChefHat className="w-4 h-4 text-orange-600" />
            <span>Khu Vực Bếp Nấu & Pha Chế Barista (Tách Biệt Với Quầy)</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522] mt-1">
            Bảng Chế Biến Món & Phiếu Bếp
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Chỉ hiển thị cấp độ cay cho Mì Cay. Bếp nhận đơn, xem công thức nguyên liệu chuẩn bị và xác nhận xong món bàn giao.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {/* Quick Recipe Library Shortcuts */}
          <button
            onClick={() => setActiveRecipe(SAMPLE_RECIPES.pearl)}
            className="px-3.5 py-2 rounded-2xl bg-amber-50 border border-amber-200 text-amber-900 text-xs font-bold hover:bg-amber-100 transition-colors flex items-center gap-1.5 shadow-sm"
          >
            <BookOpen className="w-3.5 h-3.5 text-amber-700" />
            <span>Công thức Trân Châu Đen</span>
          </button>

          <button
            onClick={() => setActiveRecipe(SAMPLE_RECIPES.spicynoodle)}
            className="px-3.5 py-2 rounded-2xl bg-red-50 border border-red-200 text-red-900 text-xs font-bold hover:bg-red-100 transition-colors flex items-center gap-1.5 shadow-sm"
          >
            <Flame className="w-3.5 h-3.5 text-red-600" />
            <span>Công thức Mì Cay</span>
          </button>

          <Link
            href="/counter"
            className="px-3.5 py-2 rounded-2xl bg-white border border-[#EFE6DC] text-xs font-bold text-[#6F4E37] hover:bg-[#FAF7F2] transition-colors"
          >
            <span>Sang Phục Vụ Quầy POS &rarr;</span>
          </Link>
        </div>
      </div>

      {/* 2. Filter Tabs & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#EFE6DC] pb-3">
        <div className="flex items-center gap-2 overflow-x-auto scrollbar-none">
          {[
            { key: "ALL", label: `Tất cả (${kitchenOrders.length})` },
            { key: "PENDING", label: `Chờ nhận (${kitchenOrders.filter((o) => o.status === "Pending").length})` },
            { key: "CONFIRMED", label: `Đã nhận (${kitchenOrders.filter((o) => o.status === "Confirmed").length})` },
            { key: "PREPARING", label: `Đang nấu/pha (${kitchenOrders.filter((o) => o.status === "Preparing").length})` },
            { key: "READY", label: `Đã xong (${kitchenOrders.filter((o) => o.status === "ReadyForPickup").length})` },
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => {
                setActiveTab(tab.key);
                setCurrentPage(1);
              }}
              className={`px-4 py-2 rounded-2xl text-xs font-bold whitespace-nowrap transition-all shadow-sm ${
                activeTab === tab.key
                  ? "bg-[#6F4E37] text-white"
                  : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="relative w-full sm:w-72">
          <input
            type="text"
            placeholder="Tìm mã đơn, tên món, khách..."
            value={searchQuery}
            onChange={(e) => {
              setSearchQuery(e.target.value);
              setCurrentPage(1);
            }}
            className="w-full pl-9 pr-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
          />
          <Search className="w-4 h-4 text-[#786C65] absolute left-3 top-2.5" />
        </div>
      </div>

      {/* 3. Kitchen Orders Grid */}
      {filteredOrders.length === 0 ? (
        <div className="bg-white rounded-3xl p-12 text-center border border-[#EFE6DC] space-y-3">
          <ChefHat className="w-12 h-12 text-[#786C65] mx-auto opacity-50" />
          <h3 className="font-extrabold text-base text-[#2D2522]">Không có đơn hàng nào cần nấu trong mục này</h3>
          <p className="text-xs text-[#786C65]">Đơn hàng mới từ khách hoặc quầy sẽ tự động hiển thị tại đây.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {paginatedOrders.map((o) => {
            return (
              <div
                key={o.id}
                className="bg-white rounded-3xl p-5 border border-[#EFE6DC] shadow-sm hover:shadow-md transition-all flex flex-col justify-between space-y-4"
              >
                {/* Header card */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="font-mono font-black text-sm text-[#6F4E37] bg-[#FAF7F2] px-2.5 py-1 rounded-lg">
                      #{o.orderCode}
                    </span>
                    {getStatusBadge(o.status)}
                  </div>

                  <div className="flex items-center justify-between text-xs text-[#786C65]">
                    <span className="font-extrabold text-[#2D2522]">Khách: {o.customerName}</span>
                    <span>{new Date(o.createdAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}</span>
                  </div>

                  {o.note && (
                    <div className="p-2 rounded-xl bg-amber-50 text-amber-900 text-[11px] font-semibold border border-amber-200">
                      Ghi chú khách: {o.note}
                    </div>
                  )}

                  <div className="h-px bg-[#FAF7F2]" />

                  {/* Order Items list */}
                  <div className="space-y-2.5 max-h-56 overflow-y-auto pr-1">
                    {o.items.map((item, idx) => {
                      const isSpicyNoodle =
                        (item.productName.toLowerCase().includes("mì cay") ||
                          item.productName.toLowerCase().includes("mi cay")) &&
                        item.itemType !== "DRINK";

                      const isMilkTea = item.itemType === "DRINK";

                      return (
                        <div key={idx} className="p-3 rounded-2xl bg-[#FAF7F2] text-xs space-y-1.5">
                          <div className="flex items-center justify-between font-bold text-[#2D2522]">
                            <span className="flex items-center gap-1.5">
                              {item.itemType === "FOOD" ? (
                                <UtensilsCrossed className="w-3.5 h-3.5 text-orange-600" />
                              ) : (
                                <CupSoda className="w-3.5 h-3.5 text-[#6F4E37]" />
                              )}
                              <span>{item.productName}</span>
                            </span>
                            <span className="text-[#6F4E37] font-black text-sm">x{item.quantity}</span>
                          </div>

                          {/* Food specs: ONLY show spicy level for Mì cay! */}
                          {item.itemType === "FOOD" && (
                            <div className="text-[11px] text-[#786C65] space-y-0.5">
                              {item.portionSize && <p className="font-semibold text-[#6F4E37]">Khẩu phần: {item.portionSize}</p>}
                              {isSpicyNoodle && item.spicyLevel && (
                                <p className="flex items-center gap-1 text-red-600 font-bold bg-red-50 border border-red-200 px-2 py-0.5 rounded-lg w-fit">
                                  <Flame className="w-3 h-3" /> MỨC CAY: {item.spicyLevel}
                                </p>
                              )}
                              {item.dippingSauce && <p className="text-gray-700">Chấm: {item.dippingSauce}</p>}
                            </div>
                          )}

                          {/* Drink specs: Absolute NO spicy level for drinks */}
                          {isMilkTea && (
                            <div className="text-[11px] text-[#786C65] font-medium">
                              Size {item.size} • {item.sugarLevel || "50%"} đường • {item.iceLevel || "70%"} đá
                            </div>
                          )}

                          {item.toppings && item.toppings.length > 0 && (
                            <div className="text-[10px] text-[#6F4E37] font-bold">
                              Topping: {item.toppings.map((t) => `${t.toppingName} (x${t.quantity})`).join(", ")}
                            </div>
                          )}

                          {/* Recipe Quick Button for this dish */}
                          <div className="pt-1 flex items-center justify-end">
                            <button
                              type="button"
                              onClick={() => {
                                if (isSpicyNoodle) {
                                  setActiveRecipe(SAMPLE_RECIPES.spicynoodle);
                                } else if (item.productName.toLowerCase().includes("trân châu") || item.toppings?.some(t => t.toppingName.toLowerCase().includes("trân châu"))) {
                                  setActiveRecipe(SAMPLE_RECIPES.pearl);
                                } else {
                                  setActiveRecipe(SAMPLE_RECIPES.milktea);
                                }
                              }}
                              className="text-[10px] text-[#6F4E37] hover:underline flex items-center gap-1 font-bold"
                            >
                              <BookOpen className="w-3 h-3" />
                              <span>Xem nguyên liệu nấu món này</span>
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {/* Workflow Buttons: Kitchen Only! */}
                <div className="space-y-2 pt-2 border-t border-[#FAF7F2]">
                  <div className="grid grid-cols-2 gap-2">
                    <button
                      onClick={() => setSelectedTicket(o)}
                      className="py-2.5 px-3 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#6F4E37] hover:bg-[#FAF7F2] flex items-center justify-center gap-1"
                    >
                      <Printer className="w-3.5 h-3.5" />
                      <span>Phiếu Bếp</span>
                    </button>

                    {o.status === "Pending" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "Confirmed", "Bếp đã tiếp nhận đơn")}
                        disabled={actionLoading}
                        className="py-2.5 px-3 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm flex items-center justify-center gap-1"
                      >
                        <CheckCircle className="w-3.5 h-3.5" />
                        <span>Nhận đơn</span>
                      </button>
                    )}

                    {o.status === "Confirmed" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "Preparing", "Bếp bắt đầu chế biến / pha chế")}
                        disabled={actionLoading}
                        className="py-2.5 px-3 rounded-xl bg-orange-600 text-white text-xs font-bold hover:bg-orange-700 shadow-sm flex items-center justify-center gap-1"
                      >
                        <ChefHat className="w-3.5 h-3.5" />
                        <span>Bắt đầu nấu</span>
                      </button>
                    )}

                    {o.status === "Preparing" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "ReadyForPickup", "Đã nấu xong - Sẵn sàng giao")}
                        disabled={actionLoading}
                        className="py-2.5 px-3 rounded-xl bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 shadow-sm flex items-center justify-center gap-1"
                      >
                        <CheckCircle className="w-3.5 h-3.5" />
                        <span>Xong món (Bàn giao)</span>
                      </button>
                    )}

                    {o.status === "ReadyForPickup" && (
                      <div className="py-2.5 px-3 rounded-xl bg-purple-50 text-purple-800 text-xs font-bold text-center flex items-center justify-center gap-1 border border-purple-200">
                        <Sparkles className="w-3.5 h-3.5" />
                        <span>Đã giao Quầy</span>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* 4. Kitchen Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between pt-4 border-t border-[#EFE6DC]">
          <span className="text-xs text-[#786C65] font-semibold">
            Hiển thị trang {currentPage} / {totalPages} ({filteredOrders.length} đơn bếp)
          </span>
          <div className="flex items-center gap-2">
            <button
              disabled={currentPage <= 1}
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              className="p-2 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] disabled:opacity-40"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((pg) => (
              <button
                key={pg}
                onClick={() => setCurrentPage(pg)}
                className={`w-8 h-8 rounded-xl text-xs font-bold transition-all ${
                  currentPage === pg
                    ? "bg-[#6F4E37] text-white"
                    : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
                }`}
              >
                {pg}
              </button>
            ))}
            <button
              disabled={currentPage >= totalPages}
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              className="p-2 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] disabled:opacity-40"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* 5. Modal Kitchen Ticket View (Phiếu in bếp không hiện cay cho trà sữa) */}
      {selectedTicket && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full border border-[#EFE6DC] shadow-2xl space-y-4">
            <div className="flex items-center justify-between border-b pb-3">
              <div className="flex items-center gap-2">
                <ChefHat className="w-6 h-6 text-[#6F4E37]" />
                <h3 className="font-black text-base text-[#2D2522]">PHIẾU IN ORDER BẾP NẤU</h3>
              </div>
              <button onClick={() => setSelectedTicket(null)} className="p-1 hover:opacity-70 text-xs font-bold">
                Đóng [X]
              </button>
            </div>

            <div className="space-y-3 font-mono text-xs">
              <div className="text-center pb-2 border-b border-dashed">
                <h4 className="font-black text-sm">MILKYVERSE GALAXY STORE</h4>
                <p className="text-[10px] text-gray-500">Mã đơn: #{selectedTicket.orderCode}</p>
                <p className="text-[10px] text-gray-500">{new Date(selectedTicket.createdAt).toLocaleString("vi-VN")}</p>
                <p className="font-bold text-xs mt-1">Khách: {selectedTicket.customerName}</p>
              </div>

              <div className="space-y-2.5 py-2 border-b border-dashed">
                {selectedTicket.items.map((it, idx) => {
                  const isSpicy =
                    (it.productName.toLowerCase().includes("mì cay") ||
                      it.productName.toLowerCase().includes("mi cay")) &&
                    it.itemType !== "DRINK";

                  return (
                    <div key={idx} className="space-y-0.5">
                      <div className="flex justify-between font-black text-sm">
                        <span>{idx + 1}. {it.productName}</span>
                        <span>x{it.quantity}</span>
                      </div>
                      {it.portionSize && <p className="text-xs text-gray-600 pl-3">Khẩu phần: {it.portionSize}</p>}
                      {/* CHỈ HIỆN CẤP ĐỘ CAY KHI LÀ MÌ CAY */}
                      {isSpicy && it.spicyLevel && (
                        <p className="text-xs text-red-600 font-bold pl-3">*** MỨC CAY: {it.spicyLevel} ***</p>
                      )}
                      {it.dippingSauce && <p className="text-xs text-gray-700 pl-3">Nước chấm: {it.dippingSauce}</p>}
                      {it.size && it.itemType === "DRINK" && (
                        <p className="text-xs text-gray-600 pl-3">Size {it.size} | {it.sugarLevel || "50%"} ĐƯỜNG | {it.iceLevel || "70%"} ĐÁ</p>
                      )}
                      {it.toppings && it.toppings.length > 0 && (
                        <p className="text-xs font-bold text-amber-900 pl-3">
                          Topping: {it.toppings.map((t) => t.toppingName).join(", ")}
                        </p>
                      )}
                      {it.customNotes && <p className="text-xs italic pl-3 text-gray-600">Lưu ý: {it.customNotes}</p>}
                    </div>
                  );
                })}
              </div>

              {selectedTicket.note && (
                <div className="py-2 border-b border-dashed font-bold text-amber-900">
                  Ghi chú khách: {selectedTicket.note}
                </div>
              )}
            </div>

            <div className="pt-2">
              <button
                onClick={() => window.print()}
                className="w-full py-3 rounded-2xl bg-[#6F4E37] text-white font-black text-xs hover:bg-[#4A3324] shadow-md flex items-center justify-center gap-2"
              >
                <Printer className="w-4 h-4" />
                <span>In Phiếu Order Bếp</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 6. Modal Xem Công Thức & Danh Sách Nguyên Liệu Nấu */}
      {activeRecipe && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-xl w-full border border-[#EFE6DC] shadow-2xl p-6 sm:p-8 space-y-6 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-3 border-b">
              <div className="flex items-center gap-2">
                <BookOpen className="w-5 h-5 text-[#6F4E37]" />
                <div>
                  <h3 className="font-black text-base text-[#2D2522]">{activeRecipe.title}</h3>
                  <span className="text-[10px] font-bold text-[#6F4E37] bg-[#FAF7F2] px-2 py-0.5 rounded-full">
                    {activeRecipe.category} • {activeRecipe.yieldInfo}
                  </span>
                </div>
              </div>
              <button
                onClick={() => setActiveRecipe(null)}
                className="text-gray-400 hover:text-gray-600 font-bold text-xs"
              >
                Đóng [X]
              </button>
            </div>

            {/* Nguyên liệu chuẩn bị */}
            <div className="bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] space-y-2">
              <h4 className="font-extrabold text-xs text-[#6F4E37] uppercase tracking-wider">
                Nguyên Liệu Chuẩn Bị ({activeRecipe.yieldInfo})
              </h4>
              <ul className="space-y-1 text-xs text-[#2D2522]">
                {activeRecipe.ingredients.map((ing, i) => (
                  <li key={i} className="flex items-start gap-2">
                    <span className="text-[#6F4E37] font-bold">•</span>
                    <span>{ing}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* Các bước thực hiện */}
            <div className="space-y-4">
              <h4 className="font-extrabold text-xs text-[#2D2522] uppercase tracking-wider">
                Các Bước Thực Hiện Nấu Chuẩn Vị
              </h4>
              <div className="space-y-3">
                {activeRecipe.steps.map((st) => (
                  <div key={st.stepNumber} className="p-4 rounded-2xl border border-[#EFE6DC] bg-white space-y-1.5">
                    <div className="flex items-center gap-2 font-black text-xs text-[#6F4E37]">
                      <span className="w-5 h-5 rounded-full bg-[#6F4E37] text-white flex items-center justify-center text-[10px]">
                        {st.stepNumber}
                      </span>
                      <span>{st.title}</span>
                    </div>
                    <p className="text-xs text-[#786C65] whitespace-pre-line leading-relaxed pl-7">
                      {st.detail}
                    </p>
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
