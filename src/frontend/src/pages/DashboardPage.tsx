import React, { useState, useEffect } from 'react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { api } from '../api/client';
import { Order, InventoryItem, Shop } from '../types';
import { DollarSign, ShoppingBag, AlertTriangle, Store, Plus, ArrowRight, RefreshCw } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useAppStore } from '../stores/appStore';

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { currentShopId } = useAppStore();
  const [orders, setOrders] = useState<Order[]>([]);
  const [inventory, setInventory] = useState<InventoryItem[]>([]);
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async (shopId?: string) => {
    setLoading(true);
    try {
      const [ordersData, invData, shopsData] = await Promise.all([
        api.getOrders(shopId),
        api.getInventory(shopId),
        api.getShops(),
      ]);
      setOrders(ordersData);
      setInventory(invData);
      setShops(shopsData);
    } catch (err) {
      console.error('Lỗi tải dữ liệu dashboard:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData(currentShopId);
  }, [currentShopId]);

  const currentShop = shops.find((s) => s.id === currentShopId);
  const currentOrders = currentShopId
    ? orders.filter((o) => o.shopId === currentShopId)
    : orders;
  const currentInventory = currentShopId
    ? inventory.filter((i) => i.shopId === currentShopId)
    : inventory;

  const completedOrders = currentOrders.filter((o) => o.status === 'Completed');
  const totalCompletedRevenue = completedOrders.reduce((sum, o) => sum + o.total, 0);

  const lowStockCount = currentInventory.filter((i) => i.currentStock <= i.reorderLevel).length;
  const activeShopsCount = shops.filter((s) => s.status === 'Active').length;

  return (
    <PageContainer
      title="Bảng Điều Khiển Tổng Quan"
      subtitle={
        currentShop
          ? `Theo dõi hiệu suất vận hành tại chi nhánh: ${currentShop.name}`
          : 'Theo dõi hiệu suất vận hành chuỗi cà phê, đơn hàng tại quầy và tồn kho thời gian thực'
      }
      actions={
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            icon={<RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />}
            onClick={() => loadData(currentShopId)}
          >
            Làm mới
          </Button>
          <Button
            variant="primary"
            size="sm"
            icon={<Plus className="w-4 h-4" />}
            onClick={() => navigate('/orders')}
          >
            Tạo đơn bán hàng
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/inventory')}
          >
            Kiểm kê kho
          </Button>
        </div>
      }
    >
      {/* 4 Metric KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-8">
        <Card noPadding className="p-5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">Doanh thu hoàn tất</span>
            <div className="w-8 h-8 rounded bg-orange-50 border border-orange-200 flex items-center justify-center text-cafe-orange">
              <DollarSign className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-neutral-900 tracking-tight font-mono">
              {loading ? '...' : `${totalCompletedRevenue.toLocaleString('vi-VN')} ₫`}
            </span>
            <span className="block text-[11px] text-neutral-500 mt-1">
              Từ {completedOrders.length} / {currentOrders.length} đơn hàng đã hoàn tất
            </span>
          </div>
        </Card>

        <Card noPadding className="p-5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">Đơn hàng trong ngày</span>
            <div className="w-8 h-8 rounded bg-neutral-100 border border-neutral-300 flex items-center justify-center text-neutral-800">
              <ShoppingBag className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-neutral-900 tracking-tight font-mono">
              {loading ? '...' : currentOrders.length}
            </span>
            <span className="block text-[11px] text-neutral-500 mt-1">
              {currentOrders.filter((o) => o.status === 'Preparing').length} đang pha chế, {currentOrders.filter((o) => o.status === 'Confirmed' || o.status === 'Pending').length} chờ thực hiện
            </span>
          </div>
        </Card>

        <Card noPadding className="p-5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">Cảnh báo nguyên liệu</span>
            <div className="w-8 h-8 rounded bg-red-50 border border-red-200 flex items-center justify-center text-red-600">
              <AlertTriangle className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-red-600 tracking-tight font-mono">
              {loading ? '...' : lowStockCount}
            </span>
            <span className="block text-[11px] text-neutral-500 mt-1">Mặt hàng dưới định mức an toàn</span>
          </div>
        </Card>

        <Card noPadding className="p-5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">Chi nhánh hoạt động</span>
            <div className="w-8 h-8 rounded bg-neutral-100 border border-neutral-300 flex items-center justify-center text-neutral-800">
              <Store className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-neutral-900 tracking-tight font-mono">
              {loading ? '...' : `${activeShopsCount} / ${shops.length}`}
            </span>
            <span className="block text-[11px] text-neutral-500 mt-1 truncate" title={currentShop?.name}>
              Đang chọn: {currentShop?.name || 'Toàn hệ thống'}
            </span>
          </div>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent Orders Table */}
        <div className="lg:col-span-2">
          <Card
            title="Đơn Hàng Gần Đây"
            subtitle="Cập nhật trực tiếp từ quầy thu ngân và bàn phục vụ"
            action={
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate('/orders')}
                icon={<ArrowRight className="w-3.5 h-3.5" />}
              >
                Xem tất cả
              </Button>
            }
            noPadding
          >
            {loading ? (
              <div className="p-6 text-center text-xs text-neutral-500">Đang tải dữ liệu từ máy chủ...</div>
            ) : currentOrders.length === 0 ? (
              <div className="p-8 text-center text-xs text-neutral-500">
                Chưa có đơn hàng nào tại chi nhánh này hôm nay.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs text-neutral-700">
                  <thead className="bg-neutral-50 text-[11px] font-bold uppercase tracking-wider text-neutral-600 border-b border-neutral-200">
                    <tr>
                      <th className="py-3 px-4">Mã đơn</th>
                      <th className="py-3 px-4">Khách hàng</th>
                      <th className="py-3 px-4">Loại đơn</th>
                      <th className="py-3 px-4">Trạng thái</th>
                      <th className="py-3 px-4 text-right">Tổng tiền</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-200">
                    {currentOrders.slice(0, 5).map((order) => {
                      const typeBadge = {
                        DineIn: <Badge variant="neutral">Tại quán</Badge>,
                        Takeaway: <Badge variant="orange">Mang đi</Badge>,
                        Delivery: <Badge variant="black">Giao hàng</Badge>,
                      }[order.type];

                      const statusBadge = {
                        Completed: <Badge variant="success">Hoàn tất</Badge>,
                        Preparing: <Badge variant="warning">Đang pha chế</Badge>,
                        Pending: <Badge variant="neutral">Chờ xác nhận</Badge>,
                        Confirmed: <Badge variant="orange">Đã nhận đơn</Badge>,
                        Ready: <Badge variant="success">Sẵn sàng</Badge>,
                        Cancelled: <Badge variant="danger">Đã hủy</Badge>,
                      }[order.status] || <Badge>{order.status}</Badge>;

                      return (
                        <tr key={order.id} className="hover:bg-neutral-50/80 transition-colors">
                          <td className="py-3 px-4 font-mono font-bold text-neutral-900">
                            {order.orderNumber}
                          </td>
                          <td className="py-3 px-4">
                            <span className="font-semibold text-neutral-900 block">
                              {order.customerInfo.name}
                            </span>
                            {order.customerInfo.tableNumber && (
                              <span className="text-[10px] text-neutral-500">
                                ({order.customerInfo.tableNumber})
                              </span>
                            )}
                          </td>
                          <td className="py-3 px-4">{typeBadge}</td>
                          <td className="py-3 px-4">{statusBadge}</td>
                          <td className="py-3 px-4 text-right font-mono font-bold text-neutral-900">
                            {order.total.toLocaleString('vi-VN')} ₫
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </Card>
        </div>

        {/* Low Stock Alert Side Card */}
        <div>
          <Card
            title="Nguyên Liệu Cần Nhập Thêm"
            subtitle="Mức tồn kho chạm ngưỡng cảnh báo tối thiểu"
            action={
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate('/inventory')}
                icon={<ArrowRight className="w-3.5 h-3.5" />}
              >
                Vào kho
              </Button>
            }
          >
            <div className="space-y-3">
              {currentInventory
                .filter((item) => item.currentStock <= item.reorderLevel)
                .slice(0, 4)
                .map((item) => (
                  <div
                    key={item.id}
                    className="p-3 border border-red-200 bg-red-50/30 rounded flex items-center justify-between"
                  >
                    <div>
                      <h4 className="text-xs font-bold text-neutral-900">{item.itemName}</h4>
                      <span className="text-[10px] text-neutral-500 block mt-0.5">
                        Định mức: {item.reorderLevel} {item.unit}
                      </span>
                    </div>
                    <div className="text-right">
                      <span className="text-xs font-black font-mono text-red-600 block">
                        Còn {item.currentStock} {item.unit}
                      </span>
                      <Badge variant="danger" size="sm">
                        Cần nhập
                      </Badge>
                    </div>
                  </div>
                ))}

              {currentInventory.filter((item) => item.currentStock <= item.reorderLevel).length === 0 && (
                <div className="py-8 text-center text-xs text-neutral-500">
                  Kho hàng tại chi nhánh này đang ổn định, chưa có mặt hàng nào cần nhập thêm.
                </div>
              )}
            </div>
          </Card>
        </div>
      </div>
    </PageContainer>
  );
};
