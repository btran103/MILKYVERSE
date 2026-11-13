"use client";

import React, { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import {
  Sparkles,
  MapPin,
  Phone,
  Mail,
  Clock,
  Heart,
  ShieldCheck,
  RefreshCcw,
  Award,
  Coins,
  X,
  ExternalLink,
} from "lucide-react";

interface PolicyContent {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  tagline: string;
  sections: { heading: string; body: string }[];
}

const POLICIES: Record<string, PolicyContent> = {
  privacy: {
    title: "Chính Sách Bảo Mật Thông Tin",
    icon: ShieldCheck,
    tagline: "Bảo vệ thông tin cá nhân và dữ liệu mua sắm của bạn là ưu tiên hàng đầu tại MilkyVerse.",
    sections: [
      {
        heading: "1. Mục đích thu thập",
        body: "MilkyVerse chỉ thu thập thông tin cơ bản (Họ tên, Số điện thoại, Địa chỉ giao hàng, Email) phục vụ cho việc xử lý đơn hàng, điều phối giao vận và tích điểm thành viên.",
      },
      {
        heading: "2. Cam kết bảo mật dữ liệu",
        body: "Toàn bộ mật khẩu và thông tin đăng nhập đều được mã hóa một chiều bằng BCrypt. Thông tin giao dịch thanh toán trực tuyến qua VietQR được bảo vệ bằng giao thức bảo mật ngân hàng MBBank/Napas.",
      },
      {
        heading: "3. Không chia sẻ cho bên thứ ba",
        body: "Chúng tôi cam kết 100% không bán, trao đổi hoặc chia sẻ dữ liệu khách hàng cho bất kỳ tổ chức quảng cáo bên ngoài nào mà không có sự đồng ý của bạn.",
      },
    ],
  },
  refund: {
    title: "Quy Chế Đổi Trả & Hoàn Tiền",
    icon: RefreshCcw,
    tagline: "Hài lòng 100% trong từng ngụm trà sữa & món ăn vặt nóng giòn.",
    sections: [
      {
        heading: "1. Đổi mới 1-1 miễn phí",
        body: "Nếu đồ uống bị đổ vỡ, tràn nắp, sai tỷ lệ đá/đường so với ghi chú hoặc món ăn vặt bị nguội/sai yêu cầu, cửa hàng sẽ pha chế và giao lại phần mới hoàn toàn miễn phí trong vòng 30 phút.",
      },
      {
        heading: "2. Hủy đơn trong vòng 2 phút",
        body: "Hệ thống hỗ trợ khách hàng tự hủy đơn trực tiếp trên website trong 2 phút đầu tiên kể từ khi đặt hàng (trước khi quầy bắt đầu pha chế). Tiền thanh toán sẽ được hoàn về tài khoản trong 24 giờ.",
      },
      {
        heading: "3. Hotline hỗ trợ 24/7",
        body: "Khi cần khiếu nại hoặc hỗ trợ hoàn tiền gấp, vui lòng gọi ngay Hotline 1900 888 999 hoặc bấm 'Chat với Quản lý' trong chi tiết đơn hàng.",
      },
    ],
  },
  hygiene: {
    title: "Cam Kết An Toàn Vệ Sinh Thực Phẩm",
    icon: Award,
    tagline: "Tiêu chuẩn quốc tế - 100% nguyên liệu tự nhiên có nguồn gốc xuất xứ rõ ràng.",
    sections: [
      {
        heading: "1. Nguồn sữa tươi & trà organic",
        body: "MilkyVerse chỉ sử dụng sữa tươi thanh trùng chuẩn VietGAP và lá trà hữu cơ Oolong/Trà xanh tuyển chọn từ nông trường Bảo Lộc và Đài Loan, ủ mới mỗi 4 tiếng.",
      },
      {
        heading: "2. Quản lý hạn dùng FEFO",
        body: "Toàn bộ nguyên vật liệu trong kho được quản lý tự động theo thuật toán First-Expired, First-Out (FEFO), đảm bảo không bao giờ sử dụng nguyên liệu cận date.",
      },
      {
        heading: "3. Chứng nhận vệ sinh an toàn thực phẩm",
        body: "100% cửa hàng và xưởng pha chế MilkyVerse đạt chứng nhận Cơ sở đủ điều kiện An toàn Thực phẩm theo quy định của Ban Quản lý ATTP TP.HCM.",
      },
    ],
  },
  rewards: {
    title: "Hướng Dẫn Tích Điểm Thành Viên",
    icon: Coins,
    tagline: "Tích điểm tự động trên mọi đơn hàng - Mở khóa ưu đãi vũ trụ độc quyền.",
    sections: [
      {
        heading: "1. Tỷ lệ tích lũy MilkyPoints",
        body: "Mỗi 10.000đ chi tiêu trên MilkyVerse, bạn nhận ngay 1 MilkyPoint (tương đương hoàn tiền 5%). Điểm thưởng được tự động cộng vào tài khoản ngay sau khi đơn hàng hoàn thành.",
      },
      {
        heading: "2. Hạng thẻ thành viên",
        body: "• Khách Mới (Bronze): Tặng voucher giảm 25.000đ khi đăng ký\n• Khách Thân Thiết (Silver - từ 500k): Giảm thêm 5% mọi đơn hàng\n• Vũ Trụ VIP (Gold - từ 2 triệu): Miễn phí giao hàng trọn đời + quà sinh nhật đặc biệt.",
      },
      {
        heading: "3. Đổi voucher & quà tặng",
        body: "Bạn có thể dùng điểm tích lũy để quy đổi trực tiếp thành mã giảm giá ở bước Thanh Toán (Checkout) hoặc nhận quà lưu niệm độc quyền của MilkyVerse.",
      },
    ],
  },
};

export const Footer: React.FC = () => {
  const [activePolicyKey, setActivePolicyKey] = useState<string | null>(null);
  const activePolicy = activePolicyKey ? POLICIES[activePolicyKey] : null;

  return (
    <>
      <footer className="bg-[#2D2522] text-[#EFE6DC] pt-16 pb-8 border-t border-[#4A3324]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-10 mb-12">
            {/* 1. Brand Info */}
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
                <Sparkles className="w-3.5 h-3.5 text-[#F7D6D0]" />
                <span>100% Sữa Tươi Thanh Trùng & Trà Thượng Hạng</span>
              </div>
            </div>

            {/* 2. Quick Links */}
            <div>
              <h4 className="text-base font-bold text-white mb-4 tracking-wide">Khám Phá</h4>
              <ul className="space-y-2.5 text-sm text-[#A89F91]">
                <li>
                  <Link href="/menu" className="hover:text-[#F7D6D0] transition-colors flex items-center gap-1.5">
                    <span>Thực Đơn Signature & Đồ Ăn Vặt</span>
                  </Link>
                </li>
                <li>
                  <Link href="/diy" className="hover:text-[#F7D6D0] transition-colors flex items-center gap-1.5">
                    <span>DIY Drink Studio</span>
                    <span className="text-[10px] bg-[#6F4E37] text-white px-1.5 py-0.5 rounded font-bold">HOT</span>
                  </Link>
                </li>
                <li>
                  <Link href="/ai-barista" className="hover:text-[#F7D6D0] transition-colors flex items-center gap-1.5">
                    <span>AI Barista Tư Vấn Đồ Uống</span>
                    <span className="text-[10px] bg-[#F7D6D0] text-[#6F4E37] px-1.5 py-0.5 rounded font-bold">AI</span>
                  </Link>
                </li>
                <li>
                  <Link href="/orders" className="hover:text-[#F7D6D0] transition-colors">
                    Tra Cứu Lịch Sử Đơn Hàng
                  </Link>
                </li>
              </ul>
            </div>

            {/* 3. Policies & Commitment (Clickable Buttons that open modal) */}
            <div>
              <h4 className="text-base font-bold text-white mb-4 tracking-wide">Chính Sách</h4>
              <ul className="space-y-2.5 text-sm text-[#A89F91]">
                <li>
                  <button
                    type="button"
                    onClick={() => setActivePolicyKey("privacy")}
                    className="hover:text-[#F7D6D0] transition-colors text-left flex items-center gap-2 group w-full"
                  >
                    <ShieldCheck className="w-4 h-4 text-[#C5A059] group-hover:scale-110 transition-transform shrink-0" />
                    <span>Chính sách bảo mật thông tin</span>
                  </button>
                </li>
                <li>
                  <button
                    type="button"
                    onClick={() => setActivePolicyKey("refund")}
                    className="hover:text-[#F7D6D0] transition-colors text-left flex items-center gap-2 group w-full"
                  >
                    <RefreshCcw className="w-4 h-4 text-[#C5A059] group-hover:scale-110 transition-transform shrink-0" />
                    <span>Quy chế đổi trả & hoàn tiền</span>
                  </button>
                </li>
                <li>
                  <button
                    type="button"
                    onClick={() => setActivePolicyKey("hygiene")}
                    className="hover:text-[#F7D6D0] transition-colors text-left flex items-center gap-2 group w-full"
                  >
                    <Award className="w-4 h-4 text-[#C5A059] group-hover:scale-110 transition-transform shrink-0" />
                    <span>Cam kết an toàn vệ sinh thực phẩm</span>
                  </button>
                </li>
                <li>
                  <button
                    type="button"
                    onClick={() => setActivePolicyKey("rewards")}
                    className="hover:text-[#F7D6D0] transition-colors text-left flex items-center gap-2 group w-full"
                  >
                    <Coins className="w-4 h-4 text-[#C5A059] group-hover:scale-110 transition-transform shrink-0" />
                    <span>Hướng dẫn tích điểm thành viên</span>
                  </button>
                </li>
              </ul>
            </div>

            {/* 4. Contact Details (Clickable links: tel, mailto, maps) */}
            <div>
              <h4 className="text-base font-bold text-white mb-4 tracking-wide">Liên Hệ Cửa Hàng</h4>
              <ul className="space-y-3 text-sm text-[#A89F91]">
                <li>
                  <a
                    href="https://maps.google.com/?q=Số+1+Đường+Thiên+Hà,+Quận+1,+TP.+Hồ+Chí+Minh"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-start gap-2.5 hover:text-[#F7D6D0] transition-colors group"
                    title="Mở trên Google Maps"
                  >
                    <MapPin className="w-4 h-4 text-[#F7D6D0] mt-0.5 shrink-0 group-hover:scale-110 transition-transform" />
                    <span className="flex-1">Số 1 Đường Thiên Hà, Quận 1, TP. Hồ Chí Minh</span>
                    <ExternalLink className="w-3.5 h-3.5 opacity-50 group-hover:opacity-100 shrink-0 mt-0.5" />
                  </a>
                </li>
                <li>
                  <a
                    href="tel:1900888999"
                    className="flex items-center gap-2.5 hover:text-[#F7D6D0] transition-colors group"
                    title="Gọi Hotline đặt món"
                  >
                    <Phone className="w-4 h-4 text-[#F7D6D0] shrink-0 group-hover:scale-110 transition-transform" />
                    <span className="font-semibold text-white group-hover:text-[#F7D6D0]">
                      Hotline: 1900 888 999
                    </span>
                    <span className="text-xs text-[#A89F91]">(08:00 - 22:30)</span>
                  </a>
                </li>
                <li>
                  <a
                    href="mailto:support@milkyverse.com"
                    className="flex items-center gap-2.5 hover:text-[#F7D6D0] transition-colors group"
                    title="Gửi email cho bộ phận CSKH"
                  >
                    <Mail className="w-4 h-4 text-[#F7D6D0] shrink-0 group-hover:scale-110 transition-transform" />
                    <span>support@milkyverse.com</span>
                  </a>
                </li>
                <li className="flex items-center gap-2.5 text-[#A89F91]">
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

      {/* Interactive Policy Modal */}
      {activePolicy && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-fadeIn"
          onClick={() => setActivePolicyKey(null)}
        >
          <div
            className="bg-white text-[#2D2522] rounded-3xl max-w-lg w-full max-h-[85vh] overflow-y-auto p-6 sm:p-8 shadow-2xl border border-[#EFE6DC] relative space-y-5 animate-scale"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className="flex items-start justify-between gap-4 border-b border-[#FAF7F2] pb-4">
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 rounded-2xl bg-[#FAF7F2] border border-[#EFE6DC] flex items-center justify-center text-[#6F4E37] shrink-0">
                  <activePolicy.icon className="w-6 h-6" />
                </div>
                <div>
                  <h3 className="font-extrabold text-lg sm:text-xl text-[#2D2522]">
                    {activePolicy.title}
                  </h3>
                  <p className="text-xs text-[#786C65] mt-0.5">{activePolicy.tagline}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setActivePolicyKey(null)}
                className="p-2 rounded-xl text-[#786C65] hover:bg-[#FAF7F2] hover:text-[#2D2522] transition-colors"
                title="Đóng cửa sổ"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Body */}
            <div className="space-y-4 text-sm leading-relaxed text-[#4A3324]">
              {activePolicy.sections.map((sec, idx) => (
                <div key={idx} className="bg-[#FAF7F2] p-4 rounded-2xl border border-[#EFE6DC]/60 space-y-1">
                  <h4 className="font-bold text-[#2D2522] text-sm flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5 text-[#C5A059]" />
                    <span>{sec.heading}</span>
                  </h4>
                  <p className="text-xs text-[#786C65] whitespace-pre-line leading-relaxed">
                    {sec.body}
                  </p>
                </div>
              ))}
            </div>

            {/* Footer action */}
            <div className="pt-2 flex justify-end">
              <button
                type="button"
                onClick={() => setActivePolicyKey(null)}
                className="px-6 py-2.5 rounded-xl bg-[#6F4E37] text-white text-xs font-bold hover:bg-[#4A3324] transition-colors shadow-md"
              >
                Đã hiểu & Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};
