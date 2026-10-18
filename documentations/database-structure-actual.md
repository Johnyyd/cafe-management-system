# Cafe Management System - Cấu Trúc Cơ Sở Dữ Liệu Thực Tế (Actual Database Structure)

Tài liệu này phản ánh **100% cấu trúc schema và các collection thực tế** đang được triển khai, kết nối và vận hành bởi backend (.NET 8.0 Clean Architecture) và cơ sở dữ liệu MongoDB (`cafe_management`). Tài liệu này dùng để so khớp trực tiếp với bản thiết kế ban đầu tại [`database-design.md`](./database-design.md).

---

## 1. Tổng Quan Cơ Sở Dữ Liệu (Database Overview)

- **Hệ quản trị CSDL**: MongoDB 7.0+ (Chạy qua Docker container `cafe_management_mongodb`)
- **Tên cơ sở dữ liệu**: `cafe_management`
- **Cơ chế Object Mapping**: MongoDB C# Driver v2.24+ kết hợp ASP.NET Core Identity Mongo
- **Số lượng collections thực tế**: 9 collections đang hoạt động

| STT | Tên Collection thực tế | Trạng thái dữ liệu | Mô tả chức năng |
|:---:|:---|:---:|:---|
| 1 | `shops` | 9 documents | Thông tin các cửa hàng, chi nhánh, địa chỉ, liên hệ và trạng thái hoạt động |
| 2 | `staff` | 103 documents | Hồ sơ nhân viên, phân quyền vai trò và lịch sử phân bổ chi nhánh (`ShopAssignments`) |
| 3 | `menuItems` | 8 documents | Danh mục đồ uống, món ăn kèm đơn giá `Money { Amount, Currency }` và công thức |
| 4 | `inventoryItems` | 72 documents | Quản lý kho nguyên vật liệu tồn tại từng cửa hàng, định mức cảnh báo tái đặt hàng |
| 5 | `shifts` | 2,493 documents | Lịch phân ca chi tiết, trạng thái ca và thông tin đổi ca/trực thay |
| 6 | `reviews` | 100 documents | Đánh giá đa chiều của khách hàng (gồm sao cửa hàng và đánh giá chi tiết từng món) |
| 7 | `orders` | 0 documents | Đơn đặt hàng, thông tin khách hàng, danh sách món và tổng thanh toán |
| 8 | `users` | 106 documents | Tài khoản đăng nhập hệ thống, mật khẩu băm Identity, vai trò và refresh tokens |
| 9 | `roles` | 5 documents | Danh mục vai trò bảo mật hệ thống (`ADMIN`, `MANAGER`, `CASHIER`, `BARISTA`, `INVENTORY`) |

---

## 2. Quy Ước Toàn Cục (System-wide Conventions)

1. **Khóa chính**: Thuộc tính `_id` kiểu `ObjectId` chuẩn MongoDB.
2. **Kế thừa Audit & Soft Delete**: Tất cả các Domain Entity kế thừa từ `Entity<ObjectId>` và luôn chứa 5 trường:
   - `createdAt`: `ISODate` (thời điểm tạo bản ghi UTC).
   - `updatedAt`: `ISODate` (thời điểm cập nhật gần nhất).
   - `deletedAt`: `ISODate` (nullable - thời điểm xóa mềm, `null` nếu còn hoạt động).
   - `createdBy`: `ObjectId` (nullable - ID người dùng tạo bản ghi).
   - `updatedBy`: `ObjectId` (nullable - ID người dùng cập nhật bản ghi).
3. **Quy chuẩn tài chính (Money Pattern)**: Toàn bộ giá trị tài chính (`Price`, `UnitPrice`, `TotalPrice`, `TotalAmount`) không lưu dưới dạng một số thập phân rời rạc, mà được đóng gói trong ValueObject `Money` dạng:
   ```json
   {
     "Amount": Decimal128,
     "Currency": "VND"
   }
   ```
