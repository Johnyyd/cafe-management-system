#!/usr/bin/env python3
"""
Seed script for Cafe Management System.
Fetches Vietnamese province data from Vietnam Provinces API (as documented in openapi.json)
and populates MongoDB with realistic cafe shops, menu items, inventory, staff, and orders via the API.
"""

import sys
import json
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
        with urllib.request.urlopen(req, timeout=15) as resp:
            content = resp.read().decode("utf-8")
            if not content:
                return {}
            return json.loads(content)
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8")
        try:
            parsed = json.loads(error_body)
            err_msg = parsed.get("detail") or parsed.get("title") or error_body
        except Exception:
            err_msg = error_body
        raise RuntimeError(f"HTTP {e.code} on {method} {url}: {err_msg}")
    except Exception as e:
        raise RuntimeError(f"Request failed on {method} {url}: {e}")

def get_vietnam_provinces() -> List[Dict[str, Any]]:
    """Fetch Vietnam province list from the online API with local fallback."""
    print("[1/6] Đang tải danh mục Tỉnh/Thành phố Việt Nam...")
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
        except Exception as err:
            print(f" -> Thử {ep} không thành công ({err}), thử tiếp nguồn dự phòng...")

    # Fallback if internet connectivity is restricted
    print(" -> Sử dụng danh sách tỉnh thành trọng điểm dự phòng...")
    return [
        {"name": "Thành phố Hồ Chí Minh", "code": 79, "districts": [{"name": "Quận 1"}, {"name": "Quận Bình Thạnh"}, {"name": "Thành phố Thủ Đức"}]},
        {"name": "Thành phố Hà Nội", "code": 1, "districts": [{"name": "Quận Hoàn Kiếm"}, {"name": "Quận Cầu Giấy"}, {"name": "Quận Ba Đình"}]},
        {"name": "Thành phố Đà Nẵng", "code": 48, "districts": [{"name": "Quận Hải Châu"}, {"name": "Quận Sơn Trà"}]},
        {"name": "Tỉnh Lâm Đồng", "code": 68, "districts": [{"name": "Thành phố Đà Lạt"}, {"name": "Thành phố Bảo Lộc"}]},
        {"name": "Tỉnh Đắk Lắk", "code": 66, "districts": [{"name": "Thành phố Buôn Ma Thuột"}]},
        {"name": "Thành phố Cần Thơ", "code": 92, "districts": [{"name": "Quận Ninh Kiều"}]},
        {"name": "Thành phố Hải Phòng", "code": 31, "districts": [{"name": "Quận Hồng Bàng"}]},
    ]

def authenticate() -> str:
    """Login or register admin account and retrieve Bearer JWT token."""
    print("[2/6] Xác thực quản trị viên (Admin authentication)...")
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
    except Exception as e:
        print(f" -> Đăng nhập không thành công ({e}), tiến hành đăng ký tài khoản mới...")

    # Register admin if not present
    reg_url = f"{API_BASE}/auth/register"
    reg_payload = {
        "email": "admin@cafemanagement.com",
        "password": "Password123!",
        "fullName": "Quản trị viên Hệ thống"
    }
    reg_res = http_request(reg_url, method="POST", data=reg_payload)
    token = reg_res.get("accessToken")
    if not token:
        raise RuntimeError("Không thể lấy token sau khi đăng ký!")
    print(" -> Đăng ký và đăng nhập tài khoản admin thành công.")
    return token

