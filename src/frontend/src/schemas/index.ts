import { z } from 'zod';

export const loginSchema = z.object({
  email: z.string().min(1, 'Email không được để trống').email('Email không đúng định dạng'),
  password: z.string().min(6, 'Mật khẩu tối thiểu 6 ký tự'),
  rememberMe: z.boolean().optional(),
});
export type LoginFormData = z.infer<typeof loginSchema>;

export const shopSchema = z.object({
  name: z.string().min(2, 'Tên quán tối thiểu 2 ký tự').max(100, 'Tên quán tối đa 100 ký tự'),
  street: z.string().min(1, 'Địa chỉ đường là bắt buộc'),
  city: z.string().min(1, 'Tỉnh / Thành phố là bắt buộc'),
  district: z.string().min(1, 'Quận / Huyện là bắt buộc'),
  ward: z.string().optional(),
  state: z.string().optional(),
  postalCode: z.string().min(1, 'Mã bưu điện là bắt buộc'),
  country: z.string().min(1, 'Quốc gia là bắt buộc'),
  phone: z.string().regex(/^[0-9+\-\s()]{8,20}$/, 'Số điện thoại không hợp lệ (8-20 ký tự số)'),
  email: z.string().email('Email quán không hợp lệ'),
  openTime: z.string().regex(/^([01]\d|2[0-3]):([0-5]\d)$/, 'Giờ mở cửa định dạng HH:mm (VD: 07:00)'),
  closeTime: z.string().regex(/^([01]\d|2[0-3]):([0-5]\d)$/, 'Giờ đóng cửa định dạng HH:mm (VD: 22:30)'),
  status: z.enum(['Active', 'UnderMaintenance', 'Closed']),
}).refine(data => data.openTime < data.closeTime, {
  message: 'Giờ mở cửa phải trước giờ đóng cửa',
  path: ['closeTime'],
});
export type ShopFormData = z.infer<typeof shopSchema>;

export const menuItemSchema = z.object({
  name: z.string().min(2, 'Tên món tối thiểu 2 ký tự').max(100, 'Tên món tối đa 100 ký tự'),
  description: z.string().min(5, 'Mô tả chi tiết tối thiểu 5 ký tự'),
  category: z.string().min(1, 'Vui lòng chọn danh mục'),
  price: z.coerce.number().positive('Giá tiền phải lớn hơn 0'),
  ingredientsText: z.string().min(1, 'Nhập các thành phần, phân tách bằng dấu phẩy'),
  allergensText: z.string().optional(),
  isAvailable: z.boolean().default(true),
});
export type MenuItemFormData = z.infer<typeof menuItemSchema>;

export const inventoryItemSchema = z.object({
  itemName: z.string().min(2, 'Tên nguyên liệu tối thiểu 2 ký tự').max(100, 'Tên tối đa 100 ký tự'),
  unit: z.string().min(1, 'Đơn vị tính là bắt buộc (kg, lít, hộp, túi)'),
  currentStock: z.coerce.number().min(0, 'Số lượng tồn không được âm'),
  reorderLevel: z.coerce.number().min(0, 'Mức cảnh báo tồn không được âm'),
  supplierName: z.string().optional(),
});
export type InventoryItemFormData = z.infer<typeof inventoryItemSchema>;

export const stockAdjustSchema = z.object({
  amount: z.coerce.number().refine(val => val !== 0, 'Số lượng điều chỉnh phải khác 0'),
  type: z.enum(['Add', 'Deduct', 'Set']),
  reason: z.string().min(3, 'Lý do điều chỉnh tối thiểu 3 ký tự'),
});
export type StockAdjustFormData = z.infer<typeof stockAdjustSchema>;

export const orderItemSchema = z.object({
  menuItemId: z.string().min(1, 'Mã món là bắt buộc'),
  name: z.string().min(1, 'Tên món là bắt buộc'),
  quantity: z.coerce.number().int('Số lượng phải là số nguyên').min(1, 'Số lượng tối thiểu là 1'),
  unitPrice: z.coerce.number().min(0, 'Giá không được âm'),
  specialInstructions: z.string().optional(),
});

export const orderSchema = z.object({
  shopId: z.string().min(1, 'Vui lòng chọn chi nhánh'),
  type: z.enum(['DineIn', 'Takeaway', 'Delivery']),
  customerName: z.string().min(2, 'Tên khách hàng tối thiểu 2 ký tự'),
  customerPhone: z.string().optional(),
  tableNumber: z.string().optional(),
  deliveryAddress: z.string().optional(),
  items: z.array(orderItemSchema).min(1, 'Đơn hàng phải có ít nhất 1 món'),
});
export type OrderFormData = z.infer<typeof orderSchema>;

export const staffSchema = z.object({
  firstName: z.string().min(2, 'Họ tối thiểu 2 ký tự'),
  lastName: z.string().min(2, 'Tên tối thiểu 2 ký tự'),
  email: z.string().email('Email không đúng định dạng'),
  role: z.enum(['Admin', 'Manager', 'Cashier', 'Barista', 'InventoryStaff']),
  phone: z.string().regex(/^[0-9+\-\s()]{8,20}$/, 'Số điện thoại không hợp lệ'),
  shopId: z.string().min(1, 'Vui lòng chọn chi nhánh làm việc'),
});
export type StaffFormData = z.infer<typeof staffSchema>;
