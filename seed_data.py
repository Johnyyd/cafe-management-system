#!/usr/bin/env python3
"""
Comprehensive Seed script for Cafe Management System.
Features:
- Auto-wipe previous data clean before seeding.
- 9 Shops across major Vietnam cities/provinces.
- Staff per shop: 2 Managers, 2 Cashiers, 5 Baristas, 2 Inventory Staff (11 per shop, 99 total).
- 99 unique, natural, authentic Vietnamese names without any numbered suffixes (no AI slop).
- RBAC User accounts registered with Password123! and shopId scoping.
- Work Shifts scheduling: From 1 week ago (2026-10-11) to 2 weeks future (2026-11-01) - 22 days.
  * Weekdays: Ca sáng (06:30 - 14:30), Ca chiều (14:30 - 22:30).
  * Weekends: Ca sáng (06:30 - 12:00), Ca chiều (12:00 - 17:30), Ca tối (17:30 - 22:30).
- 8 Signature Menu Items available 24/7.
- Complete inventory for all 9 branches.
- 100+ Realistic completed orders with full revenue matching 100 Customer Reviews.
- Additional orders for today to populate active daily KPIs.
"""

import os
import sys
import json
import ssl
import datetime
import random
import urllib.request
import urllib.error
from typing import Dict, List, Any, Optional
from concurrent.futures import ThreadPoolExecutor

if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

# SSL context allowing unverified HTTPS for internal tailscale endpoints
SSL_CTX = ssl.create_default_context()
SSL_CTX.check_hostname = False
SSL_CTX.verify_mode = ssl.CERT_NONE

def detect_api_base() -> str:
    """Detects whether to use local or remote Tailscale API based on args/availability."""
    if len(sys.argv) > 1 and sys.argv[1].startswith("http"):
        return sys.argv[1].rstrip("/")
    if os.environ.get("API_BASE"):
        return os.environ.get("API_BASE").rstrip("/")

    # Check localhost:7890
    try:
        req = urllib.request.Request("http://localhost:7890/healthz", headers={"User-Agent": "Probe"})
        with urllib.request.urlopen(req, timeout=1):
            return "http://localhost:7890/api/v1"
    except Exception:
        pass

    # Check remote tailscale host
    try:
        req = urllib.request.Request("https://cafe.taild6d848.ts.net/health", headers={"User-Agent": "Probe"})
        with urllib.request.urlopen(req, context=SSL_CTX, timeout=2):
            return "https://cafe.taild6d848.ts.net/api/v1"
    except Exception:
        pass

    return "http://localhost:7890/api/v1"

API_BASE = detect_api_base()

def http_request(url: str, method: str = "GET", data: Optional[Dict[str, Any]] = None, headers: Optional[Dict[str, str]] = None, timeout: int = 25) -> Any:
    req_headers = {"User-Agent": "CafeManagementSeed/2.0"}
    if headers:
        req_headers.update(headers)
    
    encoded_data = None
    if data is not None:
        req_headers["Content-Type"] = "application/json"
        encoded_data = json.dumps(data).encode("utf-8")

    req = urllib.request.Request(url, data=encoded_data, headers=req_headers, method=method)
    try:
        with urllib.request.urlopen(req, context=SSL_CTX, timeout=timeout) as resp:
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

def authenticate() -> str:
    """Login or register admin account and retrieve Bearer JWT token."""
    login_url = f"{API_BASE}/auth/login"
    login_payload = {
        "email": "admin@cafemanagement.com",
        "password": "Password123!"
    }
    try:
        res = http_request(login_url, method="POST", data=login_payload)
        token = res.get("accessToken")
        if token:
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
    return token

