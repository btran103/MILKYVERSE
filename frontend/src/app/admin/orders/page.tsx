"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { OrderResponse } from "@/types";
import {
  Truck,
  Package,
  RefreshCw,
  Eye,
  X,
  Printer,
  Phone,
  MapPin,
  User,
  Check,
} from "lucide-react";

export default function AdminOrdersPage() {
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState("ALL");
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  // Selected Order for Detail & Dispatch Modal
  const [selectedOrder, setSelectedOrder] = useState<OrderResponse | null>(null);
  const [dispatchNote, setDispatchNote] = useState("");
  const [shipperName, setShipperName] = useState("");
  const [actionSuccessMsg, setActionSuccessMsg] = useState<string | null>(null);

  const loadOrders = async () => {
    setLoading(true);
    try {
      const data = await api.getOrders();
      setOrders(data || []);
      // If modal is open, refresh selected order too
      if (selectedOrder) {
        const updated = data.find((o) => o.id === selectedOrder.id);
        if (updated) setSelectedOrder(updated);
      }
    } catch (err) {
      console.error("Lỗi khi tải đơn hàng:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadOrders();
  }, []);

  const handleUpdateStatus = async (
    orderId: number,
    newStatus: string,
    paymentStatus?: string,
    customNote?: string
  ) => {
    setUpdatingId(orderId);
    setActionSuccessMsg(null);
    try {
      const noteToSend =
        customNote ||
        (newStatus === "Delivering" && shipperName
          ? `Bàn giao shipper: ${shipperName}. ${dispatchNote}`
          : `Quản trị viên cập nhật trạng thái thành: ${newStatus}`);

      await api.updateOrderStatus(orderId, newStatus, paymentStatus, noteToSend);
      setActionSuccessMsg(`Đã cập nhật đơn hàng sang trạng thái: ${newStatus}`);
      await loadOrders();
    } catch {
      alert("Lỗi khi cập nhật trạng thái đơn.");
    } finally {
      setUpdatingId(null);
      setTimeout(() => setActionSuccessMsg(null), 3000);
    }
  };

  const filtered = orders.filter((o) => {
    if (statusFilter === "ALL") return true;
    return o.status === statusFilter;
  });

  return (
    <div className="space-y-6">
      {/* Title & Refresh */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-[#4E7D55] animate-ping" />
            <h1 className="text-2xl font-black text-[#2D2522]">
              Hệ Thống Duyệt Đơn & Điều Phối Vận Chuyển
            </h1>
          </div>
          <p className="text-xs text-[#786C65] mt-1">
            Tiếp nhận đơn đặt từ khách hàng, duyệt đơn, chuyển Barista pha chế và điều phối Shipper giao tận nơi
          </p>
        </div>

        <button
          onClick={loadOrders}
          className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs font-bold text-[#6F4E37] hover:bg-[#FAF7F2] self-start sm:self-auto shadow-sm"
        >
          <RefreshCw className="w-3.5 h-3.5" />
          <span>Làm mới danh sách</span>
        </button>
      </div>

      {/* Filter Tabs */}
      <div className="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
        {[
          { key: "ALL", label: "Tất Cả Đơn" },
          { key: "Pending", label: "Chờ Duyệt (Mới)" },
          { key: "Confirmed", label: "Đã Xác Nhận" },
          { key: "Preparing", label: "Đang Pha Chế" },
          { key: "Delivering", label: "Đang Vận Chuyển" },
          { key: "Completed", label: "Đã Giao Xong" },
          { key: "Cancelled", label: "Đã Hủy" },
        ].map((st) => {
          const count =
            st.key === "ALL"
              ? orders.length
              : orders.filter((o) => o.status === st.key).length;

          return (
            <button
              key={st.key}
              onClick={() => setStatusFilter(st.key)}
              className={`px-3.5 py-2 rounded-xl text-xs font-bold whitespace-nowrap transition-all flex items-center gap-1.5 ${
                statusFilter === st.key
                  ? "bg-[#6F4E37] text-white shadow-sm"
                  : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
              }`}
            >
              <span>{st.label}</span>
              <span
                className={`text-[10px] px-1.5 py-0.2 rounded-full ${
                  statusFilter === st.key
                    ? "bg-white text-[#6F4E37]"
                    : "bg-[#FAF7F2] text-[#786C65]"
                }`}
              >
                {count}
              </span>
            </button>
          );
        })}
      </div>

      {/* Orders Table */}
      <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#FAF7F2] text-[#786C65] font-bold border-b border-[#EFE6DC]">
              <tr>
                <th className="p-4">Mã đơn & Giờ đặt</th>
                <th className="p-4">Khách hàng & SĐT</th>
                <th className="p-4">Địa chỉ giao hàng</th>
                <th className="p-4">Tổng tiền & TT</th>
                <th className="p-4">Tiến trình vận đơn</th>
                <th className="p-4 text-right">Thao tác quản trị</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#FAF7F2]">
              {loading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-[#786C65]">
                    <RefreshCw className="w-5 h-5 animate-spin mx-auto mb-2 text-[#6F4E37]" />
                    Đang tải danh sách đơn hàng...
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-[#786C65]">
                    Không có đơn hàng nào trong mục này.
                  </td>
                </tr>
              ) : (
                filtered.map((o) => (
                  <tr key={o.id} className="hover:bg-[#FAF7F2]/60 transition-colors">
                    <td className="p-4 font-black text-[#2D2522]">
                      <button
                        onClick={() => setSelectedOrder(o)}
                        className="hover:underline flex items-center gap-1.5 text-[#6F4E37] font-bold"
                      >
                        <span>#{o.orderCode}</span>
                        <Eye className="w-3.5 h-3.5" />
                      </button>
                      <span className="text-[10px] text-[#786C65] font-normal block mt-0.5">
                        {new Date(o.createdAt).toLocaleString("vi-VN")}
                      </span>
                    </td>

                    <td className="p-4">
                      <p className="font-bold text-[#2D2522]">{o.customerName}</p>
                      <p className="text-[#6F4E37] font-semibold flex items-center gap-1 mt-0.5">
                        <Phone className="w-3 h-3" />
                        <span>{o.customerPhone}</span>
                      </p>
                    </td>

                    <td className="p-4 max-w-xs truncate text-[#786C65]" title={o.shippingAddress}>
                      {o.shippingAddress}
                    </td>

                    <td className="p-4">
                      <p className="font-black text-[#6F4E37] text-sm">
                        {o.totalAmount.toLocaleString("vi-VN")}đ
                      </p>
                      <span
                        className={`inline-block text-[10px] font-bold px-1.5 py-0.2 rounded mt-0.5 ${
                          o.paymentStatus === "Paid"
                            ? "bg-emerald-100 text-emerald-800"
                            : "bg-amber-100 text-amber-800"
                        }`}
                      >
                        {o.paymentMethod} • {o.paymentStatus === "Paid" ? "Đã TT" : "Chưa TT"}
                      </span>
                    </td>

                    <td className="p-4">
                      <span
                        className={`px-2.5 py-1 rounded-full font-bold text-[10px] inline-flex items-center gap-1 ${
                          o.status === "Pending"
                            ? "bg-amber-100 text-amber-900 border border-amber-300"
                            : o.status === "Confirmed"
                            ? "bg-blue-100 text-blue-900 border border-blue-300"
                            : o.status === "Preparing"
                            ? "bg-purple-100 text-purple-900 border border-purple-300"
                            : o.status === "Delivering"
                            ? "bg-cyan-100 text-cyan-900 border border-cyan-300"
                            : o.status === "Completed"
                            ? "bg-emerald-100 text-emerald-900 border border-emerald-300"
                            : "bg-red-100 text-red-900 border border-red-300"
                        }`}
                      >
                        {o.status === "Pending" && "⏳ Chờ xác nhận"}
                        {o.status === "Confirmed" && "📋 Đã xác nhận"}
                        {o.status === "Preparing" && "🧋 Đang pha chế"}
                        {o.status === "Delivering" && "🛵 Đang giao hàng"}
                        {o.status === "Completed" && "✅ Đã giao thành công"}
                        {o.status === "Cancelled" && "❌ Đã hủy đơn"}
                      </span>
                    </td>

                    <td className="p-4 text-right">
                      <div className="inline-flex items-center gap-1.5">
                        <button
                          onClick={() => setSelectedOrder(o)}
                          className="px-2.5 py-1.5 rounded-xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] font-bold text-xs shadow-2xs"
                        >
                          Chi tiết & Điều phối
                        </button>

                        {/* Quick Action buttons */}
                        {o.status === "Pending" && (
                          <button
                            onClick={() => handleUpdateStatus(o.id, "Confirmed")}
                            disabled={updatingId === o.id}
                            className="px-2.5 py-1.5 rounded-xl bg-[#4E7D55] text-white font-bold hover:bg-[#3D6343] transition-colors"
                          >
                            Duyệt đơn
                          </button>
                        )}

                        {o.status === "Confirmed" && (
                          <button
                            onClick={() => handleUpdateStatus(o.id, "Preparing")}
                            disabled={updatingId === o.id}
                            className="px-2.5 py-1.5 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324] transition-colors"
                          >
                            Pha chế
                          </button>
                        )}

                        {o.status === "Preparing" && (
                          <button
                            onClick={() => setSelectedOrder(o)}
                            className="px-2.5 py-1.5 rounded-xl bg-cyan-700 text-white font-bold hover:bg-cyan-800 transition-colors flex items-center gap-1"
                          >
                            <Truck className="w-3.5 h-3.5" />
                            <span>Giao shipper</span>
                          </button>
                        )}

                        {o.status === "Delivering" && (
                          <button
                            onClick={() => handleUpdateStatus(o.id, "Completed", "Paid")}
                            disabled={updatingId === o.id}
                            className="px-2.5 py-1.5 rounded-xl bg-emerald-600 text-white font-bold hover:bg-emerald-700 transition-colors"
                          >
                            Đã giao (Đã TT)
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* DETAILED ORDER FULFILLMENT & DISPATCH MODAL */}
      {selectedOrder && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto p-6 sm:p-8 space-y-6">
            {/* Modal Header */}
            <div className="flex items-start justify-between pb-4 border-b border-[#EFE6DC]">
              <div>
                <div className="flex items-center gap-2">
                  <h2 className="text-xl font-black text-[#2D2522]">
                    Phiếu Xử Lý Đơn Hàng #{selectedOrder.orderCode}
                  </h2>
                  <span
                    className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
                      selectedOrder.status === "Completed"
                        ? "bg-emerald-100 text-emerald-800"
                        : "bg-amber-100 text-amber-800"
                    }`}
                  >
                    {selectedOrder.status}
                  </span>
                </div>
                <p className="text-xs text-[#786C65] mt-0.5">
                  Thời gian đặt: {new Date(selectedOrder.createdAt).toLocaleString("vi-VN")}
                </p>
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => window.print()}
                  className="p-2 rounded-xl border border-[#EFE6DC] hover:bg-[#FAF7F2] text-[#6F4E37] transition-colors"
                  title="In phiếu pha chế / phiếu giao hàng"
                >
                  <Printer className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setSelectedOrder(null)}
                  className="p-2 rounded-xl hover:bg-[#FAF7F2] text-[#786C65] hover:text-black transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>

            {actionSuccessMsg && (
              <div className="p-3 bg-emerald-50 text-emerald-800 border border-emerald-200 rounded-2xl text-xs flex items-center gap-2">
                <Check className="w-4 h-4 shrink-0 text-emerald-600" />
                <span>{actionSuccessMsg}</span>
              </div>
            )}

            {/* 1. Recipient Information Card */}
            <div className="bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC] space-y-2 text-xs">
              <h4 className="font-extrabold text-[#6F4E37] uppercase tracking-wider text-[11px] flex items-center gap-1.5">
                <User className="w-3.5 h-3.5" />
                <span>Thông Tin Khách Hàng Nhận Trà Sữa</span>
              </h4>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
                <div>
                  <span className="text-[#786C65] block">Người nhận:</span>
                  <span className="font-bold text-[#2D2522] text-sm">
                    {selectedOrder.customerName}
                  </span>
                </div>

                <div>
                  <span className="text-[#786C65] block">Số điện thoại liên hệ:</span>
                  <a
                    href={`tel:${selectedOrder.customerPhone}`}
                    className="font-bold text-[#6F4E37] text-sm hover:underline flex items-center gap-1"
                  >
                    <Phone className="w-3.5 h-3.5" />
                    <span>{selectedOrder.customerPhone}</span>
                  </a>
                </div>

                <div className="sm:col-span-2">
                  <span className="text-[#786C65] block">Địa chỉ giao hàng tận nơi:</span>
                  <span className="font-semibold text-[#2D2522] flex items-start gap-1 mt-0.5">
                    <MapPin className="w-3.5 h-3.5 text-red-500 shrink-0 mt-0.5" />
                    <span>{selectedOrder.shippingAddress}</span>
                  </span>
                </div>

                {selectedOrder.note && (
                  <div className="sm:col-span-2 p-2 bg-white rounded-xl border border-[#EFE6DC] text-[11px] text-[#786C65]">
                    <strong>Ghi chú của khách:</strong> &ldquo;{selectedOrder.note}&rdquo;
                  </div>
                )}
              </div>
            </div>

            {/* 2. Items & Recipes List */}
            <div className="space-y-3">
              <h4 className="font-extrabold text-[#2D2522] text-xs uppercase tracking-wider flex items-center gap-1.5">
                <Package className="w-3.5 h-3.5 text-[#6F4E37]" />
                <span>Danh Sách Đồ Uống Cần Pha Chế ({selectedOrder.items?.length || 0} món)</span>
              </h4>

              <div className="space-y-2">
                {selectedOrder.items?.map((item, idx) => (
                  <div
                    key={item.id || idx}
                    className="p-3 bg-white border border-[#EFE6DC] rounded-2xl flex justify-between items-start text-xs"
                  >
                    <div className="space-y-0.5">
                      <div className="flex items-center gap-2">
                        <span className="font-extrabold text-[#2D2522] text-sm">
                          {item.productName}
                        </span>
                        <span className="font-bold px-1.5 py-0.2 rounded bg-[#FAF7F2] text-[#6F4E37] text-[10px]">
                          x{item.quantity} ly
                        </span>
                        {item.isCustomDIY && (
                          <span className="bg-[#F7D6D0] text-[#6F4E37] font-bold text-[10px] px-1.5 py-0.2 rounded">
                            DIY
                          </span>
                        )}
                      </div>

                      <p className="text-[#786C65]">
                        <strong>Size:</strong> {item.size} • <strong>Đường:</strong> {item.sugarLevel} •{" "}
                        <strong>Đá:</strong> {item.iceLevel}
                      </p>

                      {item.toppings?.length > 0 && (
                        <p className="text-[#6F4E37] font-semibold">
                          Topping: {item.toppings.map((t) => `${t.toppingName} (x${t.quantity})`).join(", ")}
                        </p>
                      )}

                      {item.customNotes && (
                        <p className="italic text-[11px] text-[#786C65]">
                          {item.customNotes}
                        </p>
                      )}
                    </div>

                    <span className="font-black text-[#6F4E37] text-sm shrink-0">
                      {item.totalPrice.toLocaleString("vi-VN")}đ
                    </span>
                  </div>
                ))}
              </div>

              <div className="p-3 bg-[#FAF7F2] rounded-2xl flex justify-between items-center text-xs">
                <span className="font-bold text-[#786C65]">Tổng tiền đơn hàng:</span>
                <span className="text-base font-black text-[#6F4E37]">
                  {selectedOrder.totalAmount.toLocaleString("vi-VN")}đ
                </span>
              </div>
            </div>

            {/* 3. Operational Workflow & Dispatch Controls */}
            <div className="p-5 bg-white border-2 border-dashed border-[#6F4E37]/30 rounded-2xl space-y-4">
              <h4 className="font-black text-sm text-[#2D2522] flex items-center gap-2">
                <Truck className="w-4 h-4 text-[#6F4E37]" />
                <span>Quy Trình Duyệt Đơn & Xác Nhận Vận Chuyển Cho Khách</span>
              </h4>

              <div className="space-y-3 text-xs">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="font-bold text-[#2D2522] block mb-1">
                      Tên Tài xế / Đơn vị giao hàng (Shipper)
                    </label>
                    <input
                      type="text"
                      placeholder="Ví dụ: Anh Nam (Ahamove - 0987xxx)"
                      value={shipperName}
                      onChange={(e) => setShipperName(e.target.value)}
                      className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                    />
                  </div>

                  <div>
                    <label className="font-bold text-[#2D2522] block mb-1">
                      Ghi chú vận chuyển / Mã vận đơn
                    </label>
                    <input
                      type="text"
                      placeholder="Ví dụ: Giao trước 15:00, trà sữa để túi giữ nhiệt"
                      value={dispatchNote}
                      onChange={(e) => setDispatchNote(e.target.value)}
                      className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                    />
                  </div>
                </div>

                {/* Progress Buttons */}
                <div className="pt-2 flex flex-wrap gap-2">
                  <button
                    onClick={() => handleUpdateStatus(selectedOrder.id, "Confirmed")}
                    disabled={updatingId === selectedOrder.id || selectedOrder.status === "Confirmed"}
                    className="px-3.5 py-2.5 rounded-xl bg-[#4E7D55] text-white font-bold hover:bg-[#3D6343] transition-colors disabled:opacity-40"
                  >
                    1. Duyệt & Xác nhận đơn
                  </button>

                  <button
                    onClick={() => handleUpdateStatus(selectedOrder.id, "Preparing")}
                    disabled={updatingId === selectedOrder.id || selectedOrder.status === "Preparing"}
                    className="px-3.5 py-2.5 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324] transition-colors disabled:opacity-40"
                  >
                    2. Chuyển Barista pha chế
                  </button>

                  <button
                    onClick={() => handleUpdateStatus(selectedOrder.id, "Delivering")}
                    disabled={updatingId === selectedOrder.id || selectedOrder.status === "Delivering"}
                    className="px-3.5 py-2.5 rounded-xl bg-cyan-700 text-white font-bold hover:bg-cyan-800 transition-colors disabled:opacity-40 flex items-center gap-1.5"
                  >
                    <Truck className="w-3.5 h-3.5" />
                    <span>3. Bàn giao Shipper vận chuyển</span>
                  </button>

                  <button
                    onClick={() => handleUpdateStatus(selectedOrder.id, "Completed", "Paid")}
                    disabled={updatingId === selectedOrder.id || selectedOrder.status === "Completed"}
                    className="px-3.5 py-2.5 rounded-xl bg-emerald-600 text-white font-bold hover:bg-emerald-700 transition-colors disabled:opacity-40"
                  >
                    4. Xác nhận đã giao xong & Thu tiền
                  </button>

                  <button
                    onClick={() => {
                      const reason = prompt("Nhập lý do hủy đơn hàng:");
                      if (reason) {
                        handleUpdateStatus(selectedOrder.id, "Cancelled", "Failed", `Lý do hủy: ${reason}`);
                      }
                    }}
                    disabled={updatingId === selectedOrder.id || selectedOrder.status === "Cancelled" || selectedOrder.status === "Completed"}
                    className="px-3.5 py-2.5 rounded-xl bg-red-100 text-red-700 font-bold hover:bg-red-200 transition-colors disabled:opacity-40 ml-auto"
                  >
                    Hủy đơn hàng
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
