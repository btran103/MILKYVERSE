# Dataset Huấn Luyện AI Barista MilkyVerse

Bộ dataset chuyên biệt cho Chatbot / AI Barista của thương hiệu trà sữa & đồ ăn vặt **MilkyVerse**.
Được trích xuất và đồng bộ hóa trực tiếp từ cơ sở dữ liệu thực tế của hệ thống MilkyVerse.

## Thống Kê Tổng Quan
- **Tổng số món (Products):** 213 món (Đầy đủ cả Đồ Uống & Đồ Ăn Vặt)
- **Tổng số Topping:** 23 loại topping độc quyền
- **Tổng số Danh Mục:** 23 danh mục sản phẩm
- **Hội thoại mẫu (Conversations):** 16 kịch bản đối thoại chuẩn Function-Calling / Agent SFT
- **Tự pha chế (DIY Custom Builder):** Đầy đủ cốt trà, loại sữa, syrup hương vị và combo mẫu

## Cấu Trúc Các File
| Tên File | Định Dạng | Mô Tả Chi Tiết |
|---|---|---|
| `menu.json` | JSON | Toàn bộ 213 món với đầy đủ: `id`, `slug`, `name_vi`, `category`, `sizes`, `tags`, `is_milk_tea`, `has_milk`, `has_caffeine`, `default_sugar_percent`, `product_url`, `order_url` (Mua Ngay). |
| `menu.csv` | CSV | Phiên bản bảng tính phẳng của toàn bộ thực đơn. |
| `toppings.json` | JSON | 23 loại topping: Trân châu hoàng kim, trân châu trắng, trân châu đường đen, kem cheese, phô mai kéo sợi... kèm giá và tag. |
| `custom_builder.json` | JSON | Dữ liệu các bước tự pha chế cho trang `/diy` và 3 combo pha chế thịnh hành. |
| `orders_sample.json` | JSON | Dữ liệu mẫu đơn hàng, chính sách huỷ đơn (chỉ cho phép trạng thái `pending`). |
| `tools.json` | JSON Schema | 7 Function Calling declarations: `search_menu`, `get_item_link`, `create_order`, `list_orders`, `cancel_order`, `get_custom_builder_options`, `build_custom_drink_link`. |
| `conversations.jsonl` | JSON Lines | 16 kịch bản hội thoại đa lượt chân thực có tool call và kết quả trả về, dùng để fine-tune hoặc few-shot prompting cho LLM. |

## Đường Dẫn & Tích Hợp Hệ Thống
- Xem chi tiết món: `http://localhost:3000/menu/{slug}`
- Mua ngay trực tiếp: `http://localhost:3000/checkout?item={id}&size=M`
- Tự pha chế DIY: `http://localhost:3000/diy`
- Theo dõi đơn hàng: `http://localhost:3000/orders`
- Backend API Barista: `backend/MilkyVerse.Api/Services/AIBaristaService.cs`