4. **Lưu trữ Enum**: Toàn bộ trạng thái và vai trò (`Role`, `EmploymentStatus`, `Status`, `CustomerType`, `PaymentStatus`) được serialize dạng **số nguyên (`Int32`)** để tối ưu dung lượng và tốc độ truy vấn index.

---

## 3. Cấu Trúc Chi Tiết Từng Collection Thực Tế (Detailed Collection Schemas)

### 3.1. Collection: `shops`
Quản lý các chi nhánh/địa điểm quán cà phê.

```json
{
  "_id": ObjectId("6ad42a84a70b40fd301ae309"),
  "createdAt": ISODate("2026-10-18T02:10:12.640Z"),
  "updatedAt": ISODate("2026-10-18T02:10:12.640Z"),
  "deletedAt": null,
  "createdBy": null,
  "updatedBy": null,
  "Name": "The Coffee House Landmark 81 - Bình Thạnh",
  "Address": {
    "Street": "720A Điện Biên Phủ, Phường 22",
    "City": "Thành phố Hồ Chí Minh",
    "District": "Quận Bình Thạnh",
    "ZipCode": "72300"
  },
  "Contact": {
    "Phone": "02871087088",
    "Email": "branch.landmark81@cafemanagement.com"
  },
  "OperatingHours": [
    {
      "DayOfWeek": 0,          // 0 = Chủ Nhật, 1 = Thứ Hai, ..., 6 = Thứ Bảy
      "OpenTime": "06:30:00",  // TimeSpan
      "CloseTime": "22:30:00"
    }
  ],
  "Status": 1                  // 1: Active, 2: UnderMaintenance, 3: Closed
}
```

---

### 3.2. Collection: `staff`
Hồ sơ nhân viên. Khác với thiết kế cũ, quan hệ với cửa hàng được quản lý qua mảng `ShopAssignments`.

```json
{
  "_id": ObjectId("6ad42a86a70b40fd301ae320"),
  "createdAt": ISODate("2026-10-18T02:10:14.763Z"),
  "updatedAt": ISODate("2026-10-18T02:10:14.763Z"),
  "deletedAt": null,
  "createdBy": null,
  "updatedBy": null,
  "FirstName": "Trần Quản",
  "LastName": "Lý",
  "Role": 3,                    // 1: Barista, 2: Cashier, 3: Manager, 4: Admin
  "Contact": {
    "Phone": "0908123456",
    "Email": "manager.q1@cafemanagement.com"
  },
  "EmploymentStatus": 1,        // 1: Active, 2: OnLeave, 3: Terminated
  "HireDate": ISODate("2026-01-15T08:00:00.000Z"),
  "ShopAssignments": [          // Mảng lịch sử và địa điểm phân công làm việc
    {
      "_id": ObjectId("6ad42a86a70b40fd301ae321"),
      "createdAt": ISODate("2026-10-18T02:10:14.766Z"),
      "updatedAt": ISODate("2026-10-18T02:10:14.766Z"),
      "deletedAt": null,
      "createdBy": null,
      "updatedBy": null,
      "StaffId": ObjectId("6ad42a86a70b40fd301ae320"),
      "ShopId": ObjectId("6ad42a84a70b40fd301ae309"),
      "AssignedDate": ISODate("2026-10-18T02:10:14.766Z"),
      "UnassignedDate": null,
      "IsPrimary": true
    }
  ]
}
```

---

### 3.3. Collection: `menuItems`
Món ăn và thức uống trong thực đơn.

