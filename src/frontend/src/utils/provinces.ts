/**
 * Vietnam Provinces online API helper (matches openapi.json).
 * Provides provinces, districts, and wards from https://provinces.open-api.vn
 * with offline fallbacks and caching.
 */

export interface Province {
  code: number;
  name: string;
  division_type?: string;
  codename?: string;
  phone_code?: number;
}

export interface District {
  code: number;
  name: string;
  division_type?: string;
  codename?: string;
  province_code: number;
}

export interface Ward {
  code: number;
  name: string;
  division_type?: string;
  codename?: string;
  district_code?: number;
  province_code?: number;
}

export const VIETNAM_PROVINCES: Province[] = [
  { code: 79, name: 'Thành phố Hồ Chí Minh', phone_code: 28 },
  { code: 1, name: 'Thành phố Hà Nội', phone_code: 24 },
  { code: 48, name: 'Thành phố Đà Nẵng', phone_code: 236 },
  { code: 31, name: 'Thành phố Hải Phòng', phone_code: 225 },
  { code: 92, name: 'Thành phố Cần Thơ', phone_code: 292 },
  { code: 68, name: 'Tỉnh Lâm Đồng', phone_code: 263 },
  { code: 66, name: 'Tỉnh Đắk Lắk', phone_code: 262 },
  { code: 56, name: 'Tỉnh Khánh Hòa', phone_code: 258 },
  { code: 74, name: 'Tỉnh Bình Dương', phone_code: 274 },
  { code: 75, name: 'Tỉnh Đồng Nai', phone_code: 251 },
  { code: 77, name: 'Tỉnh Bà Rịa - Vũng Tàu', phone_code: 254 },
  { code: 46, name: 'Tỉnh Thừa Thiên Huế', phone_code: 234 },
  { code: 49, name: 'Tỉnh Quảng Nam', phone_code: 235 },
  { code: 52, name: 'Tỉnh Bình Định', phone_code: 256 },
  { code: 22, name: 'Tỉnh Quảng Ninh', phone_code: 203 },
  { code: 27, name: 'Tỉnh Bắc Ninh', phone_code: 222 },
  { code: 30, name: 'Tỉnh Hải Dương', phone_code: 220 },
  { code: 38, name: 'Tỉnh Thanh Hóa', phone_code: 237 },
  { code: 40, name: 'Tỉnh Nghệ An', phone_code: 238 },
  { code: 42, name: 'Tỉnh Hà Tĩnh', phone_code: 239 },
  { code: 44, name: 'Tỉnh Quảng Bình', phone_code: 232 },
  { code: 45, name: 'Tỉnh Quảng Trị', phone_code: 233 },
  { code: 51, name: 'Tỉnh Quảng Ngãi', phone_code: 255 },
  { code: 54, name: 'Tỉnh Phú Yên', phone_code: 257 },
  { code: 58, name: 'Tỉnh Ninh Thuận', phone_code: 259 },
  { code: 60, name: 'Tỉnh Bình Thuận', phone_code: 252 },
  { code: 62, name: 'Tỉnh Kon Tum', phone_code: 260 },
  { code: 64, name: 'Tỉnh Gia Lai', phone_code: 269 },
  { code: 67, name: 'Tỉnh Đắk Nông', phone_code: 261 },
  { code: 70, name: 'Tỉnh Bình Phước', phone_code: 271 },
  { code: 72, name: 'Tỉnh Tây Ninh', phone_code: 276 },
  { code: 80, name: 'Tỉnh Long An', phone_code: 272 },
  { code: 82, name: 'Tỉnh Tiền Giang', phone_code: 273 },
  { code: 83, name: 'Tỉnh Bến Tre', phone_code: 275 },
  { code: 84, name: 'Tỉnh Trà Vinh', phone_code: 294 },
  { code: 86, name: 'Tỉnh Vĩnh Long', phone_code: 270 },
  { code: 87, name: 'Tỉnh Đồng Tháp', phone_code: 277 },
  { code: 89, name: 'Tỉnh An Giang', phone_code: 296 },
  { code: 91, name: 'Tỉnh Kiên Giang', phone_code: 297 },
  { code: 93, name: 'Tỉnh Hậu Giang', phone_code: 293 },
  { code: 94, name: 'Tỉnh Sóc Trăng', phone_code: 299 },
  { code: 95, name: 'Tỉnh Bạc Liêu', phone_code: 291 },
  { code: 96, name: 'Tỉnh Cà Mau', phone_code: 290 },
  { code: 2, name: 'Tỉnh Hà Giang', phone_code: 219 },
  { code: 4, name: 'Tỉnh Cao Bằng', phone_code: 206 },
  { code: 6, name: 'Tỉnh Bắc Kạn', phone_code: 209 },
  { code: 8, name: 'Tỉnh Tuyên Quang', phone_code: 207 },
  { code: 10, name: 'Tỉnh Lào Cai', phone_code: 214 },
  { code: 11, name: 'Tỉnh Điện Biên', phone_code: 215 },
  { code: 12, name: 'Tỉnh Lai Châu', phone_code: 213 },
  { code: 14, name: 'Tỉnh Sơn La', phone_code: 212 },
  { code: 15, name: 'Tỉnh Yên Bái', phone_code: 216 },
  { code: 17, name: 'Tỉnh Hoà Bình', phone_code: 218 },
  { code: 19, name: 'Tỉnh Thái Nguyên', phone_code: 208 },
  { code: 20, name: 'Tỉnh Lạng Sơn', phone_code: 205 },
  { code: 24, name: 'Tỉnh Bắc Giang', phone_code: 204 },
  { code: 25, name: 'Tỉnh Phú Thọ', phone_code: 210 },
  { code: 26, name: 'Tỉnh Vĩnh Phúc', phone_code: 211 },
  { code: 33, name: 'Tỉnh Hưng Yên', phone_code: 221 },
  { code: 34, name: 'Tỉnh Thái Bình', phone_code: 227 },
  { code: 35, name: 'Tỉnh Hà Nam', phone_code: 226 },
  { code: 36, name: 'Tỉnh Nam Định', phone_code: 228 },
  { code: 37, name: 'Tỉnh Ninh Bình', phone_code: 229 },
];

