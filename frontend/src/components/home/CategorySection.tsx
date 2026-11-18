import React from "react";
import Link from "next/link";
import { Category } from "@/types";

interface CategorySectionProps {
  categories: Category[];
}

export default function CategorySection({ categories }: CategorySectionProps) {
  return (
    <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
      <div>
        <h2 className="text-2xl sm:text-3xl font-extrabold text-[#2D2522]">
          Danh Mục Trà Sữa Đa Dạng
        </h2>
        <p className="text-sm text-[#786C65] mt-1">
          Mỗi bộ sưu tập mang một phong cách hương vị riêng biệt được ủ thủ công mỗi ngày.
        </p>
      </div>

      {categories.length === 0 ? (
        <div className="p-8 text-center text-[#786C65] bg-white rounded-3xl border border-[#EFE6DC]">
          Đang tải danh mục thực đơn...
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {categories.slice(0, 8).map((cat) => {
            if (!cat) return null;
            const id = cat.Id ?? cat.id;
            const name = cat.Name ?? cat.name ?? "Danh mục";
            const desc = cat.Description ?? cat.description ?? "";
            const img = (cat.ImageUrl ?? cat.imageUrl) || "/logo.png";

            return (
              <Link
                key={id}
                href={`/menu?category=${id}`}
                className="group relative rounded-3xl overflow-hidden aspect-[4/3] border border-[#EFE6DC] shadow-sm hover:shadow-xl transition-all"
              >
                <img
                  src={img}
                  alt={name}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/30 to-transparent flex flex-col justify-end p-5 text-white">
                  <h3 className="font-bold text-lg leading-tight group-hover:text-[#F7D6D0] transition-colors">
                    {name}
                  </h3>
                  <p className="text-xs text-white/80 line-clamp-1 mt-1">
                    {desc}
                  </p>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </section>
  );
}