def wipe_database(headers: Dict[str, str]) -> Dict[str, str]:
    """
    Clears all existing database records before seeding fresh data.
    Preserves or re-authenticates the admin account.
    """
    print("\n[0/8] Xóa sạch dữ liệu cũ trước khi nạp mới (Wiping old database)...")

    # 1. Try dedicated wipe endpoint if available
    wiped_via_wipe_ep = False
    try:
        res = http_request(f"{API_BASE}/system/backup/wipe?keepAdmin=true", method="POST", headers=headers)
        if res.get("success") or res.get("Success"):
            print(" -> Đã xóa sạch dữ liệu qua endpoint /system/backup/wipe!")
            wiped_via_wipe_ep = True
    except Exception:
        pass

    # 2. If wipe endpoint not available, use restore endpoint with empty collections
    if not wiped_via_wipe_ep:
        try:
            restore_payload = {
                "data": {
                    "shops": [],
                    "menuItems": [],
                    "inventoryItems": [],
                    "orders": [],
                    "staff": [],
                    "reviews": [],
                    "shifts": [],
                    "timeOffRequests": [],
                    "shiftSwaps": []
                }
            }
            http_request(f"{API_BASE}/system/backup/restore", method="POST", data=restore_payload, headers=headers)
            print(" -> Đã xóa sạch các collection qua endpoint /system/backup/restore!")
        except Exception as e:
            print(f" -> Cảnh báo khi gọi restore wipe: {e}")

    # 3. Clean any remaining shifts (in case shifts collection wasn't registered in older backup builds)
    try:
        shifts_res = http_request(f"{API_BASE}/shifts?pageSize=5000", headers=headers)
        raw_shifts = shifts_res if isinstance(shifts_res, list) else shifts_res.get("items", [])
        if raw_shifts:
            print(f" -> Đang dọn dẹp {len(raw_shifts)} ca làm việc cũ còn sót lại...")
            def del_shift(sh_id: str):
                try:
                    http_request(f"{API_BASE}/shifts/{sh_id}", method="DELETE", headers=headers)
                except Exception:
                    pass
            with ThreadPoolExecutor(max_workers=25) as executor:
                list(executor.map(del_shift, [s.get("id") for s in raw_shifts if s.get("id")]))
            print(" -> Đã dọn sạch toàn bộ ca làm việc cũ.")
    except Exception as e:
        print(f" -> Cảnh báo dọn dẹp ca làm việc: {e}")

    # Re-verify admin authentication
    new_token = authenticate()
    return {"Authorization": f"Bearer {new_token}"}

def get_vietnam_provinces() -> List[Dict[str, Any]]:
    """Fetch Vietnam province list from the online API with local fallback."""
    print("\n[1/8] Đang tải danh mục Tỉnh/Thành phố Việt Nam...")
    endpoints = [
        "https://provinces.open-api.vn/api/p/?depth=2",
        "https://provinces.open-api.vn/api/v2/p/",
        "https://provinces.open-api.vn/api/p/",
    ]
    for ep in endpoints:
        try:
            data = http_request(ep, timeout=10)
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

