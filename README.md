# 🌌 MILKYVERSE — WEBSITE THƯƠNG MẠI ĐIỆN TỬ TRÀ SỮA TÍCH HỢP AI

Chào mừng đến với **MILKYVERSE** — Nền tảng thương mại điện tử chuyên biệt cho đồ uống trà sữa thế hệ mới, tích hợp **AI Barista** tư vấn công thức cá nhân hóa và **DIY Drink Studio** mô phỏng ly trà sữa tương tác thời gian thực.

---

## 🛠️ 1. CÔNG NGHỆ BẮT BUỘC ĐÃ TRIỂN KHAI

### 🎨 Frontend
- **Framework**: [Next.js 16 (App Router)](https://nextjs.org/) + **React 19** + **TypeScript**.
- **Styling**: **Tailwind CSS v4** với bảng màu thương hiệu Trà Sữa (Nền kem `#FAF7F2`, Nâu trà `#6F4E37`, Hồng pastel `#F7D6D0`, Xanh matcha `#4E7D55`).
- **Icons**: [Lucide React](https://lucide.dev/).
- **Quản lý trạng thái & Giỏ hàng**: React Context (`CartContext`, `AuthContext`) lưu trữ snapshot cấu hình ly trà sữa và token bảo mật.
- **Trải nghiệm**: Hiệu ứng pháo hoa ăn mừng khi đặt hàng thành công (`canvas-confetti`).

### ⚙️ Backend
- **Framework**: **ASP.NET Core Web API** (ngôn ngữ **C#**).
- **ORM & Database**: **Entity Framework Core 9 / 10** + **Pomelo.EntityFrameworkCore.MySql** (kết nối **MySQL 8.0+**).
- **Cơ chế dự phòng thông minh (Auto-Fallback)**: Hệ thống tự động phát hiện cổng MySQL 3306; nếu MySQL chưa bật, hệ thống tự động khởi chạy SQLite cục bộ (`milkyverse_local.db`) để bạn có thể test chạy ngay lập tức mà không bị gián đoạn.
- **Xác thực & Bảo mật**: **JWT Bearer Token** + Hashing mật khẩu **BCrypt** + Phân quyền Role-based (`Customer`, `Staff`, `Admin`).
- **ACID Transaction**: Toàn bộ quy trình đặt hàng, áp dụng voucher, tạo snapshot giá và tự động trừ kho nguyên liệu theo Recipe đều chạy trong một Transaction duy nhất.
- **API Documentation**: **Swagger / OpenAPI** tại `/swagger`.

### 🤖 AI Barista
- **Tích hợp Cloud AI**: Kết nối trực tiếp **Google Gemini API** (`gemini-1.5-flash`) từ backend ASP.NET Core, không lộ API key ra frontend.
- **Quy tắc Sommelier dự phòng an toàn**: Khi chưa cấu hình API Key hoặc khi mất mạng, hệ thống tự động kích hoạt **Local Barista Engine** phân tích khẩu vị người dùng (ít ngọt, béo nhẹ, healthy, mất ngủ, giải nhiệt) và gợi ý chính xác các sản phẩm thật đang có trong database, có gắn nhãn minh bạch theo đúng yêu cầu đề bài.

---

## 📁 2. CẤU TRÚC THƯ MỤC DỰ ÁN

```
d:\MILKYVERSE\
├── backend\
│   ├── MilkyVerse.Api\                # Ứng dụng ASP.NET Core Web API
│   │   ├── Controllers\              # Auth, Products, Categories, Drinks, Orders, AI, Admin
│   │   ├── Services\                 # AuthService, ProductService, DIYService, OrderService, AIBaristaService, InventoryService, AdminService
│   │   ├── Models\                   # User, Category, Product, Ingredient, Topping, Recipe, Order, Detail, Promotion, ChatSession...
│   │   ├── DTOs\                     # Các lớp Request / Response Data Transfer Objects
│   │   ├── Data\                     # MilkyVerseDbContext, DbInitializer (Seed Data), schema.sql
│   │   ├── Program.cs                # Pipeline cấu hình CORS, JWT, Swagger, DI
│   │   └── appsettings.json          # Cấu hình Connection String MySQL, JWT Key, Gemini API Key
│   └── MilkyVerse.Tests\             # Bộ kiểm thử Unit Test xUnit tự động
└── frontend\
    ├── src\
    │   ├── app\                      # Next.js App Router
    │   │   ├── page.tsx              # Trang chủ (Hero, Banner, Best-sellers, Danh mục)
    │   │   ├── menu\page.tsx         # Danh mục thực đơn & tìm kiếm & phân loại
    │   │   ├── menu\[slug]\page.tsx  # Chi tiết sản phẩm (Chọn Size, Đường, Đá, Multi-topping)
    │   │   ├── diy\page.tsx          # DIY Drink Studio (Trình mô phỏng ly trà sữa tương tác)
    │   │   ├── ai-barista\page.tsx   # Trợ lý ảo AI Barista tư vấn công thức
    │   │   ├── cart\page.tsx         # Giỏ hàng & áp dụng voucher ưu đãi
    │   │   ├── checkout\page.tsx     # Thanh toán & chọn phương thức (COD / VNPAY)
    │   │   ├── orders\page.tsx       # Lịch sử đơn hàng của khách hàng
    │   │   ├── orders\[id]\page.tsx  # Theo dõi tiến trình 5 bước đơn hàng
    │   │   ├── auth\login\page.tsx   # Đăng nhập (Kèm nút Demo 1-click)
    │   │   ├── auth\register\page.tsx# Đăng ký tài khoản
    │   │   └── admin\                # Khu vực Quản trị viên
    │   │       ├── page.tsx          # Dashboard doanh thu, KPI, cảnh báo tồn kho
    │   │       ├── products\page.tsx # CRUD sản phẩm & trạng thái bán
    │   │       ├── inventory\page.tsx# Quản lý tồn kho nguyên liệu & nhập/xuất
    │   │       └── orders\page.tsx   # Quản lý đơn hàng & chuyển trạng thái
    │   ├── components\layout\        # Navbar, Footer
    │   ├── context\                  # AuthContext, CartContext
    │   ├── lib\                      # api.ts (HTTP Client)
    │   └── types\                    # index.ts (TypeScript Interfaces)
    └── package.json
```

---

## 🚀 3. HƯỚNG DẪN KHỞI CHẠY DỰ ÁN

### Bước 1: Khởi động Backend (ASP.NET Core Web API)
Mở một cửa sổ Terminal tại thư mục `backend/MilkyVerse.Api`:
```bash
cd d:\MILKYVERSE\backend\MilkyVerse.Api
dotnet run --urls "http://localhost:5000"
```
- API sẽ chạy tại: `http://localhost:5000`
- Giao diện kiểm thử Swagger API: `http://localhost:5000/swagger`
- Cơ sở dữ liệu sẽ tự động tạo bảng và nạp sẵn dữ liệu mẫu (Sản phẩm, Topping, Nguyên liệu, Tài khoản).

### Bước 2: Khởi động Frontend (Next.js)
Mở một cửa sổ Terminal thứ hai tại thư mục `frontend`:
```bash
cd d:\MILKYVERSE\frontend
npm run dev
```
- Mở trình duyệt truy cập: `http://localhost:3000`

---

## 🔑 4. TÀI KHOẢN DÙNG THỬ CÓ SẴN (PRE-SEEDED)

Tại trang Đăng nhập (`/auth/login`), bạn có thể bấm ngay các nút **Đăng Nhập Nhanh**:
1. **Quản trị viên (Admin)**:
   - Email: `admin@milkyverse.com`
   - Mật khẩu: `Admin@123`
   - Quyền: Toàn quyền truy cập Dashboard `/admin`, quản lý kho, duyệt đơn hàng và chỉnh sửa sản phẩm.
2. **Khách hàng thân thiết**:
   - Email: `khachhang@milkyverse.com`
   - Mật khẩu: `Customer@123`
   - Quyền: Tự thiết kế đồ uống, đặt hàng, lưu giỏ và tra cứu lịch sử đơn.

---

## 🏷️ 5. MÃ GIẢM GIÁ DÙNG THỬ
- **`MILKYNEW`**: Giảm 20% cho đơn hàng đầu tiên từ 50.000đ.
- **`FREESHIP`**: Miễn phí vận chuyển 15.000đ cho đơn hàng từ 99.000đ.
- **`AIBARISTA`**: Giảm trực tiếp 30.000đ cho đơn từ 120.000đ.

---

## 🧪 6. CHẠY BỘ KIỂM THỬ TỰ ĐỘNG (XUNIT)
Mở terminal và gõ:
```bash
dotnet test d:\MILKYVERSE\backend\MilkyVerse.Tests
```
Kết quả kiểm thử: **4/4 Tests Passed** (Bao gồm đăng ký, đăng nhập bảo mật, tính giá ly DIY, kiểm tra voucher và cập nhật tồn kho).
