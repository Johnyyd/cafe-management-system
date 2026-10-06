# Hệ Thống Quản Lý Chuỗi Cà Phê (Cafe Management System)

Hệ thống quản lý và điều hành chuỗi quán cà phê quy chuẩn công nghiệp, tích hợp toàn diện từ quản lý chuỗi chi nhánh, thực đơn, kho nguyên liệu, bán hàng (POS), nhân sự ca làm đến giám sát chỉ số vận hành.

---

## Mục Lục
- [Tổng Quan Hệ Thống](#tổng-quan-hệ-thống)
- [Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
- [Tiêu Chuẩn Thiết Kế Giao Diện](#tiêu-chuẩn-thiết-kế-giao-diện)
- [Cấu Trúc Thư Mục](#cấu-trúc-thư-mục)
- [Hướng Dẫn Khởi Chạy (Getting Started)](#hướng-dẫn-khởi-chạy-getting-started)
- [Khởi Tạo Dữ Liệu Thật (Seed Data)](#khởi-tạo-dữ-liệu-thật-seed-data)
- [Tài Khoản Mẫu Thử Nghiệm](#tài-khoản-mẫu-thử-nghiệm)
- [Tài Liệu Kỹ Thuật (Documentation)](#tài-liệu-kỹ-thuật-documentation)
- [Giấy Phép](#giấy-phép)

---

## Tổng Quan Hệ Thống

Hệ thống được phát triển theo mô hình Client - Server hiện đại với kiến trúc phân tầng chuyên nghiệp:
- **Quản lý Quán & Chi nhánh:** Quản lý thông tin chuỗi cửa hàng theo địa bàn các Tỉnh/Thành phố tại Việt Nam, giờ mở cửa, trạng thái hoạt động.
- **Thực đơn Đồ uống:** Danh mục món, định giá, thành phần nguyên liệu, chất gây dị ứng và tình trạng khả dụng.
- **Kho & Nguyên liệu:** Theo dõi lượng tồn kho thời gian thực, định mức tái đặt hàng (reorder level), cảnh báo thiếu hụt.
- **Bán hàng & Thu ngân (POS):** Giao diện bán hàng trực tiếp tại quầy, tính tiền tự động theo thuế VAT 8%, xử lý vòng đời đơn hàng (`Đã nhận đơn` → `Đang pha chế` → `Hoàn tất`).
- **Nhân sự & Ca làm việc:** Quản lý hồ sơ nhân viên, phân quyền vai trò (RBAC) gắn với chi nhánh cửa hàng.
- **Giám sát Hệ thống:** Theo dõi thời gian phản hồi (latency), tỷ lệ lỗi, thông lượng API và tài nguyên máy chủ.

---

## Công Nghệ Sử Dụng

### Backend (.NET 8 & Clean Architecture)
- **Framework:** ASP.NET Core Web API (.NET 8)
- **Kiến trúc:** Clean Architecture (Domain, Application, Infrastructure, Api layers)
- **Cơ sở dữ liệu:** MongoDB (Official `MongoDB.Driver`), lưu trữ NoSQL document
- **Xác thực & Phân quyền:** ASP.NET Core Identity tích hợp JWT (JSON Web Tokens)
- **Mô hình xử lý:** MediatR (CQRS pattern), FluentValidation, Result pattern
- **Ghi log & Đo kiểm:** Serilog tích hợp máy chủ Seq log aggregation
- **Tài liệu API:** OpenAPI / Swagger UI

### Frontend (React 18 & Vite)
- **Framework:** React 18, Vite, TypeScript
- **Styling:** Tailwind CSS (nguyên tắc Flat Minimalism, không gradient)
- **Quản lý trạng thái:** Zustand (Client state), tối ưu hóa hiệu năng
- **Kiểm tra dữ liệu (Form Validation):** React Hook Form kết hợp Zod schema validation
- **Hệ thống biểu tượng:** Lucide React (chuẩn vector kỹ thuật, không dùng icon emoji)
- **Bản địa hóa:** Giao diện thuần tiếng Việt, thân thiện và gần gũi với người vận hành

---

## Tiêu Chuẩn Thiết Kế Giao Diện

- **Phong cách:** Industrial Flat Minimalism (Tối giản công nghiệp).
- **Màu sắc chủ đạo:**
  - Cam thương hiệu: `#EA580C` (`rgb(234, 88, 12)`)
  - Đen công nghiệp: `#0A0A0A` / `#171717`
  - Trắng & Xám trung tính: `#FFFFFF` / `#F5F5F5` / `#E5E5E5`
- **Nguyên tắc thẩm mỹ:** Tuyệt đối không dùng gradient màu mè, không dùng icon emoji, viền phẳng sắc nét, typography tương phản cao.
- **Dữ liệu thật 100%:** Toàn bộ dữ liệu được kết nối trực tiếp với MongoDB qua Backend API, không dùng mockup data trong mã nguồn frontend.

---

## Cấu Trúc Thư Mục

```
cafe-management-system/
├── seed_data.py                  # Script Python khởi tạo dữ liệu thật (tích hợp API Tỉnh thành)
├── documentations/               # Tài liệu thiết kế hệ thống
│   ├── database-design.md        # Thiết kế các collection MongoDB
│   ├── features-and-functions.md # Chi tiết tính năng và endpoints API
│   └── project-outline.md        # Kế hoạch và phạm vi dự án
├── docs/                         # Tài liệu kiểm thử & ảnh chụp màn hình
│   └── screenshots/              # Ảnh chụp giao diện các phân hệ
├── src/
│   ├── backend/                  # Mã nguồn Backend C# .NET 8
│   │   ├── openapi.json          # Đặc tả OpenAPI danh mục Tỉnh thành Việt Nam
│   │   ├── docker-compose.yml    # Cấu hình container API, MongoDB, Seq
│   │   ├── Dockerfile
│   │   └── src/
│   │       ├── CafeManagement.Domain/          # Thực thể, Value Objects, Enums
│   │       ├── CafeManagement.Application/     # Commands, Queries, Handlers, DTOs
│   │       ├── CafeManagement.Infrastructure/  # MongoDB context, Identity, Repositories
│   │       └── CafeManagement.Api/             # Controllers, Converters, Middlewares
│   └── frontend/                 # Mã nguồn Frontend React/Vite/TypeScript
│       ├── src/
│       │   ├── api/              # HTTP Client kết nối Backend API
│       │   ├── components/       # Component UI dùng chung & Layout
│       │   ├── pages/            # Các trang chức năng (Dashboard, Shops, Menu, POS,...)
│       │   ├── schemas/          # Zod validation schemas
│       │   ├── stores/           # Zustand state management
│       │   └── types/            # TypeScript interfaces & types
│       ├── package.json
│       └── vite.config.ts
└── README.md
```

---

## Hướng Dẫn Khởi Chạy (Getting Started)

### Yêu Cầu Cài Đặt Sẵn
- **Docker & Docker Compose**
- **Node.js >= 18** và npm
- **Python >= 3.9** (dùng cho seed dữ liệu)

### Bước 1: Khởi động Hạ tầng Backend & Database
Từ thư mục gốc dự án:
```powershell
cd src/backend
docker compose up -d --build
```
Hệ thống sẽ tự động khởi tạo 3 container:
- **MongoDB:** `localhost:7891` (Database: `cafe_management`)
- **Backend API:** `http://localhost:7890` (Swagger UI: `http://localhost:7890/swagger`)
- **Seq Log Server:** `http://localhost:5341`

### Bước 2: Khởi tạo Dữ liệu Thật (Seed Data)
Từ thư mục gốc dự án:
```powershell
python seed_data.py
```
Script sẽ tự động:
1. Đồng bộ danh sách Tỉnh/Thành phố tại Việt Nam từ Vietnam Provinces API.
2. Khởi tạo tài khoản Quản trị viên (`admin@cafemanagement.com`).
3. Tạo 8 chi nhánh quán cà phê gắn với các thành phố trọng điểm (TP. Hồ Chí Minh, Hà Nội, Đà Lạt, Đà Nẵng, Buôn Ma Thuột,...).
4. Tạo thực đơn đồ uống chuẩn Việt Nam (Cà phê sữa đá, Bạc xỉu, Trà sen vàng,...).
5. Tạo danh mục tồn kho nguyên liệu gắn với từng chi nhánh.
6. Tạo hồ sơ nhân viên và các đơn hàng mẫu theo đầy đủ vòng đời vận hành.

### Bước 3: Khởi động Giao diện Frontend
Từ thư mục gốc dự án:
```powershell
cd src/frontend
npm install
npm run dev
```
Truy cập giao diện tại: **`http://localhost:3000`**

---

## Tài Khoản Mẫu Thử Nghiệm

Hệ thống hỗ trợ đăng nhập nhanh với các vai trò thực tế:

| Vai Trò | Email Đăng Nhập | Mật Khẩu | Quyền Hạn |
|---|---|---|---|
| **Quản trị viên** | `admin@cafemanagement.com` | `Password123!` | Toàn quyền cấu hình chi nhánh, nhân sự, menu, kho |
| **Quản lý quán** | `manager@district1.com` | `Password123!` | Quản lý vận hành chi nhánh, nhân sự, đơn hàng |
| **Thu ngân** | `cashier@district1.com` | `Password123!` | Bán hàng POS, tạo đơn, thu tiền |
| **Pha chế** | `barista@district1.com` | `Password123!` | Tiếp nhận và cập nhật trạng thái đơn pha chế |
| **Thủ kho** | `stock@warehouse.com` | `Password123!` | Kiểm kê và quản lý định mức nguyên liệu kho |

---

## Tài Liệu Kỹ Thuật (Documentation)

- [Thiết kế Cơ sở dữ liệu MongoDB](documentations/database-design.md)
- [Danh mục Tính năng & Đặc tả API](documentations/features-and-functions.md)
- [Đề cương và Kế hoạch Dự án](documentations/project-outline.md)
- [Kế hoạch Kiểm thử QA (QA Test Plan)](QA_TEST_PLAN.md)

---

## Giấy Phép

Dự án được cấp phép theo tiêu chuẩn học thuật và mã nguồn mở MIT License.
