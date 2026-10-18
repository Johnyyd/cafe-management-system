# Danh Sách Tài Khoản Mẫu & Ma Trận Phân Quyền (Demo Accounts & RBAC)

> **Mật khẩu chung cho tất cả tài khoản:** `Password123!`

---

## 1. Tổng Quan Hệ Thống 9 Cửa Hàng (Shops)

Hệ thống đã được khởi tạo và đồng bộ hoàn chỉnh dữ liệu cho **9 chi nhánh** tại các tỉnh/thành phố lớn trên toàn quốc:

| Mã Chi Nhánh (Slug) | Tên Chi Nhánh Quán Cà Phê | Tỉnh / Thành Phố | Địa Chỉ Trụ Sở |
|:---:|:---|:---|:---|
| `q1` / `landmark81` | **The Coffee House Landmark 81 - Bình Thạnh** | TP. Hồ Chí Minh | 720A Điện Biên Phủ, Phường 22, Quận Bình Thạnh |
| `hn` | **Highlands Coffee Nhà Hát Lớn - Hoàn Kiếm** | TP. Hà Nội | 1 Tràng Tiền, Phường Tràng Tiền, Quận Hoàn Kiếm |
| `dn` | **Phúc Long Tea & Coffee Cầu Rồng - Hải Châu** | TP. Đà Nẵng | 61 Nguyễn Văn Linh, Phường Nam Dương, Quận Hải Châu |
| `dalat` | **Cafe Cầu Đất Farm - Thành phố Đà Lạt** | Tỉnh Lâm Đồng | Thôn Trường Thọ, Xã Trạm Hành, TP. Đà Lạt |
| `bmt` | **Trung Nguyên Legend Buôn Ma Thuột** | Tỉnh Đắk Lắk | 45 Lý Thái Tổ, Phường Tân Lợi, TP. Buôn Ma Thuột |
| `thaodien` | **Cafe Thảo Điền - TP. Thủ Đức** | TP. Hồ Chí Minh | 28 Thảo Điền, Phường Thảo Điền, TP. Thủ Đức |
| `cantho` | **Highlands Coffee Bến Ninh Kiều** | TP. Cần Thơ | 02 Hai Bà Trưng, Phường Tân An, Quận Ninh Kiều |
| `hp` | **Phúc Long Nhà Hát Thành Phố - Hồng Bàng** | TP. Hải Phòng | 28 Trần Hưng Đạo, Phường Hoàng Văn Thụ, Quận Hồng Bàng |
| `hue` | **Cafe Cố Đô Sông Hương - TP. Huế** | Tỉnh Thừa Thiên Huế | 14 Lê Lợi, Phường Vĩnh Ninh, TP. Huế |

---

## 2. Cơ Cấu Nhân Sự & Quy Chuẩn Tài Khoản Mỗi Cửa Hàng

Mỗi cửa hàng trong số 9 chi nhánh được phân bổ đúng định mức **11 nhân sự cố định**:
- **2 Quản lý (Manager - Role: 1)**
- **2 Thu ngân (Cashier - Role: 2)**
- **5 Pha chế (Barista - Role: 3)** *(đáp ứng định mức 4-6 pha chế)*
- **2 Thủ kho (InventoryStaff - Role: 4)**

Toàn bộ **99 nhân sự** đều đã được kích hoạt tài khoản đăng nhập với mật khẩu `Password123!`, gắn chặt thẩm quyền và phạm vi dữ liệu theo từng chi nhánh công tác (`shopId`).

---

## 3. Bảng Danh Sách Tài Khoản Mẫu Trọng Điểm

### 3.1. Tài Khoản Quản Trị Hệ Thống (Toàn Chuỗi)