export const PROVINCE_POSTAL_CODES: Record<number, string> = {
  79: '70000', // TP. Hồ Chí Minh
  1: '100000', // Hà Nội
  48: '550000', // Đà Nẵng
  31: '180000', // Hải Phòng
  92: '900000', // Cần Thơ
  68: '670000', // Lâm Đồng
  66: '630000', // Đắk Lắk
  56: '650000', // Khánh Hòa
  74: '820000', // Bình Dương
  75: '810000', // Đồng Nai
  77: '790000', // Bà Rịa - Vũng Tàu
  46: '530000', // Thừa Thiên Huế
  49: '560000', // Quảng Nam
  52: '590000', // Bình Định
  22: '200000', // Quảng Ninh
  27: '790000', // Bắc Ninh
  30: '170000', // Hải Dương
  38: '440000', // Thanh Hóa
  40: '460000', // Nghệ An
  42: '480000', // Hà Tĩnh
  11: '380000', // Điện Biên
  10: '330000', // Lào Cai
  14: '360000', // Sơn La
  24: '220000', // Bắc Giang
  80: '850000', // Long An
  82: '860000', // Tiền Giang
  91: '920000', // Kiên Giang
  96: '970000', // Cà Mau
};

// Fallback districts for common provinces in case of offline/network issues
const FALLBACK_DISTRICTS: Record<number, District[]> = {
  // TP. Hồ Chí Minh (code: 79)
  79: [
    { code: 760, name: 'Quận 1', province_code: 79 },
    { code: 769, name: 'Thành phố Thủ Đức', province_code: 79 },
    { code: 770, name: 'Quận 3', province_code: 79 },
    { code: 773, name: 'Quận 4', province_code: 79 },
    { code: 774, name: 'Quận 5', province_code: 79 },
    { code: 775, name: 'Quận 6', province_code: 79 },
    { code: 778, name: 'Quận 7', province_code: 79 },
    { code: 776, name: 'Quận 8', province_code: 79 },
    { code: 771, name: 'Quận 10', province_code: 79 },
    { code: 772, name: 'Quận 11', province_code: 79 },
    { code: 761, name: 'Quận 12', province_code: 79 },
    { code: 765, name: 'Quận Bình Thạnh', province_code: 79 },
    { code: 764, name: 'Quận Gò Vấp', province_code: 79 },
    { code: 768, name: 'Quận Phú Nhuận', province_code: 79 },
    { code: 766, name: 'Quận Tân Bình', province_code: 79 },
    { code: 767, name: 'Quận Tân Phú', province_code: 79 },
    { code: 777, name: 'Quận Bình Tân', province_code: 79 },
    { code: 785, name: 'Huyện Bình Chánh', province_code: 79 },
    { code: 784, name: 'Huyện Hóc Môn', province_code: 79 },
    { code: 783, name: 'Huyện Củ Chi', province_code: 79 },
    { code: 786, name: 'Huyện Nhà Bè', province_code: 79 },
    { code: 787, name: 'Huyện Cần Giờ', province_code: 79 },
  ],
  // Hà Nội (code: 1)
  1: [
    { code: 2, name: 'Quận Hoàn Kiếm', province_code: 1 },
    { code: 1, name: 'Quận Ba Đình', province_code: 1 },
    { code: 3, name: 'Quận Tây Hồ', province_code: 1 },
    { code: 4, name: 'Quận Long Biên', province_code: 1 },
    { code: 5, name: 'Quận Cầu Giấy', province_code: 1 },
    { code: 6, name: 'Quận Đống Đa', province_code: 1 },
    { code: 7, name: 'Quận Hai Bà Trưng', province_code: 1 },
    { code: 8, name: 'Quận Hoàng Mai', province_code: 1 },
    { code: 9, name: 'Quận Thanh Xuân', province_code: 1 },
    { code: 19, name: 'Quận Nam Từ Liêm', province_code: 1 },
    { code: 20, name: 'Quận Bắc Từ Liêm', province_code: 1 },
    { code: 21, name: 'Quận Hà Đông', province_code: 1 },
  ],
  // Đà Nẵng (code: 48)
  48: [
    { code: 490, name: 'Quận Hải Châu', province_code: 48 },
    { code: 491, name: 'Quận Thanh Khê', province_code: 48 },
    { code: 492, name: 'Quận Sơn Trà', province_code: 48 },
    { code: 493, name: 'Quận Ngũ Hành Sơn', province_code: 48 },
    { code: 494, name: 'Quận Liên Chiểu', province_code: 48 },
    { code: 495, name: 'Quận Cẩm Lệ', province_code: 48 },
    { code: 497, name: 'Huyện Hòa Vang', province_code: 48 },
  ],
  // Điện Biên (code: 11)
  11: [
    { code: 94, name: 'Thành phố Điện Biên Phủ', province_code: 11 },
    { code: 95, name: 'Thị xã Mường Lay', province_code: 11 },
    { code: 96, name: 'Huyện Mường Nhé', province_code: 11 },
    { code: 97, name: 'Huyện Mường Chà', province_code: 11 },
    { code: 98, name: 'Huyện Tủa Chùa', province_code: 11 },
    { code: 99, name: 'Huyện Tuần Giáo', province_code: 11 },
    { code: 100, name: 'Huyện Điện Biên', province_code: 11 },
    { code: 101, name: 'Huyện Điện Biên Đông', province_code: 11 },
    { code: 102, name: 'Huyện Mường Ảng', province_code: 11 },
    { code: 103, name: 'Huyện Nậm Pồ', province_code: 11 },
  ],
  // Lâm Đồng (code: 68)
  68: [
    { code: 672, name: 'Thành phố Đà Lạt', province_code: 68 },
    { code: 673, name: 'Thành phố Bảo Lộc', province_code: 68 },
    { code: 674, name: 'Huyện Đam Rông', province_code: 68 },
    { code: 675, name: 'Huyện Lạc Dương', province_code: 68 },
    { code: 676, name: 'Huyện Lâm Hà', province_code: 68 },
    { code: 677, name: 'Huyện Đơn Dương', province_code: 68 },
    { code: 678, name: 'Huyện Đức Trọng', province_code: 68 },
    { code: 679, name: 'Huyện Di Linh', province_code: 68 },
    { code: 680, name: 'Huyện Bảo Lâm', province_code: 68 },
  ],
  // Đắk Lắk (code: 66)
  66: [
    { code: 643, name: 'Thành phố Buôn Ma Thuột', province_code: 66 },
    { code: 644, name: 'Thị xã Buôn Hồ', province_code: 66 },
    { code: 645, name: 'Huyện Ea H\'leo', province_code: 66 },
    { code: 646, name: 'Huyện Ea Súp', province_code: 66 },
    { code: 647, name: 'Huyện Buôn Đôn', province_code: 66 },
    { code: 648, name: 'Huyện Cư M\'gar', province_code: 66 },
    { code: 649, name: 'Huyện Krông Búk', province_code: 66 },
  ],
};

