export type StaffRole = 'Admin' | 'Manager' | 'Cashier' | 'Barista' | 'InventoryStaff';
export type EmploymentStatus = 'Active' | 'OnLeave' | 'Terminated';
export type ShopStatus = 'Active' | 'UnderMaintenance' | 'Closed';
export type OrderStatus = 'Pending' | 'Confirmed' | 'Preparing' | 'Ready' | 'Completed' | 'Cancelled';
export type OrderType = 'DineIn' | 'Takeaway' | 'Delivery';

export interface Address {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface ContactInfo {
  phone: string;
  email: string;
}

export interface OperatingHours {
  dayOfWeek: number; // 0=Sunday, 1=Monday, etc.
  openTime: string; // HH:mm
  closeTime: string; // HH:mm
  isClosed: boolean;
}

export interface Shop {
  id: string;
  name: string;
  address: Address;
  contact: ContactInfo;
  status: ShopStatus;
  operatingHours: OperatingHours[];
}

export interface Money {
  amount: number;
  currency: string;
}

export interface Availability {
  isAvailable: boolean;
  reason?: string;
}

export interface MenuItem {
  id: string;
  name: string;
  description: string;
  category: string;
  price: Money;
  ingredients: string[];
  allergens: string[];
  availability: Availability;
}

export interface InventoryItem {
  id: string;
  shopId: string;
  itemName: string;
  unit: string;
  currentStock: number;
  reorderLevel: number;
  supplierId?: string;
  lastRestockedAt?: string;
}

export interface OrderItem {
  menuItemId: string;
  name: string;
  quantity: number;
  unitPrice: number;
  specialInstructions?: string;
}

export interface CustomerInfo {
  name: string;
  phone?: string;
  tableNumber?: string;
  deliveryAddress?: string;
}

export interface Order {
  id: string;
  shopId: string;
  orderNumber: string;
  type: OrderType;
  status: OrderStatus;
  customerInfo: CustomerInfo;
  items: OrderItem[];
  subTotal: number;
  tax: number;
  total: number;
  createdAt: string;
}

export interface Staff {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: StaffRole;
  shopId: string;
  status: EmploymentStatus;
  contact: ContactInfo;
}

export type StaffMember = Staff;
export type UserRole = StaffRole;

export interface SystemMetrics {
  status: string;
  uptime: string;
  avgResponseTimeMs: number;
  totalRequests: number;
  errorRatePercent: number;
  cpuUsagePercent: number;
  memoryUsageMb: number;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  shopId?: string;
  roles: string[];
}

export interface MetricSnapshot {
  name: string;
  value: number;
  tags?: Record<string, string>;
  timestamp: string;
}

export type ShiftStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export interface Shift {
  id: string;
  staffId: string;
  shopId: string;
  startTime: string;
  endTime: string;
  status: ShiftStatus;
  notes?: string;
}

export interface ReviewItem {
  menuItemId: string;
  menuItemName: string;
  rating: number;
  comment?: string;
}

export interface CustomerReview {
  id: string;
  shopId: string;
  shopName: string;
  orderId?: string;
  customerName: string;
  customerPhone: string;
  shopRating: number;
  shopComment: string;
  itemReviews: ReviewItem[];
  tags: string[];
  createdAt: string;
}

export interface ReviewStats {
  totalReviews: number;
  averageShopRating: number;
  satisfactionRate: number;
  ratingDistribution: Record<string, number>;
  topProducts: Array<{
    menuItemId: string;
    menuItemName: string;
    count: number;
    averageRating: number;
  }>;
  topTags: Array<{
    tag: string;
    count: number;
  }>;
}

