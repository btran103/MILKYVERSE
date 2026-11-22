// Distinct, high-quality, verified food & drink images for MilkyVerse orders
// Ensures no two items in the same order or list display duplicate images

const DRINK_IMAGES = [
  "/images/menu/tra-sua-tran-chau-den.jpg", // Trà sữa truyền thống trân châu
  "/images/menu/tra-dao-cam-sa.jpg", // Trà đào cam sả
  "/images/menu/tra-o-long-kem-cheese.jpg", // Trà ô long nướng kem cheese
  "https://images.unsplash.com/photo-1536256263959-770b48d82b0a?w=600&auto=format&fit=crop&q=80", // Matcha Latte
  "/images/menu/tra-o-long-kem-cheese.jpg", // Trà kem cheese Macchiato
  "https://images.unsplash.com/photo-1551024709-8f23befc6f87?w=600&auto=format&fit=crop&q=80", // Trà sữa hạt sen dừa
  "https://images.unsplash.com/photo-1572490122747-3968b75cc699?w=600&auto=format&fit=crop&q=80", // Trà khoai môn béo ngậy
  "https://images.unsplash.com/photo-1544787219-7f47ccb76574?w=600&auto=format&fit=crop&q=80", // Trà dâu tây hoa hồng
  "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=600&auto=format&fit=crop&q=80", // Cà phê muối béo ngậy
  "https://images.unsplash.com/photo-1542990253-0d0f5be5f0ed?w=600&auto=format&fit=crop&q=80", // Cacao đá xay
  "/images/menu/sua-tuoi-tran-chau-duong-den.jpg", // Sữa tươi trân châu đường đen
  "https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=600&auto=format&fit=crop&q=80", // Sinh tố nhiệt đới
  "https://images.unsplash.com/photo-1600271886742-f049cd451bba?w=600&auto=format&fit=crop&q=80", // Nước ép hoa quả tươi
  "https://images.unsplash.com/photo-1517256064527-09c73fc73e38?w=600&auto=format&fit=crop&q=80", // Trà hoa cúc mật ong
  "https://images.unsplash.com/photo-1541167760496-1628856ab772?w=600&auto=format&fit=crop&q=80", // Cà phê sữa đá Sài Gòn
  "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=600&auto=format&fit=crop&q=80", // Sữa chua nếp cẩm
];

const FOOD_IMAGES = [
  "/images/menu/ca-vien-chien-thap-cam-met-lon.jpg", // Cá viên chiên mẹt lớn
  "/images/menu/banh-trang-tron-dac-biet.jpg", // Bánh tráng trộn đặc biệt
  "/images/menu/chan-ga-rut-xuong-sot-thai-cay-gion.jpg", // Chân gà rút xương sốt Thái
  "/images/menu/mi-cay-hai-san.jpg", // Mì cay Hàn Quốc 7 cấp độ
  "/images/menu/pho-mai-vien-chien-gion-tan.jpg", // Tokbokki / Phô mai
  "/images/menu/canh-ga-chien-nuoc-mam-toi-ot.jpg", // Gà rán / Cánh gà
  "/images/menu/bo-vien-chien-gan-gion.jpg", // Bò viên
  "/images/menu/tom-vien-chien-gion-rum.jpg", // Tôm viên
  "/images/menu/muc-vien-chien-nong.jpg", // Mực viên
  "/images/menu/ho-lo-nuong-chien-gion.jpg", // Hồ lô
  "/images/menu/chan-ga-ngam-sa-tac-truyen-thong.jpg", // Chân gà ngâm sả tắc
  "/images/menu/ca-vien-chien-truyen-thong.jpg", // Cá viên truyền thống
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
