"use client";

import React, { useEffect, useState, useRef } from "react";
import { api } from "@/lib/api";
import { User, UserRole } from "@/types";
import {
  UserPlus,
  Check,
  X,
  Search,
  Mail,
  Phone,
  RefreshCw,
  Edit2,
  Camera,
  MapPin,
  Save,
} from "lucide-react";

export default function AdminStaffManagementPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterRole, setFilterRole] = useState<string>("ALL");
  const [searchQuery, setSearchQuery] = useState("");

  // Modal create
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({
    fullName: "",
    email: "",
    password: "",
    phoneNumber: "",
    address: "",
    role: "Staff" as UserRole,
  });

  // Modal edit customer / staff details & upload avatar from PC
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [editForm, setEditForm] = useState({
    fullName: "",
    email: "",
    phoneNumber: "",
    address: "",
    avatarUrl: "",
    role: "Customer" as UserRole,
    newPassword: "",
  });
  const editFileInputRef = useRef<HTMLInputElement | null>(null);

  const [submitting, setSubmitting] = useState(false);
  const [msg, setMsg] = useState<{ text: string; error?: boolean } | null>(null);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const data = await api.getUsers(filterRole !== "ALL" ? filterRole : undefined);
      setUsers(data || []);
    } catch (err) {
      console.error("Lỗi khi tải danh sách người dùng:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, [filterRole]);

  const handleOpenEditModal = (u: User) => {
    setEditingUser(u);
    setEditForm({
      fullName: u.fullName || "",
      email: u.email || "",
      phoneNumber: u.phoneNumber || "",
      address: u.address || "",
      avatarUrl: u.avatarUrl || "",
      role: (u.role as UserRole) || "Customer",
      newPassword: "",
    });
  };

  const handleUploadEditAvatar = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.type.startsWith("image/")) {
      setMsg({ text: "Vui lòng chọn tệp hình ảnh hợp lệ (.jpg, .png, .webp...).", error: true });
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      const img = new window.Image();
      img.onload = () => {
        const canvas = document.createElement("canvas");
        const size = 256;
        canvas.width = size;
        canvas.height = size;
        const ctx = canvas.getContext("2d");
        if (ctx) {
          const minSide = Math.min(img.width, img.height);
          const sx = (img.width - minSide) / 2;
          const sy = (img.height - minSide) / 2;
          ctx.drawImage(img, sx, sy, minSide, minSide, 0, 0, size, size);
          const compressedDataUrl = canvas.toDataURL("image/jpeg", 0.85);
          setEditForm((prev) => ({ ...prev, avatarUrl: compressedDataUrl }));
        }
      };
      img.src = event.target?.result as string;
    };
    reader.readAsDataURL(file);
    e.target.value = "";
  };

  const handleSaveUserEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingUser) return;
    if (!editForm.fullName.trim()) {
      setMsg({ text: "Họ và tên không được để trống.", error: true });
      return;
    }

    setSubmitting(true);
    setMsg(null);
    try {
      const updated = await api.updateUserDetails(editingUser.id, {
        fullName: editForm.fullName.trim(),
        email: editForm.email.trim(),
        phoneNumber: editForm.phoneNumber.trim(),
        address: editForm.address.trim(),
        avatarUrl: editForm.avatarUrl,
        role: editForm.role,
        newPassword: editForm.newPassword.trim() || undefined,
      });

      setUsers((prev) => prev.map((u) => (u.id === editingUser.id ? { ...u, ...updated } : u)));
      setMsg({ text: `Đã cập nhật thông tin & ảnh đại diện cho "${editForm.fullName}" thành công!` });
      setEditingUser(null);
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : "Cập nhật thông tin thất bại.";
      setMsg({ text: errorMsg, error: true });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateEmployee = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!createForm.email || !createForm.password || !createForm.fullName) {
      setMsg({ text: "Vui lòng nhập đầy đủ tên, email và mật khẩu", error: true });
      return;
    }

    setSubmitting(true);
    setMsg(null);
    try {
      await api.createEmployee(createForm);
      setMsg({ text: `Đã tạo tài khoản ${createForm.fullName} thành công!` });
      setShowCreateModal(false);
      setCreateForm({
        fullName: "",
        email: "",
        password: "",
        phoneNumber: "",
        address: "",
        role: "Staff",
      });
      loadUsers();
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : "Tạo tài khoản thất bại.";
      setMsg({ text: errorMsg, error: true });
    } finally {
      setSubmitting(false);
    }
  };

  const handleRoleChange = async (userId: number, newRole: string, isActive: boolean) => {
    try {
      await api.updateUserRole(userId, { role: newRole, isActive });
      setUsers((prev) =>
        prev.map((u) => (u.id === userId ? { ...u, role: newRole as UserRole, isActive } : u))
      );
      setMsg({ text: "Cập nhật quyền hạn thành công!" });
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : "Cập nhật quyền thất bại.";
      setMsg({ text: errorMsg, error: true });
    }
  };

  const filteredUsers = users.filter((u) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    return (
      u.fullName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q) ||
      (u.phoneNumber && u.phoneNumber.includes(q))
    );
  });

  const getRoleBadge = (role: string) => {
    switch (role) {
      case "Admin":
        return <span className="px-2.5 py-1 rounded-full bg-red-100 text-red-800 text-xs font-black">Quản trị tối cao (Admin)</span>;
      case "Manager":
        return <span className="px-2.5 py-1 rounded-full bg-purple-100 text-purple-800 text-xs font-black">Cửa hàng trưởng (Manager)</span>;
      case "Staff":
        return <span className="px-2.5 py-1 rounded-full bg-amber-100 text-amber-800 text-xs font-black">Quầy & Bếp (Staff)</span>;
      case "WarehouseStaff":
        return <span className="px-2.5 py-1 rounded-full bg-blue-100 text-blue-800 text-xs font-black">Thủ kho (Warehouse)</span>;
      case "DeliveryStaff":
        return <span className="px-2.5 py-1 rounded-full bg-emerald-100 text-emerald-800 text-xs font-black">Giao hàng (Shipper)</span>;
      default:
        return <span className="px-2.5 py-1 rounded-full bg-gray-100 text-gray-700 text-xs font-bold">Khách hàng</span>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-[#EFE6DC]">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-[#2D2522]">
            Quản Lý Khách Hàng, Nhân Sự & Phân Quyền
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Chỉnh sửa thông tin khách hàng, gắn ảnh đại diện từ máy tính và phân quyền nhân viên
          </p>
        </div>

        <button
          onClick={() => setShowCreateModal(true)}
          className="inline-flex items-center gap-2 px-4 py-2.5 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all"
        >
          <UserPlus className="w-4 h-4" />
          <span>Thêm Tài Khoản Mới</span>
        </button>
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

      {/* Filter Tabs & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
          {[
            { key: "ALL", label: "Tất cả" },
            { key: "Customer", label: "Khách hàng" },
            { key: "Manager", label: "Quản lý (Manager)" },
            { key: "Staff", label: "Quầy & Bếp" },
            { key: "WarehouseStaff", label: "Thủ kho" },
            { key: "DeliveryStaff", label: "Giao hàng (Shipper)" },
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => setFilterRole(tab.key)}
              className={`px-3.5 py-2 rounded-xl text-xs font-bold whitespace-nowrap transition-all ${
                filterRole === tab.key
                  ? "bg-[#6F4E37] text-white shadow-sm"
                  : "bg-white border border-[#EFE6DC] text-[#2D2522] hover:bg-[#FAF7F2]"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="relative w-full sm:w-64">
          <input
            type="text"
            placeholder="Tìm theo tên, email, sđt..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-3 py-2 rounded-xl bg-white border border-[#EFE6DC] text-xs focus:outline-none focus:border-[#6F4E37]"
          />
          <Search className="w-3.5 h-3.5 text-[#786C65] absolute left-3 top-3" />
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#FAF7F2] border-b border-[#EFE6DC] text-[#786C65] font-bold uppercase tracking-wider">
              <tr>
                <th className="py-3 px-4">Ảnh & Họ tên</th>
                <th className="py-3 px-4">Email & Liên hệ</th>
                <th className="py-3 px-4">Vai trò hiện tại</th>
                <th className="py-3 px-4">Trạng thái</th>
                <th className="py-3 px-4 text-right">Thao tác & Chỉnh sửa</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#FAF7F2]">
              {loading ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-[#786C65]">
                    <RefreshCw className="w-6 h-6 animate-spin mx-auto text-[#6F4E37] mb-2" />
                    Đang tải danh sách...
                  </td>
                </tr>
              ) : filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-[#786C65]">
                    Không có người dùng nào phù hợp.
                  </td>
                </tr>
              ) : (
                filteredUsers.map((u) => (
                  <tr key={u.id} className="hover:bg-[#FAF7F2]/50 transition-colors">
                    <td className="py-3.5 px-4">
                      <div className="flex items-center gap-3">
                        <div
                          onClick={() => handleOpenEditModal(u)}
                          title="Nhấn để sửa thông tin & đổi ảnh"
                          className="w-10 h-10 rounded-full overflow-hidden border-2 border-[#F7D6D0] bg-[#FAF7F2] shrink-0 flex items-center justify-center cursor-pointer"
                        >
                          {u.avatarUrl ? (
                            <img src={u.avatarUrl} alt={u.fullName} className="w-full h-full object-cover" />
                          ) : (
                            <span className="font-black text-xs text-[#6F4E37]">
                              {u.fullName ? u.fullName.charAt(0).toUpperCase() : "U"}
                            </span>
                          )}
                        </div>
                        <div>
                          <div className="font-extrabold text-[#2D2522]">{u.fullName}</div>
                          <div className="text-[10px] text-[#786C65]">ID: #{u.id}</div>
                        </div>
                      </div>
                    </td>
                    <td className="py-3.5 px-4 space-y-0.5">
                      <div className="flex items-center gap-1.5 text-[#2D2522]">
                        <Mail className="w-3 h-3 text-[#786C65]" />
                        <span>{u.email}</span>
                      </div>
                      {u.phoneNumber && (
                        <div className="flex items-center gap-1.5 text-[#786C65]">
                          <Phone className="w-3 h-3" />
                          <span>{u.phoneNumber}</span>
                        </div>
                      )}
                      {u.address && (
                        <div className="flex items-center gap-1.5 text-[11px] text-[#786C65] truncate max-w-xs">
                          <MapPin className="w-3 h-3 shrink-0" />
                          <span className="truncate">{u.address}</span>
                        </div>
                      )}
                    </td>
                    <td className="py-3.5 px-4">
                      {getRoleBadge(u.role)}
                    </td>
                    <td className="py-3.5 px-4">
                      {u.isActive !== false ? (
                        <span className="inline-flex items-center gap-1 text-emerald-700 font-bold">
                          <Check className="w-3.5 h-3.5" /> Hoạt động
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-red-600 font-bold">
                          <X className="w-3.5 h-3.5" /> Đã khóa
                        </span>
                      )}
                    </td>
                    <td className="py-3.5 px-4 text-right space-x-1.5 whitespace-nowrap">
                      <button
                        type="button"
                        onClick={() => handleOpenEditModal(u)}
                        className="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl bg-[#FAF7F2] border border-[#6F4E37]/40 text-[#6F4E37] hover:bg-[#6F4E37] hover:text-white font-bold text-[11px] transition-colors"
                      >
                        <Edit2 className="w-3 h-3" />
                        <span>Sửa TT & Ảnh</span>
                      </button>

                      <select
                        value={u.role}
                        onChange={(e) => handleRoleChange(u.id, e.target.value, u.isActive !== false)}
                        className="bg-white border border-[#EFE6DC] rounded-xl px-2 py-1.5 font-bold text-xs text-[#2D2522] focus:outline-none focus:border-[#6F4E37]"
                      >
                        <option value="Customer">Khách hàng</option>
                        <option value="Staff">Quầy & Bếp (Staff)</option>
                        <option value="WarehouseStaff">Thủ kho (Warehouse)</option>
                        <option value="DeliveryStaff">Giao hàng (Shipper)</option>
                        <option value="Manager">Quản lý (Manager)</option>
                        <option value="Admin">Quản trị (Admin)</option>
                      </select>

                      <button
                        type="button"
                        onClick={() => handleRoleChange(u.id, u.role, !(u.isActive !== false))}
                        className={`px-2.5 py-1.5 rounded-xl font-bold text-[11px] transition-colors ${
                          u.isActive !== false
                            ? "bg-red-50 text-red-700 hover:bg-red-100"
                            : "bg-emerald-50 text-emerald-700 hover:bg-emerald-100"
                        }`}
                      >
                        {u.isActive !== false ? "Khóa" : "Mở khóa"}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Sửa Thông Tin Khách Hàng / Nhân Sự & Gắn Ảnh Từ Máy Tính */}
      {editingUser && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-lg w-full border border-[#EFE6DC] shadow-2xl space-y-5 max-h-[92vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-3 border-b border-[#EFE6DC]">
              <div className="flex items-center gap-2">
                <Edit2 className="w-5 h-5 text-[#6F4E37]" />
                <h3 className="font-black text-lg text-[#2D2522]">
                  Sửa Thông Tin Khách Hàng / Tài Khoản #{editingUser.id}
                </h3>
              </div>
              <button onClick={() => setEditingUser(null)} className="p-1 hover:opacity-70">
                <X className="w-5 h-5 text-[#786C65]" />
              </button>
            </div>

            <form onSubmit={handleSaveUserEdit} className="space-y-4 text-xs">
              {/* Khung Ảnh Đại Diện + Nút Dưới Ảnh Nhấn Vô Thay Đổi Ảnh Từ Máy Tính */}
              <div className="flex flex-col items-center text-center p-4 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] space-y-3">
                <div
                  onClick={() => editFileInputRef.current?.click()}
                  className="group relative w-24 h-24 rounded-full overflow-hidden border-4 border-white shadow-md bg-white flex items-center justify-center cursor-pointer"
                >
                  {editForm.avatarUrl ? (
                    <img
                      src={editForm.avatarUrl}
                      alt={editForm.fullName}
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center bg-[#6F4E37] text-white font-black text-2xl">
                      {editForm.fullName ? editForm.fullName.charAt(0).toUpperCase() : "U"}
                    </div>
                  )}
                  <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center text-white">
                    <Camera className="w-5 h-5" />
                  </div>
                </div>

                <input
                  ref={editFileInputRef}
                  type="file"
                  accept="image/*"
                  onChange={handleUploadEditAvatar}
                  className="hidden"
                />
                <button
                  type="button"
                  onClick={() => editFileInputRef.current?.click()}
                  className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all active:scale-95"
                >
                  <Camera className="w-4 h-4 text-[#F7D6D0]" />
                  <span>Thay Đổi Ảnh Từ Máy Tính</span>
                </button>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Họ và tên khách hàng *</label>
                  <input
                    type="text"
                    required
                    value={editForm.fullName}
                    onChange={(e) => setEditForm({ ...editForm, fullName: e.target.value })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Email *</label>
                  <input
                    type="email"
                    required
                    value={editForm.email}
                    onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Số điện thoại</label>
                  <input
                    type="tel"
                    value={editForm.phoneNumber}
                    onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })}
                    placeholder="0901234567"
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Vai trò</label>
                  <select
                    value={editForm.role}
                    onChange={(e) => setEditForm({ ...editForm, role: e.target.value as UserRole })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] font-bold focus:outline-none focus:border-[#6F4E37]"
                  >
                    <option value="Customer">Khách hàng (Customer)</option>
                    <option value="Staff">Quầy & Bếp (Staff)</option>
                    <option value="WarehouseStaff">Thủ kho (WarehouseStaff)</option>
                    <option value="DeliveryStaff">Giao hàng (DeliveryStaff)</option>
                    <option value="Manager">Quản lý (Manager)</option>
                    <option value="Admin">Quản trị (Admin)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Địa chỉ nhận hàng</label>
                <input
                  type="text"
                  value={editForm.address}
                  onChange={(e) => setEditForm({ ...editForm, address: e.target.value })}
                  placeholder="Số nhà, đường, phường/xã, quận/huyện..."
                  className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">
                  Đặt lại mật khẩu mới (Để trống nếu giữ nguyên)
                </label>
                <input
                  type="password"
                  value={editForm.newPassword}
                  onChange={(e) => setEditForm({ ...editForm, newPassword: e.target.value })}
                  placeholder="Nhập mật khẩu mới nếu muốn cấp lại..."
                  className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="pt-2 flex items-center justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setEditingUser(null)}
                  className="px-4 py-2.5 rounded-xl border border-[#EFE6DC] font-bold text-[#786C65] hover:bg-[#FAF7F2]"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="inline-flex items-center gap-1.5 px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324] shadow-sm disabled:opacity-50"
                >
                  <Save className="w-3.5 h-3.5" />
                  <span>{submitting ? "Đang lưu..." : "Lưu Thông Tin Khách Hàng"}</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Create Employee */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-lg w-full border border-[#EFE6DC] shadow-2xl space-y-5 animate-in fade-in zoom-in duration-200">
            <div className="flex items-center justify-between pb-3 border-b border-[#EFE6DC]">
              <div className="flex items-center gap-2">
                <UserPlus className="w-5 h-5 text-[#6F4E37]" />
                <h3 className="font-black text-lg text-[#2D2522]">Tạo Tài Khoản Mới</h3>
              </div>
              <button onClick={() => setShowCreateModal(false)} className="p-1 hover:opacity-70">
                <X className="w-5 h-5 text-[#786C65]" />
              </button>
            </div>

            <form onSubmit={handleCreateEmployee} className="space-y-4 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Họ Và Tên:</label>
                <input
                  type="text"
                  required
                  placeholder="Ví dụ: Nguyễn Văn A"
                  value={createForm.fullName}
                  onChange={(e) => setCreateForm({ ...createForm, fullName: e.target.value })}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Email:</label>
                  <input
                    type="email"
                    required
                    placeholder="user@milkyverse.com"
                    value={createForm.email}
                    onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>

                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Mật Khẩu Khởi Tạo:</label>
                  <input
                    type="password"
                    required
                    placeholder="Mật khẩu ít nhất 8 ký tự"
                    value={createForm.password}
                    onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Số Điện Thoại:</label>
                  <input
                    type="tel"
                    placeholder="090..."
                    value={createForm.phoneNumber}
                    onChange={(e) => setCreateForm({ ...createForm, phoneNumber: e.target.value })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                  />
                </div>

                <div>
                  <label className="font-bold text-[#2D2522] block mb-1">Vai Trò Phân Bổ:</label>
                  <select
                    value={createForm.role}
                    onChange={(e) => setCreateForm({ ...createForm, role: e.target.value as UserRole })}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] font-bold focus:outline-none focus:border-[#6F4E37]"
                  >
                    <option value="Customer">Khách hàng (Customer)</option>
                    <option value="Staff">Quầy & Bếp (Staff)</option>
                    <option value="WarehouseStaff">Thủ kho (WarehouseStaff)</option>
                    <option value="DeliveryStaff">Giao hàng (DeliveryStaff)</option>
                    <option value="Manager">Quản lý (Manager)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Địa Chỉ:</label>
                <input
                  type="text"
                  placeholder="Ví dụ: Quận 1, TP.HCM"
                  value={createForm.address}
                  onChange={(e) => setCreateForm({ ...createForm, address: e.target.value })}
                  className="w-full px-3.5 py-2.5 rounded-xl border border-[#EFE6DC] focus:outline-none focus:border-[#6F4E37]"
                />
              </div>

              <div className="pt-2 flex items-center justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="px-4 py-2.5 rounded-xl border border-[#EFE6DC] font-bold text-[#786C65] hover:bg-[#FAF7F2]"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2.5 rounded-xl bg-[#6F4E37] text-white font-bold hover:bg-[#4A3324] shadow-sm disabled:opacity-50"
                >
                  {submitting ? "Đang tạo..." : "Xác nhận tạo tài khoản"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
