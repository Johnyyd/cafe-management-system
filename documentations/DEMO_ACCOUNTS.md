# Danh Sách Tài Khoản Mẫu & Ma Trận Phân Quyền (Demo Accounts & RBAC)

> **Mật khẩu chung cho tất cả tài khoản mẫu:** `Password123!`

---

## 1. Bảng Danh Sách Tài Khoản

| STT | Họ & Tên | Email Đăng Nhập | Mật Khẩu | Vai Trò (Role) | Chi Nhánh Phụ Trách | Các Tab Được Truy Cập | Ghi Chú Quyền Hạn |
|:---:|:---|:---|:---|:---|:---|:---|:---|
| **1** | **Quản Trị Viên Hệ Thống** | `admin@cafemanagement.com` | `Password123!` | **Admin** | **Tất cả các chi nhánh** (toàn chuỗi) | • Tổng quan (`/dashboard`)<br>• Chi nhánh (`/shops`)<br>• Thực đơn (`/menu`)<br>• Kho (`/inventory`)<br>• Bán hàng (`/orders`)<br>• Nhân sự (`/staff`)<br>• Giám sát & Backup (`/metrics`) | **Toàn quyền tối cao**: Chuyển đổi qua lại giữa tất cả các chi nhánh, thêm/xóa/sửa tất cả dữ liệu, test hệ thống, **sao lưu (export) và phục hồi (restore)** cơ sở dữ liệu. |
| **2** | **Trần Quản Lý (Q1)** | `manager.q1@cafemanagement.com` | `Password123!` | **Manager** | **Cafe Trung Tâm - Quận 1** | • Tổng quan (`/dashboard`)<br>• Bán hàng (`/orders`)<br>• Kho (`/inventory`)<br>• Nhân sự (`/staff`) | **Quản lý chi nhánh Q1**: Chỉ xem và thao tác dữ liệu thuộc chi nhánh Q1. Không được chuyển sang chi nhánh khác, không có quyền vào Quán & Chi nhánh hoặc tab Giám sát / Backup. |
| **3** | **Lê Thu Ngân (Q1)** | `cashier.q1@cafemanagement.com` | `Password123!` | **Cashier** | **Cafe Trung Tâm - Quận 1** | • Bán hàng & Thu ngân (`/orders`) | **Thu ngân chi nhánh Q1**: Chỉ mở và làm việc duy nhất tại tab Bán hàng & Thu ngân của chi nhánh Q1. Tự động điều hướng đến `/orders` khi đăng nhập. |
| **4** | **Phạm Pha Chế (Q1)** | `barista.q1@cafemanagement.com` | `Password123!` | **Barista** | **Cafe Trung Tâm - Quận 1** | • Bán hàng & Chế biến (`/orders`) | **Pha chế chi nhánh Q1**: Tiếp nhận đơn và cập nhật trạng thái pha chế tại chi nhánh Q1. Tự động điều hướng đến `/orders` khi đăng nhập. |
| **5** | **Hoàng Thủ Kho (Q1)** | `stock.q1@cafemanagement.com` | `Password123!` | **InventoryStaff** | **Cafe Trung Tâm - Quận 1** | • Kho & Nguyên liệu (`/inventory`) | **Thủ kho chi nhánh Q1**: Kiểm kê và điều chỉnh số lượng tồn kho nguyên vật liệu tại chi nhánh Q1. Tự động điều hướng đến `/inventory` khi đăng nhập. |
| **6** | **Nguyễn Quản Lý (Thảo Điền)** | `manager.thaodien@cafemanagement.com` | `Password123!` | **Manager** | **Cafe Thảo Điền - TP. Thủ Đức** | • Tổng quan (`/dashboard`)<br>• Bán hàng (`/orders`)<br>• Kho (`/inventory`)<br>• Nhân sự (`/staff`) | **Quản lý chi nhánh Thảo Điền**: Quản lý độc lập tại cơ sở Thảo Điền, cố định chi nhánh, không can thiệp chi nhánh khác. |
| **7** | **Đỗ Thu Ngân (Thảo Điền)** | `cashier.thaodien@cafemanagement.com` | `Password123!` | **Cashier** | **Cafe Thảo Điền - TP. Thủ Đức** | • Bán hàng & Thu ngân (`/orders`) | **Thu ngân chi nhánh Thảo Điền**: Xử lý tạo đơn tại quầy cho chi nhánh Thảo Điền. |