// Fallback wards for common districts
const FALLBACK_WARDS: Record<number, Ward[]> = {
  // Quận 1 (code: 760)
  760: [
    { code: 26734, name: 'Phường Bến Nghé', district_code: 760 },
    { code: 26737, name: 'Phường Bến Thành', district_code: 760 },
    { code: 26740, name: 'Phường Đa Kao', district_code: 760 },
    { code: 26743, name: 'Phường Tân Định', district_code: 760 },
    { code: 26746, name: 'Phường Nguyễn Thái Bình', district_code: 760 },
    { code: 26749, name: 'Phường Phạm Ngũ Lão', district_code: 760 },
    { code: 26752, name: 'Phường Cầu Ông Lãnh', district_code: 760 },
    { code: 26755, name: 'Phường Cô Giang', district_code: 760 },
    { code: 26758, name: 'Phường Nguyễn Cư Trinh', district_code: 760 },
    { code: 26761, name: 'Phường Cầu Kho', district_code: 760 },
  ],
  // Quận Hoàn Kiếm (code: 2)
  2: [
    { code: 37, name: 'Phường Hàng Bạc', district_code: 2 },
    { code: 40, name: 'Phường Hàng Đào', district_code: 2 },
    { code: 43, name: 'Phường Tràng Tiền', district_code: 2 },
    { code: 46, name: 'Phường Lý Thái Tổ', district_code: 2 },
    { code: 49, name: 'Phường Phan Chu Trinh', district_code: 2 },
    { code: 52, name: 'Phường Hàng Gai', district_code: 2 },
    { code: 55, name: 'Phường Cửa Đông', district_code: 2 },
    { code: 58, name: 'Phường Cửa Nam', district_code: 2 },
  ],
  // Quận Hải Châu (code: 490)
  490: [
    { code: 20194, name: 'Phường Hải Châu I', district_code: 490 },
    { code: 20197, name: 'Phường Hải Châu II', district_code: 490 },
    { code: 20200, name: 'Phường Thạch Thang', district_code: 490 },
    { code: 20203, name: 'Phường Thanh Bình', district_code: 490 },
    { code: 20206, name: 'Phường Thuận Phước', district_code: 490 },
    { code: 20209, name: 'Phường Hòa Thuận Đông', district_code: 490 },
    { code: 20212, name: 'Phường Nam Dương', district_code: 490 },
  ],
  // Thành phố Điện Biên Phủ (code: 94)
  94: [
    { code: 3124, name: 'Phường Noong Bua', district_code: 94 },
    { code: 3127, name: 'Phường Him Lam', district_code: 94 },
    { code: 3130, name: 'Phường Thanh Bình', district_code: 94 },
    { code: 3133, name: 'Phường Tân Thanh', district_code: 94 },
    { code: 3136, name: 'Phường Mường Thanh', district_code: 94 },
    { code: 3139, name: 'Phường Nam Thanh', district_code: 94 },
    { code: 3142, name: 'Phường Thanh Trường', district_code: 94 },
    { code: 3145, name: 'Xã Thanh Minh', district_code: 94 },
    { code: 3325, name: 'Xã Mường Phăng', district_code: 94 },
  ],
};

