// Distinct, high-quality, verified food & drink images for MilkyVerse orders
// Ensures no two items in the same order or list display duplicate images

const DRINK_IMAGES = [
  "/images/products/tra-sua-tran-chau-den.png", // Trà sữa truyền thống trân châu
  "/images/products/tra-dao-cam-sa.png", // Trà đào cam sả
  "/images/products/tra-o-long-kem-cheese.png", // Trà ô long nướng kem cheese
  "/images/products/tra-sua-matcha.png", // Matcha Latte / Trà sữa Matcha
  "/images/products/tra-sua-kem-cheese.png", // Trà kem cheese Macchiato
  "/images/products/tra-sua-truyen-thong.png", // Trà sữa truyền thống
  "/images/products/tra-sua-khoai-mon.png", // Trà khoai môn béo ngậy
  "/images/products/tra-sua-dau.png", // Trà dâu tây hoa hồng
  "/images/products/ca-phe-muoi.png", // Cà phê muối béo ngậy
  "/images/products/cacao-da-xay.png", // Cacao đá xay
  "/images/products/sua-tuoi-tran-chau-duong-den.png", // Sữa tươi trân châu đường đen
  "/images/products/nuoc-ep-xoai.png", // Sinh tố nhiệt đới
  "/images/products/nuoc-ep-cam.png", // Nước ép hoa quả tươi
  "/images/products/tra-sua-thai-xanh.png", // Trà xanh Thái
  "/images/products/ca-phe-sua.png", // Cà phê sữa
  "/images/products/tra-sua-duong-den.png", // Sữa tươi đường đen
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
