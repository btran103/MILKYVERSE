"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { Ingredient } from "@/types";
import {
  RefreshCw,
  AlertTriangle,
  ArrowDownRight,
  ArrowUpRight,
  X,
} from "lucide-react";

export default function AdminInventoryPage() {
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [loading, setLoading] = useState(true);

  // Modal State
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedIngredient, setSelectedIngredient] = useState<Ingredient | null>(null);
  const [adjustType, setAdjustType] = useState<"IMPORT" | "EXPORT" | "ADJUST">("IMPORT");
  const [quantity, setQuantity] = useState<number>(1000);
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await api.getIngredients();
      setIngredients(data || []);
    } catch (err) {
      console.error("Lỗi khi tải kho:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenAdjust = (ing: Ingredient, type: "IMPORT" | "EXPORT") => {
    setSelectedIngredient(ing);
    setAdjustType(type);
    setQuantity(type === "IMPORT" ? 2000 : 500);
    setNote(type === "IMPORT" ? "Nhập kho định kỳ từ nhà cung ứng" : "Xuất hao hụt / pha chế");
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedIngredient) return;

    setSubmitting(true);
    try {
      await api.adjustStock(selectedIngredient.id, adjustType, quantity, note);
      setModalOpen(false);
      loadData();
    } catch {
      alert("Lỗi khi cập nhật kho.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Title */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-[#2D2522]">Quản Lý Tồn Kho & Nguyên Liệu</h1>
          <p className="text-xs text-[#786C65] mt-0.5">
            Theo dõi định lượng lá trà, sữa, trân châu và cập nhật lịch sử nhập xuất kho
          </p>
        </div>

        <button
          onClick={loadData}
          className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs font-bold text-[#6F4E37] hover:bg-[#FAF7F2] self-start sm:self-auto shadow-sm"
        >
          <RefreshCw className="w-3.5 h-3.5" />
          <span>Làm mới số liệu</span>
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#FAF7F2] text-[#786C65] font-bold border-b border-[#EFE6DC]">
              <tr>
                <th className="p-4">Tên nguyên liệu</th>
                <th className="p-4">Đơn vị</th>
                <th className="p-4">Tồn kho hiện tại</th>
                <th className="p-4">Ngưỡng cảnh báo</th>
                <th className="p-4">Trạng thái</th>
                <th className="p-4 text-right">Điều chỉnh kho</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#FAF7F2]">
              {loading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-[#786C65]">
                    <RefreshCw className="w-5 h-5 animate-spin mx-auto mb-2 text-[#6F4E37]" />
                    Đang tải danh sách kho...
                  </td>
                </tr>
              ) : ingredients.length === 0 ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-[#786C65]">Chưa có nguyên liệu nào trong kho.</td>
                </tr>
              ) : (
                ingredients.map((ing) => {
                const isLow = ing.currentStock <= ing.minAlertThreshold;

                return (
                  <tr key={ing.id} className="hover:bg-[#FAF7F2] transition-colors">
                    <td className="p-4 font-bold text-[#2D2522]">{ing.name}</td>
                    <td className="p-4 text-[#786C65]">{ing.unit}</td>
                    <td className="p-4 font-black text-[#2D2522]">
                      {ing.currentStock.toLocaleString("vi-VN")} {ing.unit}
                    </td>
                    <td className="p-4 text-[#786C65]">
                      {ing.minAlertThreshold.toLocaleString("vi-VN")} {ing.unit}
                    </td>
                    <td className="p-4">
                      {isLow ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full font-bold text-[10px] bg-amber-100 text-amber-800">
                          <AlertTriangle className="w-3 h-3" />
                          <span>Sắp hết</span>
                        </span>
                      ) : (
                        <span className="px-2.5 py-0.5 rounded-full font-bold text-[10px] bg-emerald-100 text-emerald-800">
                          Đủ tồn kho
                        </span>
                      )}
                    </td>
                    <td className="p-4 text-right">
                      <div className="inline-flex items-center gap-2">
                        <button
                          onClick={() => handleOpenAdjust(ing, "IMPORT")}
                          className="px-2.5 py-1 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 font-bold transition-colors flex items-center gap-1"
                          title="Nhập thêm"
                        >
                          <ArrowDownRight className="w-3.5 h-3.5" />
                          <span>Nhập</span>
                        </button>
                        <button
                          onClick={() => handleOpenAdjust(ing, "EXPORT")}
                          className="px-2.5 py-1 rounded-lg bg-red-50 text-red-700 hover:bg-red-100 font-bold transition-colors flex items-center gap-1"
                          title="Xuất kho"
                        >
                          <ArrowUpRight className="w-3.5 h-3.5" />
                          <span>Xuất</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              }))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Adjust Modal */}
      {modalOpen && selectedIngredient && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-xl w-full max-w-md p-6 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-[#FAF7F2]">
              <h3 className="font-black text-sm text-[#2D2522]">
                {adjustType === "IMPORT" ? "Nhập Kho Thêm" : "Xuất Kho Nguyên Liệu"}: {selectedIngredient.name}
              </h3>
              <button onClick={() => setModalOpen(false)} className="text-[#786C65] hover:text-black">
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-3 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">
                  Số lượng ({selectedIngredient.unit}) *
                </label>
                <input
                  type="number"
                  required
                  min="1"
                  value={quantity}
                  onChange={(e) => setQuantity(parseFloat(e.target.value))}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Ghi chú / Lý do *</label>
                <textarea
                  rows={2}
                  required
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="pt-3 flex justify-end gap-2 border-t border-[#FAF7F2]">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="px-4 py-2 rounded-xl bg-[#FAF7F2] text-[#786C65] font-bold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className={`px-5 py-2 rounded-xl font-bold text-white ${
                    adjustType === "IMPORT" ? "bg-[#4E7D55]" : "bg-red-600"
                  }`}
                >
                  {submitting ? "Đang xử lý..." : "Xác Nhận"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
