import React, { useState, useEffect } from 'react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { Input } from '../components/common/Input';
import { Select } from '../components/common/Select';
import { api } from '../api/client';
import { Order, OrderItem, OrderStatus, OrderType, MenuItem, Shop } from '../types';
import { orderSchema, OrderFormData } from '../schemas';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Minus, Trash2, ShoppingCart, CheckCircle, Clock } from 'lucide-react';
import { useAppStore } from '../stores/appStore';

export const OrdersPage: React.FC = () => {
  const [orders, setOrders] = useState<Order[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [cartItems, setCartItems] = useState<OrderItem[]>([]);
  const { currentShopId, notify } = useAppStore();

  const {
    register,
    handleSubmit,
    setValue,
    reset,
    watch,
    formState: { errors, isSubmitting, isValid },
  } = useForm<OrderFormData>({
    resolver: zodResolver(orderSchema),
    mode: 'onChange',
    defaultValues: {
      shopId: currentShopId || '',
      type: 'DineIn',
      customerName: '',
      customerPhone: '',
      tableNumber: 'Bàn 01',
      deliveryAddress: '',
      items: [],
    },
  });

  const loadData = async (shopId?: string) => {
    setLoading(true);
    try {
      const [ordersData, menuData, shopsData] = await Promise.all([
        api.getOrders(shopId),
        api.getMenuItems(),
        api.getShops(),
      ]);
      setOrders(ordersData);
      setMenuItems(menuData);
      setShops(shopsData);
      const targetShopId = shopId || currentShopId || (shopsData.length > 0 ? shopsData[0].id : '');
      if (targetShopId) {
        setValue('shopId', targetShopId);
      }
    } catch (err: any) {
      notify('error', err.message || 'Không thể tải dữ liệu đơn hàng');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData(currentShopId);
    if (currentShopId) {
      setValue('shopId', currentShopId);
    }
  }, [currentShopId]);

  const selectedOrderType = watch('type');

  // Add item from menu to cart
  const addItemToCart = (menuItemId: string, name: string, unitPrice: number) => {
    setCartItems((prev) => {
      const existing = prev.find((item) => item.menuItemId === menuItemId);
      let updated: OrderItem[];
      if (existing) {
        updated = prev.map((item) =>
          item.menuItemId === menuItemId ? { ...item, quantity: item.quantity + 1 } : item
        );
      } else {
        updated = [...prev, { menuItemId, name, quantity: 1, unitPrice }];
      }
      setValue('items', updated, { shouldValidate: true });
      return updated;
    });
  };

  const updateItemQuantity = (menuItemId: string, delta: number) => {
    setCartItems((prev) => {
      const updated = prev
        .map((item) => {
          if (item.menuItemId === menuItemId) {
            const newQty = item.quantity + delta;
            return newQty > 0 ? { ...item, quantity: newQty } : null;
          }
          return item;
        })
        .filter(Boolean) as OrderItem[];
      setValue('items', updated, { shouldValidate: true });
      return updated;
    });
  };

  const removeItem = (menuItemId: string) => {
    setCartItems((prev) => {
      const updated = prev.filter((i) => i.menuItemId !== menuItemId);
      setValue('items', updated, { shouldValidate: true });
      return updated;
    });
  };

  const subTotal = cartItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  const tax = Math.round(subTotal * 0.08);
  const total = subTotal + tax;

  const onSubmitOrder = async (data: OrderFormData) => {
    try {
      const newOrder = await api.createOrder({
        shopId: data.shopId,
        type: data.type as OrderType,
        customerInfo: {
          name: data.customerName,
          phone: data.customerPhone,
          tableNumber: data.type === 'DineIn' ? data.tableNumber : undefined,
          deliveryAddress: data.type === 'Delivery' ? data.deliveryAddress : undefined,
        },
        items: cartItems,
      });

      setOrders((prev) => [newOrder, ...prev]);
      notify('success', `Tạo đơn hàng ${newOrder.orderNumber} thành công!`);
      setCartItems([]);
      reset({
        shopId: currentShopId,
        type: 'DineIn',
        customerName: '',
        customerPhone: '',
        tableNumber: 'Bàn 01',
        deliveryAddress: '',
        items: [],
      });
    } catch (err: any) {
      notify('error', err.message || 'Lỗi khi tạo đơn');
    }
  };

  const updateOrderStatus = (orderId: string, nextStatus: OrderStatus) => {
    setOrders((prev) =>
      prev.map((o) => (o.id === orderId ? { ...o, status: nextStatus } : o))
    );
    notify('info', `Đã cập nhật trạng thái đơn thành ${nextStatus}`);
  };

  const displayedOrders = currentShopId ? orders.filter((o) => o.shopId === currentShopId) : orders;

  return (
    <PageContainer
      title="Bán Hàng & Xử Lý Đơn Hàng (POS)"
      subtitle="Giao diện tạo đơn tại quầy thu ngân, tính tiền tự động và quản lý trạng thái pha chế"
    >
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 mb-12">
        {/* Left: Menu catalog selection (7 cols) */}
        <div className="lg:col-span-7 space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-black uppercase tracking-wider text-neutral-900">
              Chọn món từ Thực đơn
            </h2>
            <span className="text-xs text-neutral-500 font-medium">
              Nhấn để thêm vào đơn hàng
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-h-[520px] overflow-y-auto pr-1">
            {loading ? (
              <div className="col-span-2 p-8 text-center text-xs text-neutral-500">
                Đang tải thực đơn từ máy chủ...
              </div>
            ) : menuItems.length === 0 ? (
              <div className="col-span-2 p-8 text-center text-xs text-neutral-500">
                Chưa có món nào trong thực đơn.
              </div>
            ) : (
              menuItems.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() =>
                    item.availability.isAvailable &&
                    addItemToCart(item.id, item.name, item.price.amount)
                  }
                  disabled={!item.availability.isAvailable}
                  className={`p-3.5 text-left rounded border transition-all flex flex-col justify-between ${
                    item.availability.isAvailable
                      ? 'bg-white border-neutral-200 hover:border-cafe-orange hover:bg-orange-50/10 cursor-pointer'
                      : 'bg-neutral-100/70 border-neutral-200 opacity-60 cursor-not-allowed'
                  }`}
                >
                  <div>
                    <div className="flex items-start justify-between gap-1">
                      <span className="font-bold text-xs text-neutral-900 leading-tight block">
                        {item.name}
                      </span>
                      <Badge variant={item.availability.isAvailable ? 'neutral' : 'danger'} size="sm">
                        {item.category}
                      </Badge>
                    </div>
                    <p className="text-[11px] text-neutral-500 mt-1 line-clamp-1">{item.description}</p>
                  </div>
                  <div className="mt-3 flex items-center justify-between border-t border-neutral-100 pt-2">
                    <span className="font-mono font-bold text-xs text-cafe-orange">
                      {item.price.amount.toLocaleString('vi-VN')} ₫
                    </span>
                    <span className="text-[10px] font-bold text-neutral-600 bg-neutral-100 px-2 py-0.5 rounded border border-neutral-200">
                      + Thêm
                    </span>
                  </div>
                </button>
              ))
            )}
          </div>
        </div>

        {/* Right: POS Order Form & Cart Checkout (5 cols) */}
        <div className="lg:col-span-5">
          <Card
            title="Đơn hàng hiện tại"
            subtitle={`${cartItems.length} món được chọn`}
            noPadding
          >
            <form onSubmit={handleSubmit(onSubmitOrder)} className="p-5 space-y-4">
              {/* Order Type & Shop Selection */}
              <div className="grid grid-cols-2 gap-3">
                <Select
                  label="Loại phục vụ"
                  options={[
                    { value: 'DineIn', label: 'Tại quán' },
                    { value: 'Takeaway', label: 'Mang đi' },
                    { value: 'Delivery', label: 'Giao hàng' },
                  ]}
                  {...register('type')}
                />
                <Select
                  label="Chi nhánh"
                  options={shops.map((s) => ({ value: s.id, label: s.name }))}
                  {...register('shopId')}
                  error={errors.shopId?.message}
                />
              </div>

              {/* Customer Info */}
              <div className="grid grid-cols-2 gap-3">
                <Input
                  label="Tên khách hàng"
                  placeholder="Anh Tuấn"
                  required
                  {...register('customerName')}
                  error={errors.customerName?.message}
                />
                {selectedOrderType === 'DineIn' ? (
                  <Input
                    label="Số bàn"
                    placeholder="Bàn 04"
                    {...register('tableNumber')}
                  />
                ) : (
                  <Input
                    label="Số điện thoại"
                    placeholder="0901234567"
                    {...register('customerPhone')}
                  />
                )}
              </div>

              {selectedOrderType === 'Delivery' && (
                <Input
                  label="Địa chỉ giao nhận"
                  placeholder="Số 10 Nguyễn Huệ, P. Bến Nghé, Q.1"
                  required
                  {...register('deliveryAddress')}
                />
              )}

              {/* Cart Items List */}
              <div className="border border-neutral-200 rounded divide-y divide-neutral-200 max-h-48 overflow-y-auto bg-neutral-50/50">
                {cartItems.length === 0 ? (
                  <div className="p-6 text-center text-xs text-neutral-400">
                    <ShoppingCart className="w-6 h-6 mx-auto mb-1 text-neutral-300" />
                    Chưa chọn món nào từ thực đơn
                  </div>
                ) : (
                  cartItems.map((item) => (
                    <div key={item.menuItemId} className="p-2.5 flex items-center justify-between text-xs bg-white">
                      <div className="flex-1 pr-2">
                        <span className="font-bold text-neutral-900 block leading-tight">{item.name}</span>
                        <span className="font-mono text-[11px] text-neutral-500">
                          {item.unitPrice.toLocaleString('vi-VN')} ₫
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <button
                          type="button"
                          onClick={() => updateItemQuantity(item.menuItemId, -1)}
                          className="w-5 h-5 flex items-center justify-center rounded border border-neutral-300 bg-neutral-100 hover:bg-neutral-200 text-neutral-800"
                        >
                          <Minus className="w-3 h-3" />
                        </button>
                        <span className="font-mono font-bold w-5 text-center text-neutral-900">
                          {item.quantity}
                        </span>
                        <button
                          type="button"
                          onClick={() => updateItemQuantity(item.menuItemId, 1)}
                          className="w-5 h-5 flex items-center justify-center rounded border border-neutral-300 bg-neutral-100 hover:bg-neutral-200 text-neutral-800"
                        >
                          <Plus className="w-3 h-3" />
                        </button>
                        <button
                          type="button"
                          onClick={() => removeItem(item.menuItemId)}
                          className="ml-1 text-neutral-400 hover:text-red-600 p-1"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  ))
                )}
              </div>
              {errors.items && (
                <p className="text-xs font-semibold text-red-600 flex items-center gap-1">
                  <span>•</span> {errors.items.message}
                </p>
              )}

              {/* Bill totals breakdown */}
              <div className="p-3 bg-neutral-100 rounded border border-neutral-200 space-y-1.5 text-xs">
                <div className="flex justify-between text-neutral-600">
                  <span>Tạm tính tiền món:</span>
                  <span className="font-mono">{subTotal.toLocaleString('vi-VN')} ₫</span>
                </div>
                <div className="flex justify-between text-neutral-600">
                  <span>Thuế VAT (8%):</span>
                  <span className="font-mono">{tax.toLocaleString('vi-VN')} ₫</span>
                </div>
                <div className="flex justify-between text-neutral-900 font-bold border-t border-neutral-300 pt-1.5 text-sm">
                  <span>Tổng thanh toán:</span>
                  <span className="font-mono font-black text-cafe-orange text-base">
                    {total.toLocaleString('vi-VN')} ₫
                  </span>
                </div>
              </div>

              {/* Submit button */}
              <Button
                type="submit"
                variant="primary"
                className="w-full py-2.5 font-bold"
                isLoading={isSubmitting}
                disabled={cartItems.length === 0 || !isValid || isSubmitting}
              >
                Hoàn tất & In hóa đơn ({total.toLocaleString('vi-VN')} ₫)
              </Button>
            </form>
          </Card>
        </div>
      </div>

      {/* Orders List & Status Lifecycle */}
      <Card
        title="Danh Sách Đơn Hàng Vận Hành"
        subtitle="Theo dõi tiến độ chuẩn bị thức uống và xác nhận thanh toán hoàn tất"
        noPadding
      >
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50 text-[11px] font-bold uppercase tracking-wider text-neutral-600">
                <th className="py-3 px-4">Mã đơn</th>
                <th className="py-3 px-4">Khách hàng</th>
                <th className="py-3 px-4">Món đã gọi</th>
                <th className="py-3 px-4">Hình thức</th>
                <th className="py-3 px-4">Trạng thái</th>
                <th className="py-3 px-4 text-right">Tổng tiền</th>
                <th className="py-3 px-4 text-right">Chuyển trạng thái</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200 text-xs">
              {loading ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-neutral-500">
                    Đang tải danh sách đơn hàng từ máy chủ...
                  </td>
                </tr>
              ) : displayedOrders.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-neutral-500">
                    Chưa có đơn hàng nào tại chi nhánh này hôm nay.
                  </td>
                </tr>
              ) : (
                displayedOrders.map((o) => {
                  const statusBadge = {
                    Completed: <Badge variant="success" size="sm">Hoàn tất</Badge>,
                    Preparing: <Badge variant="warning" size="sm">Đang pha chế</Badge>,
                    Pending: <Badge variant="neutral" size="sm">Chờ xử lý</Badge>,
                    Confirmed: <Badge variant="orange" size="sm">Đã nhận đơn</Badge>,
                    Ready: <Badge variant="success" size="sm">Sẵn sàng giao</Badge>,
                    Cancelled: <Badge variant="danger" size="sm">Đã hủy</Badge>,
                  }[o.status];

                  return (
                    <tr key={o.id} className="hover:bg-neutral-50 transition-colors">
                      <td className="py-3 px-4 font-mono font-bold text-neutral-900">{o.orderNumber}</td>
                      <td className="py-3 px-4 font-medium text-neutral-800">
                        {o.customerInfo.name}
                        {o.customerInfo.tableNumber && (
                          <span className="text-neutral-500 ml-1 font-mono">({o.customerInfo.tableNumber})</span>
                        )}
                      </td>
                      <td className="py-3 px-4 text-neutral-600 max-w-xs truncate">
                        {o.items.map((i) => `${i.name} (x${i.quantity})`).join(', ')}
                      </td>
                      <td className="py-3 px-4">
                        <Badge variant="neutral" size="sm">
                          {o.type === 'DineIn' ? 'Tại quán' : o.type === 'Takeaway' ? 'Mang đi' : 'Giao hàng'}
                        </Badge>
                      </td>
                      <td className="py-3 px-4">{statusBadge}</td>
                      <td className="py-3 px-4 text-right font-mono font-bold text-neutral-900">
                        {o.total.toLocaleString('vi-VN')} ₫
                      </td>
                      <td className="py-3 px-4 text-right">
                        {o.status === 'Pending' && (
                          <Button
                            variant="secondary"
                            size="sm"
                            icon={<Clock className="w-3 h-3" />}
                            onClick={() => updateOrderStatus(o.id, 'Preparing')}
                          >
                            Pha chế
                          </Button>
                        )}
                        {o.status === 'Preparing' && (
                          <Button
                            variant="primary"
                            size="sm"
                            icon={<CheckCircle className="w-3 h-3" />}
                            onClick={() => updateOrderStatus(o.id, 'Completed')}
                          >
                            Hoàn tất
                          </Button>
                        )}
                        {o.status === 'Completed' && (
                          <span className="text-neutral-400 font-mono text-[11px]">-</span>
                        )}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </PageContainer>
  );
};
