"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { Category, Product } from "@/types";
import {
  Plus,
  Trash2,
  Edit2,
  X,
  Search,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

export default function AdminProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  // Modal State
  const [modalOpen, setModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);

  // Form State
  const [formData, setFormData] = useState({
    name: "",
    categoryId: 1,
    basePrice: 45000,
    imageUrl: "",
    description: "",
    isBestSeller: false,
    isNew: false,
    stockStatus: 1,
  });

  const loadData = async () => {
    setLoading(true);
    try {
      const [prods, cats] = await Promise.all([
        api.getProducts(),
        api.getCategories(),
      ]);
      setProducts(prods || []);
      setCategories(cats || []);
      if (cats?.length > 0 && formData.categoryId === 1) {
        setFormData((prev) => ({ ...prev, categoryId: cats[0].Id ?? cats[0].id }));
      }
    } catch (err) {
      console.error("Lỗi khi tải sản phẩm:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenAdd = () => {
    setEditingProduct(null);
    setFormData({
      name: "",
      categoryId: categories[0]?.Id ?? categories[0]?.id ?? 1,
      basePrice: 45000,
      imageUrl: "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=800&auto=format&fit=crop&q=80",
      description: "",
      isBestSeller: false,
      isNew: true,
      stockStatus: 1,
    });
    setModalOpen(true);
  };

  const handleOpenEdit = (p: Product) => {
    setEditingProduct(p);
    setFormData({
      name: p.Name ?? p.name,
      categoryId: p.CategoryId ?? p.categoryId,
      basePrice: p.BasePrice ?? p.basePrice,
      imageUrl: p.ImageUrl ?? p.imageUrl,
      description: p.Description ?? p.description,
      isBestSeller: p.IsBestSeller ?? p.isBestSeller,
      isNew: p.IsNew ?? p.isNew,
      stockStatus: p.StockStatus ?? p.stockStatus,
    });
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingProduct) {
        const id = editingProduct.Id ?? editingProduct.id;
        await api.updateProduct(id, formData);
      } else {
        await api.createProduct(formData);
      }
      setModalOpen(false);
      loadData();
    } catch {
      alert("Lỗi khi lưu sản phẩm.");
    }
  };

  const handleDelete = async (id: number) => {
    if (confirm("Bạn có chắc chắn muốn ngừng kinh doanh sản phẩm này?")) {
      try {
        await api.deleteProduct(id);
        loadData();
      } catch {
        alert("Lỗi khi ngừng kinh doanh sản phẩm.");
      }
    }
  };

  // Pagination (4 món 1 trang)
  const ITEMS_PER_PAGE = 4;
  const [currentPage, setCurrentPage] = useState(1);

  const filtered = products.filter((p) => {
    const name = p.Name ?? p.name ?? "";
    const cat = p.CategoryName ?? p.categoryName ?? "";
    return (
      name.toLowerCase().includes(search.toLowerCase()) ||
      cat.toLowerCase().includes(search.toLowerCase())
    );
  });

  const totalItems = filtered.length;
  const totalPages = Math.ceil(totalItems / ITEMS_PER_PAGE) || 1;
  const validPage = Math.min(Math.max(1, currentPage), totalPages);
  const startIndex = (validPage - 1) * ITEMS_PER_PAGE;
  const endIndex = Math.min(startIndex + ITEMS_PER_PAGE, totalItems);
  const paginated = filtered.slice(startIndex, endIndex);

  const handlePageChange = (newPage: number) => {
    if (newPage < 1 || newPage > totalPages) return;
    setCurrentPage(newPage);
  };

  return (
    <div className="space-y-6">
      {/* Title & Action Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-[#2D2522]">Quản Lý Thực Đơn & Sản Phẩm</h1>
          <p className="text-xs text-[#786C65] mt-0.5">
            Thêm mới, sửa giá, cập nhật trạng thái còn hàng và mô tả món đồ uống
          </p>
        </div>

        <button
          onClick={handleOpenAdd}
          className="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shadow-md self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          <span>Thêm Sản Phẩm Mới</span>
        </button>
      </div>

      {/* Search Input */}
      <div className="relative max-w-sm">
        <input
          type="text"
          placeholder="Tìm theo tên sản phẩm..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setCurrentPage(1);
          }}
          className="w-full pl-9 pr-4 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
        />
        <Search className="w-4 h-4 text-[#786C65] absolute left-3 top-2.5" />
      </div>

      {/* Products Table */}
      <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#FAF7F2] text-[#786C65] font-bold border-b border-[#EFE6DC]">
              <tr>
                <th className="p-4">Hình ảnh</th>
                <th className="p-4">Tên sản phẩm</th>
                <th className="p-4">Danh mục</th>
                <th className="p-4">Giá cơ bản</th>
                <th className="p-4">Tình trạng</th>
                <th className="p-4 text-right">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#FAF7F2]">
              {loading ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-[#786C65]">
                    Đang tải danh sách món...
                  </td>
                </tr>
              ) : paginated.length === 0 ? (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-[#786C65]">
                    Không tìm thấy sản phẩm nào phù hợp
                  </td>
                </tr>
              ) : (
                paginated.map((p) => {
                  const id = p.Id ?? p.id;
                  const name = p.Name ?? p.name;
                  const price = p.BasePrice ?? p.basePrice;
                  const img = p.ImageUrl ?? p.imageUrl;
                  const catName = p.CategoryName ?? p.categoryName;
                  const inStock = (p.StockStatus ?? p.stockStatus) === 1;

                  return (
                    <tr key={id} className="hover:bg-[#FAF7F2] transition-colors">
                      <td className="p-4">
                        <div className="w-12 h-12 rounded-xl overflow-hidden bg-[#FAF7F2] border border-[#EFE6DC]">
                          <img src={img} alt={name} className="w-full h-full object-cover" />
                        </div>
                      </td>
                      <td className="p-4 font-bold text-[#2D2522]">{name}</td>
                      <td className="p-4 text-[#786C65]">{catName}</td>
                      <td className="p-4 font-black text-[#6F4E37]">{price.toLocaleString("vi-VN")}đ</td>
                      <td className="p-4">
                        <span
                          className={`px-2.5 py-0.5 rounded-full font-bold text-[10px] ${
                            inStock ? "bg-emerald-100 text-emerald-800" : "bg-red-100 text-red-800"
                          }`}
                        >
                          {inStock ? "Còn hàng" : "Hết hàng"}
                        </span>
                      </td>
                      <td className="p-4 text-right">
                        <div className="inline-flex items-center gap-2">
                          <button
                            onClick={() => handleOpenEdit(p)}
                            className="p-1.5 text-[#6F4E37] hover:bg-[#FAF7F2] rounded-lg transition-colors"
                            title="Chỉnh sửa"
                          >
                            <Edit2 className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleDelete(id)}
                            className="p-1.5 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                            title="Ngừng bán"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Bar (4 món / trang) */}
        {totalItems > 0 && (
          <div className="p-4 bg-[#FAF7F2] border-t border-[#EFE6DC] flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
            <div className="text-[#786C65] font-semibold">
              Đang hiển thị <span className="font-black text-[#2D2522]">{startIndex + 1} - {endIndex}</span> trên tổng số <span className="font-black text-[#6F4E37]">{totalItems} món</span> (4 món / trang)
            </div>

            <div className="flex items-center gap-1.5">
              <button
                onClick={() => handlePageChange(validPage - 1)}
                disabled={validPage === 1}
                className={`px-3 py-1.5 rounded-xl font-bold flex items-center gap-1 transition-all ${
                  validPage === 1
                    ? "bg-white text-gray-300 border border-[#EFE6DC] cursor-not-allowed"
                    : "bg-white text-[#2D2522] border border-[#EFE6DC] hover:bg-[#6F4E37] hover:text-white shadow-xs active:scale-95"
                }`}
                title="Trang trước"
              >
                <ChevronLeft className="w-3.5 h-3.5" />
                <span>Trước</span>
              </button>

              <span className="px-3 py-1 font-black text-[#6F4E37] bg-white border border-[#EFE6DC] rounded-xl">
                Trang {validPage} / {totalPages}
              </span>

              <button
                onClick={() => handlePageChange(validPage + 1)}
                disabled={validPage === totalPages}
                className={`px-3 py-1.5 rounded-xl font-bold flex items-center gap-1 transition-all ${
                  validPage === totalPages
                    ? "bg-white text-gray-300 border border-[#EFE6DC] cursor-not-allowed"
                    : "bg-white text-[#2D2522] border border-[#EFE6DC] hover:bg-[#6F4E37] hover:text-white shadow-xs active:scale-95"
                }`}
                title="Trang tiếp theo"
              >
                <span>Sau</span>
                <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Modal Add / Edit */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-xl w-full max-w-lg p-6 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-[#FAF7F2]">
              <h3 className="font-black text-sm text-[#2D2522]">
                {editingProduct ? "Chỉnh Sửa Sản Phẩm" : "Thêm Sản Phẩm Mới"}
              </h3>
              <button onClick={() => setModalOpen(false)} className="text-[#786C65] hover:text-black">
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-3 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Tên món trà sữa *</label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Danh mục *</label>
                  <select
                    value={formData.categoryId}
                    onChange={(e) => setFormData({ ...formData, categoryId: parseInt(e.target.value, 10) })}
                    className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  >
                    {categories.map((c) => (
                      <option key={c.Id ?? c.id} value={c.Id ?? c.id}>
                        {c.Name ?? c.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Giá cơ bản (VNĐ) *</label>
                  <input
                    type="number"
                    required
                    value={formData.basePrice}
                    onChange={(e) => setFormData({ ...formData, basePrice: parseFloat(e.target.value) })}
                    className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">URL Hình ảnh</label>
                <input
                  type="text"
                  value={formData.imageUrl}
                  onChange={(e) => setFormData({ ...formData, imageUrl: e.target.value })}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Mô tả sản phẩm</label>
                <textarea
                  rows={3}
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-3 py-2 rounded-xl bg-[#FAF7F2] border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="flex items-center gap-6 pt-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isBestSeller}
                    onChange={(e) => setFormData({ ...formData, isBestSeller: e.target.checked })}
                    className="rounded text-[#6F4E37]"
                  />
                  <span>Bán chạy (Best Seller)</span>
                </label>

                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={formData.isNew}
                    onChange={(e) => setFormData({ ...formData, isNew: e.target.checked })}
                    className="rounded text-[#6F4E37]"
                  />
                  <span>Món mới</span>
                </label>
              </div>

              <div className="pt-4 flex justify-end gap-2 border-t border-[#FAF7F2]">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="px-4 py-2 rounded-xl bg-[#FAF7F2] text-[#786C65] font-bold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324]"
                >
                  Lưu Sản Phẩm
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