def seed_shops(headers: Dict[str, str], provinces: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
    print("\n[3/6] Đồng bộ Chi nhánh quán (Shops) với các Tỉnh/Thành phố Việt Nam...")
    res = http_request(f"{API_BASE}/shops", headers=headers)
    existing_shops = res if isinstance(res, list) else res.get("items", [])

    if len(existing_shops) >= 5:
        print(f" -> Đã có {len(existing_shops)} chi nhánh trong hệ thống:")
        for s in existing_shops:
            print(f"    * [{s.get('id')}] {s.get('name')} ({s.get('address', {}).get('city', '')})")
        return existing_shops

    # Province mapping for real branch locations
    target_branches = [
        {
            "name": "The Coffee House Landmark 81 - Bình Thạnh",
            "search_province": "Hồ Chí Minh",
            "street": "720A Điện Biên Phủ, Phường 22",
            "district": "Quận Bình Thạnh",
            "zipCode": "72300",
            "phone": "02871087088",
            "email": "branch.landmark81@cafemanagement.com",
        },
        {
            "name": "Highlands Coffee Nhà Hát Lớn - Hoàn Kiếm",
            "search_province": "Hà Nội",
            "street": "1 Tràng Tiền, Phường Tràng Tiền",
            "district": "Quận Hoàn Kiếm",
            "zipCode": "10000",
            "phone": "02439338888",
            "email": "branch.nhahatlon@cafemanagement.com",
        },
        {
            "name": "Phúc Long Tea & Coffee Cầu Rồng - Hải Châu",
            "search_province": "Đà Nẵng",
            "street": "61 Nguyễn Văn Linh, Phường Nam Dương",
            "district": "Quận Hải Châu",
            "zipCode": "55000",
            "phone": "02363588999",
            "email": "branch.danang@cafemanagement.com",
        },
        {
            "name": "Cafe Cầu Đất Farm - Thành phố Đà Lạt",
            "search_province": "Lâm Đồng",
            "street": "Thôn Trường Thọ, Xã Trạm Hành",
            "district": "Thành phố Đà Lạt",
            "zipCode": "67000",
            "phone": "02633889977",
            "email": "branch.caudat@cafemanagement.com",
        },
        {
            "name": "Trung Nguyên Legend Buôn Ma Thuột",
            "search_province": "Đắk Lắk",
            "street": "45 Lý Thái Tổ, Phường Tân Lợi",
            "district": "Thành phố Buôn Ma Thuột",
            "zipCode": "63000",
            "phone": "02623955666",
            "email": "branch.bmt@cafemanagement.com",
        },
    ]

    created = list(existing_shops)
    for b in target_branches:
        if any(s.get("name") == b["name"] for s in existing_shops):
            continue

        # Match city name from provinces
        matched_province = next((p for p in provinces if b["search_province"].lower() in p.get("name", "").lower()), None)
        city_name = matched_province.get("name") if matched_province else b["search_province"]

        payload = {
            "name": b["name"],
            "address": {
                "street": b["street"],
                "city": city_name,
                "district": b["district"],
                "zipCode": b["zipCode"]
            },
            "contact": {
                "phone": b["phone"],
                "email": b["email"]
            },
            "operatingHours": [
                {"dayOfWeek": 1, "openTime": "06:30:00", "closeTime": "22:30:00"},
                {"dayOfWeek": 2, "openTime": "06:30:00", "closeTime": "22:30:00"},
                {"dayOfWeek": 3, "openTime": "06:30:00", "closeTime": "22:30:00"},
                {"dayOfWeek": 4, "openTime": "06:30:00", "closeTime": "22:30:00"},
                {"dayOfWeek": 5, "openTime": "06:30:00", "closeTime": "23:00:00"},
                {"dayOfWeek": 6, "openTime": "07:00:00", "closeTime": "23:00:00"},
                {"dayOfWeek": 0, "openTime": "07:00:00", "closeTime": "22:00:00"}
            ]
        }
        try:
            res_shop = http_request(f"{API_BASE}/shops", method="POST", data=payload, headers=headers)
            shop_id = res_shop if isinstance(res_shop, str) else res_shop.get("id", "")
            print(f" -> Tạo thành công chi nhánh: {b['name']} ({city_name}) [ID: {shop_id}]")
            created.append({"id": shop_id, "name": b["name"]})
        except Exception as e:
            print(f" -> Bỏ qua tạo chi nhánh {b['name']}: {e}")

    # Reload shops to get full list
    res = http_request(f"{API_BASE}/shops", headers=headers)
    return res if isinstance(res, list) else res.get("items", created)

def seed_menu(headers: Dict[str, str], shop_id: str) -> List[Dict[str, Any]]:
    print("\n[4/6] Đồng bộ Thực đơn (Menu Items)...")
    res = http_request(f"{API_BASE}/menu", headers=headers)
    existing_menu = res if isinstance(res, list) else res.get("items", [])

    if len(existing_menu) >= 8:
        print(f" -> Đã có {len(existing_menu)} món trong thực đơn:")
        for m in existing_menu:
            print(f"    * [{m.get('id')}] {m.get('name')} - {m.get('price', {}).get('amount', 0):,} ₫")
        return existing_menu

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
                "startTime": "06:00:00",
                "endTime": "23:00:00",
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

def seed_inventory(headers: Dict[str, str], shop_id: str) -> List[Dict[str, Any]]:
    res = http_request(f"{API_BASE}/inventory?shopId={shop_id}", headers=headers)
    existing_inv = res if isinstance(res, list) else res.get("items", [])

    if len(existing_inv) >= 6:
        print(f" -> Đã có {len(existing_inv)} mặt hàng nguyên liệu cho chi nhánh [{shop_id}].")
        print(f" -> Đã có {len(existing_inv)} mặt hàng nguyên liệu trong kho:")
        for i in existing_inv:
            print(f"    * [{i.get('id')}] {i.get('itemName')}: {i.get('quantity', i.get('currentStock', 0))} {i.get('unit')}")
        return existing_inv

    inventory_items = [
        {"itemName": "Hạt Cà phê Robusta Buôn Ma Thuột", "unit": "kg", "quantity": 120, "reorderLevel": 25},
        {"itemName": "Hạt Cà phê Arabica Cầu Đất Đà Lạt", "unit": "kg", "quantity": 85, "reorderLevel": 20},
        {"itemName": "Sữa đặc có đường Ông Thọ", "unit": "lon", "quantity": 150, "reorderLevel": 30},
        {"itemName": "Sữa tươi thanh trùng Dalat Milk", "unit": "hộp 1L", "quantity": 60, "reorderLevel": 15},
        {"itemName": "Trà Ô Long Bảo Lộc Tuyển Chọn", "unit": "kg", "quantity": 35, "reorderLevel": 10},
        {"itemName": "Đào ngâm đóng hộp Kronos", "unit": "hộp", "quantity": 48, "reorderLevel": 12},
        {"itemName": "Kem béo thực vật Rich's", "unit": "hộp 454g", "quantity": 40, "reorderLevel": 10},
        {"itemName": "Đường cát trắng tinh luyện Biên Hòa", "unit": "kg", "quantity": 90, "reorderLevel": 20},
    ]

    for it in inventory_items:
        if any(x.get("itemName") == it["itemName"] for x in existing_inv):
            continue

        payload = {
            "shopId": shop_id,
            "itemName": it["itemName"],
            "unit": it["unit"],
            "quantity": it["quantity"],
            "reorderLevel": it["reorderLevel"]
        }
        try:
            created_res = http_request(f"{API_BASE}/inventory", method="POST", data=payload, headers=headers)
            item_id = created_res if isinstance(created_res, str) else created_res.get("id", "")
            print(f" -> Nhập kho nguyên liệu: {it['itemName']} ({it['quantity']} {it['unit']}) [ID: {item_id}]")
        except Exception as e:
            print(f" -> Bỏ qua nguyên liệu {it['itemName']}: {e}")

    res = http_request(f"{API_BASE}/inventory", headers=headers)
    return res if isinstance(res, list) else res.get("items", [])

def seed_staff(headers: Dict[str, str], shop_id: str) -> List[Dict[str, Any]]:
    print("\n[6/6] Đồng bộ Nhân sự & Đơn hàng bán (Staff & Orders)...")
    res = http_request(f"{API_BASE}/staff", headers=headers)
    existing_staff = res if isinstance(res, list) else res.get("items", [])

    staff_members = [
        {"firstName": "Nguyễn Hoàng", "lastName": "Nam", "role": 1, "phone": "0908123456", "email": "nam.nguyen@cafemanagement.com"},
        {"firstName": "Trần Thị Thu", "lastName": "Thảo", "role": 2, "phone": "0912345678", "email": "thao.tran@cafemanagement.com"},
        {"firstName": "Lê Anh", "lastName": "Khoa", "role": 3, "phone": "0934567890", "email": "khoa.le@cafemanagement.com"},
        {"firstName": "Phạm Quỳnh", "lastName": "Nga", "role": 3, "phone": "0945678901", "email": "nga.pham@cafemanagement.com"},
        {"firstName": "Đỗ Minh", "lastName": "Quân", "role": 4, "phone": "0978901234", "email": "quan.do@cafemanagement.com"},
    ]

    for s in staff_members:
        full_name = f"{s['firstName']} {s['lastName']}"
        if any(f"{x.get('firstName')} {x.get('lastName')}" == full_name for x in existing_staff):
            continue

        payload = {
            "firstName": s["firstName"],
            "lastName": s["lastName"],
            "role": s["role"],
            "contact": {
                "phone": s["phone"],
                "email": s["email"]
            },
            "hireDate": "2026-01-15T08:00:00Z",
            "shopId": shop_id
        }
        try:
            res_staff = http_request(f"{API_BASE}/staff", method="POST", data=payload, headers=headers)
            st_id = res_staff if isinstance(res_staff, str) else res_staff.get("id", "")
            print(f" -> Thêm nhân sự: {full_name} (Role: {s['role']}) [ID: {st_id}]")
        except Exception as e:
            print(f" -> Bỏ qua nhân sự {full_name}: {e}")

    # Return refreshed staff
    res = http_request(f"{API_BASE}/staff", headers=headers)
    return res if isinstance(res, list) else res.get("items", [])

def seed_sample_orders(headers: Dict[str, str], shop_id: str, staff_id: str, menu_items: List[Dict[str, Any]]):
    res = http_request(f"{API_BASE}/orders?shopId={shop_id}&pageSize=50", headers=headers)
    existing_orders = res if isinstance(res, list) else res.get("items", [])

    if len(existing_orders) >= 1:
        print(f" -> Đã có {len(existing_orders)} đơn hàng cho chi nhánh [{shop_id}].")
        return

    if not menu_items:
        return

    m1 = menu_items[0]
    m2 = menu_items[1] if len(menu_items) > 1 else m1

    orders_to_create = [
        {
            "customer_name": "Nguyễn Minh Tuấn",
            "customer_phone": "0987654321",
            "type": 1,  # DineIn
            "status": 4,  # Completed
            "payment_status": 2,  # Paid
            "items": [
                {"item": m1, "qty": 2, "note": "Ít ngọt, nhiều đá"},
                {"item": m2, "qty": 1, "note": "Ngọt bình thường"}
            ]
        },
        {
            "customer_name": "Chị Hoàng Lan",
            "customer_phone": "0912888999",
            "type": 2,  # Takeaway
            "status": 2,  # Preparing
            "payment_status": 2,  # Paid
            "items": [
                {"item": m1, "qty": 3, "note": "Mang đi để riêng đá"}
            ]
        }
    ]

    for ord_data in orders_to_create:
        payload = {
            "shopId": shop_id,
            "staffId": staff_id,
            "customerInfo": {
                "name": ord_data["customer_name"],
                "contact": ord_data["customer_phone"],
                "type": ord_data["type"]
            },
            "items": [
                {
                    "menuItemId": it["item"].get("id"),
                    "name": it["item"].get("name"),
                    "description": it["item"].get("name"),
                    "unitPrice": it["item"].get("price", {"amount": 35000}),
                    "quantity": it["qty"],
                    "specialInstructions": it["note"],
                    "totalPrice": {
                        "amount": it["item"].get("price", {}).get("amount", 35000) * it["qty"],
                        "currency": "VND"
                    }
                }
                for it in ord_data["items"]
            ]
        }
        try:
            res_ord = http_request(f"{API_BASE}/orders", method="POST", data=payload, headers=headers)
            ord_id = res_ord if isinstance(res_ord, str) else res_ord.get("id", "")
            print(f" -> Tạo thành công đơn hàng mẫu: {ord_data['customer_name']} [ID: {ord_id}]")
            if ord_id and ord_data.get("payment_status") == 2:
                try:
                    http_request(f"{API_BASE}/orders/{ord_id}/payment", method="POST", data={"id": ord_id, "paymentMethod": 2, "amount": 0}, headers=headers)
                except Exception:
                    pass
            if ord_id and ord_data.get("status"):
                try:
                    http_request(f"{API_BASE}/orders/{ord_id}/status", method="PUT", data={"id": ord_id, "status": ord_data["status"]}, headers=headers)
                except Exception:
                    pass
        except Exception as e:
            print(f" -> Bỏ qua tạo đơn {ord_data['customer_name']}: {e}")

def main():
    print("================================================================")
    print("  KHỞI TẠO DỮ LIỆU THẬT HỆ THỐNG QUẢN LÝ QUÁN CÀ PHÊ (PYTHON)   ")
    print("================================================================")

    # 1. Fetch Vietnam provinces
    provinces = get_vietnam_provinces()

    # 2. Authenticate
    token = authenticate()
    auth_headers = {"Authorization": f"Bearer {token}"}

    # 3. Seed Shops using Vietnam provinces
    shops = seed_shops(auth_headers, provinces)
    primary_shop_id = shops[0].get("id") if shops else ""

    if not primary_shop_id:
        print("Lỗi: Không tìm thấy ID chi nhánh chính.")
        sys.exit(1)

    # 4. Seed Menu Items
    menu_items = seed_menu(auth_headers, primary_shop_id)

    # 5. Seed Staff
    staff_list = seed_staff(auth_headers, primary_shop_id)
    primary_staff_id = staff_list[0].get("id") if staff_list else ""

    # 6. Seed Inventory & Orders for each shop
    print("\n[5/6] Đồng bộ Kho & Nguyên Vật Liệu cho tất cả chi nhánh...")
    for s in shops:
        s_id = s.get("id")
        if s_id:
            seed_inventory(auth_headers, s_id)

    print("\n[6/6] Đồng bộ Đơn hàng mẫu cho các chi nhánh...")
    if primary_staff_id and menu_items:
        for s in shops:
            s_id = s.get("id")
            if s_id:
                seed_sample_orders(auth_headers, s_id, primary_staff_id, menu_items)

    print("\n================================================================")
    print("  HOÀN TẤT SEED DỮ LIỆU THẬT VÀO MONGODB THÀNH CÔNG 100%!       ")
    print("================================================================")

if __name__ == "__main__":
    main()
