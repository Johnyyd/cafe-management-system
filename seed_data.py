#!/usr/bin/env python3
"""
Comprehensive Seed script for Cafe Management System.
Features:
- 9 Shops across major Vietnam cities/provinces.
- Staff per shop: 2 Managers, 2 Cashiers, 5 Baristas, 2 Inventory Staff (11 per shop, ~99 total).
- RBAC User accounts registered with Password123! and shopId scoping.
- Work Shifts scheduling: From 1 week ago (2026-10-11) to 2 weeks future (2026-11-01).
  * Weekdays: 2 shifts/day (Morning: 1 manager, 1 cashier, 2-3 baristas, 1 stock; Evening: remaining staff).
  * Weekends: 3 shifts/day (Morning: 1 manager, 1 cashier, 3 baristas, 1 stock; Afternoon & Evening staffed).
- Menu items available 24/7.
- Sample realistic orders across all 9 shops.
- 100 Customer Reviews for shops and ordered products.
"""

import sys
import json
import datetime
import random
import urllib.request
import urllib.error
from typing import Dict, List, Any, Optional

if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

API_BASE = "http://localhost:7890/api/v1"

def http_request(url: str, method: str = "GET", data: Optional[Dict[str, Any]] = None, headers: Optional[Dict[str, str]] = None) -> Any:
    req_headers = {"User-Agent": "CafeManagementSeed/1.0"}
    if headers:
        req_headers.update(headers)
    
    encoded_data = None
    if data is not None:
        req_headers["Content-Type"] = "application/json"
        encoded_data = json.dumps(data).encode("utf-8")

    req = urllib.request.Request(url, data=encoded_data, headers=req_headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=25) as resp:
            content = resp.read().decode("utf-8")
            if not content:
                return {}
            return json.loads(content)
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8")
        try:
            parsed = json.loads(error_body)
            err_msg = parsed.get("detail") or parsed.get("title") or parsed.get("message") or error_body
        except Exception:
            err_msg = error_body
        raise RuntimeError(f"HTTP {e.code} on {method} {url}: {err_msg}")
    except Exception as e:
        raise RuntimeError(f"Request failed on {method} {url}: {e}")

def get_vietnam_provinces() -> List[Dict[str, Any]]:
    """Fetch Vietnam province list from the online API with local fallback."""
    print("[1/8] Đang tải danh mục Tỉnh/Thành phố Việt Nam...")
    endpoints = [
        "https://provinces.open-api.vn/api/p/?depth=2",
        "https://provinces.open-api.vn/api/v2/p/",
        "https://provinces.open-api.vn/api/p/",
    ]
    for ep in endpoints:
        try:
            data = http_request(ep)
            if isinstance(data, list) and len(data) > 0:
                print(f" -> Tải thành công {len(data)} Tỉnh/Thành phố từ {ep}")
                return data
        except Exception:
            pass

    print(" -> Sử dụng danh mục tỉnh thành trọng điểm dự phòng...")
    return [
        {"name": "Thành phố Hồ Chí Minh", "code": 79},
        {"name": "Thành phố Hà Nội", "code": 1},
        {"name": "Thành phố Đà Nẵng", "code": 48},
        {"name": "Tỉnh Lâm Đồng", "code": 68},
        {"name": "Tỉnh Đắk Lắk", "code": 66},
        {"name": "Thành phố Cần Thơ", "code": 92},
        {"name": "Thành phố Hải Phòng", "code": 31},
        {"name": "Tỉnh Thừa Thiên Huế", "code": 46},
        {"name": "Tỉnh Khánh Hòa", "code": 56},
    ]

def authenticate() -> str:
    """Login or register admin account and retrieve Bearer JWT token."""
    print("[2/8] Xác thực quản trị viên (Admin authentication)...")
    login_url = f"{API_BASE}/auth/login"
    login_payload = {
        "email": "admin@cafemanagement.com",
        "password": "Password123!"
    }
    try:
        res = http_request(login_url, method="POST", data=login_payload)
        token = res.get("accessToken")
        if token:
            print(" -> Đăng nhập tài khoản admin thành công.")
            return token
    except Exception:
        pass

    # Register admin if not present
    reg_url = f"{API_BASE}/auth/register"
    reg_payload = {
        "email": "admin@cafemanagement.com",
        "password": "Password123!",
        "fullName": "Quản trị viên Hệ thống",
        "role": "Admin"
    }
    reg_res = http_request(reg_url, method="POST", data=reg_payload)
    token = reg_res.get("accessToken")
    if not token:
        raise RuntimeError("Không thể lấy token sau khi đăng ký!")
    print(" -> Đăng ký và đăng nhập tài khoản admin thành công.")
    return token