```json
{
  "_id": ObjectId("6ad42a86a70b40fd301ae318"),
  "createdAt": ISODate("2026-10-18T02:10:14.455Z"),
  "updatedAt": ISODate("2026-10-18T02:10:14.455Z"),
  "deletedAt": null,
  "createdBy": null,
  "updatedBy": null,
  "ShopId": ObjectId("6ad42a84a70b40fd301ae309"),
  "Category": "Cà phê",
  "Name": "Cà Phê Muối Cố Đô",
  "Description": "Cà phê Robusta phin truyền thống phối lớp kem muối mặn béo ngậy đặc trưng xứ Huế.",
  "Price": {
    "Amount": Decimal128("38000"),
    "Currency": "VND"
  },
  "Ingredients": [
    "Cà phê Robusta Đắk Lắk",
    "Sữa đặc Ngôi Sao Phương Nam",
    "Kem béo Rich",
    "Muối biển tinh khiết"
  ],
  "Allergens": [
    "Sữa bò (Lactose)"
  ],
  "Availability": {
    "StartTime": "06:00:00",
    "EndTime": "23:00:00",
    "DaysOfWeek": [0, 1, 2, 3, 4, 5, 6]
  },
  "Status": 1                   // 1: Available, 2: Unavailable, 3: Seasonal
}
```

---

### 3.4. Collection: `inventoryItems`
Nguyên vật liệu và bán thành phẩm trong kho theo từng cửa hàng.

```json
{
  "_id": ObjectId("6ad42a87a70b40fd301ae32c"),
  "createdAt": ISODate("2026-10-18T02:10:15.118Z"),
  "updatedAt": ISODate("2026-10-18T02:10:15.118Z"),
  "deletedAt": null,
  "createdBy": null,
  "updatedBy": null,
  "ShopId": ObjectId("6ad42a84a70b40fd301ae309"),
  "ItemName": "Hạt Cà phê Robusta Buôn Ma Thuột",
  "Unit": "kg",
  "Quantity": "120",            // Decimal
  "ReorderLevel": "25",         // Decimal - Mức tồn tối thiểu kích hoạt cảnh báo đặt hàng
  "SupplierId": null,           // ObjectId tham chiếu (nullable)
  "LastUpdated": ISODate("2026-10-18T02:10:15.118Z")
}
```

---

### 3.5. Collection: `shifts`
Lịch phân ca làm việc của nhân sự tại các cửa hàng, tích hợp cơ chế đổi ca/trực thay.

```json
{
  "_id": ObjectId("6ad42a86a70b40fd301ae328"),
  "createdAt": ISODate("2026-10-18T02:10:14.989Z"),
  "updatedAt": ISODate("2026-10-18T02:10:14.989Z"),
  "deletedAt": null,
  "createdBy": null,
  "updatedBy": null,
  "StaffId": ObjectId("6ad42a86a70b40fd301ae320"),
  "ShopId": ObjectId("6ad42a84a70b40fd301ae309"),
  "StartTime": ISODate("2026-10-18T00:10:14.000Z"),
  "EndTime": ISODate("2026-10-18T08:10:14.000Z"),
  "Status": 0,                  // 0: Scheduled, 1: InProgress, 2: Completed, 3: Cancelled, 4: Swapped
  "Notes": "Ca sáng: Mở quán, kiểm tra máy pha espresso, đón khách",
  "CoveredByStaffId": null,     // ObjectId nhân viên trực thay (nếu có)
  "CoveredAt": null,
  "CoveredBy": null,
  "SwappedAt": null,            // Thời điểm hoán đổi ca (nếu có)
  "SwappedWithStaffId": null,   // ObjectId nhân viên đổi ca cùng
  "SwappedWithShiftId": null    // ObjectId ca đối ứng
}
```

---

### 3.6. Collection: `reviews`
Hệ thống đánh giá đa chiều từ khách hàng, bao gồm đánh giá tổng thể cửa hàng và chi tiết từng món đã đặt.

