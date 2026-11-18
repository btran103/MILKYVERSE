"use client";

import React from "react";

const MarqueeBlock: React.FC = () => (
  <div className="flex items-center shrink-0 gap-8 pr-8 text-xs sm:text-[13px] tracking-wide">
    <span className="font-extrabold text-[#FAF7F2]">
      MILKYVERSE – Một ly trà sữa, ngàn cách tận hưởng.
    </span>
    <span className="text-[#C5A059]/70" aria-hidden="true">•</span>
    <span className="font-medium text-[#F7D6D0]">
      🧋 Thế giới đồ uống và món ngon dành riêng cho bạn!
    </span>
    <span className="text-[#C5A059]/70" aria-hidden="true">•</span>
    <a
      href="tel:19001003"
      className="inline-flex items-center gap-1.5 font-extrabold text-[#FAF7F2] bg-white/10 hover:bg-white/20 px-2.5 py-0.5 rounded-full border border-[#F7D6D0]/30 transition-colors"
    >
      <span>📞 HOTLINE: 1900 1003</span>
    </a>
    <span className="text-[#C5A059]/70" aria-hidden="true">✦</span>
  </div>
);

export const AnnouncementMarquee: React.FC = () => {
  return (
    <div
      className="relative w-full overflow-hidden bg-gradient-to-r from-[#4A3324] via-[#6F4E37] to-[#4A3324] text-[#FAF7F2] py-2 border-b border-[#C5A059]/25 select-none"
      role="region"
      aria-label="Thông báo MilkyVerse"
    >
      {/* Hiệu ứng mờ nhẹ 2 mép cho cảm giác chạy mượt sang trọng */}
      <div className="pointer-events-none absolute inset-y-0 left-0 w-10 bg-gradient-to-r from-[#4A3324] to-transparent z-10" />
      <div className="pointer-events-none absolute inset-y-0 right-0 w-10 bg-gradient-to-l from-[#4A3324] to-transparent z-10" />

      <div className="animate-milky-marquee">
        {/* Track 1 */}
        <div className="flex items-center shrink-0">
          <MarqueeBlock />
          <MarqueeBlock />
          <MarqueeBlock />
        </div>
        {/* Track 2 (Bản sao y hệt để vòng lặp 0% -> -50% không bị giật) */}
        <div className="flex items-center shrink-0" aria-hidden="true">
          <MarqueeBlock />
          <MarqueeBlock />
          <MarqueeBlock />
        </div>
      </div>
    </div>
  );
};
