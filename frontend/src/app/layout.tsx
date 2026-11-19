import React, { Suspense } from "react";
import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/context/AuthContext";
import { CartProvider } from "@/context/CartContext";
import { AnnouncementMarquee } from "@/components/layout/AnnouncementMarquee";
import { Navbar } from "@/components/layout/Navbar";
import { Footer } from "@/components/layout/Footer";
import { AIBaristaWidget } from "@/components/layout/AIBaristaWidget";

export const metadata: Metadata = {
  title: "MILKYVERSE — Vũ Trụ Trà Sữa & Đồ Ăn Vặt Tích Hợp AI",
  description: "Khám phá thế giới trà sữa tươi cao cấp và đồ ăn vặt nóng giòn, tự thiết kế đồ uống với DIY Drink Studio và nhận tư vấn thông minh từ AI Barista.",
  icons: {
    icon: [
      { url: "/logo.png?v=5", type: "image/png" },
      { url: "/favicon.ico?v=5", sizes: "any" },
    ],
    shortcut: "/logo.png?v=5",
    apple: "/logo.png?v=5",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi" className="h-full antialiased" suppressHydrationWarning>
      <head>
        <link rel="icon" href="/logo.png?v=5" type="image/png" sizes="any" />
        <link rel="shortcut icon" href="/favicon.ico?v=5" />
        <link rel="apple-touch-icon" href="/logo.png?v=5" />
      </head>
      <body className="min-h-full flex flex-col bg-[#FAF7F2] text-[#2D2522]" suppressHydrationWarning>
        <AuthProvider>
          <CartProvider>
            <Suspense fallback={<div className="h-20 bg-[#FAF7F2]" />}>
              <Navbar />
            </Suspense>
            <AnnouncementMarquee />
            <main className="flex-1">{children}</main>
            <Footer />
            <AIBaristaWidget />
          </CartProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
