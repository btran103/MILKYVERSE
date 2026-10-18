"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { ShipperDashboard } from "@/types";
import { useAuth } from "@/context/AuthContext";
import {
  Truck,
  Phone,
  MapPin,
  CheckCircle,
  XCircle,
  RotateCcw,
  Navigation,
  ExternalLink,
} from "lucide-react";

export default function DeliveryStaffPage() {
  const router = useRouter();
  const { user, isDelivery, isLoading } = useAuth();

  const [dashboard, setDashboard] = useState<ShipperDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<"ACTIVE" | "COMPLETED" | "ALL">("ACTIVE");

  // Failure modal
  const [failingOrderId, setFailingOrderId] = useState<number | null>(null);
  const [failureReason, setFailureReason] = useState("Khách không nghe máy");
  const [failureNote, setFailureNote] = useState("");

  useEffect(() => {
    if (!isLoading && (!user || !isDelivery)) {
      router.push("/auth/login?redirect=/delivery");
    }
  }, [user, isDelivery, isLoading, router]);

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await api.getShipperDashboard();
      setDashboard(data);
    } catch (err) {
      console.error("Lỗi khi tải bảng giao hàng:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user && isDelivery) {
      loadData();
    }
  }, [user, isDelivery]);

  const handleUpdateStatus = async (
    orderId: number,
    status: string,
    isCodCollected?: boolean,
    reason?: string
  ) => {
    setActionLoading(true);
    try {
      await api.updateDeliveryStatus(orderId, {
        status,
        isCodCollected,
        failureReason: reason,
        notes: reason || `Shipper cập nhật trạng thái: ${status}`,
      });
      await loadData();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Cập nhật đơn thất bại.";
      alert(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const handleReconcileCod = async () => {
    if (!dashboard || dashboard.orders.length === 0) return;
    const unreconciledOrderIds = dashboard.orders
      .filter((o) => o.isCodCollected && !o.codReconciled)
      .map((o) => o.id);

    if (unreconciledOrderIds.length === 0) {
      alert("Không có khoản tiền COD nào cần đối soát nộp lại.");
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn nộp đối soát tổng số tiền ${dashboard.totalCodPendingReconcile.toLocaleString("vi-VN")}đ cho thu ngân / quản lý?`)) {
      setActionLoading(true);
      try {
        await api.reconcileCod(unreconciledOrderIds);
        alert("Đối soát nộp tiền COD thành công!");
        await loadData();
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : "Đối soát thất bại.";
        alert(msg);
      } finally {
        setActionLoading(false);
      }
    }
  };

  if (isLoading || !user || !isDelivery) {
    return (
      <div className="max-w-md mx-auto py-20 px-4 text-center space-y-4">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-sm font-bold text-[#786C65]">Đang kiểm tra quyền nhân viên Giao Vận (Shipper)...</p>
      </div>
    );
  }

  const allOrders = dashboard?.orders || [];
  const activeOrders = allOrders.filter(
    (o) => o.status === "Delivering" || o.deliveryStatus === "Assigned" || o.deliveryStatus === "PickedUp" || o.deliveryStatus === "InTransit"
  );
  const completedOrders = allOrders.filter(
    (o) => o.status === "Completed" || o.deliveryStatus === "Delivered"
  );

  const displayedOrders = activeTab === "ACTIVE" ? activeOrders : activeTab === "COMPLETED" ? completedOrders : allOrders;

  return (
    <div className="max-w-3xl mx-auto px-4 sm:px-6 py-6 space-y-6">
      {/* Shipper Profile & Refresh */}
      <div className="bg-white p-5 rounded-3xl border border-[#EFE6DC] shadow-sm flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-2xl bg-emerald-100 text-emerald-800 flex items-center justify-center font-black">
            <Truck className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <span className="text-xs font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full">
                Shipper Trực Tuyến
              </span>
              <span className="text-xs text-[#786C65]">#{user.id}</span>
            </div>
            <h2 className="text-lg font-black text-[#2D2522] mt-0.5">{user.fullName}</h2>
            <p className="text-xs text-[#786C65]">{user.phoneNumber || "0977889900"}</p>
          </div>
        </div>

        <button
          onClick={loadData}
          className="p-3 rounded-2xl bg-[#FAF7F2] hover:bg-[#EFE6DC] text-[#2D2522] transition-colors"
          title="Làm mới đơn"
        >
          <RotateCcw className={`w-5 h-5 ${loading ? "animate-spin text-[#6F4E37]" : ""}`} />
        </button>
      </div>

      {/* COD & KPI Status Banner */}
      <div className="grid grid-cols-2 gap-3 sm:gap-4">
        <div className="bg-white p-4 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-1">
          <span className="text-[11px] font-bold text-[#786C65] uppercase">Đơn Đang Giao</span>
          <h3 className="text-2xl font-black text-blue-600">{dashboard?.inTransitOrders || activeOrders.length}</h3>
          <p className="text-[10px] text-[#786C65]">Cần hoàn thành trong ngày</p>
        </div>

        <div className="bg-white p-4 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-1">
          <span className="text-[11px] font-bold text-[#786C65] uppercase">Giao Xong Hôm Nay</span>
          <h3 className="text-2xl font-black text-emerald-700">{dashboard?.deliveredToday || completedOrders.length}</h3>
          <p className="text-[10px] text-[#786C65]">Đơn đã trao tận tay khách</p>
        </div>

        <div className="col-span-2 bg-gradient-to-r from-[#6F4E37] to-[#4A3324] p-5 rounded-3xl text-white shadow-md space-y-3">
          <div className="flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-[#F7D6D0]">Tiền Thu Hộ COD Cần Nộp Cho Quản Lý:</span>
              <h3 className="text-2xl sm:text-3xl font-black mt-0.5 text-amber-300">
                {(dashboard?.totalCodPendingReconcile || 0).toLocaleString("vi-VN")}đ
              </h3>
            </div>
            <button
              onClick={handleReconcileCod}
              disabled={actionLoading || (dashboard?.totalCodPendingReconcile || 0) === 0}
              className="px-4 py-2.5 rounded-2xl bg-amber-400 text-[#2D2522] font-black text-xs hover:bg-amber-300 disabled:opacity-50 transition-all shadow-sm"
            >
              Nộp Đối Soát COD
            </button>
          </div>
          <p className="text-[11px] text-gray-200">
            Tổng tiền mặt COD đã thu trong ca: {(dashboard?.totalCodCollected || 0).toLocaleString("vi-VN")}đ
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-[#EFE6DC] pb-2">
        <button
          onClick={() => setActiveTab("ACTIVE")}
          className={`flex-1 py-2.5 rounded-2xl text-xs font-bold transition-all ${
            activeTab === "ACTIVE"
              ? "bg-[#6F4E37] text-white shadow-sm"
              : "bg-white border border-[#EFE6DC] text-[#2D2522]"
          }`}
        >
          Cần Giao Ngay ({activeOrders.length})
        </button>

        <button
          onClick={() => setActiveTab("COMPLETED")}
          className={`flex-1 py-2.5 rounded-2xl text-xs font-bold transition-all ${
            activeTab === "COMPLETED"
              ? "bg-[#6F4E37] text-white shadow-sm"
              : "bg-white border border-[#EFE6DC] text-[#2D2522]"
          }`}
        >
          Lịch Sử Đã Giao ({completedOrders.length})
        </button>
      </div>

      {/* Order Cards List */}
      <div className="space-y-4">
        {displayedOrders.length === 0 ? (
          <div className="bg-white rounded-3xl p-10 text-center border border-[#EFE6DC] space-y-2">
            <Truck className="w-10 h-10 text-gray-400 mx-auto" />
            <h4 className="font-extrabold text-sm text-[#2D2522]">Không có đơn hàng nào trong mục này</h4>
            <p className="text-xs text-[#786C65]">Khi quầy bán bàn giao đơn mới, bạn sẽ nhận được thông báo tại đây.</p>
          </div>
        ) : (
          displayedOrders.map((order) => {
            const isDelivered = order.status === "Completed" || order.deliveryStatus === "Delivered";
            const isCOD = order.paymentMethod === "COD";

            return (
              <div
                key={order.id}
                className={`bg-white rounded-3xl p-5 border shadow-sm space-y-4 transition-all ${
                  isDelivered ? "border-emerald-200 opacity-90" : "border-[#EFE6DC] hover:border-[#6F4E37]"
                }`}
              >
                {/* Header card */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="font-mono font-black text-sm text-[#6F4E37] bg-[#FAF7F2] px-2.5 py-1 rounded-xl">
                      {order.orderCode}
                    </span>
                    {isCOD ? (
                      <span className="px-2 py-0.5 rounded-full bg-amber-100 text-amber-900 font-black text-[10px]">
                        THU TIỀN MẶT COD
                      </span>
                    ) : (
                      <span className="px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-bold text-[10px]">
                        ĐÃ THANH TOÁN (VIETQR)
                      </span>
                    )}
                  </div>

                  <span className="font-black text-base text-[#6F4E37]">
                    {order.totalAmount.toLocaleString("vi-VN")}đ
                  </span>
                </div>

                {/* Customer Details & One-Click Actions */}
                <div className="p-3.5 rounded-2xl bg-[#FAF7F2] space-y-2.5 text-xs">
                  <div className="flex items-center justify-between font-bold text-[#2D2522]">
                    <span>Khách: {order.customerName}</span>
                    <a
                      href={`tel:${order.customerPhone}`}
                      className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-emerald-600 text-white font-bold text-xs hover:bg-emerald-700 shadow-sm"
                    >
                      <Phone className="w-3.5 h-3.5" />
                      <span>Gọi ngay ({order.customerPhone})</span>
                    </a>
                  </div>

                  <div className="space-y-1">
                    <p className="text-[#786C65] font-semibold flex items-start gap-1">
                      <MapPin className="w-3.5 h-3.5 text-red-500 shrink-0 mt-0.5" />
                      <span>{order.shippingAddress}</span>
                    </p>

                    <a
                      href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(order.shippingAddress)}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1 text-[11px] font-bold text-blue-700 hover:underline pt-0.5"
                    >
                      <Navigation className="w-3 h-3" />
                      <span>Mở Google Maps chỉ đường</span>
                      <ExternalLink className="w-2.5 h-2.5" />
                    </a>
                  </div>

                  {order.note && (
                    <p className="text-[11px] font-semibold text-amber-800 bg-amber-50 p-2 rounded-lg border border-amber-200">
                      Lưu ý khách: {order.note}
                    </p>
                  )}
                </div>

                {/* Items Summary */}
                <div className="space-y-1 text-xs">
                  <span className="font-bold text-[#786C65] uppercase text-[10px]">Món Cần Giao:</span>
                  <div className="divide-y divide-gray-100">
                    {order.items.map((it, idx) => (
                      <div key={idx} className="py-1 flex items-center justify-between">
                        <span className="font-semibold text-[#2D2522]">
                          • {it.productName} ({it.portionSize || it.size})
                          {it.spicyLevel && ` - ${it.spicyLevel}`}
                        </span>
                        <span className="font-bold text-[#6F4E37]">x{it.quantity}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Action Buttons for active delivery */}
                {!isDelivered ? (
                  <div className="space-y-2 pt-2 border-t">
                    <div className="grid grid-cols-2 gap-2">
                      <button
                        onClick={() => handleUpdateStatus(order.id, "InTransit")}
                        disabled={actionLoading}
                        className="py-3 px-3 rounded-2xl bg-blue-600 text-white font-bold text-xs hover:bg-blue-700 shadow-sm flex items-center justify-center gap-1.5"
                      >
                        <Navigation className="w-4 h-4" />
                        <span>Đang giao xe</span>
                      </button>

                      <button
                        onClick={() => handleUpdateStatus(order.id, "Delivered", isCOD)}
                        disabled={actionLoading}
                        className="py-3 px-3 rounded-2xl bg-emerald-600 text-white font-black text-xs hover:bg-emerald-700 shadow-md flex items-center justify-center gap-1.5"
                      >
                        <CheckCircle className="w-4 h-4" />
                        <span>Giao xong {isCOD ? "+ Thu COD" : ""}</span>
                      </button>
                    </div>

                    <button
                      onClick={() => setFailingOrderId(order.id)}
                      className="w-full py-2 text-center text-xs text-red-600 font-bold hover:underline"
                    >
                      Báo giao không thành công
                    </button>
                  </div>
                ) : (
                  <div className="p-2.5 rounded-2xl bg-emerald-50 text-emerald-800 text-xs font-bold flex items-center justify-between border border-emerald-200">
                    <span className="flex items-center gap-1.5">
                      <CheckCircle className="w-4 h-4 text-emerald-600" />
                      Đã giao thành công
                    </span>
                    <span>
                      {order.isCodCollected
                        ? order.codReconciled
                          ? "Đã nộp COD"
                          : "Đã thu COD (Chờ nộp)"
                        : "Đã thu"}
                    </span>
                  </div>
                )}
              </div>
            );
          })
        )}
      </div>

      {/* Failure Report Modal */}
      {failingOrderId && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 max-w-sm w-full border border-[#EFE6DC] shadow-2xl space-y-4">
            <div className="flex items-center gap-2 text-red-600 font-black">
              <XCircle className="w-5 h-5" />
              <h3 className="text-base text-[#2D2522]">Báo Giao Thất Bại</h3>
            </div>

            <div className="space-y-3 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Lý do thất bại:</label>
                <select
                  value={failureReason}
                  onChange={(e) => setFailureReason(e.target.value)}
                  className="w-full p-2.5 rounded-xl border border-[#EFE6DC] font-bold"
                >
                  <option value="Khách không nghe máy (Gọi 3 lần)">Khách không nghe máy (Gọi 3 lần)</option>
                  <option value="Khách hẹn dời lại thời gian">Khách hẹn dời lại thời gian</option>
                  <option value="Sai địa chỉ / Không tìm thấy số nhà">Sai địa chỉ / Không tìm thấy số nhà</option>
                  <option value="Khách từ chối nhận món">Khách từ chối nhận món</option>
                  <option value="Món bị sự cố trong lúc vận chuyển">Món bị sự cố trong lúc vận chuyển</option>
                </select>
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Chi tiết bổ sung:</label>
                <input
                  type="text"
                  placeholder="Ghi rõ thời gian gọi..."
                  value={failureNote}
                  onChange={(e) => setFailureNote(e.target.value)}
                  className="w-full p-2.5 rounded-xl border border-[#EFE6DC]"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                onClick={() => setFailingOrderId(null)}
                className="px-4 py-2 rounded-xl border text-xs font-bold"
              >
                Hủy
              </button>
              <button
                onClick={() => {
                  handleUpdateStatus(failingOrderId, "Failed", false, `${failureReason}: ${failureNote}`);
                  setFailingOrderId(null);
                }}
                disabled={actionLoading}
                className="px-4 py-2 rounded-xl bg-red-600 text-white text-xs font-bold hover:bg-red-700"
              >
                Xác nhận báo lỗi
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
