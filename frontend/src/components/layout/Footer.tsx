"use client";

import React from "react";
import Link from "next/link";
import Image from "next/image";
import { Sparkles, MapPin, Phone, Mail, Clock, Heart } from "lucide-react";

export const Footer: React.FC = () => {
  return (
    <footer className="bg-[#2D2522] text-[#EFE6DC] pt-16 pb-8 border-t border-[#4A3324]">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-10 mb-12">
          {/* Brand Info */}
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="relative w-11 h-11 rounded-full overflow-hidden bg-white border border-[#4A3324] shadow-sm flex items-center justify-center p-0.5">
                <Image
                  src="/logo.png"
                  alt="MilkyVerse Logo"
                  width={44}
                  height={44}
                  className="w-full h-full object-cover rounded-full"
                />
              </div>
              <span className="font-extrabold text-2xl tracking-wider text-white">
                MILKYVERSE
              </span>
            </div>
            <p className="text-sm text-[#A89F91] leading-relaxed">
              Vũ trụ trà sữa công nghệ cao kết hợp hương vị trà truyền thống Đài Loan cùng trợ lý pha chế thông minh AI Barista.
            </p>
            <div className="flex items-center gap-2 text-xs text-[#D4A373]">
              <Sparkles className="w-3.5 h-3.5" />
              <span>100% Sữa Tươi Thanh Trùng & Trà Thượng Hạng</span>
            </div>
          </div>

          {/* Quick Links */}
          <div>
            <h4 className="text-base font-bold text-white mb-4 tracking-wide">Khám Phá</h4>
            <ul className="space-y-2.5 text-sm text-[#A89F91]">
              <li>
                <Link href="/menu" className="hover:text-[#F7D6D0] transition-colors">
                  Thực Đơn Signature
                </Link>
              </li>
              <li>
                <Link href="/diy" className="hover:text-[#F7D6D0] transition-colors flex items-center gap-1.5">
                  <span>DIY Drink Studio</span>
                  <span className="text-[10px] bg-[#6F4E37] text-white px-1.5 py-0.2 rounded font-bold">HOT</span>
                </Link>
              </li>
              <li>
                <Link href="/ai-barista" className="hover:text-[#F7D6D0] transition-colors">
                  AI Barista Tư Vấn Đồ Uống
                </Link>
              </li>
              <li>
                <Link href="/orders" className="hover:text-[#F7D6D0] transition-colors">
                  Tra Cứu Lịch Sử Đơn Hàng
                </Link>
              </li>
            </ul>
          </div>

          {/* Policies & Commitment */}
          <div>
            <h4 className="text-base font-bold text-white mb-4 tracking-wide">Chính Sách</h4>
            <ul className="space-y-2.5 text-sm text-[#A89F91]">
              <li className="hover:text-[#F7D6D0] cursor-pointer">Chính sách bảo mật thông tin</li>
              <li className="hover:text-[#F7D6D0] cursor-pointer">Quy chế đổi trả & hoàn tiền</li>
              <li className="hover:text-[#F7D6D0] cursor-pointer">Cam kết an toàn vệ sinh thực phẩm</li>
              <li className="hover:text-[#F7D6D0] cursor-pointer">Hướng dẫn tích điểm thành viên</li>
            </ul>
          </div>

          {/* Contact Details */}
          <div>
            <h4 className="text-base font-bold text-white mb-4 tracking-wide">Liên Hệ Cửa Hàng</h4>
            <ul className="space-y-3 text-sm text-[#A89F91]">
              <li className="flex items-start gap-2.5">
                <MapPin className="w-4 h-4 text-[#F7D6D0] mt-0.5 shrink-0" />
                <span>Số 1 Đường Thiên Hà, Quận 1, TP. Hồ Chí Minh</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Phone className="w-4 h-4 text-[#F7D6D0] shrink-0" />
                <span>Hotline: 1900 888 999 (08:00 - 22:30)</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Mail className="w-4 h-4 text-[#F7D6D0] shrink-0" />
                <span>support@milkyverse.com</span>
              </li>
              <li className="flex items-center gap-2.5">
                <Clock className="w-4 h-4 text-[#F7D6D0] shrink-0" />
                <span>Mở cửa: 07:30 - 23:00 Hàng ngày</span>
              </li>
            </ul>
          </div>
        </div>

        <div className="pt-8 border-t border-[#4A3324] flex flex-col sm:flex-row items-center justify-between text-xs text-[#786C65] gap-4">
          <p>© 2026 MILKYVERSE Inc. Bản quyền thuộc về Vũ Trụ Trà Sữa Milkyverse.</p>
          <p className="flex items-center gap-1">
            Được phát triển với <Heart className="w-3.5 h-3.5 text-red-500 fill-red-500" /> bằng Next.js 15 & ASP.NET Core Web API
          </p>
        </div>
      </div>
    </footer>
  );
};
