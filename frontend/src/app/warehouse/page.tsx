"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Ingredient, PurchaseReceipt, StockAudit, StockBatch } from "@/types";
import { useAuth } from "@/context/AuthContext";
import {
  Boxes,
  AlertTriangle,
  Calendar,
  FileText,
  ClipboardCheck,
  Plus,
  RefreshCw,
  Search,
  Check,
  X,
} from "lucide-react";

export default function WarehousePage() {
  const router = useRouter();
  const { user, isWarehouse, isLoading } = useAuth();

  const [activeTab, setActiveTab] = useState<"STOCK" | "FEFO" | "RECEIPTS" | "AUDITS">("STOCK");
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [batches, setBatches] = useState<StockBatch[]>([]);
  const [receipts, setReceipts] = useState<PurchaseReceipt[]>([]);
  const [audits, setAudits] = useState<StockAudit[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");

  // Modal Create Receipt
  const [showReceiptModal, setShowReceiptModal] = useState(false);
  const [supplierName, setSupplierName] = useState("");
  const [receiptNotes, setReceiptNotes] = useState("");
  const [receiptItems, setReceiptItems] = useState<
    { ingredientId: number; quantity: number; unitPrice: number; batchNumber: string; expiryDate: string }[]
  >([]);

  // Modal Create Audit
  const [showAuditModal, setShowAuditModal] = useState(false);
  const [auditNotes, setAuditNotes] = useState("");
  const [auditCounts, setAuditCounts] = useState<{ [ingId: number]: { actualQty: number; reason: string } }>({});

  const [msg, setMsg] = useState<{ text: string; error?: boolean } | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!isLoading && (!user || !isWarehouse)) {
      router.push("/auth/login?redirect=/warehouse");
    }
  }, [user, isWarehouse, isLoading, router]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [ings, bat, rec, aud] = await Promise.all([
        api.getIngredients(),
        api.getStockBatches(),
        api.getPurchaseReceipts(),
        api.getStockAudits(),
      ]);
      setIngredients(ings || []);
      setBatches(bat || []);
      setReceipts(rec || []);
      setAudits(aud || []);

      // initialize audit counts with current stocks
      if (ings) {
        const counts: { [ingId: number]: { actualQty: number; reason: string } } = {};
        ings.forEach((i) => {
          counts[i.id] = { actualQty: i.currentStock, reason: "Khớp thực tế" };
        });
        setAuditCounts(counts);
      }
    } catch (err) {
      console.error("Lỗi khi tải dữ liệu kho:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user && isWarehouse) {
      loadData();
    }
  }, [user, isWarehouse]);

  const handleOpenReceiptModal = () => {
    if (ingredients.length > 0) {
      setReceiptItems([
        {
          ingredientId: ingredients[0].id,
          quantity: 100,
          unitPrice: ingredients[0].unitCost || 5000,
          batchNumber: `LOT-${Date.now().toString().slice(-6)}`,
          expiryDate: new Date(Date.now() + 90 * 86400000).toISOString().split("T")[0],
        },
      ]);
    }
    setShowReceiptModal(true);
  };

  const handleAddReceiptItem = () => {
    if (ingredients.length > 0) {
      setReceiptItems([
        ...receiptItems,
        {
          ingredientId: ingredients[0].id,
          quantity: 100,
          unitPrice: ingredients[0].unitCost || 5000,
          batchNumber: `LOT-${Date.now().toString().slice(-6)}`,
          expiryDate: new Date(Date.now() + 90 * 86400000).toISOString().split("T")[0],
        },
      ]);
    }
  };

  const handleRemoveReceiptItem = (index: number) => {
    setReceiptItems(receiptItems.filter((_, i) => i !== index));
  };

  const handleSubmitReceipt = async (e: React.FormEvent) => {
    e.preventDefault();
    if (receiptItems.length === 0) return;
    setSubmitting(true);
    setMsg(null);
    try {
      await api.createPurchaseReceipt({
        supplierName,
        notes: receiptNotes,
        items: receiptItems,
      });
      setMsg({ text: "Nhập kho thành công và cập nhật lô FEFO mới!" });
      setShowReceiptModal(false);
      setSupplierName("");
      setReceiptNotes("");
      loadData();
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : "Nhập kho thất bại.";
      setMsg({ text: errorMsg, error: true });
    } finally {
      setSubmitting(false);
    }
  };

  const handleSubmitAudit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setMsg(null);
    try {
      const itemsPayload = Object.entries(auditCounts).map(([ingId, data]) => ({
        ingredientId: parseInt(ingId, 10),
        actualQuantity: data.actualQty,
        reason: data.reason || "Kiểm kê định kỳ",
      }));

      await api.createStockAudit({
        notes: auditNotes,
        items: itemsPayload,
      });
      setMsg({ text: "Hoàn tất kiểm kê kho và cân bằng tồn kho thành công!" });
      setShowAuditModal(false);
      setAuditNotes("");
      loadData();
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : "Kiểm kê kho thất bại.";
      setMsg({ text: errorMsg, error: true });
    } finally {
      setSubmitting(false);
    }
  };

  if (isLoading || !user || !isWarehouse) {
    return (
      <div className="max-w-md mx-auto py-20 px-4 text-center space-y-4">
        <div className="w-10 h-10 border-4 border-[#6F4E37] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-sm font-bold text-[#786C65]">Đang kiểm tra quyền Thủ kho & Quản lý...</p>
      </div>
    );
  }

  const lowStockCount = ingredients.filter((i) => i.currentStock <= i.minAlertThreshold).length;
  const expiredBatchesCount = batches.filter((b) => b.isExpired).length;
  const nearExpiryBatchesCount = batches.filter((b) => b.isNearExpiry).length;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-[#EFE6DC]">
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-[#6F4E37] uppercase">
            <Boxes className="w-4 h-4 text-[#C5A059]" />
            <span>WAREHOUSE & INVENTORY MANAGEMENT</span>
          </div>
          <h1 className="text-3xl font-black text-[#2D2522] mt-1">
            Quản Lý Kho Hàng & Quản Trị Lô FEFO
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Nguyên tắc FEFO (First Expired, First Out), tạo phiếu nhập kho, kiểm kê định kỳ và cảnh báo dưới mức an toàn
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <button
            onClick={handleOpenReceiptModal}
            className="px-4 py-2.5 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm flex items-center gap-2 transition-all"
          >
            <Plus className="w-4 h-4" />
            <span>Tạo Phiếu Nhập Kho</span>
          </button>

          <button
            onClick={() => setShowAuditModal(true)}
            className="px-4 py-2.5 rounded-2xl bg-white border border-[#6F4E37] text-[#6F4E37] text-xs font-bold hover:bg-[#FAF7F2] shadow-sm flex items-center gap-2 transition-all"
          >
            <ClipboardCheck className="w-4 h-4" />
            <span>Kiểm Kê Kho</span>
          </button>

          <button
            onClick={loadData}
            className="p-2.5 rounded-2xl bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2] shadow-sm transition-all"
            title="Làm mới"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
          </button>
        </div>
      </div>

      {msg && (
        <div
          className={`p-3.5 rounded-2xl text-xs font-bold flex items-center justify-between ${
            msg.error ? "bg-red-50 text-red-700 border border-red-200" : "bg-emerald-50 text-emerald-700 border border-emerald-200"
          }`}
        >
          <span>{msg.text}</span>
          <button onClick={() => setMsg(null)} className="p-1 hover:opacity-70">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="bg-white p-5 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-2">
          <div className="flex items-center justify-between text-xs font-bold text-[#786C65] uppercase">
            <span>Tổng Mặt Hàng</span>
            <Boxes className="w-4 h-4 text-[#6F4E37]" />
          </div>
          <h3 className="text-2xl font-black text-[#2D2522]">{ingredients.length}</h3>
          <p className="text-[11px] text-[#786C65]">Đồ ăn, trà sữa, bao bì</p>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-2">
          <div className="flex items-center justify-between text-xs font-bold text-amber-700 uppercase">
            <span>Cảnh Báo Thiếu Hàng</span>
            <AlertTriangle className="w-4 h-4 text-amber-600" />
          </div>
          <h3 className="text-2xl font-black text-amber-600">{lowStockCount}</h3>
          <p className="text-[11px] text-[#786C65]">Mặt hàng dưới ngưỡng tối thiểu</p>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-2">
          <div className="flex items-center justify-between text-xs font-bold text-red-700 uppercase">
            <span>Lô FEFO Cần Lưu Ý</span>
            <Calendar className="w-4 h-4 text-red-600" />
          </div>
          <h3 className="text-2xl font-black text-red-600">
            {expiredBatchesCount + nearExpiryBatchesCount}
          </h3>
          <p className="text-[11px] text-[#786C65]">
            {expiredBatchesCount} đã hết hạn • {nearExpiryBatchesCount} cận hạn (≤15 ngày)
          </p>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-[#EFE6DC] shadow-sm space-y-2">
          <div className="flex items-center justify-between text-xs font-bold text-emerald-700 uppercase">
            <span>Phiếu Nhập & Kiểm Kê</span>
            <FileText className="w-4 h-4 text-emerald-600" />
          </div>
          <h3 className="text-2xl font-black text-emerald-700">
            {receipts.length} / {audits.length}
          </h3>
          <p className="text-[11px] text-[#786C65]">Phiếu nhập kho / Lần kiểm kê</p>
        </div>
      </div>

      {/* Tabs Menu */}
      <div className="flex items-center gap-2 border-b border-[#EFE6DC] pb-2 overflow-x-auto scrollbar-none">
        {[
          { key: "STOCK", label: "Tồn Kho Hiện Tại (All Items)", icon: Boxes },
          { key: "FEFO", label: "Quản Lý Lô Hàng FEFO (Hạn Sử Dụng)", icon: Calendar },
          { key: "RECEIPTS", label: "Lịch Sử Phiếu Nhập Kho", icon: FileText },
          { key: "AUDITS", label: "Lịch Sử Kiểm Kê & Cân Kho", icon: ClipboardCheck },
        ].map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.key;
          return (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key as "STOCK" | "FEFO" | "RECEIPTS" | "AUDITS")}
              className={`px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 whitespace-nowrap transition-all ${
                isActive
                  ? "bg-[#6F4E37] text-white shadow-sm"
                  : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
              }`}
            >
              <Icon className="w-3.5 h-3.5" />
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      {/* TAB 1: CURRENT STOCK */}
      {activeTab === "STOCK" && (
        <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
          <div className="p-4 border-b border-[#EFE6DC] flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <h3 className="font-extrabold text-sm text-[#2D2522]">Danh Sách Nguyên Liệu Thực Phẩm & Pha Chế</h3>
            <div className="relative w-full sm:w-64">
              <input
                type="text"
                placeholder="Tìm nguyên liệu..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 rounded-xl border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
              />
              <Search className="w-3.5 h-3.5 text-[#786C65] absolute left-3 top-2.5" />
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#FAF7F2] text-[#786C65] font-bold uppercase tracking-wider">
                <tr>
                  <th className="py-3 px-4">Tên nguyên liệu</th>
                  <th className="py-3 px-4">Phân loại</th>
                  <th className="py-3 px-4">Tồn kho hiện tại</th>
                  <th className="py-3 px-4">Ngưỡng cảnh báo</th>
                  <th className="py-3 px-4">Đơn giá vốn</th>
                  <th className="py-3 px-4">Trạng thái</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#FAF7F2]">
                {ingredients
                  .filter((i) => !searchQuery || i.name.toLowerCase().includes(searchQuery.toLowerCase()))
                  .map((i) => {
                    const isLow = i.currentStock <= i.minAlertThreshold;
                    return (
                      <tr key={i.id} className="hover:bg-[#FAF7F2]/50 transition-colors">
                        <td className="py-3 px-4 font-bold text-[#2D2522]">{i.name}</td>
                        <td className="py-3 px-4">
                          <span className="px-2 py-0.5 rounded-md bg-[#FAF7F2] text-[#6F4E37] font-semibold text-[10px]">
                            {i.categoryType || "BEVERAGE"}
                          </span>
                        </td>
                        <td className="py-3 px-4 font-black text-sm">
                          {i.currentStock.toLocaleString("vi-VN")} {i.unit}
                        </td>
                        <td className="py-3 px-4 text-[#786C65]">
                          {i.minAlertThreshold.toLocaleString("vi-VN")} {i.unit}
                        </td>
                        <td className="py-3 px-4 text-[#6F4E37] font-bold">
                          {i.unitCost.toLocaleString("vi-VN")}đ/{i.unit}
                        </td>
                        <td className="py-3 px-4">
                          {isLow ? (
                            <span className="inline-flex items-center gap-1 text-red-600 font-bold bg-red-50 px-2 py-0.5 rounded-full text-[11px]">
                              <AlertTriangle className="w-3 h-3" /> Cảnh báo sắp hết
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 text-emerald-700 font-bold bg-emerald-50 px-2 py-0.5 rounded-full text-[11px]">
                              <Check className="w-3 h-3" /> Tồn kho an toàn
                            </span>
                          )}
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 2: FEFO BATCHES */}
      {activeTab === "FEFO" && (
        <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
          <div className="p-4 border-b border-[#EFE6DC]">
            <h3 className="font-extrabold text-sm text-[#2D2522]">Quản Lý Hạn Sử Dụng Theo Quy Trình FEFO (First Expired, First Out)</h3>
            <p className="text-xs text-[#786C65] mt-0.5">Các lô hàng có hạn sử dụng gần nhất được ưu tiên xuất kho và sử dụng trước</p>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#FAF7F2] text-[#786C65] font-bold uppercase tracking-wider">
                <tr>
                  <th className="py-3 px-4">Số lô (Batch)</th>
                  <th className="py-3 px-4">Mặt hàng</th>
                  <th className="py-3 px-4">Số lượng còn</th>
                  <th className="py-3 px-4">Ngày sản xuất</th>
                  <th className="py-3 px-4">Hạn sử dụng (EXP)</th>
                  <th className="py-3 px-4">Thời hạn còn lại</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#FAF7F2]">
                {batches.map((b) => (
                  <tr key={b.id} className="hover:bg-[#FAF7F2]/50 transition-colors">
                    <td className="py-3 px-4 font-mono font-bold text-[#6F4E37]">{b.batchNumber}</td>
                    <td className="py-3 px-4 font-bold text-[#2D2522]">{b.ingredientName}</td>
                    <td className="py-3 px-4 font-black">
                      {b.currentQuantity.toLocaleString("vi-VN")} {b.unit}
                    </td>
                    <td className="py-3 px-4 text-[#786C65]">
                      {new Date(b.manufactureDate).toLocaleDateString("vi-VN")}
                    </td>
                    <td className="py-3 px-4 font-bold text-[#2D2522]">
                      {new Date(b.expiryDate).toLocaleDateString("vi-VN")}
                    </td>
                    <td className="py-3 px-4">
                      {b.isExpired ? (
                        <span className="px-2 py-0.5 rounded-full bg-red-100 text-red-800 font-bold text-[10px]">
                          Đã hết hạn ({b.daysRemaining} ngày)
                        </span>
                      ) : b.isNearExpiry ? (
                        <span className="px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 font-bold text-[10px]">
                          Cận hạn (Còn {b.daysRemaining} ngày)
                        </span>
                      ) : (
                        <span className="px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-bold text-[10px]">
                          Còn {b.daysRemaining} ngày
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: RECEIPTS */}
      {activeTab === "RECEIPTS" && (
        <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
          <div className="p-4 border-b border-[#EFE6DC] flex items-center justify-between">
            <h3 className="font-extrabold text-sm text-[#2D2522]">Lịch Sử Các Phiếu Nhập Kho</h3>
            <button
              onClick={handleOpenReceiptModal}
              className="px-3.5 py-1.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324]"
            >
              + Tạo phiếu nhập mới
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#FAF7F2] text-[#786C65] font-bold uppercase tracking-wider">
                <tr>
                  <th className="py-3 px-4">Mã phiếu</th>
                  <th className="py-3 px-4">Nhà cung cấp</th>
                  <th className="py-3 px-4">Số mặt hàng</th>
                  <th className="py-3 px-4">Tổng tiền vốn</th>
                  <th className="py-3 px-4">Ngày tạo</th>
                  <th className="py-3 px-4">Ghi chú</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#FAF7F2]">
                {receipts.map((r) => (
                  <tr key={r.id} className="hover:bg-[#FAF7F2]/50 transition-colors">
                    <td className="py-3 px-4 font-mono font-bold text-[#6F4E37]">{r.receiptCode}</td>
                    <td className="py-3 px-4 font-bold text-[#2D2522]">{r.supplierName}</td>
                    <td className="py-3 px-4">{r.itemCount} món</td>
                    <td className="py-3 px-4 font-black text-[#6F4E37]">
                      {r.totalCost.toLocaleString("vi-VN")}đ
                    </td>
                    <td className="py-3 px-4 text-[#786C65]">
                      {new Date(r.createdAt).toLocaleString("vi-VN")}
                    </td>
                    <td className="py-3 px-4 text-[#786C65] italic">{r.notes || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 4: AUDITS */}
      {activeTab === "AUDITS" && (
        <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
          <div className="p-4 border-b border-[#EFE6DC] flex items-center justify-between">
            <h3 className="font-extrabold text-sm text-[#2D2522]">Lịch Sử Các Đợt Kiểm Kê Cân Đối Tồn Kho</h3>
            <button
              onClick={() => setShowAuditModal(true)}
              className="px-3.5 py-1.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324]"
            >
              + Thực hiện đợt kiểm kê mới
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#FAF7F2] text-[#786C65] font-bold uppercase tracking-wider">
                <tr>
                  <th className="py-3 px-4">Mã kiểm kê</th>
                  <th className="py-3 px-4">Ngày kiểm kê</th>
                  <th className="py-3 px-4">Số mặt hàng kiểm đếm</th>
                  <th className="py-3 px-4">Trạng thái</th>
                  <th className="py-3 px-4">Ghi chú</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#FAF7F2]">
                {audits.map((a) => (
                  <tr key={a.id} className="hover:bg-[#FAF7F2]/50 transition-colors">
                    <td className="py-3 px-4 font-mono font-bold text-[#6F4E37]">{a.auditCode}</td>
                    <td className="py-3 px-4 text-[#786C65]">
                      {new Date(a.auditDate).toLocaleString("vi-VN")}
                    </td>
                    <td className="py-3 px-4 font-bold text-[#2D2522]">{a.items?.length || 0} món</td>
                    <td className="py-3 px-4">
                      <span className="px-2.5 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-bold text-[10px]">
                        Đã cân bằng tồn kho
                      </span>
                    </td>
                    <td className="py-3 px-4 text-[#786C65] italic">{a.notes || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal Create Purchase Receipt */}
      {showReceiptModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 max-w-2xl w-full border border-[#EFE6DC] shadow-2xl space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-3 border-b">
              <div className="flex items-center gap-2">
                <FileText className="w-5 h-5 text-[#6F4E37]" />
                <h3 className="font-black text-base text-[#2D2522]">Tạo Phiếu Nhập Kho Nguyên Liệu Mới</h3>
              </div>
              <button onClick={() => setShowReceiptModal(false)} className="p-1 hover:opacity-70 text-xs font-bold">
                [X]
              </button>
            </div>

            <form onSubmit={handleSubmitReceipt} className="space-y-4 text-xs">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Tên Nhà Cung Cấp:</label>
                  <input
                    type="text"
                    required
                    placeholder="Công ty CP Thực Phẩm Vàng"
                    value={supplierName}
                    onChange={(e) => setSupplierName(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Ghi Chú Phiếu Nhập:</label>
                  <input
                    type="text"
                    placeholder="Nhập hàng tuần 42..."
                    value={receiptNotes}
                    onChange={(e) => setReceiptNotes(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
              </div>

              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-[#2D2522]">Danh Sách Mặt Hàng Nhập Kho (Tự Động Sinh Lô FEFO):</span>
                  <button
                    type="button"
                    onClick={handleAddReceiptItem}
                    className="px-2.5 py-1 rounded-lg bg-emerald-50 text-emerald-800 font-bold hover:bg-emerald-100"
                  >
                    + Thêm dòng
                  </button>
                </div>

                {receiptItems.map((item, idx) => (
                  <div key={idx} className="p-3 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-2">
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                      <div>
                        <label className="font-bold text-[#786C65] block mb-0.5">Nguyên liệu:</label>
                        <select
                          value={item.ingredientId}
                          onChange={(e) => {
                            const next = [...receiptItems];
                            next[idx].ingredientId = parseInt(e.target.value, 10);
                            setReceiptItems(next);
                          }}
                          className="w-full bg-white border border-[#EFE6DC] rounded-xl px-2 py-1.5 font-bold"
                        >
                          {ingredients.map((ing) => (
                            <option key={ing.id} value={ing.id}>
                              {ing.name} ({ing.unit})
                            </option>
                          ))}
                        </select>
                      </div>

                      <div>
                        <label className="font-bold text-[#786C65] block mb-0.5">Số lượng nhập:</label>
                        <input
                          type="number"
                          min="1"
                          required
                          value={item.quantity}
                          onChange={(e) => {
                            const next = [...receiptItems];
                            next[idx].quantity = parseFloat(e.target.value) || 0;
                            setReceiptItems(next);
                          }}
                          className="w-full bg-white border border-[#EFE6DC] rounded-xl px-2 py-1.5 font-bold"
                        />
                      </div>

                      <div>
                        <label className="font-bold text-[#786C65] block mb-0.5">Đơn giá vốn:</label>
                        <input
                          type="number"
                          min="0"
                          required
                          value={item.unitPrice}
                          onChange={(e) => {
                            const next = [...receiptItems];
                            next[idx].unitPrice = parseFloat(e.target.value) || 0;
                            setReceiptItems(next);
                          }}
                          className="w-full bg-white border border-[#EFE6DC] rounded-xl px-2 py-1.5 font-bold"
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
                      <div>
                        <label className="font-bold text-[#786C65] block mb-0.5">Số Lô Hàng (FEFO):</label>
                        <input
                          type="text"
                          required
                          value={item.batchNumber}
                          onChange={(e) => {
                            const next = [...receiptItems];
                            next[idx].batchNumber = e.target.value;
                            setReceiptItems(next);
                          }}
                          className="w-full bg-white border border-[#EFE6DC] rounded-xl px-2 py-1.5 font-bold font-mono"
                        />
                      </div>

                      <div>
                        <label className="font-bold text-[#786C65] block mb-0.5">Hạn Sử Dụng (EXP):</label>
                        <input
                          type="date"
                          required
                          value={item.expiryDate}
                          onChange={(e) => {
                            const next = [...receiptItems];
                            next[idx].expiryDate = e.target.value;
                            setReceiptItems(next);
                          }}
                          className="w-full bg-white border border-[#EFE6DC] rounded-xl px-2 py-1.5 font-bold"
                        />
                      </div>
                    </div>

                    {receiptItems.length > 1 && (
                      <div className="text-right">
                        <button
                          type="button"
                          onClick={() => handleRemoveReceiptItem(idx)}
                          className="text-red-600 font-bold hover:underline text-[11px]"
                        >
                          Xóa dòng này
                        </button>
                      </div>
                    )}
                  </div>
                ))}
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t">
                <button
                  type="button"
                  onClick={() => setShowReceiptModal(false)}
                  className="px-4 py-2 rounded-xl border border-[#EFE6DC] font-bold text-[#786C65]"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324] disabled:opacity-50"
                >
                  {submitting ? "Đang lưu..." : "Xác nhận nhập kho"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Stock Audit */}
      {showAuditModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 max-w-2xl w-full border border-[#EFE6DC] shadow-2xl space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-3 border-b">
              <div className="flex items-center gap-2">
                <ClipboardCheck className="w-5 h-5 text-[#6F4E37]" />
                <h3 className="font-black text-base text-[#2D2522]">Phiếu Kiểm Kê & Cân Bằng Kho Định Kỳ</h3>
              </div>
              <button onClick={() => setShowAuditModal(false)} className="p-1 hover:opacity-70 text-xs font-bold">
                [X]
              </button>
            </div>

            <form onSubmit={handleSubmitAudit} className="space-y-4 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Ghi Chú Đợt Kiểm Kê:</label>
                <input
                  type="text"
                  placeholder="Kiểm kê đầu tuần / cuối tháng..."
                  value={auditNotes}
                  onChange={(e) => setAuditNotes(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="space-y-2 max-h-72 overflow-y-auto pr-1">
                {ingredients.map((ing) => {
                  const itemData = auditCounts[ing.id] || { actualQty: ing.currentStock, reason: "" };
                  const variance = itemData.actualQty - ing.currentStock;

                  return (
                    <div key={ing.id} className="p-2.5 rounded-xl bg-[#FAF7F2] flex items-center justify-between gap-3">
                      <div className="w-48 shrink-0">
                        <span className="font-bold text-[#2D2522] block">{ing.name}</span>
                        <span className="text-[10px] text-[#786C65]">
                          Hệ thống: {ing.currentStock} {ing.unit}
                        </span>
                      </div>

                      <div className="flex items-center gap-2">
                        <span className="text-[10px] font-bold text-[#786C65]">Thực tế:</span>
                        <input
                          type="number"
                          step="any"
                          value={itemData.actualQty}
                          onChange={(e) => {
                            setAuditCounts({
                              ...auditCounts,
                              [ing.id]: {
                                ...itemData,
                                actualQty: parseFloat(e.target.value) || 0,
                              },
                            });
                          }}
                          className="w-24 bg-white border border-[#EFE6DC] rounded-lg px-2 py-1 font-bold text-center"
                        />
                        <span className="text-[10px] text-[#786C65]">{ing.unit}</span>
                      </div>

                      <div className="w-24 text-right">
                        {variance === 0 ? (
                          <span className="text-emerald-700 font-bold text-[11px]">Khớp (0)</span>
                        ) : variance > 0 ? (
                          <span className="text-blue-700 font-black text-[11px]">+{variance} {ing.unit}</span>
                        ) : (
                          <span className="text-red-600 font-black text-[11px]">{variance} {ing.unit}</span>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t">
                <button
                  type="button"
                  onClick={() => setShowAuditModal(false)}
                  className="px-4 py-2 rounded-xl border border-[#EFE6DC] font-bold text-[#786C65]"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324] disabled:opacity-50"
                >
                  {submitting ? "Đang xử lý..." : "Cập nhật và cân kho"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
