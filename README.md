# 🌌 MILKYVERSE — Website Thương Mại Điện Tử Trà Sữa Tích Hợp AI

**MILKYVERSE** là nền tảng thương mại điện tử chuyên biệt cho đồ uống trà sữa, tích hợp trí tuệ nhân tạo (AI) nhằm cá nhân hóa trải nghiệm người dùng. Hệ thống hỗ trợ tư vấn đồ uống thông minh, tự thiết kế công thức trà sữa, đặt hàng trực tuyến và quản lý kho nguyên liệu.

## ✨ 1. Chức năng chính

- **AI Barista:** Tư vấn và đề xuất đồ uống theo khẩu vị, ngân sách và nhu cầu của khách hàng.
- **DIY Drink Studio:** Cho phép khách hàng tự thiết kế ly trà sữa bằng cách lựa chọn kích cỡ, lượng đường, lượng đá và topping.
- **Quản lý thực đơn:** Hiển thị danh mục, tìm kiếm sản phẩm và xem chi tiết đồ uống.
- **Giỏ hàng và thanh toán:** Quản lý giỏ hàng, áp dụng mã giảm giá và đặt hàng trực tuyến.
- **Theo dõi đơn hàng:** Xem lịch sử mua hàng và theo dõi trạng thái đơn hàng.
- **Quản lý kho nguyên liệu:** Theo dõi số lượng tồn kho, quản lý nhập xuất và cảnh báo nguyên liệu sắp hết.
- **Quản trị hệ thống:** Quản lý sản phẩm, đơn hàng, doanh thu và tồn kho.
- **Xác thực và phân quyền:** Đăng ký, đăng nhập bằng JWT và phân quyền Customer, Staff, Admin.
- **Kiểm thử tự động:** Sử dụng xUnit để kiểm thử các chức năng của hệ thống.

## 🛠️ 2. Công nghệ sử dụng

### Frontend
- Next.js 16 (App Router)
- React 19
- TypeScript
- Tailwind CSS v4
- Lucide React
- React Context API

### Backend
- ASP.NET Core Web API
- C#
- Entity Framework Core
- MySQL 8.0+
- SQLite dự phòng khi được cấu hình
- JWT Bearer Authentication
- BCrypt
- Swagger / OpenAPI

### Trí tuệ nhân tạo và kiểm thử
- Google Gemini API
- Local Barista Engine dự phòng
- xUnit

## 📁 3. Cấu trúc thư mục dự án

```text
MILKYVERSE/
├── backend/
│   ├── MilkyVerse.Api/
│   │   ├── Controllers/    # Xử lý các yêu cầu API
│   │   ├── Services/       # Xử lý nghiệp vụ
│   │   ├── Models/         # Các lớp mô hình dữ liệu
│   │   ├── DTOs/           # Đối tượng truyền dữ liệu
│   │   ├── Data/           # DbContext và khởi tạo dữ liệu
│   │   ├── Program.cs      # Cấu hình ứng dụng
│   │   └── appsettings.json
│   └── MilkyVerse.Tests/   # Bộ kiểm thử tự động
└── frontend/
    ├── src/
    │   ├── app/            # Các trang giao diện
    │   ├── components/     # Các thành phần giao diện
    │   ├── context/        # Quản lý trạng thái
    │   ├── lib/            # Các tiện ích và kết nối API
    │   └── types/          # Khai báo kiểu dữ liệu
    └── package.json
```

## 🚀 4. Hướng dẫn cài đặt và chạy dự án

### Yêu cầu hệ thống

- Node.js và npm
- .NET SDK tương thích với dự án Backend
- MySQL 8.0+ nếu sử dụng MySQL

### Bước 1: Khởi chạy Backend

Mở Terminal tại thư mục dự án và chạy:

```bash
cd backend/MilkyVerse.Api
dotnet restore
dotnet run --urls "http://localhost:5000"
```

Sau khi khởi chạy thành công:

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

### Bước 2: Khởi chạy Frontend

Mở Terminal thứ hai và chạy:

```bash
cd frontend
npm install
npm run dev
```

Mở trình duyệt tại địa chỉ: http://localhost:3000

### Bước 3: Chạy kiểm thử tự động

```bash
dotnet test backend/MilkyVerse.Tests
```

Kiểm tra kết quả Terminal để biết số lượng kiểm thử thành công và thất bại.

## 🔐 5. Tài khoản dùng thử

Nếu cơ sở dữ liệu đã được khởi tạo dữ liệu mẫu, bạn có thể sử dụng các tài khoản sau:

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin@milkyverse.com` | `Admin@123` |
| Khách hàng | `khachhang@milkyverse.com` | `Customer@123` |

**Lưu ý:** Đây là tài khoản phục vụ phát triển và kiểm thử. Không sử dụng thông tin đăng nhập mặc định trong môi trường thực tế.

## 🏷️ 6. Mã giảm giá dùng thử

| Mã giảm giá | Ưu đãi |
|---|---|
| `MILKYNEW` | Giảm 20% cho đơn hàng đầu tiên từ 50.000đ |
| `FREESHIP` | Giảm 15.000đ phí vận chuyển cho đơn từ 99.000đ |
| `AIBARISTA` | Giảm 30.000đ cho đơn từ 120.000đ |

Các mã giảm giá cần được cấu hình và khởi tạo trong Backend để có thể sử dụng.

## 🔒 7. Bảo mật và cấu hình

- Cấu hình chuỗi kết nối cơ sở dữ liệu phù hợp với môi trường chạy.
- Lưu API Key và JWT Secret trong biến môi trường hoặc cấu hình cục bộ an toàn.
- Không tải thông tin bí mật lên GitHub.
- Chỉ sử dụng cơ chế SQLite dự phòng khi Backend đã được cấu hình để phát hiện lỗi kết nối MySQL và khởi tạo SQLite.
- Đảm bảo dữ liệu mẫu đã được khởi tạo trước khi sử dụng tài khoản và mã giảm giá dùng thử.

## 📌 8. Mục tiêu dự án

MILKYVERSE hướng đến việc xây dựng một nền tảng kinh doanh trà sữa hiện đại, kết hợp thương mại điện tử và trí tuệ nhân tạo. Dự án giúp khách hàng dễ dàng tìm kiếm, cá nhân hóa và đặt mua đồ uống, đồng thời hỗ trợ nhân viên và quản trị viên quản lý hoạt động kinh doanh hiệu quả.

## 📄 9. Giấy phép

Nếu muốn công khai và phân phối dự án, hãy bổ sung tệp `LICENSE` với giấy phép phù hợp.