| STT | Họ & Tên | Email Đăng Nhập | Mật Khẩu | Vai Trò | Chi Nhánh | Thẩm Quyền & Tab Được Truy Cập |
|:---:|:---|:---|:---|:---|:---|:---|
| **1** | **Quản Trị Viên Hệ Thống** | `admin@cafemanagement.com` | `Password123!` | **Admin** | **Toàn chuỗi (Tất cả)** | • Tổng quan (`/dashboard`)<br>• Chi nhánh (`/shops`)<br>• Thực đơn (`/menu`)<br>• Kho (`/inventory`)<br>• Bán hàng (`/orders`)<br>• Nhân sự (`/staff`)<br>• Đánh giá (`/reviews`)<br>• Giám sát & Backup (`/metrics`)<br><br>**Toàn quyền tối cao**: Chuyển đổi chi nhánh tự do, sao lưu & phục hồi toàn bộ cơ sở dữ liệu. |

---

### 3.2. Bảng Tra Cứu Tài Khoản 9 Chi Nhánh (Cú Pháp Đồng Nhất)

Dưới đây là danh sách email tài khoản mẫu cho từng chi nhánh. Bạn có thể sử dụng bất kỳ tài khoản nào để đăng nhập trực tiếp:

| Chi Nhánh | Vai Trò | Email Đăng Nhập | Mật Khẩu | Tab Mặc Định / Quyền Hạn |
|:---|:---|:---|:---|:---|
| **Landmark 81 (TP.HCM)** | Quản lý 1 & 2 | `manager1.q1@cafemanagement.com`<br>`manager2.q1@cafemanagement.com` | `Password123!` | Dashboard, Bán hàng, Kho, Nhân sự, Đánh giá |
| | Thu ngân 1 & 2 | `cashier1.q1@cafemanagement.com`<br>`cashier2.q1@cafemanagement.com` | `Password123!` | Bán hàng POS, Nhân sự (lịch ca), Đánh giá |
| | Pha chế (1 - 5) | `barista1.q1@cafemanagement.com` ... `barista5.q1@cafemanagement.com` | `Password123!` | Bán hàng (KDS pha chế), Nhân sự, Đánh giá |
| | Thủ kho 1 & 2 | `stock1.q1@cafemanagement.com`<br>`stock2.q1@cafemanagement.com` | `Password123!` | Kho & Nguyên liệu, Nhân sự, Đánh giá |
| **Nhà Hát Lớn (Hà Nội)** | Quản lý 1 & 2 | `manager1.hn@cafemanagement.com`<br>`manager2.hn@cafemanagement.com` | `Password123!` | Giới hạn theo chi nhánh Hà Nội |
| | Thu ngân 1 & 2 | `cashier1.hn@cafemanagement.com`<br>`cashier2.hn@cafemanagement.com` | `Password123!` | Bán hàng POS Hà Nội |
| | Pha chế (1 - 5) | `barista1.hn@cafemanagement.com` ... `barista5.hn@cafemanagement.com` | `Password123!` | Pha chế chi nhánh Hà Nội |
| | Thủ kho 1 & 2 | `stock1.hn@cafemanagement.com`<br>`stock2.hn@cafemanagement.com` | `Password123!` | Quản lý kho nguyên liệu Hà Nội |
| **Cầu Rồng (Đà Nẵng)** | Quản lý 1 & 2 | `manager1.dn@cafemanagement.com`<br>`manager2.dn@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Đà Nẵng |
| | Thu ngân 1 & 2 | `cashier1.dn@cafemanagement.com`<br>`cashier2.dn@cafemanagement.com` | `Password123!` | Thu ngân Đà Nẵng |
| | Pha chế (1 - 5) | `barista1.dn@cafemanagement.com` ... `barista5.dn@cafemanagement.com` | `Password123!` | Pha chế Đà Nẵng |
| | Thủ kho 1 & 2 | `stock1.dn@cafemanagement.com`<br>`stock2.dn@cafemanagement.com` | `Password123!` | Thủ kho Đà Nẵng |
| **Cầu Đất Farm (Đà Lạt)** | Quản lý 1 & 2 | `manager1.dalat@cafemanagement.com`<br>`manager2.dalat@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Đà Lạt |
| | Thu ngân 1 & 2 | `cashier1.dalat@cafemanagement.com`<br>`cashier2.dalat@cafemanagement.com` | `Password123!` | Thu ngân Đà Lạt |
| | Pha chế (1 - 5) | `barista1.dalat@cafemanagement.com` ... `barista5.dalat@cafemanagement.com` | `Password123!` | Pha chế Đà Lạt |
| | Thủ kho 1 & 2 | `stock1.dalat@cafemanagement.com`<br>`stock2.dalat@cafemanagement.com` | `Password123!` | Thủ kho Đà Lạt |
| **Trung Nguyên (BMT)** | Quản lý 1 & 2 | `manager1.bmt@cafemanagement.com`<br>`manager2.bmt@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Buôn Ma Thuột |
| | Thu ngân 1 & 2 | `cashier1.bmt@cafemanagement.com`<br>`cashier2.bmt@cafemanagement.com` | `Password123!` | Thu ngân BMT |
| | Pha chế (1 - 5) | `barista1.bmt@cafemanagement.com` ... `barista5.bmt@cafemanagement.com` | `Password123!` | Pha chế BMT |
| | Thủ kho 1 & 2 | `stock1.bmt@cafemanagement.com`<br>`stock2.bmt@cafemanagement.com` | `Password123!` | Thủ kho BMT |
| **Thảo Điền (TP. Thủ Đức)** | Quản lý 1 & 2 | `manager1.thaodien@cafemanagement.com`<br>`manager2.thaodien@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Thảo Điền |
| | Thu ngân 1 & 2 | `cashier1.thaodien@cafemanagement.com`<br>`cashier2.thaodien@cafemanagement.com` | `Password123!` | Thu ngân Thảo Điền |
| | Pha chế (1 - 5) | `barista1.thaodien@cafemanagement.com` ... `barista5.thaodien@cafemanagement.com` | `Password123!` | Pha chế Thảo Điền |
| | Thủ kho 1 & 2 | `stock1.thaodien@cafemanagement.com`<br>`stock2.thaodien@cafemanagement.com` | `Password123!` | Thủ kho Thảo Điền |
| **Bến Ninh Kiều (Cần Thơ)** | Quản lý 1 & 2 | `manager1.cantho@cafemanagement.com`<br>`manager2.cantho@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Cần Thơ |
| | Thu ngân 1 & 2 | `cashier1.cantho@cafemanagement.com`<br>`cashier2.cantho@cafemanagement.com` | `Password123!` | Thu ngân Cần Thơ |
| | Pha chế (1 - 5) | `barista1.cantho@cafemanagement.com` ... `barista5.cantho@cafemanagement.com` | `Password123!` | Pha chế Cần Thơ |
| | Thủ kho 1 & 2 | `stock1.cantho@cafemanagement.com`<br>`stock2.cantho@cafemanagement.com` | `Password123!` | Thủ kho Cần Thơ |
| **Nhà Hát TP (Hải Phòng)** | Quản lý 1 & 2 | `manager1.hp@cafemanagement.com`<br>`manager2.hp@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Hải Phòng |
| | Thu ngân 1 & 2 | `cashier1.hp@cafemanagement.com`<br>`cashier2.hp@cafemanagement.com` | `Password123!` | Thu ngân Hải Phòng |
| | Pha chế (1 - 5) | `barista1.hp@cafemanagement.com` ... `barista5.hp@cafemanagement.com` | `Password123!` | Pha chế Hải Phòng |
| | Thủ kho 1 & 2 | `stock1.hp@cafemanagement.com`<br>`stock2.hp@cafemanagement.com` | `Password123!` | Thủ kho Hải Phòng |
| **Sông Hương (TP. Huế)** | Quản lý 1 & 2 | `manager1.hue@cafemanagement.com`<br>`manager2.hue@cafemanagement.com` | `Password123!` | Giới hạn chi nhánh Huế |
| | Thu ngân 1 & 2 | `cashier1.hue@cafemanagement.com`<br>`cashier2.hue@cafemanagement.com` | `Password123!` | Thu ngân Huế |
| | Pha chế (1 - 5) | `barista1.hue@cafemanagement.com` ... `barista5.hue@cafemanagement.com` | `Password123!` | Pha chế Huế |
| | Thủ kho 1 & 2 | `stock1.hue@cafemanagement.com`<br>`stock2.hue@cafemanagement.com` | `Password123!` | Thủ kho Huế |

