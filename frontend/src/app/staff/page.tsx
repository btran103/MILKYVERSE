"use client";

import React, { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { OrderResponse, User } from "@/types";
import { useAuth } from "@/context/AuthContext";
import {
  ChefHat,
  CheckCircle,
  Truck,
  RotateCcw,
  Search,
  Flame,
  UtensilsCrossed,
  CupSoda,
  Printer,
  Sparkles,
  Phone,
  MapPin,
} from "lucide-react";

export default function StaffOrderBoardPage() {
  const router = useRouter();
  const { user, isStaff, isLoading } = useAuth();

  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [shippers, setShippers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<string>("ALL");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedTicket, setSelectedTicket] = useState<OrderResponse | null>(null);

  // Assign shipper modal
  const [assigningOrder, setAssigningOrder] = useState<OrderResponse | null>(null);
  const [selectedShipperId, setSelectedShipperId] = useState<number | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (!isLoading && (!user || !isStaff)) {
      router.push("/auth/login?redirect=/staff");
    }
  }, [user, isStaff, isLoading, router]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [orderList, shipperList] = await Promise.all([
        api.getOrders(),
        api.getShippers(),
      ]);
      setOrders(orderList || []);
      setShippers(shipperList || []);
      if (shipperList && shipperList.length > 0) {
        setSelectedShipperId(shipperList[0].id);
      }
    } catch (err) {
      console.error("Lỗi khi tải đơn bếp:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user && isStaff) {
      loadData();
      const interval = setInterval(loadData, 10000); // 10s auto-refresh for POS
      return () => clearInterval(interval);
    }
  }, [user, isStaff]);

  const handleUpdateStatus = async (orderId: number, nextStatus: string, note?: string) => {
    setActionLoading(true);
    try {
      await api.updateOrderStatus(orderId, nextStatus, undefined, note);
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Cập nhật đơn thất bại.";
      alert(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleAssignShipper = async () => {
    if (!assigningOrder || !selectedShipperId) return;
    setActionLoading(true);
    try {
      await api.assignShipper({
        orderId: assigningOrder.id,
        shipperId: selectedShipperId,
        notes: "Bàn giao từ quầy bếp",
      });
      setAssigningOrder(null);
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Phân công shipper thất bại.";
      alert(msg);
    } finally {
      setActionLoading(false);
    }
  };

  if (isLoading || !user || !isStaff) {
    return (
      <div className="max-w-md mx-auto py-20 px-4 text-center space-y-4">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-sm font-bold text-[#786C65]">Đang kiểm tra quyền nhân viên Quầy & Bếp...</p>
      </div>
    );
  }

  const filteredOrders = orders.filter((o) => {
    if (activeTab === "PENDING" && o.status !== "Pending") return false;
    if (activeTab === "PREPARING" && o.status !== "Confirmed" && o.status !== "Preparing") return false;
    if (activeTab === "READY" && o.status !== "ReadyForPickup" && o.status !== "Delivering") return false;
    if (activeTab === "COMPLETED" && o.status !== "Completed") return false;

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      return (
        o.orderCode.toLowerCase().includes(q) ||
        o.customerName.toLowerCase().includes(q) ||
        o.customerPhone.includes(q)
      );
    }
    return true;
  });

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Pending":
        return <span className="px-2.5 py-1 rounded-full bg-amber-100 text-amber-900 font-black text-xs animate-pulse">Chờ xác nhận</span>;
      case "Confirmed":
        return <span className="px-2.5 py-1 rounded-full bg-blue-100 text-blue-900 font-bold text-xs">Đã xác nhận</span>;
      case "Preparing":
        return <span className="px-2.5 py-1 rounded-full bg-orange-100 text-orange-900 font-black text-xs">Đang nấu / Pha chế</span>;
      case "ReadyForPickup":
        return <span className="px-2.5 py-1 rounded-full bg-purple-100 text-purple-900 font-bold text-xs">Chờ shipper lấy</span>;
      case "Delivering":
        return <span className="px-2.5 py-1 rounded-full bg-cyan-100 text-cyan-900 font-bold text-xs">Shipper đang giao</span>;
      case "Completed":
        return <span className="px-2.5 py-1 rounded-full bg-emerald-100 text-emerald-900 font-bold text-xs">Đã hoàn tất</span>;
      case "Cancelled":
        return <span className="px-2.5 py-1 rounded-full bg-red-100 text-red-900 font-bold text-xs">Đã hủy</span>;
      default:
        return <span className="px-2.5 py-1 rounded-full bg-gray-100 text-gray-800 font-bold text-xs">{status}</span>;
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* Top Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-[#6F4E37] uppercase">
            <ChefHat className="w-4 h-4 text-[#C5A059]" />
            <span>KITCHEN & POS ORDER BOARD</span>
          </div>
          <h1 className="text-3xl font-black text-[#2D2522] mt-1">
            Bảng Điều Phối Bếp & Quầy Bán Hàng
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Tiếp nhận đơn tức thời, hiển thị chi tiết mức cay, nước chấm, định lượng và phân công shipper
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadData}
            className="p-2.5 rounded-2xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] shadow-sm transition-all flex items-center gap-2 text-xs font-bold"
          >
            <RotateCcw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
            <span>Làm mới</span>
          </button>

          {user.role === "Manager" || user.role === "Admin" ? (
            <Link
              href="/admin"
              className="px-4 py-2.5 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all"
            >
              Vào Admin Portal
            </Link>
          ) : null}
        </div>
      </div>

      {/* Tabs & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
          {[
            { key: "ALL", label: `Tất cả (${orders.length})` },
            { key: "PENDING", label: `Đơn mới (${orders.filter((o) => o.status === "Pending").length})` },
            { key: "PREPARING", label: `Đang nấu/pha (${orders.filter((o) => o.status === "Confirmed" || o.status === "Preparing").length})` },
            { key: "READY", label: `Chờ/Đang giao (${orders.filter((o) => o.status === "ReadyForPickup" || o.status === "Delivering").length})` },
            { key: "COMPLETED", label: `Hoàn tất (${orders.filter((o) => o.status === "Completed").length})` },
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
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
            placeholder="Tìm mã đơn, tên, số điện thoại..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
          />
          <Search className="w-4 h-4 text-[#786C65] absolute left-3 top-2.5" />
        </div>
      </div>

      {/* Orders Grid */}
      {filteredOrders.length === 0 ? (
        <div className="bg-white rounded-3xl p-12 text-center border border-[#EFE6DC] space-y-3">
          <ChefHat className="w-12 h-12 text-[#786C65] mx-auto opacity-50" />
          <h3 className="font-extrabold text-base text-[#2D2522]">Không có đơn hàng nào trong mục này</h3>
          <p className="text-xs text-[#786C65]">Đơn hàng mới từ khách sẽ tự động hiển thị tại đây.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredOrders.map((o) => {
            const hasFood = o.items.some((i) => i.itemType === "FOOD" || i.itemType === "COMBO");
            const hasDrink = o.items.some((i) => i.itemType === "DRINK" || i.itemType === "COMBO");

            return (
              <div
                key={o.id}
                className="bg-white rounded-3xl p-5 border border-[#EFE6DC] shadow-sm hover:shadow-md transition-all flex flex-col justify-between space-y-4"
              >
                {/* Header card */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-1.5 flex-wrap">
                      <span className="font-mono font-black text-sm text-[#6F4E37] bg-[#FAF7F2] px-2.5 py-1 rounded-lg">
                        {o.orderCode}
                      </span>
                      {hasFood && (
                        <span className="text-[10px] bg-amber-50 text-amber-800 border border-amber-200 font-bold px-2 py-0.5 rounded-md">
                          Bếp Ăn
                        </span>
                      )}
                      {hasDrink && (
                        <span className="text-[10px] bg-blue-50 text-blue-800 border border-blue-200 font-bold px-2 py-0.5 rounded-md">
                          Quầy Pha
                        </span>
                      )}
                    </div>
                    {getStatusBadge(o.status)}
                  </div>

                  <div className="flex items-center justify-between text-xs text-[#786C65]">
                    <span className="font-extrabold text-[#2D2522]">{o.customerName}</span>
                    <span>{new Date(o.createdAt).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}</span>
                  </div>

                  <div className="flex items-center gap-1.5 text-xs text-[#786C65]">
                    <Phone className="w-3.5 h-3.5" />
                    <span>{o.customerPhone}</span>
                  </div>

                  <div className="flex items-center gap-1.5 text-xs text-[#786C65] line-clamp-1">
                    <MapPin className="w-3.5 h-3.5 shrink-0" />
                    <span className="truncate">{o.shippingAddress}</span>
                  </div>

                  {o.note && (
                    <div className="p-2 rounded-xl bg-amber-50 text-amber-900 text-[11px] font-semibold border border-amber-200">
                      Ghi chú: {o.note}
                    </div>
                  )}

                  <div className="h-px bg-[#FAF7F2]" />

                  {/* Order Items list */}
                  <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                    {o.items.map((item, idx) => (
                      <div key={idx} className="p-2.5 rounded-2xl bg-[#FAF7F2] text-xs space-y-1">
                        <div className="flex items-center justify-between font-bold text-[#2D2522]">
                          <span className="flex items-center gap-1.5">
                            {item.itemType === "FOOD" ? (
                              <UtensilsCrossed className="w-3.5 h-3.5 text-orange-600" />
                            ) : item.itemType === "COMBO" ? (
                              <Sparkles className="w-3.5 h-3.5 text-purple-600" />
                            ) : (
                              <CupSoda className="w-3.5 h-3.5 text-[#6F4E37]" />
                            )}
                            {item.productName}
                          </span>
                          <span className="text-[#6F4E37] font-black">x{item.quantity}</span>
                        </div>

                        {/* Food specs */}
                        {(item.itemType === "FOOD" || item.itemType === "COMBO") && (
                          <div className="text-[11px] text-[#786C65] space-y-0.5">
                            {item.portionSize && <p className="font-semibold text-[#6F4E37]">Khẩu phần: {item.portionSize}</p>}
                            {item.spicyLevel && (
                              <p className="flex items-center gap-1 text-red-600 font-bold">
                                <Flame className="w-3 h-3" /> Mức cay: {item.spicyLevel}
                              </p>
                            )}
                            {item.dippingSauce && <p className="text-gray-700">Chấm: {item.dippingSauce}</p>}
                          </div>
                        )}

                        {/* Drink specs */}
                        {item.itemType === "DRINK" && (
                          <div className="text-[11px] text-[#786C65]">
                            Size {item.size} • {item.sugarLevel} đường • {item.iceLevel} đá
                          </div>
                        )}

                        {item.toppings && item.toppings.length > 0 && (
                          <div className="text-[10px] text-[#6F4E37] font-bold">
                            Topping: {item.toppings.map((t) => `${t.toppingName} (x${t.quantity})`).join(", ")}
                          </div>
                        )}

                        {item.customNotes && (
                          <p className="text-[10px] text-gray-600 italic">&ldquo;{item.customNotes}&rdquo;</p>
                        )}
                      </div>
                    ))}
                  </div>

                  {/* Total & Shipper Info */}
                  <div className="pt-2 flex items-center justify-between text-xs">
                    <span className="text-[#786C65]">Tổng thu ({o.paymentMethod}):</span>
                    <span className="font-black text-sm text-[#6F4E37]">{o.totalAmount.toLocaleString("vi-VN")}đ</span>
                  </div>

                  {o.shipperName && (
                    <div className="p-2 rounded-xl bg-emerald-50 text-emerald-800 text-[11px] font-bold flex items-center justify-between">
                      <span>Shipper: {o.shipperName}</span>
                      <span>{o.shipperPhone}</span>
                    </div>
                  )}
                </div>

                {/* Workflow Buttons */}
                <div className="space-y-2 pt-2 border-t border-[#FAF7F2]">
                  <div className="grid grid-cols-2 gap-2">
                    <button
                      onClick={() => setSelectedTicket(o)}
                      className="py-2 px-3 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#6F4E37] hover:bg-[#FAF7F2] flex items-center justify-center gap-1"
                    >
                      <Printer className="w-3.5 h-3.5" />
                      <span>Phiếu Bếp</span>
                    </button>

                    {o.status === "Pending" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "Confirmed", "Quầy đã xác nhận đơn")}
                        disabled={actionLoading}
                        className="py-2 px-3 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm flex items-center justify-center gap-1"
                      >
                        <CheckCircle className="w-3.5 h-3.5" />
                        <span>Xác nhận</span>
                      </button>
                    )}

                    {o.status === "Confirmed" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "Preparing", "Bếp bắt đầu chế biến")}
                        disabled={actionLoading}
                        className="py-2 px-3 rounded-xl bg-orange-600 text-white text-xs font-bold hover:bg-orange-700 shadow-sm flex items-center justify-center gap-1"
                      >
                        <ChefHat className="w-3.5 h-3.5" />
                        <span>Nấu / Pha chế</span>
                      </button>
                    )}

                    {o.status === "Preparing" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "ReadyForPickup", "Món ăn & trà sữa đã sẵn sàng")}
                        disabled={actionLoading}
                        className="py-2 px-3 rounded-xl bg-purple-600 text-white text-xs font-bold hover:bg-purple-700 shadow-sm flex items-center justify-center gap-1"
                      >
                        <Sparkles className="w-3.5 h-3.5" />
                        <span>Đã làm xong</span>
                      </button>
                    )}

                    {o.status === "ReadyForPickup" && (
                      <button
                        onClick={() => setAssigningOrder(o)}
                        disabled={actionLoading}
                        className="py-2 px-3 rounded-xl bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 shadow-sm flex items-center justify-center gap-1"
                      >
                        <Truck className="w-3.5 h-3.5" />
                        <span>Giao Shipper</span>
                      </button>
                    )}

                    {o.status === "Delivering" && (
                      <button
                        onClick={() => handleUpdateStatus(o.id, "Completed", "Khách đã nhận hàng tại quầy")}
                        disabled={actionLoading}
                        className="py-2 px-3 rounded-xl bg-emerald-700 text-white text-xs font-bold hover:bg-emerald-800 shadow-sm flex items-center justify-center gap-1"
                      >
                        <CheckCircle className="w-3.5 h-3.5" />
                        <span>Đã giao xong</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal Kitchen Ticket View */}
      {selectedTicket && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full border border-[#EFE6DC] shadow-2xl space-y-4 animate-in fade-in zoom-in duration-150">
            <div className="flex items-center justify-between border-b pb-3">
              <div className="flex items-center gap-2">
                <ChefHat className="w-6 h-6 text-[#6F4E37]" />
                <h3 className="font-black text-base text-[#2D2522]">PHIẾU IN ORDER BẾP & QUẦY</h3>
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
                <p className="font-bold text-xs mt-1">Khách: {selectedTicket.customerName} - {selectedTicket.customerPhone}</p>
              </div>

              <div className="space-y-2 py-2 border-b border-dashed">
                {selectedTicket.items.map((it, idx) => (
                  <div key={idx} className="space-y-0.5">
                    <div className="flex justify-between font-black text-sm">
                      <span>{idx + 1}. {it.productName}</span>
                      <span>x{it.quantity}</span>
                    </div>
                    {it.portionSize && <p className="text-xs text-gray-600 pl-3">Khẩu phần: {it.portionSize}</p>}
                    {it.spicyLevel && <p className="text-xs text-red-600 font-bold pl-3">*** MỨC CAY: {it.spicyLevel} ***</p>}
                    {it.dippingSauce && <p className="text-xs text-gray-700 pl-3">Nước chấm: {it.dippingSauce}</p>}
                    {it.size && it.itemType === "DRINK" && (
                      <p className="text-xs text-gray-600 pl-3">Size {it.size} | {it.sugarLevel} ĐƯỜNG | {it.iceLevel} ĐÁ</p>
                    )}
                    {it.toppings && it.toppings.length > 0 && (
                      <p className="text-xs font-bold text-amber-900 pl-3">
                        Topping: {it.toppings.map((t) => t.toppingName).join(", ")}
                      </p>
                    )}
                    {it.customNotes && <p className="text-xs italic pl-3 text-gray-600">Lưu ý: {it.customNotes}</p>}
                  </div>
                ))}
              </div>

              {selectedTicket.note && (
                <div className="py-2 border-b border-dashed font-bold text-amber-900">
                  Ghi chú khách: {selectedTicket.note}
                </div>
              )}

              <div className="flex justify-between font-black text-sm pt-1">
                <span>TỔNG TIỀN:</span>
                <span>{selectedTicket.totalAmount.toLocaleString("vi-VN")}đ ({selectedTicket.paymentMethod})</span>
              </div>
            </div>

            <div className="pt-2">
              <button
                onClick={() => {
                  window.print();
                }}
                className="w-full py-3 rounded-2xl bg-[#6F4E37] text-white font-black text-xs hover:bg-[#4A3324] shadow-md flex items-center justify-center gap-2"
              >
                <Printer className="w-4 h-4" />
                <span>In Phiếu Order Bếp</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Assign Shipper */}
      {assigningOrder && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-md w-full border border-[#EFE6DC] shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b">
              <div className="flex items-center gap-2">
                <Truck className="w-5 h-5 text-emerald-600" />
                <h3 className="font-black text-base text-[#2D2522]">Phân Công Shipper Giao Đơn</h3>
              </div>
              <button onClick={() => setAssigningOrder(null)} className="p-1 hover:opacity-70 text-xs font-bold">
                [X]
              </button>
            </div>

            <div className="text-xs text-[#786C65] space-y-1">
              <p>Mã đơn: <span className="font-bold text-[#2D2522]">{assigningOrder.orderCode}</span></p>
              <p>Khách hàng: <span className="font-bold text-[#2D2522]">{assigningOrder.customerName}</span></p>
              <p>Địa chỉ: <span className="font-bold text-[#2D2522]">{assigningOrder.shippingAddress}</span></p>
              <p>Cần thu COD: <span className="font-bold text-[#6F4E37]">{assigningOrder.paymentMethod === "COD" ? `${assigningOrder.totalAmount.toLocaleString("vi-VN")}đ` : "0đ (Đã thanh toán)"}</span></p>
            </div>

            <div className="space-y-2">
              <label className="text-xs font-bold text-[#2D2522] block">Chọn Nhân Viên Giao Hàng:</label>
              {shippers.length === 0 ? (
                <p className="text-xs text-red-600 font-bold">Chưa có tài khoản Shipper nào trong hệ thống.</p>
              ) : (
                <select
                  value={selectedShipperId || ""}
                  onChange={(e) => setSelectedShipperId(parseInt(e.target.value, 10))}
                  className="w-full bg-white border border-[#EFE6DC] rounded-xl px-3 py-2 text-xs font-bold text-[#2D2522] focus:outline-none focus:border-[#6F4E37]"
                >
                  {shippers.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.fullName} - {s.phoneNumber || "Chưa có SĐT"}
                    </option>
                  ))}
                </select>
              )}
            </div>

            <div className="flex items-center justify-end gap-3 pt-3">
              <button
                onClick={() => setAssigningOrder(null)}
                className="px-4 py-2.5 rounded-xl border border-[#EFE6DC] text-xs font-bold text-[#786C65]"
              >
                Hủy
              </button>
              <button
                onClick={handleAssignShipper}
                disabled={actionLoading || !selectedShipperId}
                className="px-5 py-2.5 rounded-xl bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-700 disabled:opacity-50"
              >
                {actionLoading ? "Đang giao..." : "Xác nhận bàn giao đơn"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