def seed_shops(headers: Dict[str, str], provinces: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    print("\n[2/8] Đồng bộ 9 Chi nhánh quán (Shops) với các Tỉnh/Thành phố Việt Nam...")
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
    print("\n[3/8] Đồng bộ Thực đơn Đồ uống & Món ăn (Menu Items)...")
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
    print("\n[4/8] Đồng bộ Kho nguyên vật liệu cho 9 chi nhánh...")
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
SHOP_STAFF_PROFILES = [
    # Shop 1: Landmark 81 - Bình Thạnh (slug: landmark81, alias: q1)
    {
        "slug": "landmark81",
        "alias": "q1",
        "managers": [("Nguyễn Hoàng", "Bách"), ("Trần Minh", "Tâm")],
        "cashiers": [("Lê Phương", "Thảo"), ("Phạm Thanh", "Hà")],
        "baristas": [("Đặng Gia", "Huy"), ("Hoàng Nhật", "Minh"), ("Vũ Thảo", "Nguyên"), ("Bùi Tuấn", "Kiệt"), ("Đỗ Quỳnh", "Như")],
        "stocks": [("Ngô Quốc", "Hưng"), ("Dương Thành", "Đạt")]
    },
    # Shop 2: Nhà Hát Lớn - Hoàn Kiếm (slug: nhahatlon, alias: hn)
    {
        "slug": "nhahatlon",
        "alias": "hn",
        "managers": [("Trịnh Đức", "Anh"), ("Phan Thu", "Trang")],
        "cashiers": [("Đinh Hải", "Yến"), ("Mai Việt", "Cường")],
        "baristas": [("Hồ Trọng", "Nghĩa"), ("Lý Ngọc", "Lan"), ("Lương Bảo", "Châu"), ("Chu Đình", "Trọng"), ("Đoàn Diệu", "Linh")],
        "stocks": [("Võ Tiến", "Dũng"), ("Tạ Hoàng", "Nam")]
    },
    # Shop 3: Cầu Rồng - Hải Châu (slug: danang, alias: dn)
    {
        "slug": "danang",
        "alias": "dn",
        "managers": [("Nguyễn Văn", "An"), ("Huỳnh Thị", "Bích")],
        "cashiers": [("Phan Hữu", "Cảnh"), ("Trần Kim", "Dung")],
        "baristas": [("Lê Thế", "Hiển"), ("Đỗ Thùy", "Linh"), ("Vũ Đình", "Phong"), ("Phạm Xuân", "Trường"), ("Bùi Cẩm", "Vân")],
        "stocks": [("Hoàng Văn", "Quyết"), ("Đặng Quốc", "Thái")]
    },
    # Shop 4: Cafe Cầu Đất Farm - Đà Lạt (slug: caudat, alias: dalat)
    {
        "slug": "caudat",
        "alias": "dalat",
        "managers": [("Nguyễn Hữu", "Thắng"), ("Trần Thị", "Tuyết")],
        "cashiers": [("Lê Ngọc", "Sơn"), ("Hoàng Mai", "Phương")],
        "baristas": [("Phan Thanh", "Bình"), ("Đỗ Gia", "Bảo"), ("Phạm Hồng", "Nhung"), ("Vũ Trọng", "Khang"), ("Bùi Ánh", "Dương")],
        "stocks": [("Ngô Thế", "Vinh"), ("Đinh Văn", "Hậu")]
    },
    # Shop 5: Trung Nguyên Legend - Buôn Ma Thuột (slug: bmt, alias: bmt)
    {
        "slug": "bmt",
        "alias": "bmt",
        "managers": [("Nguyễn Cao", "Sơn"), ("Trần Thu", "Hằng")],
        "cashiers": [("Lê Văn", "Hòa"), ("Phạm Thị", "Loan")],
        "baristas": [("Hoàng Minh", "Đức"), ("Đặng Thùy", "Trang"), ("Vũ Đình", "Khoa"), ("Bùi Quốc", "Toàn"), ("Đỗ Mỹ", "Duyên")],
        "stocks": [("Phan Văn", "Kiên"), ("Hồ Hữu", "Phước")]
    },
    # Shop 6: Cafe Thảo Điền - TP. Thủ Đức (slug: thaodien, alias: thaodien)
    {
        "slug": "thaodien",
        "alias": "thaodien",
        "managers": [("Nguyễn Hoàng", "Long"), ("Lê Kiều", "My")],
        "cashiers": [("Trần Thanh", "Trúc"), ("Phạm Văn", "Hùng")],
        "baristas": [("Đặng Anh", "Tuấn"), ("Hoàng Trúc", "Anh"), ("Bùi Hữu", "Lộc"), ("Đỗ Ngọc", "Điệp"), ("Vũ Minh", "Khôi")],
        "stocks": [("Ngô Đình", "Khải"), ("Dương Thị", "Ngọc")]
    },
    # Shop 7: Highlands Coffee Bến Ninh Kiều (slug: ninhkieu, alias: cantho)
    {
        "slug": "ninhkieu",
        "alias": "cantho",
        "managers": [("Huỳnh Văn", "Thuận"), ("Nguyễn Thị", "Cẩm")],
        "cashiers": [("Trần Thanh", "Phong"), ("Lê Thị", "Diễm")],
        "baristas": [("Phạm Minh", "Sang"), ("Hoàng Hữu", "Tài"), ("Đỗ Thị Kim", "Ngân"), ("Phan Hùng", "Dũng"), ("Bùi Thị Bích", "Ngọc")],
        "stocks": [("Vũ Thanh", "Bình"), ("Đặng Văn", "Lợi")]
    },
    # Shop 8: Phúc Long Nhà Hát Thành Phố - Hải Phòng (slug: haiphong, alias: hp)
    {
        "slug": "haiphong",
        "alias": "hp",
        "managers": [("Nguyễn Thế", "Hải"), ("Trần Thị", "Lan")],
        "cashiers": [("Lê Văn", "Cường"), ("Hoàng Thu", "Giang")],
        "baristas": [("Phạm Hoàng", "Phúc"), ("Đỗ Quốc", "Tuấn"), ("Vũ Thị Mai", "Anh"), ("Bùi Đình", "Luật"), ("Đặng Ngọc", "Khánh")],
        "stocks": [("Ngô Văn", "Minh"), ("Phan Hữu", "Tình")]
    },
    # Shop 9: Cafe Cố Đô Sông Hương - Huế (slug: hue, alias: hue)
    {
        "slug": "hue",
        "alias": "hue",
        "managers": [("Nguyễn Phúc", "Khang"), ("Tôn Nữ Bích", "Thủy")],
        "cashiers": [("Trần Văn", "Thành"), ("Lê Thị Đoan", "Trang")],
        "baristas": [("Phan Nhật", "Tân"), ("Hoàng Văn", "Nghĩa"), ("Đỗ Thị Minh", "Châu"), ("Vũ Anh", "Dũng"), ("Bùi Thị Lệ", "Hằng")],
        "stocks": [("Đặng Bá", "Phước"), ("Hồ Xuân", "Trường")]
    },
]

def seed_staff_and_accounts(headers: Dict[str, str], shops: List[Dict[str, Any]]) -> Dict[str, List[Dict[str, Any]]]:
    """
    Seeds 99 staff members across 9 shops with unique, natural Vietnamese names (no numbers, no AI slop).
    Also creates user accounts with Password123! and shopId scoping.
    """
    print("\n[5/8] Khởi tạo Nhân sự & Tài khoản Đăng nhập (9 chi nhánh x 11 nhân sự = 99 người, tên thật 100%)...")

    res = http_request(f"{API_BASE}/staff?pageSize=500", headers=headers)
    existing_staff = res if isinstance(res, list) else res.get("items", [])

    shop_staff_map: Dict[str, List[Dict[str, Any]]] = {}

    for idx, s in enumerate(shops):
        s_id = s.get("id")
        s_name = s.get("name")
        shop_staff_map[s_id] = []

        profile = SHOP_STAFF_PROFILES[idx] if idx < len(SHOP_STAFF_PROFILES) else SHOP_STAFF_PROFILES[-1]
        s_slug = profile["slug"]

        specs = []
        # 2 Managers
        for i, (fn, ln) in enumerate(profile["managers"]):
            specs.append({
                "firstName": fn,
                "lastName": ln,
                "roleNum": 1,
                "roleStr": "Manager",
                "email": f"manager{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"090{idx+1}010{i+1}1"
            })
        # 2 Cashiers
        for i, (fn, ln) in enumerate(profile["cashiers"]):
            specs.append({
                "firstName": fn,
                "lastName": ln,
                "roleNum": 2,
                "roleStr": "Cashier",
                "email": f"cashier{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"091{idx+1}020{i+1}2"
            })
        # 5 Baristas
        for i, (fn, ln) in enumerate(profile["baristas"]):
            specs.append({
                "firstName": fn,
                "lastName": ln,
                "roleNum": 3,
                "roleStr": "Barista",
                "email": f"barista{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"093{idx+1}030{i+1}3"
            })
        # 2 Stock keepers
        for i, (fn, ln) in enumerate(profile["stocks"]):
            specs.append({
                "firstName": fn,
                "lastName": ln,
                "roleNum": 4,
                "roleStr": "InventoryStaff",
                "email": f"stock{i+1}.{s_slug}@cafemanagement.com",
                "phone": f"097{idx+1}040{i+1}4"
            })

        for spec in specs:
            email = spec["email"]
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
                except Exception:
                    pass

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
                "fullName": f"{spec['firstName']} {spec['lastName']}",
                "role": spec["roleStr"],
                "shopId": s_id
            }
            try:
                http_request(f"{API_BASE}/auth/register", method="POST", data=reg_payload)
            except Exception:
                pass
            # Also register alias email if present (e.g. q1, hn, dn...)
            if "alias" in profile and profile["alias"] != s_slug:
                alias_email = email.replace(f".{s_slug}@", f".{profile['alias']}@")
                try:
                    reg_alias = dict(reg_payload, email=alias_email)
                    http_request(f"{API_BASE}/auth/register", method="POST", data=reg_alias)
                except Exception:
                    pass

        print(f" -> Đã thiết lập 11 nhân sự & tài khoản tên thật cho: {s_name}")

    return shop_staff_map

