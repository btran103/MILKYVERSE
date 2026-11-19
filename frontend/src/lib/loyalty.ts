"use client";

export type TierType = "BRONZE" | "SILVER" | "GOLD" | "DIAMOND";

export interface LoyaltyTierInfo {
  tier: TierType;
  name: string;
  badgeColor: string;
  gradientClass: string;
  minPoints: number;
  nextTierPoints: number | null;
  discountRate: number; // e.g. 0.05 for 5%
  benefits: string[];
}

export const TIERS: Record<TierType, LoyaltyTierInfo> = {
  BRONZE: {
    tier: "BRONZE",
    name: "Thành Viên Đồng",
    badgeColor: "#CD7F32",
    gradientClass: "from-[#8B5A2B] via-[#A0522D] to-[#6F4E37]",
    minPoints: 0,
    nextTierPoints: 100,
    discountRate: 0,
    benefits: [
      "Tích lũy 1 điểm cho mỗi 10.000đ chi tiêu",
      "Dùng điểm trừ tiền trực tiếp trên đơn hàng (1 điểm = 1.000đ)",
      "Quà tặng sinh nhật voucher 20.000đ",
    ],
  },
  SILVER: {
    tier: "SILVER",
    name: "Hội Viên Bạc",
    badgeColor: "#C0C0C0",
    gradientClass: "from-[#708090] via-[#A8B2C1] to-[#5A6B7C]",
    minPoints: 100,
    nextTierPoints: 300,
    discountRate: 0.05,
    benefits: [
      "Tất cả quyền lợi Hạng Đồng",
      "Giảm 5% cho tất cả đơn hàng từ 100.000đ",
      "Ưu tiên pha chế nhanh tại Quầy & Bếp KDS",
      "Tặng 1 topping miễn phí vào thứ 4 hàng tuần",
    ],
  },
  GOLD: {
    tier: "GOLD",
    name: "Hội Viên Vàng",
    badgeColor: "#FFD700",
    gradientClass: "from-[#B8860B] via-[#DAA520] to-[#8B6508]",
    minPoints: 300,
    nextTierPoints: 600,
    discountRate: 0.10,
    benefits: [
      "Tất cả quyền lợi Hạng Bạc",
      "Giảm 10% cho mọi đơn hàng",
      "Freeship đơn hàng bán kính 5km",
      "Quyền dùng thử món mới trước ngày ra mắt",
    ],
  },
  DIAMOND: {
    tier: "DIAMOND",
    name: "Thành Viên Kim Cương",
    badgeColor: "#B9F2FF",
    gradientClass: "from-[#1E3C72] via-[#2A5298] to-[#1E3C72]",
    minPoints: 600,
    nextTierPoints: null,
    discountRate: 0.15,
    benefits: [
      "Đặc quyền tối thượng Milky Galaxy VIP",
      "Giảm 15% trọn đời toàn menu",
      "Miễn phí 100% phí giao hàng không giới hạn khoảng cách",
      "Tặng quà sinh nhật hộp quà cao cấp MilkyVerse",
    ],
  },
};

const LOYALTY_STORAGE_PREFIX = "milky_loyalty_";

export function getLoyaltyPoints(userId: number): number {
  if (typeof window === "undefined") return 50;
  try {
    const raw = localStorage.getItem(`${LOYALTY_STORAGE_PREFIX}${userId}`);
    if (raw) return parseInt(raw, 10);
    // Initial welcome points for users
    const defaultPts = 120; // Default to Silver tier for rich demo experience
    localStorage.setItem(`${LOYALTY_STORAGE_PREFIX}${userId}`, defaultPts.toString());
    return defaultPts;
  } catch {
    return 120;
  }
}

export function saveLoyaltyPoints(userId: number, points: number): void {
  if (typeof window === "undefined") return;
  try {
    localStorage.setItem(`${LOYALTY_STORAGE_PREFIX}${userId}`, Math.max(0, points).toString());
    window.dispatchEvent(new CustomEvent("milky_loyalty_updated", { detail: { userId, points } }));
  } catch (e) {
    console.error("Lỗi khi lưu điểm tích lũy:", e);
  }
}

export function calculateTier(points: number): LoyaltyTierInfo {
  if (points >= 600) return TIERS.DIAMOND;
  if (points >= 300) return TIERS.GOLD;
  if (points >= 100) return TIERS.SILVER;
  return TIERS.BRONZE;
}

export function getLoyaltyOverview(userId: number) {
  const points = getLoyaltyPoints(userId);
  const currentTier = calculateTier(points);

  let progress = 100;
  let pointsToNext = 0;

  if (currentTier.nextTierPoints) {
    const range = currentTier.nextTierPoints - currentTier.minPoints;
    const gained = points - currentTier.minPoints;
    progress = Math.min(100, Math.max(0, Math.round((gained / range) * 100)));
    pointsToNext = Math.max(0, currentTier.nextTierPoints - points);
  }

  return {
    points,
    currentTier,
    progress,
    pointsToNext,
  };
}