---

## 4. Ma Trận Phân Quyền Chi Tiết (RBAC Matrix)

```
+--------------------------------+-------+---------+---------+---------+----------------+
| Chức Năng / Tab Trên Giao Diện | Admin | Manager | Cashier | Barista | InventoryStaff |
+--------------------------------+-------+---------+---------+---------+----------------+
| Tổng quan (/dashboard)         |   ✓   |    ✓    |    -    |    -    |       -        |
| Quán & Chi nhánh (/shops)      |   ✓   |    -    |    -    |    -    |       -        |
| Thực đơn đồ uống (/menu)       |   ✓   |    -    |    -    |    -    |       -        |
| Kho & Nguyên liệu (/inventory) |   ✓   |    ✓    |    -    |    -    |       ✓        |
| Bán hàng & Thu ngân (/orders)  |   ✓   |    ✓    |    ✓    |    ✓    |       -        |
| Nhân sự & Ca làm (/staff)      |   ✓   |    ✓    |    ✓    |    ✓    |       ✓        |
| Đánh giá & Phản hồi (/reviews) |   ✓   |    ✓    |    ✓    |    ✓    |       ✓        |
| Giám sát & Backup (/metrics)   |   ✓   |    -    |    -    |    -    |       -        |
+--------------------------------+-------+---------+---------+---------+----------------+
| Chuyển đổi chi nhánh tự do     |   ✓   |    -    |    -    |    -    |       -        |
| Sao lưu & Phục hồi toàn chuỗi  |   ✓   |    -    |    -    |    -    |       -        |
| Tự động khóa chi nhánh gán     |   -   |    ✓    |    ✓    |    ✓    |       ✓        |
+--------------------------------+-------+---------+---------+---------+----------------+
```

