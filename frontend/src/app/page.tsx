"use client";

import React, { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Category, Product } from "@/types";
import { useCart } from "@/context/CartContext";
import { useAuth } from "@/context/AuthContext";

import HeroSection from "@/components/home/HeroSection";
import SignatureSection from "@/components/home/SignatureSection";
import ReviewSection from "@/components/home/ReviewSection";
import CategorySection from "@/components/home/CategorySection";
import PromoBanner from "@/components/home/PromoBanner";
import ProductDetailModal from "@/components/home/ProductDetailModal";
import { CommentItem, INITIAL_COMMENTS } from "@/components/home/types";

export default function HomePage() {
  const router = useRouter();
  const { user } = useAuth();
  const { addItem, openAuthModal } = useCart();

  const [categories, setCategories] = useState<Category[]>([]);
  const [signatureProducts, setSignatureProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [addedId, setAddedId] = useState<number | null>(null);

  // Modal Detail & Review State
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [comments, setComments] = useState<CommentItem[]>(INITIAL_COMMENTS);
  const [newCommentText, setNewCommentText] = useState("");
  const [newCommentRating, setNewCommentRating] = useState(5);

  // Dish-specific review state
  const [dishReviewText, setDishReviewText] = useState("");
  const [dishReviewRating, setDishReviewRating] = useState(5);
  const [dishReviewVerdict, setDishReviewVerdict] = useState("Rất ngon / Chuẩn vị");
  const [dishReviewSuccess, setDishReviewSuccess] = useState(false);

  useEffect(() => {
    async function fetchData() {
      try {
        const [catData, prodData, reviewData] = await Promise.all([
          api.getCategories().catch(() => []),
          api.getProducts().catch(() => []),
          api.getRecentReviews().catch(() => []),
        ]);
        const catList = Array.isArray(catData) ? catData : [];
        const prodList = Array.isArray(prodData) ? prodData : [];
        setCategories(catList);

        if (Array.isArray(reviewData) && reviewData.length > 0) {
          const mappedReviews: CommentItem[] = reviewData.map((r) => ({
            id: r.id,
            userName: r.userName || "Khách Hàng",
            avatarUrl: r.userAvatar,
            rating: r.rating || 5,
            date: new Date(r.createdAt).toLocaleDateString("vi-VN"),
            content: `${r.verdict ? `[${r.verdict}] ` : ""}${r.comment}`,
            productId: r.productId,
          }));
          setComments(mappedReviews);
        }

        // 5 Món Signature mỗi ngày bất kì (Ưu tiên BestSeller hoặc lấy đúng 5 món đầu tiên/đặc sắc)
        const best = prodList.filter((p) => p && Boolean(p.isBestSeller));
        const finalFive = best.length >= 5 ? best.slice(0, 5) : prodList.slice(0, 5);
        setSignatureProducts(finalFive);
      } catch (err) {
        console.error("Lỗi khi tải dữ liệu trang chủ:", err);
        setCategories([]);
        setSignatureProducts([]);
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  const handleQuickAdd = (product: Product, e?: React.MouseEvent) => {
    if (e) e.stopPropagation();
    if (!product) return;
    const prodId = product.Id ?? product.id;
    const prodName = product.Name ?? product.name ?? "Trà sữa MilkyVerse";
    const prodPrice = product.BasePrice ?? product.basePrice ?? 0;
    const prodImage = product.ImageUrl ?? product.imageUrl ?? "/logo.png";
    const type = product.ProductType ?? product.productType ?? "DRINK";

    const success = addItem({
      productId: prodId,
      name: prodName,
      imageUrl: prodImage,
      itemType: type,
      size: type === "DRINK" ? "M" : "Standard",
      sugarLevel: type === "DRINK" ? "50%" : undefined,
      iceLevel: type === "DRINK" ? "70%" : undefined,
      spicyLevel: type === "FOOD" && product.spicyLevel ? `Cấp ${product.spicyLevel}` : undefined,
      unitPrice: prodPrice,
      quantity: 1,
      totalPrice: prodPrice,
      isCustomDIY: false,
      toppings: [],
    });

    if (success) {
      setAddedId(prodId);
      setTimeout(() => setAddedId(null), 1500);
    }
  };

  const handleQuickBuyNow = (product: Product, e?: React.MouseEvent) => {
    if (e) e.stopPropagation();
    if (!product) return;
    const prodId = product.Id ?? product.id;
    const prodName = product.Name ?? product.name ?? "Trà sữa MilkyVerse";
    const prodPrice = product.BasePrice ?? product.basePrice ?? 0;
    const prodImage = product.ImageUrl ?? product.imageUrl ?? "/logo.png";
    const type = product.ProductType ?? product.productType ?? "DRINK";

    const success = addItem({
      productId: prodId,
      name: prodName,
      imageUrl: prodImage,
      itemType: type,
      size: type === "DRINK" ? "M" : "Standard",
      sugarLevel: type === "DRINK" ? "50%" : undefined,
      iceLevel: type === "DRINK" ? "70%" : undefined,
      spicyLevel: type === "FOOD" && product.spicyLevel ? `Cấp ${product.spicyLevel}` : undefined,
      unitPrice: prodPrice,
      quantity: 1,
      totalPrice: prodPrice,
      isCustomDIY: false,
      toppings: [],
    });

    if (success) {
      router.push("/checkout");
    }
  };

  const handleAddReview = (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) {
      openAuthModal?.("Vui lòng đăng nhập tài khoản để gửi bình luận & đánh giá!");
      return;
    }
    if (!newCommentText.trim()) return;

    const newRev: CommentItem = {
      id: Date.now(),
      userName: user.fullName || "Khách Hàng MilkyVerse",
      avatarUrl: user.avatarUrl || "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
      rating: newCommentRating,
      date: "Vừa xong",
      content: newCommentText.trim(),
      productId: selectedProduct ? (selectedProduct.Id ?? selectedProduct.id) : undefined,
    };

    setComments([newRev, ...comments]);
    setNewCommentText("");
  };

  const handleDishReviewSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) {
      openAuthModal?.("Vui lòng đăng nhập để bình luận cho món này!");
      return;
    }
    if (!dishReviewText.trim()) return;

    const newDishRev: CommentItem = {
      id: Date.now(),
      userName: user.fullName || "Khách Hàng MilkyVerse",
      avatarUrl: user.avatarUrl || "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150&auto=format&fit=crop&q=80",
      rating: dishReviewRating,
      date: "Vừa xong",
      content: `${dishReviewVerdict ? `[${dishReviewVerdict}] ` : ""}${dishReviewText.trim()}`,
      productId: selectedProduct ? (selectedProduct.Id ?? selectedProduct.id) : undefined,
    };

    setComments([newDishRev, ...comments]);
    setDishReviewText("");
    setDishReviewSuccess(true);
    setTimeout(() => setDishReviewSuccess(false), 3000);
  };

  return (
    <div className="space-y-16 pb-16">
      {/* 1. Hero Section */}
      <HeroSection featuredProducts={signatureProducts} />

      {/* 2. 5 Món Signature Mỗi Ngày */}
      <SignatureSection
        products={signatureProducts}
        loading={loading}
        addedId={addedId}
        onSelectProduct={(p) => setSelectedProduct(p)}
        onQuickAdd={handleQuickAdd}
        onQuickBuyNow={handleQuickBuyNow}
      />

      {/* 3. Đánh Giá Số Sao & Bình Luận Toàn Bộ Món */}
      <ReviewSection
        comments={comments}
        user={user}
        newCommentText={newCommentText}
        setNewCommentText={setNewCommentText}
        newCommentRating={newCommentRating}
        setNewCommentRating={setNewCommentRating}
        onAddReview={handleAddReview}
      />

      {/* 4. Danh Mục Trà Sữa Đa Dạng */}
      <CategorySection categories={categories} />

      {/* 5. Khuyến Mãi / Voucher */}
      <PromoBanner />

      {/* Modal Xem Chi Tiết Món Signature */}
      <ProductDetailModal
        selectedProduct={selectedProduct}
        onClose={() => setSelectedProduct(null)}
        user={user}
        comments={comments}
        onQuickAdd={handleQuickAdd}
        onQuickBuyNow={handleQuickBuyNow}
        dishReviewText={dishReviewText}
        setDishReviewText={setDishReviewText}
        dishReviewRating={dishReviewRating}
        setDishReviewRating={setDishReviewRating}
        dishReviewSuccess={dishReviewSuccess}
        onDishReviewSubmit={handleDishReviewSubmit}
      />
    </div>
  );
}