```json
{
  "_id": ObjectId("6ad42f56634109e831bff5c9"),
  "ShopId": "6ad42a84a70b40fd301ae309",
  "ShopName": "The Coffee House Landmark 81 - Bình Thạnh",  // Denormalized
  "OrderId": "6ad42a86a70b40fd301ae399",
  "CustomerName": "Phạm Văn Thảo",
  "CustomerPhone": "0934744854",
  "ShopRating": 5,                                         // 1 đến 5 sao
  "ShopComment": "Nhân viên nhiệt tình, gọi món nhanh và rất chu đáo khi mang nước tận bàn.",
  "ItemReviews": [                                         // Đánh giá chi tiết từng món đã dùng
    {
      "MenuItemId": "6ad42a86a70b40fd301ae319",
      "MenuItemName": "Cà Phê Sữa Đá Sài Gòn",
      "Rating": 4,
      "Comment": "Cà phê sữa đá đậm đà đúng chất Sài Gòn, ngọt thơm không bị khé cổ."
    },
    {
      "MenuItemId": "6ad42a86a70b40fd301ae31c",
      "MenuItemName": "Trà Đào Cam Sả Tươi",
      "Rating": 4,
      "Comment": "Độ ngọt vừa vặn, uống sảng khoái và thanh mát cơ thể."
    }
  ],
  "Tags": [
    "View đẹp check-in",
    "Pha chế nhanh"
  ],
  "CreatedAt": ISODate("2026-10-03T16:30:46.000Z")
}
```

---

### 3.7. Collection: `orders`
Đơn đặt hàng của khách hàng (dine-in, takeaway, online).

```json
{
  "_id": ObjectId("..."),
  "createdAt": ISODate("..."),
  "updatedAt": ISODate("..."),
  "deletedAt": null,
  "createdBy": null,
  "updatedBy": null,
  "ShopId": ObjectId("6ad42a84a70b40fd301ae309"),
  "StaffId": ObjectId("6ad42a86a70b40fd301ae320"),
  "CustomerInfo": {
    "Name": "Nguyễn Văn A",
    "Contact": "0912345678",
    "Type": 1                   // 1: DineIn, 2: Takeaway, 3: Online
  },
  "Items": [
    {
      "MenuItemId": ObjectId("6ad42a86a70b40fd301ae318"),
      "Name": "Cà Phê Muối Cố Đô",
      "Description": "Cà phê Robusta phin truyền thống phối lớp kem muối mặn",
      "UnitPrice": {
        "Amount": Decimal128("38000"),
        "Currency": "VND"
      },
      "Quantity": 2,
      "SpecialInstructions": "Ít đá, ít ngọt",
      "TotalPrice": {
        "Amount": Decimal128("76000"),
        "Currency": "VND"
      }
    }
  ],
  "Status": 1,                  // 1: Placed, 2: Preparing, 3: Ready, 4: Completed, 5: Cancelled
  "PaymentStatus": 2,           // 1: Pending, 2: Paid, 3: Failed, 4: Refunded
  "TotalAmount": {
    "Amount": Decimal128("76000"),
    "Currency": "VND"
  },
  "OrderTime": ISODate("..."),
  "CompletedTime": ISODate("...")
}
```

---

### 3.8. Collection: `users`
Tài khoản người dùng và xác thực bảo mật hệ thống (ASP.NET Core Identity Mongo).

```json
{
  "_id": ObjectId("6ad42a83a70b40fd301ae307"),
  "UserName": "admin@cafemanagement.com",
  "NormalizedUserName": "ADMIN@CAFEMANAGEMENT.COM",
  "Email": "admin@cafemanagement.com",
  "NormalizedEmail": "ADMIN@CAFEMANAGEMENT.COM",
  "EmailConfirmed": true,
  "PasswordHash": "AQAAAAIAAYagAAAAEOgAzSXxadep7Hr/pWr8f0uNun6vaPVzjauO6+pCxgh5n+VQMTbEaMf+W7oyuxkmZw==",
  "FullName": "Quản trị viên Hệ thống",
  "shopId": null,              // Gắn với ShopId nếu là nhân viên chi nhánh
  "roles": [
    "ADMIN"                    // Danh sách mã vai trò được cấp
  ],
  "refreshTokens": [           // Mảng lưu trữ token cấp mới phiên làm việc
    {
      "token": "...",
      "expires": ISODate("2026-10-25T02:10:12.249Z"),
      "created": ISODate("2026-10-18T02:10:12.250Z"),
      "revoked": null,
      "revokedByIp": null
    }
  ],
  "createdAt": ISODate("2026-10-18T02:10:11.694Z"),
  "updatedAt": ISODate("2026-10-18T03:48:32.849Z")
}
```