---

## 5. Quy Chuẩn Lập Lịch Ca Làm Việc (Work Shifts Scheduling)

Hệ thống đã tự động lập lịch **1.710 ca làm việc** từ **1 tuần trước (11/10/2026)** đến **2 tuần tới (01/11/2026)** theo chuẩn vận hành công nghiệp:
- **Ngày thường (Thứ 2 đến Thứ 6) - 2 ca/ngày:**
  - **Ca sáng (06:30 - 14:30):** 1 Quản lý, 1 Thu ngân, 2 Pha chế, 1 Thủ kho.
  - **Ca chiều/tối (14:30 - 22:30):** 1 Quản lý, 1 Thu ngân, 3 Pha chế, 1 Thủ kho.
- **Cuối tuần (Thứ 7 & Chủ Nhật) - 3 ca/ngày:**
  - **Ca sáng (06:30 - 12:00):** 1 Quản lý, 1 Thu ngân, 3 Pha chế, 1 Thủ kho *(Đỉnh điểm đón khách cuối tuần)*.
  - **Ca chiều (12:00 - 17:30):** 1 Quản lý, 1 Thu ngân, 2 Pha chế, 1 Thủ kho.
  - **Ca tối (17:30 - 22:30):** 1 Quản lý, 1 Thu ngân, 2 Pha chế, 1 Thủ kho *(Phục vụ khách tối & chốt quỹ)*.

---

## 6. Dữ Liệu Đánh Giá & Phản Hồi Khách Hàng (100 Khách Hàng Thật)

- Hệ thống đã nạp sẵn **100 đánh giá chi tiết** từ 100 khách hàng với họ tên tiếng Việt, số điện thoại, ngày trải nghiệm, chấm điểm cửa hàng và chi tiết từng món đồ uống (Cà Phê Muối Cố Đô, Bạc Xỉu 3 Tầng, Trà Sen Vàng Kem Cheese, Bánh Croissant,...).
- Tab **"Đánh giá & Phản hồi" (`/reviews`)** cho phép:
  - Xem thống kê trực quan: Điểm trung bình hệ thống, tỷ lệ hài lòng, biểu đồ phân bổ 1 - 5 sao.
  - Lọc theo từng chi nhánh, số sao, món đồ uống và tìm kiếm khách hàng.
  - Khách hàng (hoặc nhân viên tạo hộ) có thể gửi đánh giá mới kèm chấm sao tương tác và cảm nhận hương vị đồ uống.
