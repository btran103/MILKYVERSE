import React from "react";
import Link from "next/link";

export default function PromoBanner() {
  return (
    <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div className="bg-[#FAF7F2] border-2 border-dashed border-[#6F4E37]/30 rounded-3xl p-6 sm:p-8">
        <div className="flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="space-y-1 text-center md:text-left">
            <span className="text-xs font-bold uppercase tracking-wider text-[#6F4E37]">
              Ưu Đãi Đặc Quyền Hôm Nay
            </span>
            <h3 className="text-xl sm:text-2xl font-extrabold text-[#2D2522]">
              Nhập Mã <span className="text-[#6F4E37] underline decoration-[#C5A059]">#MK2095F54</span> Giảm Ngay 25.000đ
            </h3>
            <p className="text-xs text-[#786C65]">
              Áp dụng cho mọi hóa đơn. Mã <strong>FREESHIP</strong> miễn phí vận chuyển cho đơn từ 99k!
            </p>
          </div>
          <Link
            href="/menu"
            className="px-6 py-3 rounded-2xl bg-[#6F4E37] text-white font-bold text-sm hover:bg-[#4A3324] shadow-md transition-all shrink-0"
          >
            Đặt Món Ngay
          </Link>
        </div>
      </div>
    </section>
  );
}