// In-memory caches to guarantee fast responsiveness
let cachedProvinces: Province[] | null = null;
const districtCache = new Map<number, District[]>();
const wardCache = new Map<number, Ward[]>();

/**
 * Fetch list of all provinces in Vietnam from online API with fallback.
 */
export async function fetchOnlineProvinces(): Promise<Province[]> {
  if (cachedProvinces && cachedProvinces.length > 0) {
    return cachedProvinces;
  }

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 4000);

    const res = await fetch('https://provinces.open-api.vn/api/v1/p/', {
      signal: controller.signal,
    });
    clearTimeout(timeoutId);

    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        cachedProvinces = data.map((p) => ({
          code: p.code,
          name: p.name,
          division_type: p.division_type,
          codename: p.codename,
          phone_code: p.phone_code,
        }));
        return cachedProvinces;
      }
    }
  } catch {
    // network or abort error -> use fallback
  }

  cachedProvinces = VIETNAM_PROVINCES;
  return VIETNAM_PROVINCES;
}

/**
 * Fetch districts for a given province code.
 */
export async function fetchDistrictsByProvince(provinceCode: number): Promise<District[]> {
  if (districtCache.has(provinceCode)) {
    return districtCache.get(provinceCode)!;
  }

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 4000);

    const res = await fetch(`https://provinces.open-api.vn/api/v1/p/${provinceCode}?depth=2`, {
      signal: controller.signal,
    });
    clearTimeout(timeoutId);

    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data.districts) && data.districts.length > 0) {
        const districts: District[] = data.districts.map((d: any) => ({
          code: d.code,
          name: d.name,
          division_type: d.division_type,
          codename: d.codename,
          province_code: provinceCode,
        }));
        districtCache.set(provinceCode, districts);
        return districts;
      }
    }
  } catch {
    // network error -> fallback
  }

  // Use fallback if exists
  if (FALLBACK_DISTRICTS[provinceCode]) {
    const fallback = FALLBACK_DISTRICTS[provinceCode];
    districtCache.set(provinceCode, fallback);
    return fallback;
  }

  return [];
}