---

## 2. Chi Tiết Ma Trận Phân Quyền (RBAC Matrix)

```
+-----------------------------+-------+---------+---------+---------+----------------+
| Chức Năng / Tab             | Admin | Manager | Cashier | Barista | InventoryStaff |
+-----------------------------+-------+---------+---------+---------+----------------+
| Tổng quan (/dashboard)      |   ✓   |    ✓    |    -    |    -    |       -        |
| Quán & Chi nhánh (/shops)   |   ✓   |    -    |    -    |    -    |       -        |
| Thực đơn đồ uống (/menu)    |   ✓   |    -    |    -    |    -    |       -        |
| Kho & Nguyên liệu (/inventory)|  ✓   |    ✓    |    -    |    -    |       ✓        |
| Bán hàng & Thu ngân (/orders)|  ✓   |    ✓    |    ✓    |    ✓    |       -        |
| Nhân sự & Ca làm (/staff)   |   ✓   |    ✓    |    -    |    -    |       -        |
| Giám sát & Backup (/metrics)|   ✓   |    -    |    -    |    -    |       -        |
+-----------------------------+-------+---------+---------+---------+----------------+
| Đổi chi nhánh tự do         |   ✓   |    -    |    -    |    -    |       -        |
| Sao lưu & Phục hồi (DB)     |   ✓   |    -    |    -    |    -    |       -        |
| Khóa vào chi nhánh chỉ định |   -   |    ✓    |    ✓    |    ✓    |       ✓        |
+-----------------------------+-------+---------+---------+---------+----------------+
```

---

## 3. Các Điểm Đã Nâng Cấp & Hoàn Thiện

1. **Sao lưu (Backup) & Phục hồi (Restore) toàn hệ thống:**
   - **Backend:** Thêm `BackupController` (`/api/v1/system/backup/summary`, `/api/v1/system/backup/export`, `/api/v1/system/backup/restore`) bảo vệ bằng quyền `Admin`.
   - **Frontend:** Tab Giám sát hệ thống (`/metrics`) có bảng thống kê tổng số bản ghi của 6 collections, nút tải về file JSON sao lưu tức thì và chức năng nạp/phục hồi file sao lưu kèm modal cảnh báo xác nhận.
2. **Khóa chi nhánh đối với Quản lý và Nhân viên:**
   - Khi tài khoản đăng nhập có chi nhánh làm việc (`shopId`), hệ thống tự động khóa và chỉ hiển thị badge chi nhánh phụ trách trên Header, không cho phép chọn chuyển sang chi nhánh khác.
   - Các API lấy dữ liệu Kho (`/inventory`), Đơn hàng (`/orders`), và Nhân sự (`/staff`) tự động lọc theo đúng `shopId` của chi nhánh đó.
3. **Phân quyền và bảo vệ Router (RoleRouteGuard):**
   - Nhân viên thu ngân (`Cashier`) và pha chế (`Barista`) chỉ truy cập được tab `/orders`.
   - Thủ kho (`InventoryStaff`) chỉ truy cập được tab `/inventory`.
   - Nếu người dùng nhập URL khác thẩm quyền trên thanh địa chỉ, router sẽ tự động chuyển hướng về trang hợp lệ của họ.
4. **Trang Đăng Nhập Chuẩn Hóa:**
   - Đã gỡ bỏ khối nút chọn tài khoản mẫu trên trang đăng nhập (`LoginPage.tsx`) theo yêu cầu.
   - Đăng nhập bảo mật chuẩn REST API JWT Bearer token và chuyển hướng thông minh theo vai trò người dùng.
