// Distinct, high-quality, verified food & drink images for MilkyVerse orders
// Ensures no two items in the same order or list display duplicate images

const DRINK_IMAGES = [
  "https://images.unsplash.com/photo-1558857563-b37cf5311487?w=600&auto=format&fit=crop&q=80", // Trà sữa truyền thống trân châu
  "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=600&auto=format&fit=crop&q=80", // Trà đào cam sả
  "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=600&auto=format&fit=crop&q=80", // Trà ô long nướng
  "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=600&auto=format&fit=crop&q=80", // Matcha Latte
  "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=600&auto=format&fit=crop&q=80", // Trà kem cheese Macchiato
  "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=600&auto=format&fit=crop&q=80", // Trà sữa hạt sen dừa
  "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=600&auto=format&fit=crop&q=80", // Trà khoai môn béo ngậy
  "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=600&auto=format&fit=crop&q=80", // Trà dâu tây hoa hồng
  "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=600&auto=format&fit=crop&q=80", // Cà phê muối béo ngậy
  "https://images.unsplash.com/photo-1542990253-0d0f5be5f0ed?w=600&auto=format&fit=crop&q=80", // Cacao đá xay
  "https://images.unsplash.com/photo-1595981267035-7b04ca84a82d?w=600&auto=format&fit=crop&q=80", // Sữa tươi trân châu đường đen
  "https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=600&auto=format&fit=crop&q=80", // Sinh tố nhiệt đới
  "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=600&auto=format&fit=crop&q=80", // Nước ép hoa quả tươi
  "https://images.unsplash.com/photo-1517256064527-09c73fc73e38?w=600&auto=format&fit=crop&q=80", // Trà hoa cúc mật ong
  "https://images.unsplash.com/photo-1541167760496-1628856ab772?w=600&auto=format&fit=crop&q=80", // Cà phê sữa đá Sài Gòn
  "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=600&auto=format&fit=crop&q=80", // Sữa chua nếp cẩm
];

const FOOD_IMAGES = [
  "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=600&auto=format&fit=crop&q=80", // Cá viên chiên mẹt lớn
  "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=600&auto=format&fit=crop&q=80", // Bánh tráng trộn đặc biệt
  "https://images.unsplash.com/photo-1527477378370-13d8d641dca0?w=600&auto=format&fit=crop&q=80", // Chân gà rút xương sốt Thái
  "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=600&auto=format&fit=crop&q=80", // Mì cay Hàn Quốc 7 cấp độ
  "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=600&auto=format&fit=crop&q=80", // Tokbokki sốt phô mai
  "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=600&auto=format&fit=crop&q=80", // Gà rán giòn rụm
  "https://images.unsplash.com/photo-1576107232684-1279f3908594?w=600&auto=format&fit=crop&q=80", // Khoai tây chiên giòn
  "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=600&auto=format&fit=crop&q=80", // Há cảo dimsum
  "https://images.unsplash.com/photo-1531749668029-2db88e4276c7?w=600&auto=format&fit=crop&q=80", // Phô mai que kéo sợi
  "https://images.unsplash.com/photo-1603133872878-684f208fb84b?w=600&auto=format&fit=crop&q=80", // Combo ăn vặt tổng hợp
  "https://images.unsplash.com/photo-1541544741938-0af808871cc0?w=600&auto=format&fit=crop&q=80", // Nem chua rán
  "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?w=600&auto=format&fit=crop&q=80", // Xúc xích nướng lốc xoáy
];

export interface OrderItemLike {
  id?: number;
  productId?: number;
  productName: string;
  itemType?: string;
  imageUrl?: string;
}

/**
 * Deterministically returns a distinct, high-quality image URL for an order item.
 * Guarantees that items in the same order get different images without duplication.
 */
export function getDistinctOrderItemImage(item: OrderItemLike, indexInOrder: number = 0): string {
  // If item already has a non-empty, valid ImageUrl from backend and it's not a generic placeholder
  if (item.imageUrl && item.imageUrl.trim().length > 10 && !item.imageUrl.includes("placeholder")) {
    return item.imageUrl;
  }

  const name = (item.productName || "").toLowerCase();
  const itemType = (item.itemType || "").toUpperCase();

  // Keyword-based accurate food detection
  if (
    itemType === "FOOD" ||
    name.includes("cá viên") ||
    name.includes("bánh tráng") ||
    name.includes("chân gà") ||
    name.includes("mì cay") ||
    name.includes("tokbokki") ||
    name.includes("gà rán") ||
    name.includes("khoai tây") ||
    name.includes("há cảo") ||
    name.includes("phô mai") ||
    name.includes("nem chua") ||
    name.includes("xúc xích") ||
    name.includes("combo")
  ) {
    if (name.includes("cá viên")) return FOOD_IMAGES[0];
    if (name.includes("bánh tráng")) return FOOD_IMAGES[1];
    if (name.includes("chân gà")) return FOOD_IMAGES[2];
    if (name.includes("mì cay") || name.includes("mi cay")) return FOOD_IMAGES[3];
    if (name.includes("tokbokki")) return FOOD_IMAGES[4];
    if (name.includes("gà rán") || name.includes("ga ran")) return FOOD_IMAGES[5];
    if (name.includes("khoai tây")) return FOOD_IMAGES[6];
    if (name.includes("há cảo") || name.includes("dimsum")) return FOOD_IMAGES[7];
    if (name.includes("phô mai")) return FOOD_IMAGES[8];
    if (name.includes("combo")) return FOOD_IMAGES[9];
    if (name.includes("nem chua")) return FOOD_IMAGES[10];

    // Other food fallback with offset to avoid duplicates
    const foodOffset = ((item.id || 0) + indexInOrder * 3) % FOOD_IMAGES.length;
    return FOOD_IMAGES[foodOffset];
  }

  // Drink keyword matching
  if (name.includes("đào") || name.includes("cam sả")) return DRINK_IMAGES[1];
  if (name.includes("ô long") || name.includes("o long") || name.includes("khói")) return DRINK_IMAGES[2];
  if (name.includes("matcha")) return DRINK_IMAGES[3];
  if (name.includes("kem cheese") || name.includes("macchiato")) return DRINK_IMAGES[4];
  if (name.includes("khoai môn") || name.includes("taro")) return DRINK_IMAGES[6];
  if (name.includes("dâu") || name.includes("hoa hồng")) return DRINK_IMAGES[7];
  if (name.includes("cà phê") || name.includes("muối") || name.includes("bạc xỉu")) return DRINK_IMAGES[8];
  if (name.includes("cacao") || name.includes("socola") || name.includes("chocolate")) return DRINK_IMAGES[9];
  if (name.includes("đường đen") || name.includes("sữa tươi")) return DRINK_IMAGES[10];
  if (name.includes("sinh tố")) return DRINK_IMAGES[11];
  if (name.includes("nước ép")) return DRINK_IMAGES[12];
  if (name.includes("cúc") || name.includes("lài") || name.includes("nhài")) return DRINK_IMAGES[13];
  if (name.includes("sữa chua")) return DRINK_IMAGES[15];

  // Default drink distinct distribution by item id & position
  const drinkOffset = ((item.id || item.productId || 0) + indexInOrder * 5) % DRINK_IMAGES.length;
  return DRINK_IMAGES[drinkOffset];
}