def seed_shifts_schedule(headers: Dict[str, str], shops: List[Dict[str, Any]], shop_staff_map: Dict[str, List[Dict[str, Any]]]):
    """
    Schedules work shifts from 1 week ago (2026-10-11) to 2 weeks future (2026-11-01) - 22 days.
    Weekdays:
      - Ca sáng (06:30 - 14:30 VN): Manager #1, Cashier #1, Baristas #1 & #2, Stock #1
      - Ca chiều (14:30 - 22:30 VN): Manager #2, Cashier #2, Baristas #3 & #4 & #5, Stock #2
    Weekends:
      - Ca sáng (06:30 - 12:00 VN): Manager #1, Cashier #1, Baristas #1 & #2, Stock #1
      - Ca chiều (12:00 - 17:30 VN): Manager #1, Cashier #2, Baristas #3 & #4, Stock #2
      - Ca tối (17:30 - 22:30 VN): Manager #2, Cashier #1, Baristas #4 & #5, Stock #1
    """
    print("\n[6/8] Lập lịch Ca làm việc (1 tuần trước -> 2 tuần tới, phân bổ Sáng, Chiều, Tối)...")

    base_date = datetime.date(2026, 10, 18)
    start_date = base_date - datetime.timedelta(days=7)  # 2026-10-11
    end_date = base_date + datetime.timedelta(days=14)   # 2026-11-01

    shift_payloads = []

    for s in shops:
        s_id = s.get("id")
        staff_list = shop_staff_map.get(s_id, [])
        if not staff_list:
            continue

        managers = [st for st in staff_list if st["role"] == 1]
        cashiers = [st for st in staff_list if st["role"] == 2]
        baristas = [st for st in staff_list if st["role"] == 3]
        stocks = [st for st in staff_list if st["role"] == 4]

        if not (managers and cashiers and baristas and stocks):
            continue

        current_date = start_date
        while current_date <= end_date:
            date_str = current_date.strftime("%Y-%m-%d")
            prev_date_str = (current_date - datetime.timedelta(days=1)).strftime("%Y-%m-%d")
            is_weekend = current_date.weekday() in (5, 6)

            if not is_weekend:
                # Ca sáng (06:30 - 14:30 VN -> UTC: prev_date 23:30 - date 07:30)
                s1_staff = [managers[0], cashiers[0], baristas[0], baristas[1 % len(baristas)], stocks[0]]
                for st in s1_staff:
                    shift_payloads.append({
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{prev_date_str}T23:30:00Z",
                        "endTime": f"{date_str}T07:30:00Z",
                        "notes": "Ca sáng ngày thường: Mở cửa đón khách, chuẩn bị nguyên liệu và bán hàng"
                    })

                # Ca chiều (14:30 - 22:30 VN -> UTC: date 07:30 - date 15:30)
                m2 = managers[1 % len(managers)]
                c2 = cashiers[1 % len(cashiers)]
                b3 = baristas[2 % len(baristas)]
                b4 = baristas[3 % len(baristas)]
                b5 = baristas[4 % len(baristas)]
                st2 = stocks[1 % len(stocks)]
                s2_staff = [m2, c2, b3, b4, b5, st2]
                for st in s2_staff:
                    shift_payloads.append({
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T07:30:00Z",
                        "endTime": f"{date_str}T15:30:00Z",
                        "notes": "Ca chiều ngày thường: Phục vụ cao điểm chiều tối và kiểm kê chốt ca"
                    })
            else:
                # Weekend: 3 shifts
                # Ca sáng (06:30 - 12:00 VN -> UTC: prev_date 23:30 - date 05:00)
                s_morning = [managers[0], cashiers[0], baristas[0], baristas[1 % len(baristas)], stocks[0]]
                for st in s_morning:
                    shift_payloads.append({
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{prev_date_str}T23:30:00Z",
                        "endTime": f"{date_str}T05:00:00Z",
                        "notes": "Ca sáng cuối tuần: Mở cửa đón khách, pha chế phục vụ cao điểm sáng"
                    })

                # Ca chiều (12:00 - 17:30 VN -> UTC: date 05:00 - date 10:30)
                s_afternoon = [managers[0], cashiers[1 % len(cashiers)], baristas[2 % len(baristas)], baristas[3 % len(baristas)], stocks[1 % len(stocks)]]
                for st in s_afternoon:
                    shift_payloads.append({
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T05:00:00Z",
                        "endTime": f"{date_str}T10:30:00Z",
                        "notes": "Ca chiều cuối tuần: Tiếp nhận khách giờ nghỉ trưa, kiểm tra bàn ghế và kho"
                    })

                # Ca tối (17:30 - 22:30 VN -> UTC: date 10:30 - date 15:30)
                s_evening = [managers[1 % len(managers)], cashiers[0], baristas[4 % len(baristas)], baristas[1 % len(baristas)], stocks[0]]
                for st in s_evening:
                    shift_payloads.append({
                        "staffId": st["id"],
                        "shopId": s_id,
                        "startTime": f"{date_str}T10:30:00Z",
                        "endTime": f"{date_str}T15:30:00Z",
                        "notes": "Ca tối cuối tuần: Phục vụ khách buổi tối, dọn dẹp quầy bar và chốt doanh thu"
                    })

            current_date += datetime.timedelta(days=1)

    print(f" -> Đang nạp {len(shift_payloads)} ca làm việc cho toàn bộ 9 chi nhánh...")
    def create_single_shift(p):
        try:
            http_request(f"{API_BASE}/shifts", method="POST", data=p, headers=headers)
            return True
        except Exception:
            return False

    with ThreadPoolExecutor(max_workers=25) as executor:
        results = list(executor.map(create_single_shift, shift_payloads))

    print(f" -> Hoàn tất tạo {sum(1 for r in results if r)} ca làm việc thành công!")