/**
 * Fetch wards for a given district code.
 */
export async function fetchWardsByDistrict(districtCode: number): Promise<Ward[]> {
  if (wardCache.has(districtCode)) {
    return wardCache.get(districtCode)!;
  }

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 4000);

    const res = await fetch(`https://provinces.open-api.vn/api/v1/d/${districtCode}?depth=2`, {
      signal: controller.signal,
    });
    clearTimeout(timeoutId);

    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data.wards) && data.wards.length > 0) {
        const wards: Ward[] = data.wards.map((w: any) => ({
          code: w.code,
          name: w.name,
          division_type: w.division_type,
          codename: w.codename,
          district_code: districtCode,
        }));
        wardCache.set(districtCode, wards);
        return wards;
      }
    }
  } catch {
    // network error -> fallback
  }

  if (FALLBACK_WARDS[districtCode]) {
    const fallback = FALLBACK_WARDS[districtCode];
    wardCache.set(districtCode, fallback);
    return fallback;
  }

  return [];
}

/**
 * Resolve standard postal code from province code or name.
 */
export function getPostalCodeForProvince(codeOrName: number | string): string {
  if (typeof codeOrName === 'number') {
    return PROVINCE_POSTAL_CODES[codeOrName] || '70000';
  }

  const match = VIETNAM_PROVINCES.find(
    (p) => p.name.toLowerCase() === codeOrName.trim().toLowerCase()
  );
  if (match && PROVINCE_POSTAL_CODES[match.code]) {
    return PROVINCE_POSTAL_CODES[match.code];
  }

  return '70000';
}