---

### 3.9. Collection: `roles`
Danh mục các vai trò bảo mật trong phân quyền người dùng.

```json
{
  "_id": ObjectId("6ad42a84a70b40fd301ae308"),
  "Name": "Admin",
  "NormalizedName": "ADMIN",
  "ConcurrencyStamp": null,
  "Description": "Quản trị viên toàn hệ thống"
}
```

---

## 4. Bảng So Sánh Đối Chiếu Với Thiết Kế Ban Đầu (`database-design.md`)

| Collection trong `database-design.md` | Collection Thực tế | Loại thay đổi | Chi tiết thay đổi cụ thể |
|:---|:---|:---:|:---|
| **Shops** | `shops` | Cập nhật nhỏ | Cấu trúc dữ liệu giữ nguyên. `Status` lưu dạng số nguyên (`int`) thay vì chuỗi enum. |
| **Staff** | `staff` | 🔄 Cấu trúc lớn | Xóa bỏ trường đơn lẻ `shopId`. Thay thế bằng mảng `ShopAssignments` chứa lịch sử phân công và cờ `IsPrimary`. |
| **MenuItems** | `menuItems` | 🔄 Cấu trúc | Trường `price` (số đơn) được nâng cấp thành Object `Price: { Amount, Currency }`. Tên collection viết thường dạng camelCase `menuItems`. |
| **Inventory** | `inventoryItems` | ✏️ Đổi tên | Đổi tên collection từ `Inventory` thành `inventoryItems`. Các trường thuộc tính bên trong giữ nguyên. |
| **Orders** | `orders` | 🔄 Cấu trúc | Các trường tiền `UnitPrice`, `TotalPrice`, `TotalAmount` được nâng cấp từ số đơn thành Object `Money { Amount, Currency }`. |
| **Shifts** | `shifts` | 🔄 Cấu trúc lớn | **Bỏ trường:** `breakDuration`.<br>**Thêm trường:** `Status` (trạng thái ca) và 6 trường hỗ trợ đổi ca/trực thay (`CoveredByStaffId`, `CoveredAt`, `CoveredBy`, `SwappedAt`, `SwappedWithStaffId`, `SwappedWithShiftId`). |
| **CustomerFeedback** | `reviews` | 🔄 Cấu trúc lớn | **Đổi tên collection:** thành `reviews`.<br>**Bỏ:** các điểm phẳng `ratingOverall`, `ratingService`, `ratingProduct`.<br>**Thêm:** Đánh giá cửa hàng (`ShopRating`, `ShopComment`) + Đánh giá chi tiết từng món (`ItemReviews: [...]`) + Định danh khách hàng (`CustomerName`, `CustomerPhone`) + `Tags: [...]`. |
| **Suppliers** | *(Không có)* | ❌ Lược bỏ | Không tạo collection `suppliers` riêng; chỉ duy trì trường tham chiếu `SupplierId` trong `inventoryItems`. |
| **QAChecklists** | *(Không có)* | ❌ Lược bỏ | Tính năng danh mục kiểm tra chất lượng không có trong hệ thống thực tế. |
| **QARecords** | *(Không có)* | ❌ Lược bỏ | Tính năng lưu lịch sử kiểm tra chất lượng không có trong hệ thống thực tế. |
| *(Chưa có trong thiết kế)* | `users` | ➕ Thêm mới | Tạo mới collection quản lý đăng nhập, bảo mật JWT, BCrypt mật khẩu và refresh tokens của ASP.NET Identity. |
| *(Chưa có trong thiết kế)* | `roles` | ➕ Thêm mới | Tạo mới danh mục vai trò (`ADMIN`, `MANAGER`, `CASHIER`, `BARISTA`, `INVENTORY`). |