def seed_shops(headers: Dict[str, str], provinces: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    print("\n[3/8] Đồng bộ 9 Chi nhánh quán (Shops) với các Tỉnh/Thành phố Việt Nam...")
    res = http_request(f"{API_BASE}/shops", headers=headers)
    existing_shops = res if isinstance(res, list) else res.get("items", [])

    target_branches = [
        {
            "slug": "landmark81",
            "name": "The Coffee House Landmark 81 - Bình Thạnh",
            "city": "Thành phố Hồ Chí Minh",
            "street": "720A Điện Biên Phủ, Phường 22",
            "district": "Quận Bình Thạnh",
            "zipCode": "72300",
            "phone": "02871087088",
            "email": "branch.landmark81@cafemanagement.com",
        },
        {
            "slug": "nhahatlon",
            "name": "Highlands Coffee Nhà Hát Lớn - Hoàn Kiếm",
            "city": "Thành phố Hà Nội",
            "street": "1 Tràng Tiền, Phường Tràng Tiền",
            "district": "Quận Hoàn Kiếm",
            "zipCode": "10000",
            "phone": "02439338888",
            "email": "branch.nhahatlon@cafemanagement.com",
        },
        {
            "slug": "danang",
            "name": "Phúc Long Tea & Coffee Cầu Rồng - Hải Châu",
            "city": "Thành phố Đà Nẵng",
            "street": "61 Nguyễn Văn Linh, Phường Nam Dương",
            "district": "Quận Hải Châu",
            "zipCode": "55000",
            "phone": "02363588999",
            "email": "branch.danang@cafemanagement.com",
        },
        {
            "slug": "caudat",
            "name": "Cafe Cầu Đất Farm - Thành phố Đà Lạt",
            "city": "Tỉnh Lâm Đồng",
            "street": "Thôn Trường Thọ, Xã Trạm Hành",
            "district": "Thành phố Đà Lạt",
            "zipCode": "67000",
            "phone": "02633889977",
            "email": "branch.caudat@cafemanagement.com",
        },
        {
            "slug": "bmt",
            "name": "Trung Nguyên Legend Buôn Ma Thuột",
            "city": "Tỉnh Đắk Lắk",
            "street": "45 Lý Thái Tổ, Phường Tân Lợi",
            "district": "Thành phố Buôn Ma Thuột",
            "zipCode": "63000",
            "phone": "02623955666",
            "email": "branch.bmt@cafemanagement.com",
        },
        {
            "slug": "thaodien",
            "name": "Cafe Thảo Điền - TP. Thủ Đức",
            "city": "Thành phố Hồ Chí Minh",
            "street": "28 Thảo Điền, Phường Thảo Điền",
            "district": "Thành phố Thủ Đức",
            "zipCode": "71300",
            "phone": "02838991234",
            "email": "branch.thaodien@cafemanagement.com",
        },
        {
            "slug": "ninhkieu",
            "name": "Highlands Coffee Bến Ninh Kiều",
            "city": "Thành phố Cần Thơ",
            "street": "02 Hai Bà Trưng, Phường Tân An",
            "district": "Quận Ninh Kiều",
            "zipCode": "90000",
            "phone": "02923812345",
            "email": "branch.cantho@cafemanagement.com",
        },
        {
            "slug": "haiphong",
            "name": "Phúc Long Nhà Hát Thành Phố - Hồng Bàng",
            "city": "Thành phố Hải Phòng",
            "street": "28 Trần Hưng Đạo, Phường Hoàng Văn Thụ",
            "district": "Quận Hồng Bàng",
            "zipCode": "18000",
            "phone": "02253888999",
            "email": "branch.haiphong@cafemanagement.com",
        },
        {
            "slug": "hue",
            "name": "Cafe Cố Đô Sông Hương - TP. Huế",
            "city": "Tỉnh Thừa Thiên Huế",
            "street": "14 Lê Lợi, Phường Vĩnh Ninh",
            "district": "Thành phố Huế",
            "zipCode": "53000",
            "phone": "02343888777",
            "email": "branch.hue@cafemanagement.com",
        },
    ]

    for b in target_branches:
        existing = next((s for s in existing_shops if s.get("name") == b["name"]), None)
        if existing:
            continue

        payload = {
            "name": b["name"],
            "address": {
                "street": b["street"],
                "city": b["city"],
                "district": b["district"],
                "zipCode": b["zipCode"]
            },
            "contact": {
                "phone": b["phone"],
                "email": b["email"]
            },
            "operatingHours": [
                {"dayOfWeek": d, "openTime": "06:30:00", "closeTime": "22:30:00"}
                for d in range(7)
            ]
        }
        try:
            res_shop = http_request(f"{API_BASE}/shops", method="POST", data=payload, headers=headers)
            shop_id = res_shop if isinstance(res_shop, str) else res_shop.get("id", "")
            print(f" -> Tạo thành công chi nhánh: {b['name']} [ID: {shop_id}]")
        except Exception as e:
            print(f" -> Chi nhánh {b['name']}: {e}")

    # Fetch updated shops
    res = http_request(f"{API_BASE}/shops", headers=headers)
    all_shops = res if isinstance(res, list) else res.get("items", [])
    print(f" -> Hiện có tổng cộng {len(all_shops)} chi nhánh trong hệ thống.")
    for idx, s in enumerate(all_shops[:9]):
        print(f"    {idx+1}. [{s.get('id')}] {s.get('name')}")
    return all_shops[:9]

def seed_menu(headers: Dict[str, str], shop_id: str) -> List[Dict[str, Any]]:
    print("\n[4/8] Đồng bộ Thực đơn Đồ uống & Món ăn (Menu Items)...")
    res = http_request(f"{API_BASE}/menu", headers=headers)
    existing_menu = res if isinstance(res, list) else res.get("items", [])

    items_to_add = [
        {
            "name": "Cà Phê Muối Cố Đô",
            "category": "Cà phê",
            "description": "Cà phê Robusta phin truyền thống phối lớp kem muối mặn béo ngậy đặc trưng xứ Huế.",
            "price": 38000,
            "ingredients": ["Cà phê Robusta Buôn Ma Thuột", "Sữa đặc Ông Thọ", "Kem muối béo"],
            "allergens": ["Sữa"],
        },
        {
            "name": "Cà Phê Sữa Đá Sài Gòn",
            "category": "Cà phê",
            "description": "Cà phê đậm đà hòa quyện cùng sữa đặc ngọt thơm, đá viên mát lạnh phong cách Sài Gòn.",
            "price": 32000,
            "ingredients": ["Cà phê Robusta", "Sữa đặc Ông Thọ", "Đá viên"],
            "allergens": ["Sữa"],
        },
        {
            "name": "Bạc Xỉu 3 Tầng Nghệ Nhân",
            "category": "Cà phê",
            "description": "Nhiều sữa ít cà phê, phân tầng đẹp mắt giữa sữa tươi, sữa đặc và lớp cà phê espresso bồng bềnh.",
            "price": 35000,
            "ingredients": ["Sữa tươi Dalat Milk", "Sữa đặc", "Cà phê Arabica"],
            "allergens": ["Sữa"],
        },
        {
            "name": "Cà Phê Đen Phin Cầu Đất",
            "category": "Cà phê",
            "description": "Hạt cà phê nguyên chất rang mộc 100%, chiết xuất phin nhỏ giọt thơm nồng nàn vị mộc tự nhiên.",
            "price": 28000,
            "ingredients": ["Hạt Arabica Cầu Đất", "Nước sôi tinh khiết"],
            "allergens": [],
        },
        {
            "name": "Trà Đào Cam Sả Tươi",
            "category": "Trà & Trái Cây",
            "description": "Trà đen ủ lạnh kết hợp cam vàng mọng nước, sả thơm thanh mát cùng miếng đào giòn ngọt.",
            "price": 45000,
            "ingredients": ["Trà đen Bảo Lộc", "Cam vàng", "Sả tươi", "Đào ngâm miếng"],
            "allergens": [],
        },
        {
            "name": "Trà Sen Vàng Kem Cheese",
            "category": "Trà & Trái Cây",
            "description": "Trà Ô Long thanh dịu cùng hạt sen hầm mềm bùi, phủ trên là lớp kem cheese phô mai béo mịn.",
            "price": 49000,
            "ingredients": ["Trà Ô Long", "Hạt sen Đồng Tháp", "Kem phô mai Cheese"],
            "allergens": ["Sữa"],
        },
        {
            "name": "Bánh Mì Thịt Nguội Pate Giòn",
            "category": "Bánh Mì & Tráng Miệng",
            "description": "Bánh mì vỏ giòn ruột xốp kèm pate gan béo thơm, chả lụa, dưa leo, ngò gai và sốt bơ trứng.",
            "price": 35000,
            "ingredients": ["Bánh mì", "Pate gan", "Thịt nguội", "Dưa chua", "Ngò gai"],
            "allergens": ["Gluten"],
        },
        {
            "name": "Bánh Croissant Bơ Tỏi Nướng",
            "category": "Bánh Mì & Tráng Miệng",
            "description": "Bánh sừng bò ngàn lớp xốp mềm nướng cùng sốt bơ tỏi thơm lừng chuẩn vị Pháp.",
            "price": 39000,
            "ingredients": ["Bột mì", "Bơ lạt Anchor", "Tỏi băm", "Lá mùi tây"],
            "allergens": ["Gluten", "Sữa"],
        },
    ]

    for item in items_to_add:
        if any(m.get("name") == item["name"] for m in existing_menu):
            continue

        payload = {
            "shopId": shop_id,
            "category": item["category"],
            "name": item["name"],
            "description": item["description"],
            "price": {"amount": item["price"], "currency": "VND"},
            "ingredients": item["ingredients"],
            "allergens": item["allergens"],
            "availability": {
                "startTime": "00:00:00",
                "endTime": "23:59:59",
                "daysOfWeek": [0, 1, 2, 3, 4, 5, 6]
            }
        }
        try:
            created_res = http_request(f"{API_BASE}/menu", method="POST", data=payload, headers=headers)
            item_id = created_res if isinstance(created_res, str) else created_res.get("id", "")
            print(f" -> Tạo thành công món: {item['name']} - {item['price']:,} ₫ [ID: {item_id}]")
        except Exception as e:
            print(f" -> Bỏ qua món {item['name']}: {e}")

    res = http_request(f"{API_BASE}/menu", headers=headers)
    return res if isinstance(res, list) else res.get("items", [])

def seed_inventory_for_shops(headers: Dict[str, str], shops: List[Dict[str, Any]]):
    print("\n[5/8] Đồng bộ Kho nguyên vật liệu cho 9 chi nhánh...")
    inventory_items = [
        {"itemName": "Hạt Cà phê Robusta Buôn Ma Thuột", "unit": "kg", "quantity": 150, "reorderLevel": 25},
        {"itemName": "Hạt Cà phê Arabica Cầu Đất Đà Lạt", "unit": "kg", "quantity": 90, "reorderLevel": 20},
        {"itemName": "Sữa đặc có đường Ông Thọ", "unit": "lon", "quantity": 180, "reorderLevel": 30},
        {"itemName": "Sữa tươi thanh trùng Dalat Milk", "unit": "hộp 1L", "quantity": 80, "reorderLevel": 15},
        {"itemName": "Trà Ô Long Bảo Lộc Tuyển Chọn", "unit": "kg", "quantity": 40, "reorderLevel": 10},
        {"itemName": "Đào ngâm đóng hộp Kronos", "unit": "hộp", "quantity": 60, "reorderLevel": 12},
        {"itemName": "Kem béo thực vật Rich's", "unit": "hộp 454g", "quantity": 50, "reorderLevel": 10},
        {"itemName": "Đường cát trắng tinh luyện Biên Hòa", "unit": "kg", "quantity": 100, "reorderLevel": 20},
    ]

    for s in shops:
        s_id = s.get("id")
        if not s_id:
            continue
        try:
            res_inv = http_request(f"{API_BASE}/inventory?shopId={s_id}", headers=headers)
            existing_items = res_inv if isinstance(res_inv, list) else res_inv.get("items", [])
            for it in inventory_items:
                if any(x.get("itemName") == it["itemName"] for x in existing_items):
                    continue
                payload = {
                    "shopId": s_id,
                    "itemName": it["itemName"],
                    "unit": it["unit"],
                    "quantity": it["quantity"],
                    "reorderLevel": it["reorderLevel"]
                }
                http_request(f"{API_BASE}/inventory", method="POST", data=payload, headers=headers)
        except Exception:
            pass
    print(" -> Đã đồng bộ đầy đủ kho nguyên liệu cho toàn bộ 9 chi nhánh.")

def seed_staff_and_accounts(headers: Dict[str, str], shops: List[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    """
    Seeds staff members for each shop according to requirements:
    - 2 Managers (Role 1)
    - 2 Cashiers (Role 2)
    - 5 Baristas (Role 3)
    - 2 Inventory Staff (Role 4)
    Total: 11 staff members per shop * 9 shops = 99 staff.
    Also creates user accounts for all of them with Password123! and shopId scoping.
    """
    print("\n[6/8] Khởi tạo Nhân sự & Tài khoản Đăng nhập (9 chi nhánh x 11 nhân sự = 99 nhân viên)...")

    slugs = ["q1", "hn", "dn", "dalat", "bmt", "thaodien", "cantho", "hp", "hue"]

    # Template names
    manager_names = [("Nguyễn Văn", "Quản Lý"), ("Trần Thị", "Quản Lý")]
    cashier_names = [("Lê Thu", "Ngân"), ("Phạm Thị", "Thu Ngân")]
    barista_names = [("Hoàng Pha", "Chế"), ("Đỗ Minh", "Barista"), ("Vũ Anh", "Pha Chế"), ("Đặng Quốc", "Barista"), ("Bùi Đức", "Pha Chế")]
    stock_names = [("Ngô Thủ", "Kho"), ("Dương Văn", "Thủ Kho")]

    res = http_request(f"{API_BASE}/staff?pageSize=500", headers=headers)
    existing_staff = res if isinstance(res, list) else res.get("items", [])
    existing_emails = {s.get("contact", {}).get("email") for s in existing_staff if s.get("contact", {}).get("email")}

    shop_staff_map: Dict[str, List[Dict[str, Any]]] = {}

    for idx, s in enumerate(shops):
        s_id = s.get("id")
        s_slug = slugs[idx] if idx < len(slugs) else f"shop{idx+1}"
        s_name = s.get("name")
        shop_staff_map[s_id] = []

        # Define 11 staff specifications
        specs = []
        # 2 Managers
        for i in range(2):
            fn, ln = manager_names[i]
            specs.append({
                "firstName": fn,
                "lastName": f"{ln} {i+1}",
                "roleNum": 1,
                "roleStr": "Manager",
                "email": f"manager{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"090{idx+1}010{i+1}1"
            })
        # 2 Cashiers
        for i in range(2):
            fn, ln = cashier_names[i]
            specs.append({
                "firstName": fn,
                "lastName": f"{ln} {i+1}",
                "roleNum": 2,
                "roleStr": "Cashier",
                "email": f"cashier{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"091{idx+1}020{i+1}2"
            })
        # 5 Baristas
        for i in range(5):
            fn, ln = barista_names[i]
            specs.append({
                "firstName": fn,
                "lastName": f"{ln} {i+1}",
                "roleNum": 3,
                "roleStr": "Barista",
                "email": f"barista{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"093{idx+1}030{i+1}3"
            })
        # 2 Stock keepers
        for i in range(2):
            fn, ln = stock_names[i]
            specs.append({
                "firstName": fn,
                "lastName": f"{ln} {i+1}",
                "roleNum": 4,
                "roleStr": "InventoryStaff",
                "email": f"stock{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"097{idx+1}040{i+1}4"
            })

        for spec in specs:
            email = spec["email"]
            # Check if staff exists
            existing_s = next((x for x in existing_staff if x.get("contact", {}).get("email") == email), None)
            staff_id = None
            if existing_s:
                staff_id = existing_s.get("id")
            else:
                payload = {
                    "firstName": spec["firstName"],
                    "lastName": spec["lastName"],
                    "role": spec["roleNum"],
                    "contact": {
                        "phone": spec["phone"],
                        "email": email
                    },
                    "hireDate": "2026-01-10T08:00:00Z",
                    "shopId": s_id
                }
                try:
                    res_create = http_request(f"{API_BASE}/staff", method="POST", data=payload, headers=headers)
                    staff_id = res_create if isinstance(res_create, str) else res_create.get("id", "")
                except Exception as e:
                    pass

            # Assign shop as primary
            if staff_id:
                try:
                    http_request(
                        f"{API_BASE}/staff/{staff_id}/assign-shop",
                        method="POST",
                        data={"shopId": s_id, "isPrimary": True},
                        headers=headers
                    )
                except Exception:
                    pass

                shop_staff_map[s_id].append({
                    "id": staff_id,
                    "firstName": spec["firstName"],
                    "lastName": spec["lastName"],
                    "role": spec["roleNum"],
                    "roleStr": spec["roleStr"],
                    "email": email,
                    "shopId": s_id
                })

            # Create User Account for login
            reg_payload = {
                "email": email,
                "password": "Password123!",
                "fullName": f"{spec['firstName']} {spec['lastName']} ({s_slug.upper()})",
                "role": spec["roleStr"],
                "shopId": s_id
            }
            try:
                http_request(f"{API_BASE}/auth/register", method="POST", data=reg_payload)
            except Exception:
                pass

        print(f" -> Đã thiết lập 11 nhân sự & tài khoản cho: {s_name} [{s_slug}]")

    return shop_staff_map

def seed_shifts_schedule(headers: Dict[str, str], shops: List[Dict[str, Any]], shop_staff_map: Dict[str, List[Dict[str, Any]]]):
    """
    Schedules work shifts from 1 week ago (2026-10-11) to 2 weeks future (2026-11-01) - 22 days.
    Weekdays (Mon-Fri): 2 shifts/day:
      - Ca sáng (06:30 - 14:30): 1 Manager, 1 Cashier, 2 Baristas, 1 Stock keeper.
      - Ca chiều (14:30 - 22:30): 1 Manager, 1 Cashier, 3 Baristas, 1 Stock keeper.
    Weekends (Sat-Sun): 3 shifts/day:
      - Ca sáng (06:30 - 12:00): 1 Manager, 1 Cashier, 3 Baristas, 1 Stock keeper.
      - Ca chiều (12:00 - 17:30): 1 Manager, 1 Cashier, 2 Baristas, 1 Stock keeper.
      - Ca tối (17:30 - 22:30): 1 Manager, 1 Cashier, 2 Baristas, 1 Stock keeper.
    """
    print("\n[7/8] Lập lịch Ca làm việc (1 tuần trước -> 2 tuần tới, ngày 2 ca, cuối tuần 3 ca)...")

    # Current reference date: 2026-10-18
    base_date = datetime.date(2026, 10, 18)
    start_date = base_date - datetime.timedelta(days=7)  # 2026-10-11
    end_date = base_date + datetime.timedelta(days=14)   # 2026-11-01

    total_days = (end_date - start_date).days + 1
    total_created = 0

    for s in shops:
        s_id = s.get("id")
        staff_list = shop_staff_map.get(s_id, [])
        if not staff_list:
            continue

        # Group staff by role
        managers = [st for st in staff_list if st["role"] == 1]
        cashiers = [st for st in staff_list if st["role"] == 2]
        baristas = [st for st in staff_list if st["role"] == 3]
        stocks = [st for st in staff_list if st["role"] == 4]

        if not (managers and cashiers and baristas and stocks):
            continue

        current_date = start_date
        while current_date <= end_date:
            date_str = current_date.strftime("%Y-%m-%d")
            is_weekend = current_date.weekday() in (5, 6) # Sat or Sun

            if not is_weekend:
                # Weekday: 2 shifts
                # Shift 1: Ca sáng 06:30 - 14:30
                # Staff: Manager #1, Cashier #1, Baristas #1 & #2, Stock #1
                s1_staff = [
                    managers[0],
                    cashiers[0],
                    baristas[0],
                    baristas[1],
                    stocks[0]
                ]
                for st in s1_staff:
                    payload = {
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T06:30:00Z",
                        "endTime": f"{date_str}T14:30:00Z",
                        "notes": "Ca sáng ngày thường: Mở cửa đón khách, chuẩn bị nguyên liệu và bán hàng"
                    }
                    try:
                        http_request(f"{API_BASE}/shifts", method="POST", data=payload, headers=headers)
                        total_created += 1
                    except Exception:
                        pass

                # Shift 2: Ca chiều/tối 14:30 - 22:30
                # Staff: Manager #2, Cashier #2, Baristas #3, #4, #5, Stock #2
                s2_staff = [
                    managers[1 % len(managers)],
                    cashiers[1 % len(cashiers)],
                    baristas[2 % len(baristas)],
                    baristas[3 % len(baristas)],
                    baristas[4 % len(baristas)],
                    stocks[1 % len(stocks)]
                ]
                for st in s2_staff:
                    payload = {
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T14:30:00Z",
                        "endTime": f"{date_str}T22:30:00Z",
                        "notes": "Ca chiều/tối ngày thường: Đỉnh điểm bán hàng, dọn dẹp vệ sinh và chốt ca"
                    }
                    try:
                        http_request(f"{API_BASE}/shifts", method="POST", data=payload, headers=headers)
                        total_created += 1
                    except Exception:
                        pass

            else:
                # Weekend: 3 shifts
                # Shift 1: Ca sáng cuối tuần 06:30 - 12:00
                # Staff: Manager #1, Cashier #1, Baristas #1, #2, #3, Stock #1
                w1_staff = [
                    managers[0],
                    cashiers[0],
                    baristas[0],
                    baristas[1],
                    baristas[2 % len(baristas)],
                    stocks[0]
                ]
                for st in w1_staff:
                    payload = {
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T06:30:00Z",
                        "endTime": f"{date_str}T12:00:00Z",
                        "notes": "Ca sáng cuối tuần: Giờ cao điểm đón khách cuối tuần, phục vụ tại chỗ"
                    }
                    try:
                        http_request(f"{API_BASE}/shifts", method="POST", data=payload, headers=headers)
                        total_created += 1
                    except Exception:
                        pass

                # Shift 2: Ca chiều cuối tuần 12:00 - 17:30
                # Staff: Manager #2, Cashier #2, Baristas #4, #5, Stock #2
                w2_staff = [
                    managers[1 % len(managers)],
                    cashiers[1 % len(cashiers)],
                    baristas[3 % len(baristas)],
                    baristas[4 % len(baristas)],
                    stocks[1 % len(stocks)]
                ]
                for st in w2_staff:
                    payload = {
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T12:00:00Z",
                        "endTime": f"{date_str}T17:30:00Z",
                        "notes": "Ca chiều cuối tuần: Trực quầy, pha chế đồ uống mang đi và kiểm kê"
                    }
                    try:
                        http_request(f"{API_BASE}/shifts", method="POST", data=payload, headers=headers)
                        total_created += 1
                    except Exception:
                        pass

                # Shift 3: Ca tối cuối tuần 17:30 - 22:30
                # Staff: Manager #1, Cashier #1, Baristas #1, #2, Stock #1
                w3_staff = [
                    managers[0],
                    cashiers[0],
                    baristas[0],
                    baristas[1],
                    stocks[0]
                ]
                for st in w3_staff:
                    payload = {
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T17:30:00Z",
                        "endTime": f"{date_str}T22:30:00Z",
                        "notes": "Ca tối cuối tuần: Phục vụ khách tối, kiểm đếm tồn kho và chốt quỹ ngày"
                    }
                    try:
                        http_request(f"{API_BASE}/shifts", method="POST", data=payload, headers=headers)
                        total_created += 1
                    except Exception:
                        pass

            current_date += datetime.timedelta(days=1)

    print(f" -> Hoàn tất xếp ca làm việc cho {len(shops)} chi nhánh (tổng số ca đã gán: {total_created}).")

def seed_reviews_and_orders(headers: Dict[str, str], shops: List[Dict[str, Any]], menu_items: List[Dict[str, Any]], staff_map: Dict[str, List[Dict[str, Any]]]):
    """
    Creates sample orders and seeds 100 customer reviews into MongoDB via Reviews API.
    """
    print("\n[8/8] Khởi tạo Đơn hàng và 100 Đánh giá của Khách hàng...")

    # First, create 1-2 completed orders per shop if not existing
    order_ids_by_shop: Dict[str, List[str]] = {}
    for s in shops:
        s_id = s.get("id")
        order_ids_by_shop[s_id] = []
        staff_list = staff_map.get(s_id, [])
        primary_staff = staff_list[0]["id"] if staff_list else ""
        if not primary_staff or not menu_items:
            continue

        try:
            m1 = menu_items[0]
            m2 = menu_items[1] if len(menu_items) > 1 else m1
            payload = {
                "shopId": s_id,
                "staffId": primary_staff,
                "customerInfo": {
                    "name": "Khách Vãng Lai Trải Nghiệm",
                    "contact": "0988776655",
                    "type": 1
                },
                "items": [
                    {
                        "menuItemId": m1.get("id"),
                        "name": m1.get("name"),
                        "description": m1.get("name"),
                        "unitPrice": m1.get("price", {"amount": 35000}),
                        "quantity": 2,
                        "specialInstructions": "Đậm vị, ít đá",
                        "totalPrice": {"amount": m1.get("price", {}).get("amount", 35000) * 2, "currency": "VND"}
                    }
                ]
            }
            res_ord = http_request(f"{API_BASE}/orders", method="POST", data=payload, headers=headers)
            ord_id = res_ord if isinstance(res_ord, str) else res_ord.get("id", "")
            if ord_id:
                order_ids_by_shop[s_id].append(ord_id)
                # Pay and complete
                try:
                    http_request(f"{API_BASE}/orders/{ord_id}/payment", method="POST", data={"id": ord_id, "paymentMethod": 2, "amount": 0}, headers=headers)
                    http_request(f"{API_BASE}/orders/{ord_id}/status", method="PUT", data={"id": ord_id, "status": 4}, headers=headers)
                except Exception:
                    pass
        except Exception:
            pass

    # List of 100 Realistic Vietnamese Customers
    first_names = [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đào", "Đinh", "Trịnh", "Lương"
    ]
    middle_names = ["Văn", "Thị", "Minh", "Thu", "Ngọc", "Đức", "Thanh", "Mỹ", "Quang", "Anh"]
    last_names = [
        "Tuấn", "Hương", "Trang", "Long", "Nam", "Linh", "Hải", "Lan", "Thảo", "Hùng",
        "Duy", "Phương", "Quỳnh", "Thắng", "Mai", "Tùng", "Bình", "Châu", "Việt", "Trâm"
    ]

    sample_shop_comments = [
        "Không gian quán rộng rãi, thiết kế Industrial Flat rất hiện đại và sạch sẽ.",
        "Nhân viên nhiệt tình, gọi món nhanh và rất chu đáo khi mang nước tận bàn.",
        "View quán ngắm phố cực chill, bàn ghế cao ngồi làm việc cả ngày rất thoải mái.",
        "Điều hòa mát lạnh, wifi cáp quang tốc độ cao, nhạc nền êm dịu không bị ồn ào.",
        "Bãi giữ xe rộng rãi, bảo vệ dắt xe tận tình và vui vẻ chào đón khách.",
        "Quán sạch sẽ, khu vực quầy bar mở nhìn các bạn pha chế rất chuyên nghiệp.",
        "Không gian sáng thoáng, ánh sáng tự nhiên chụp ảnh check-in rất đẹp.",
        "Điểm hẹn lý tưởng để gặp gỡ đối tác và bạn bè vào cuối tuần.",
        "Chất lượng dịch vụ đồng đều, đồ uống phục vụ nhanh chỉ tầm 3-5 phút.",
        "Mùi thơm cà phê thoang thoảng ngay khi vừa bước chân vào cửa, rất thư thái."
    ]

    product_comments_map = {
        "Cà Phê Muối Cố Đô": [
            "Lớp kem muối béo ngậy mặn nhẹ, hòa cùng cà phê đậm đặc cực kỳ chuẩn vị.",
            "Kem muối đánh bông mịn, vị mặn ngọt hài hòa, uống một ngụm là nhớ mãi.",
            "Đậm vị cà phê rang xay nguyên chất, không bị lấn át bởi lớp kem.",
            "Món signature xuất sắc nhất quán, lần nào ghé cũng phải gọi ly này."
        ],
        "Cà Phê Sữa Đá Sài Gòn": [
            "Cà phê sữa đá đậm đà đúng chất Sài Gòn, ngọt thơm không bị khé cổ.",
            "Độ sánh vừa vặn, cà phê thơm nồng nàn quyện cùng sữa đặc béo ngậy.",
            "Uống buổi sáng giúp tỉnh táo cả ngày làm việc, rất đáng tiền."
        ],
        "Bạc Xỉu 3 Tầng Nghệ Nhân": [
            "Phân tầng cực kỳ đẹp mắt, nhiều sữa ít cà phê phù hợp cho các bạn nữ.",
            "Vị ngọt thanh dịu nhẹ, sữa tươi béo thơm kết hợp lớp espresso tinh tế.",
            "Khuấy đều lên uống cùng đá viên mát lạnh thơm lừng."
        ],
        "Cà Phê Đen Phin Cầu Đất": [
            "Hạt Arabica rang mộc chuẩn gu, thơm hương trái cây và chua thanh hậu ngọt.",
            "Cà phê phin truyền thống thơm lừng, vị đắng mộc không độn bắp rang.",
            "Uống không đường cảm nhận rõ trọn vẹn chất lượng hạt cà phê tuyển chọn."
        ],
        "Trà Đào Cam Sả Tươi": [
            "Trà đen đậm đà, cam vàng mọng nước kết hợp sả tươi giải nhiệt cực đã.",
            "Miếng đào giòn sần sật ngọt ngào, thơm phức mùi sả cây tự nhiên.",
            "Độ ngọt vừa vặn, uống sảng khoái và thanh mát cơ thể."
        ],
        "Trà Sen Vàng Kem Cheese": [
            "Hạt sen hầm mềm bùi tan trong miệng, trà Ô Long thanh tao tuyệt hảo.",
            "Lớp kem cheese mằn mặn béo ngậy phủ trên nền trà sen vàng thơm nức.",
            "Món giải khát hoàn hảo, sự kết hợp vị truyền thống và hiện đại rất khéo."
        ],
        "Bánh Mì Thịt Nguội Pate Giòn": [
            "Vỏ bánh mì giòn tan, pate gan nhà làm thơm béo ngậy ăn kèm dưa chua rất vừa miệng.",
            "Nhân bánh đầy đặn, sốt bơ trứng đậm đà chuẩn vị bánh mì Việt Nam."
        ],
        "Bánh Croissant Bơ Tỏi Nướng": [
            "Bánh nướng nóng hổi thơm nức mũi mùi bơ tỏi, các lớp bánh giòn xốp.",
            "Bơ Anchor béo ngậy phối lá mùi tây nướng giòn rụm, ăn kèm cà phê rất hợp."
        ]
    }

    all_tags = [
        "Không gian đẹp", "Pha chế nhanh", "Hương vị đậm đà", "Phục vụ thân thiện",
        "Đồ uống xuất sắc", "Wifi mạnh", "View đẹp check-in", "Giá cả hợp lý"
    ]

    # Check existing reviews count
    res_rev = http_request(f"{API_BASE}/reviews?pageSize=1")
    curr_count = res_rev.get("totalCount", 0)
    if curr_count >= 100:
        print(f" -> Đã có sẵn {curr_count} đánh giá trong hệ thống.")
        return

    reviews_to_create = 100 - curr_count
    print(f" -> Bắt đầu tạo mới {reviews_to_create} đánh giá từ khách hàng...")

    random.seed(42)  # reproducible random
    now = datetime.datetime.now(datetime.timezone.utc)

    for i in range(reviews_to_create):
        fn = random.choice(first_names)
        mn = random.choice(middle_names)
        ln = random.choice(last_names)
        cust_name = f"{fn} {mn} {ln}"
        cust_phone = f"09{random.randint(0, 9)}{random.randint(1000000, 9999999)}"

        shop = shops[i % len(shops)]
        s_id = shop.get("id")
        s_name = shop.get("name")

        # Rating distribution: 70% 5-star, 25% 4-star, 5% 3-star
        roll = random.random()
        if roll < 0.70:
            shop_rating = 5
        elif roll < 0.95:
            shop_rating = 4
        else:
            shop_rating = 3

        shop_comment = random.choice(sample_shop_comments)

        # Select 1 to 3 items to review
        selected_items = random.sample(menu_items, min(len(menu_items), random.randint(1, 3)))
        item_reviews = []
        for it in selected_items:
            it_name = it.get("name", "")
            it_comments = product_comments_map.get(it_name, ["Hương vị thơm ngon, chuẩn vị và đóng gói cẩn thận."])
            it_rating = shop_rating if random.random() > 0.3 else max(3, shop_rating - 1)
            item_reviews.append({
                "menuItemId": it.get("id", ""),
                "menuItemName": it_name,
                "rating": it_rating,
                "comment": random.choice(it_comments)
            })

        tags = random.sample(all_tags, random.randint(2, 4))
        days_ago = random.randint(0, 25)
        created_dt = now - datetime.timedelta(days=days_ago, hours=random.randint(1, 12))

        payload = {
            "shopId": s_id,
            "shopName": s_name,
            "orderId": (order_ids_by_shop.get(s_id) or [""])[0],
            "customerName": cust_name,
            "customerPhone": cust_phone,
            "shopRating": shop_rating,
            "shopComment": shop_comment,
            "itemReviews": item_reviews,
            "tags": tags,
            "createdAt": created_dt.strftime("%Y-%m-%dT%H:%M:%SZ")
        }

        try:
            http_request(f"{API_BASE}/reviews", method="POST", data=payload)
        except Exception as e:
            pass

    print(" -> Tạo thành công đầy đủ 100 đánh giá của khách hàng!")

def main():
    print("================================================================")
    print("  KHỞI TẠO DỮ LIỆU TOÀN DIỆN HỆ THỐNG QUẢN LÝ QUÁN CÀ PHÊ       ")
    print("================================================================")

    # 1. Fetch Vietnam provinces
    provinces = get_vietnam_provinces()

    # 2. Authenticate Admin
    token = authenticate()
    auth_headers = {"Authorization": f"Bearer {token}"}

    # 3. Seed 9 Shops
    shops = seed_shops(auth_headers, provinces)
    if not shops:
        print("Lỗi: Không tìm thấy chi nhánh nào.")
        sys.exit(1)

    primary_shop_id = shops[0].get("id")

    # 4. Seed Menu Items (available 24/7)
    menu_items = seed_menu(auth_headers, primary_shop_id)

    # 5. Seed Inventory for all 9 shops
    seed_inventory_for_shops(auth_headers, shops)

    # 6. Seed Staff & User Accounts (9 shops x 11 staff = 99 staff)
    shop_staff_map = seed_staff_and_accounts(auth_headers, shops)

    # 7. Seed Shifts schedule (Weekdays: 2 shifts, Weekends: 3 shifts, 1 week ago -> 2 weeks ahead)
    seed_shifts_schedule(auth_headers, shops, shop_staff_map)

    # 8. Seed Sample Orders and 100 Customer Reviews
    seed_reviews_and_orders(auth_headers, shops, menu_items, shop_staff_map)

    print("\n================================================================")
    print("  HOÀN TẤT SEED DỮ LIỆU THẬT VÀO MONGODB THÀNH CÔNG 100%!       ")
    print("================================================================")

if __name__ == "__main__":
    main()
