"use client";

import React, { useEffect, useState } from "react";
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
  const [submitting, setSubmitting] = useState(false);
  const [msg, setMsg] = useState<{ text: string; error?: boolean } | null>(null);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const data = await api.getUsers(filterRole !== "ALL" ? filterRole : undefined);
      setUsers(data || []);
    } catch (err) {
      console.error("Lỗi khi tải danh sách nhân sự:", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, [filterRole]);

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
      setMsg({ text: `Đã tạo nhân viên ${createForm.fullName} thành công!` });
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
      const errorMsg = err instanceof Error ? err.message : "Tạo nhân viên thất bại.";
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
            Quản Lý Nhân Sự & Phân Quyền
          </h1>
          <p className="text-xs text-[#786C65] mt-1">
            Quản lý tài khoản 4 vai trò nhân viên: Quản lý, Quầy & Bếp, Thủ kho, và Giao hàng (Shipper)
          </p>
        </div>

        <button
          onClick={() => setShowCreateModal(true)}
          className="inline-flex items-center gap-2 px-4 py-2.5 rounded-2xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] shadow-sm transition-all"
        >
          <UserPlus className="w-4 h-4" />
          <span>Thêm Nhân Viên Mới</span>
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
            { key: "Manager", label: "Quản lý (Manager)" },
            { key: "Staff", label: "Quầy & Bếp" },
            { key: "WarehouseStaff", label: "Thủ kho" },
            { key: "DeliveryStaff", label: "Giao hàng (Shipper)" },
            { key: "Customer", label: "Khách hàng" },
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

      {/* Staff Table */}
      <div className="bg-white rounded-3xl border border-[#EFE6DC] shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#FAF7F2] border-b border-[#EFE6DC] text-[#786C65] font-bold uppercase tracking-wider">
              <tr>
                <th className="py-3 px-4">Nhân viên / Họ tên</th>
                <th className="py-3 px-4">Email & Liên hệ</th>
                <th className="py-3 px-4">Vai trò hiện tại</th>
                <th className="py-3 px-4">Trạng thái</th>
                <th className="py-3 px-4 text-right">Phân quyền trực tiếp</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#FAF7F2]">
              {loading ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-[#786C65]">
                    <RefreshCw className="w-6 h-6 animate-spin mx-auto text-[#6F4E37] mb-2" />
                    Đang tải danh sách nhân sự...
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
                      <div className="font-extrabold text-[#2D2522]">{u.fullName}</div>
                      <div className="text-[10px] text-[#786C65]">ID: #{u.id}</div>
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
                    <td className="py-3.5 px-4 text-right space-x-2">
                      <select
                        value={u.role}
                        onChange={(e) => handleRoleChange(u.id, e.target.value, u.isActive !== false)}
                        className="bg-white border border-[#EFE6DC] rounded-xl px-2.5 py-1.5 font-bold text-xs text-[#2D2522] focus:outline-none focus:border-[#6F4E37]"
                      >
                        <option value="Staff">Quầy & Bếp (Staff)</option>
                        <option value="WarehouseStaff">Thủ kho (Warehouse)</option>
                        <option value="DeliveryStaff">Giao hàng (Shipper)</option>
                        <option value="Manager">Quản lý (Manager)</option>
                        <option value="Admin">Quản trị (Admin)</option>
                        <option value="Customer">Khách hàng</option>
                      </select>

                      <button
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

      {/* Modal Create Employee */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 bg-black/50 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl p-6 sm:p-8 max-w-lg w-full border border-[#EFE6DC] shadow-2xl space-y-5 animate-in fade-in zoom-in duration-200">
            <div className="flex items-center justify-between pb-3 border-b border-[#EFE6DC]">
              <div className="flex items-center gap-2">
                <UserPlus className="w-5 h-5 text-[#6F4E37]" />
                <h3 className="font-black text-lg text-[#2D2522]">Tạo Tài Khoản Nhân Viên Mới</h3>
              </div>
              <button onClick={() => setShowCreateModal(false)} className="p-1 hover:opacity-70">
                <X className="w-5 h-5 text-[#786C65]" />
              </button>
            </div>

            <form onSubmit={handleCreateEmployee} className="space-y-4 text-xs">
              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Họ Và Tên Nhân Viên:</label>
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
                  <label className="font-bold text-[#2D2522] block mb-1">Email Công Việc:</label>
                  <input
                    type="email"
                    required
                    placeholder="nhanvien@milkyverse.com"
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
                    <option value="Staff">Quầy & Bếp (Staff)</option>
                    <option value="WarehouseStaff">Thủ kho (WarehouseStaff)</option>
                    <option value="DeliveryStaff">Giao hàng (DeliveryStaff)</option>
                    <option value="Manager">Quản lý (Manager)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="font-bold text-[#2D2522] block mb-1">Địa Chỉ Chi Nhánh / Nơi Công Tác:</label>
                <input
                  type="text"
                  placeholder="Ví dụ: Chi nhánh Quận 1, TP.HCM"
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