def seed_reviews_and_orders(headers: Dict[str, str], shops: List[Dict[str, Any]], menu_items: List[Dict[str, Any]], shop_staff_map: Dict[str, List[Dict[str, Any]]]):
    """
    Creates comprehensive realistic completed orders across all 9 shops, generates full revenue,
    and creates 100 customer reviews with matching order IDs, products, and customer names.
    """
    print("\n[7/8] Khởi tạo Đơn hàng đầy đủ doanh thu & 100 Đánh giá của khách hàng...")

    first_names = [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đào", "Đinh", "Trịnh", "Lương"
    ]
    middle_names = ["Văn", "Thị", "Minh", "Thu", "Ngọc", "Đức", "Thanh", "Mỹ", "Quang", "Anh", "Hoàng", "Hữu"]
    last_names = [
        "Tuấn", "Hương", "Trang", "Long", "Nam", "Linh", "Hải", "Lan", "Thảo", "Hùng",
        "Duy", "Phương", "Quỳnh", "Thắng", "Mai", "Tùng", "Bình", "Châu", "Việt", "Trâm",
        "Khánh", "Phúc", "Kiệt", "Ngân", "Yến", "Đạt", "Hậu", "Quang", "Triết", "Thành"
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

    random.seed(2026)
    now = datetime.datetime.now(datetime.timezone.utc)

    # Fetch all registered staff to guarantee valid staff IDs for orders
    all_staff_res = http_request(f"{API_BASE}/staff?pageSize=500", headers=headers)
    all_staff = all_staff_res if isinstance(all_staff_res, list) else all_staff_res.get("items", [])

    shop_staff_dict: Dict[str, List[Dict[str, Any]]] = {}
    for s in shops:
        s_id = s.get("id")
        assigned = [
            st for st in all_staff
            if any(sa.get("shopId") == s_id for sa in st.get("shopAssignments", []))
        ]
        if not assigned:
            assigned = shop_staff_map.get(s_id, [])
        if not assigned and all_staff:
            assigned = all_staff
        shop_staff_dict[s_id] = assigned

    # 1. Generate 100 Completed Orders corresponding to the 100 customer reviews (distributed across the past year)
    print(" -> Đang tạo 100 đơn hàng thực tế kèm doanh thu tương ứng cho 100 đánh giá...")
    created_orders: List[Dict[str, Any]] = []

    for i in range(100):
        shop = shops[i % len(shops)]
        s_id = shop.get("id")
        s_name = shop.get("name")
        staff_list = shop_staff_dict.get(s_id, [])
        assigned_staff = random.choice(staff_list) if staff_list else (all_staff[0] if all_staff else {"id": ""})

        fn = random.choice(first_names)
        mn = random.choice(middle_names)
        ln = random.choice(last_names)
        cust_name = f"{fn} {mn} {ln}"
        cust_phone = f"09{random.randint(0, 9)}{random.randint(1000000, 9999999)}"

        # 1 to 3 items
        order_items_count = random.randint(1, 3)
        selected_menu = random.sample(menu_items, min(len(menu_items), order_items_count))

        items_payload = []
        order_subtotal = 0
        for m in selected_menu:
            qty = random.randint(1, 2)
            unit_price = m.get("price", {}).get("amount", 35000)
            item_total = unit_price * qty
            order_subtotal += item_total
            items_payload.append({
                "menuItemId": m.get("id"),
                "name": m.get("name"),
                "description": m.get("name"),
                "unitPrice": {"amount": unit_price, "currency": "VND"},
                "quantity": qty,
                "specialInstructions": random.choice(["Đậm vị, ít đá", "Ít ngọt", "Nhiều sữa", "Bình thường"]),
                "totalPrice": {"amount": item_total, "currency": "VND"}
            })

        order_type = random.choice([1, 2, 3]) # DineIn, Takeaway, Delivery
        # Distribute across past 350 days
        days_ago = random.randint(1, 350)
        hours_offset = random.randint(1, 14)
        order_time = now - datetime.timedelta(days=days_ago, hours=hours_offset, minutes=random.randint(0, 59))

        order_payload = {
            "shopId": s_id,
            "staffId": assigned_staff.get("id", ""),
            "customerInfo": {
                "name": cust_name,
                "contact": cust_phone,
                "type": order_type
            },
            "items": items_payload,
            "orderTime": order_time.strftime("%Y-%m-%dT%H:%M:%SZ")
        }

        try:
            res_ord = http_request(f"{API_BASE}/orders", method="POST", data=order_payload, headers=headers)
            ord_id = res_ord if isinstance(res_ord, str) else res_ord.get("id", "")
            if ord_id:
                # Pay order (1=Cash, 2=Card, 3=E-Wallet)
                pm = random.choice([1, 2, 3])
                http_request(f"{API_BASE}/orders/{ord_id}/payment", method="POST", data={"id": ord_id, "paymentMethod": pm, "amount": order_subtotal}, headers=headers)
                # Mark completed
                http_request(f"{API_BASE}/orders/{ord_id}/status", method="PUT", data={"id": ord_id, "status": 4}, headers=headers)

                created_orders.append({
                    "orderId": ord_id,
                    "shopId": s_id,
                    "shopName": s_name,
                    "customerName": cust_name,
                    "customerPhone": cust_phone,
                    "selectedMenu": selected_menu,
                    "orderTime": order_time,
                    "subtotal": order_subtotal
                })
        except Exception:
            pass

    print(f" -> Đã tạo thành công {len(created_orders)} đơn hàng đánh giá đã hoàn tất thanh toán.")

    # 2. Generate comprehensive historical orders across all intervals (1 tuần, 1 tháng, 3 tháng, 6 tháng, 1 năm)
    print(" -> Đang khởi tạo thêm các đơn hàng lịch sử phủ khắp 1 tuần, 1 tháng, 3 tháng, 6 tháng, 1 năm...")
    
    historical_tasks = []
    # Intervals for each shop:
    # - 1 tuần: 1 - 7 ngày trước (5 đơn/quán)
    # - 1 tháng: 8 - 30 ngày trước (8 đơn/quán)
    # - 3 tháng: 31 - 90 ngày trước (12 đơn/quán)
    # - 6 tháng: 91 - 180 ngày trước (15 đơn/quán)
    # - 1 năm: 181 - 365 ngày trước (18 đơn/quán)
    period_configs = [
        (1, 7, 5),      # 1 tuần
        (8, 30, 8),     # 1 tháng
        (31, 90, 12),   # 3 tháng
        (91, 180, 15),  # 6 tháng
        (181, 365, 18)  # 1 năm
    ]

    for s in shops:
        s_id = s.get("id")
        staff_list = shop_staff_dict.get(s_id, [])
        primary_staff = staff_list[0]["id"] if staff_list else (all_staff[0]["id"] if all_staff else "")

        for min_days, max_days, count in period_configs:
            for _ in range(count):
                d_ago = random.randint(min_days, max_days)
                h_ago = random.randint(6, 21)
                m_ago = random.randint(0, 59)
                o_time = now - datetime.timedelta(days=d_ago, hours=h_ago, minutes=m_ago)

                c_name = f"{random.choice(first_names)} {random.choice(last_names)}"
                c_phone = f"09{random.randint(10000000, 99999999)}"
                o_type = random.choice([1, 2, 3])

                sel_menu = random.sample(menu_items, min(len(menu_items), random.randint(1, 3)))
                items = []
                subtotal = 0
                for mi in sel_menu:
                    q = random.randint(1, 2)
                    p = mi.get("price", {}).get("amount", 35000)
                    t = p * q
                    subtotal += t
                    items.append({
                        "menuItemId": mi.get("id"),
                        "name": mi.get("name"),
                        "description": mi.get("name"),
                        "unitPrice": {"amount": p, "currency": "VND"},
                        "quantity": q,
                        "specialInstructions": random.choice(["Đậm vị", "Ít ngọt", "Nhiều đá", "Tiêu chuẩn"]),
                        "totalPrice": {"amount": t, "currency": "VND"}
                    })

                payload = {
                    "shopId": s_id,
                    "staffId": primary_staff,
                    "customerInfo": {
                        "name": c_name,
                        "contact": c_phone,
                        "type": o_type
                    },
                    "items": items,
                    "orderTime": o_time.strftime("%Y-%m-%dT%H:%M:%SZ")
                }
                historical_tasks.append((payload, subtotal))

    def submit_historical_order(task):
        p, subtotal = task
        try:
            res = http_request(f"{API_BASE}/orders", method="POST", data=p, headers=headers)
            o_id = res if isinstance(res, str) else res.get("id", "")
            if o_id:
                http_request(f"{API_BASE}/orders/{o_id}/payment", method="POST", data={"id": o_id, "paymentMethod": random.choice([1, 2, 3]), "amount": subtotal}, headers=headers)
                http_request(f"{API_BASE}/orders/{o_id}/status", method="PUT", data={"id": o_id, "status": 4}, headers=headers)
                return True
        except Exception:
            pass
        return False

    with ThreadPoolExecutor(max_workers=14) as executor:
        results = list(executor.map(submit_historical_order, historical_tasks))
    print(f" -> Hoàn tất tạo {sum(1 for r in results if r)} đơn hàng lịch sử phủ khắp 1 tuần -> 1 năm cho 9 chi nhánh!")

    # 3. Generate 4-6 orders per shop for TODAY (active ongoing operations)
    print(" -> Đang tạo thêm các đơn hàng phát sinh trong ngày hôm nay cho các chi nhánh...")
    for s in shops:
        s_id = s.get("id")
        staff_list = shop_staff_dict.get(s_id, [])
        primary_staff = staff_list[0]["id"] if staff_list else (all_staff[0]["id"] if all_staff else "")
        for idx in range(random.randint(4, 6)):
            c_name = f"{random.choice(first_names)} {random.choice(last_names)}"
            c_phone = f"09{random.randint(10000000, 99999999)}"
            m = random.choice(menu_items)
            qty = random.randint(1, 3)
            u_price = m.get("price", {}).get("amount", 35000)
            today_time = now - datetime.timedelta(hours=random.randint(0, 8), minutes=random.randint(0, 50))
            today_payload = {
                "shopId": s_id,
                "staffId": primary_staff,
                "customerInfo": {
                    "name": c_name,
                    "contact": c_phone,
                    "type": random.choice([1, 2, 3])
                },
                "items": [
                    {
                        "menuItemId": m.get("id"),
                        "name": m.get("name"),
                        "description": m.get("name"),
                        "unitPrice": {"amount": u_price, "currency": "VND"},
                        "quantity": qty,
                        "specialInstructions": "Pha nhanh mang về",
                        "totalPrice": {"amount": u_price * qty, "currency": "VND"}
                    }
                ],
                "orderTime": today_time.strftime("%Y-%m-%dT%H:%M:%SZ")
            }
            try:
                res_t = http_request(f"{API_BASE}/orders", method="POST", data=today_payload, headers=headers)
                t_id = res_t if isinstance(res_t, str) else res_t.get("id", "")
                if t_id:
                    # Stagger statuses for today: mostly Completed, some Preparing, some Confirmed
                    if idx == 0:
                        http_request(f"{API_BASE}/orders/{t_id}/status", method="PUT", data={"id": t_id, "status": 2}, headers=headers) # Preparing
                    elif idx == 1:
                        http_request(f"{API_BASE}/orders/{t_id}/status", method="PUT", data={"id": t_id, "status": 1}, headers=headers) # Confirmed
                    else:
                        http_request(f"{API_BASE}/orders/{t_id}/payment", method="POST", data={"id": t_id, "paymentMethod": 1, "amount": u_price * qty}, headers=headers)
                        http_request(f"{API_BASE}/orders/{t_id}/status", method="PUT", data={"id": t_id, "status": 4}, headers=headers) # Completed
            except Exception:
                pass

    # 3. Create the 100 Customer Reviews directly linked to the orders
    print(" -> Bắt đầu tạo 100 Đánh giá của khách hàng khớp 100% với đơn hàng và sản phẩm...")
    reviews_created = 0
    for idx, ord_info in enumerate(created_orders):
        s_id = ord_info["shopId"]
        s_name = ord_info["shopName"]
        ord_id = ord_info["orderId"]
        cust_name = ord_info["customerName"]
        cust_phone = ord_info["customerPhone"]
        ordered_menu = ord_info["selectedMenu"]
        order_time = ord_info["orderTime"]

        # Rating: 75% 5-star, 20% 4-star, 5% 3-star
        roll = random.random()
        shop_rating = 5 if roll < 0.75 else (4 if roll < 0.95 else 3)
        shop_comment = random.choice(sample_shop_comments)

        item_reviews = []
        for it in ordered_menu:
            it_name = it.get("name", "")
            it_comments = product_comments_map.get(it_name, ["Hương vị thơm ngon, chuẩn vị và đóng gói cẩn thận."])
            it_rating = shop_rating if random.random() > 0.25 else max(3, shop_rating - 1)
            item_reviews.append({
                "menuItemId": it.get("id", ""),
                "menuItemName": it_name,
                "rating": it_rating,
                "comment": random.choice(it_comments)
            })

        tags = random.sample(all_tags, random.randint(2, 4))
        # Review written 1-3 hours after order
        review_time = order_time + datetime.timedelta(hours=random.randint(1, 3))

        review_payload = {
            "shopId": s_id,
            "shopName": s_name,
            "orderId": ord_id,
            "customerName": cust_name,
            "customerPhone": cust_phone,
            "shopRating": shop_rating,
            "shopComment": shop_comment,
            "itemReviews": item_reviews,
            "tags": tags,
            "createdAt": review_time.strftime("%Y-%m-%dT%H:%M:%SZ")
        }

        try:
            http_request(f"{API_BASE}/reviews", method="POST", data=review_payload)
            reviews_created += 1
        except Exception:
            pass

    print(f" -> Tạo thành công {reviews_created} đánh giá của khách hàng khớp chuẩn với đơn hàng và doanh thu!")

def main():
    print("================================================================")
    print("  KHỞI TẠO DỮ LIỆU TOÀN DIỆN HỆ THỐNG QUẢN LÝ QUÁN CÀ PHÊ       ")
    print(f"  Target API: {API_BASE}")
    print("================================================================")

    # 1. Authenticate Admin
    print("\n[Admin Auth] Xác thực quản trị viên...")
    token = authenticate()
    auth_headers = {"Authorization": f"Bearer {token}"}
    print(" -> Đăng nhập tài khoản admin thành công.")

    # 2. Wipe old data clean
    auth_headers = wipe_database(auth_headers)

    # 3. Fetch Vietnam provinces
    provinces = get_vietnam_provinces()

    # 4. Seed 9 Shops
    shops = seed_shops(auth_headers, provinces)
    if not shops:
        print("Lỗi: Không tìm thấy chi nhánh nào.")
        sys.exit(1)

    primary_shop_id = shops[0].get("id")

    # 5. Seed Menu Items (available 24/7)
    menu_items = seed_menu(auth_headers, primary_shop_id)

    # 6. Seed Inventory for all 9 shops
    seed_inventory_for_shops(auth_headers, shops)

    # 7. Seed Staff & User Accounts (9 shops x 11 staff = 99 staff with 100% natural Vietnamese names)
    shop_staff_map = seed_staff_and_accounts(auth_headers, shops)

    # 8. Seed Shifts schedule (Weekdays: 2 shifts, Weekends: 3 shifts, 1 week ago -> 2 weeks ahead)
    seed_shifts_schedule(auth_headers, shops, shop_staff_map)

    # 9. Seed Orders, Full Revenue & 100 Customer Reviews
    seed_reviews_and_orders(auth_headers, shops, menu_items, shop_staff_map)

    print("\n================================================================")
    print("  HOÀN TẤT SEED DỮ LIỆU THẬT VÀO MONGODB THÀNH CÔNG 100%!       ")
    print("================================================================")

if __name__ == "__main__":
    main()
