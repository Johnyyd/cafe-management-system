import React, { useState, useEffect, useMemo } from 'react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { api } from '../api/client';
import { Order, InventoryItem, Shop } from '../types';
import {
  DollarSign,
  ShoppingBag,
  AlertTriangle,
  Store,
  Plus,
  ArrowRight,
  RefreshCw,
  Calendar,
  BarChart2,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useAppStore } from '../stores/appStore';

type Timeframe = '1d' | '1w' | '1m' | '3m' | '6m' | '1y' | 'all';

interface TimeframeConfig {
  key: Timeframe;
  label: string;
  orderTitle: string;
  days: number;
}

const TIMEFRAME_CONFIGS: TimeframeConfig[] = [
  { key: '1d', label: '1 Ngày', orderTitle: 'Đơn hàng hôm nay', days: 1 },
  { key: '1w', label: '1 Tuần', orderTitle: 'Đơn hàng 1 tuần qua', days: 7 },
  { key: '1m', label: '1 Tháng', orderTitle: 'Đơn hàng 1 tháng qua', days: 30 },
  { key: '3m', label: '3 Tháng', orderTitle: 'Đơn hàng 3 tháng qua', days: 90 },
  { key: '6m', label: '6 Tháng', orderTitle: 'Đơn hàng 6 tháng qua', days: 180 },
  { key: '1y', label: '1 Năm', orderTitle: 'Đơn hàng 1 năm qua', days: 365 },
  { key: 'all', label: 'Tất cả', orderTitle: 'Tổng số đơn hàng', days: 0 },
];

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { currentShopId } = useAppStore();
  const [orders, setOrders] = useState<Order[]>([]);
  const [inventory, setInventory] = useState<InventoryItem[]>([]);
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState(true);
  const [timeframe, setTimeframe] = useState<Timeframe>('1m');

  const loadData = async (shopId?: string) => {
    setLoading(true);
    try {
      const [ordersData, invData, shopsData] = await Promise.all([
        api.getOrders(shopId, undefined, undefined, 2000),
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

  // Shop-scoped inventory
  const currentInventory = currentShopId
    ? inventory.filter((i) => i.shopId === currentShopId)
    : inventory;

  // Shop-scoped all orders
  const currentShopOrders = useMemo(() => {
    return currentShopId ? orders.filter((o) => o.shopId === currentShopId) : orders;
  }, [currentShopId, orders]);

  // Orders filtered by the selected timeframe
  const filteredOrders = useMemo(() => {
    if (timeframe === 'all') return currentShopOrders;

    const now = new Date();
    const cutoff = new Date();

    if (timeframe === '1d') {
      cutoff.setHours(0, 0, 0, 0);
    } else {
      const cfg = TIMEFRAME_CONFIGS.find((c) => c.key === timeframe);
      const days = cfg ? cfg.days : 30;
      cutoff.setDate(now.getDate() - days);
    }

    return currentShopOrders.filter((o) => {
      const d = new Date(o.createdAt);
      return !isNaN(d.getTime()) && d >= cutoff;
    });
  }, [currentShopOrders, timeframe]);

  // Completed orders in timeframe
  const completedOrders = useMemo(() => {
    return filteredOrders.filter((o) => o.status === 'Completed');
  }, [filteredOrders]);

  const totalCompletedRevenue = useMemo(() => {
    return completedOrders.reduce((sum, o) => sum + o.total, 0);
  }, [completedOrders]);

  const avgOrderValue = useMemo(() => {
    return completedOrders.length > 0
      ? Math.round(totalCompletedRevenue / completedOrders.length)
      : 0;
  }, [completedOrders, totalCompletedRevenue]);

  const preparingCount = filteredOrders.filter((o) => o.status === 'Preparing').length;
  const pendingCount = filteredOrders.filter(
    (o) => o.status === 'Confirmed' || o.status === 'Pending'
  ).length;

  const lowStockCount = currentInventory.filter((i) => i.currentStock <= i.reorderLevel).length;
  const activeShopsCount = shops.filter((s) => s.status === 'Active').length;

  const currentConfig = TIMEFRAME_CONFIGS.find((c) => c.key === timeframe) || TIMEFRAME_CONFIGS[2];

  // Date Range Display label
  const dateRangeLabel = useMemo(() => {
    const now = new Date();
    const formatD = (d: Date) =>
      `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1)
        .toString()
        .padStart(2, '0')}/${d.getFullYear()}`;

    if (timeframe === '1d') {
      return `Hôm nay, ngày ${formatD(now)}`;
    }
    if (timeframe === 'all') {
      return 'Toàn bộ lịch sử vận hành hệ thống';
    }
    const past = new Date();
    past.setDate(now.getDate() - currentConfig.days);
    return `${formatD(past)} — ${formatD(now)} (${currentConfig.days} ngày)`;
  }, [timeframe, currentConfig]);

  // Channel breakdown (DineIn, Takeaway, Delivery)
  const channelBreakdown = useMemo(() => {
    const total = filteredOrders.length || 1;
    const dineIn = filteredOrders.filter((o) => o.type === 'DineIn');
    const takeaway = filteredOrders.filter((o) => o.type === 'Takeaway');
    const delivery = filteredOrders.filter((o) => o.type === 'Delivery');

    return {
      dineIn: {
        count: dineIn.length,
        percent: Math.round((dineIn.length / total) * 100),
        revenue: dineIn.reduce((sum, o) => sum + (o.status === 'Completed' ? o.total : 0), 0),
      },
      takeaway: {
        count: takeaway.length,
        percent: Math.round((takeaway.length / total) * 100),
        revenue: takeaway.reduce((sum, o) => sum + (o.status === 'Completed' ? o.total : 0), 0),
      },
      delivery: {
        count: delivery.length,
        percent: Math.round((delivery.length / total) * 100),
        revenue: delivery.reduce((sum, o) => sum + (o.status === 'Completed' ? o.total : 0), 0),
      },
    };
  }, [filteredOrders]);

  // Top selling menu items in the period
  const topSellingItems = useMemo(() => {
    const map: Record<string, { name: string; quantity: number; revenue: number }> = {};
    completedOrders.forEach((o) => {
      o.items.forEach((item) => {
        if (!map[item.name]) {
          map[item.name] = { name: item.name, quantity: 0, revenue: 0 };
        }
        map[item.name].quantity += item.quantity;
        map[item.name].revenue += item.unitPrice * item.quantity;
      });
    });
    return Object.values(map)
      .sort((a, b) => b.revenue - a.revenue)
      .slice(0, 4);
  }, [completedOrders]);

  // Timeline / Trend Bar Chart data
  const trendBuckets = useMemo(() => {
    const now = new Date();
    interface Bucket {
      label: string;
      revenue: number;
      ordersCount: number;
    }

    if (timeframe === '1d') {
      // 8 slots of 2 hours: 06-08, 08-10, 10-12, 12-14, 14-16, 16-18, 18-20, 20-22
      const slots: Bucket[] = [
        { label: '06-08h', revenue: 0, ordersCount: 0 },
        { label: '08-10h', revenue: 0, ordersCount: 0 },
        { label: '10-12h', revenue: 0, ordersCount: 0 },
        { label: '12-14h', revenue: 0, ordersCount: 0 },
        { label: '14-16h', revenue: 0, ordersCount: 0 },
        { label: '16-18h', revenue: 0, ordersCount: 0 },
        { label: '18-20h', revenue: 0, ordersCount: 0 },
        { label: '20-22h', revenue: 0, ordersCount: 0 },
      ];
      filteredOrders.forEach((o) => {
        const h = new Date(o.createdAt).getHours();
        let idx = Math.floor((h - 6) / 2);
        if (idx < 0) idx = 0;
        if (idx > 7) idx = 7;
        slots[idx].ordersCount += 1;
        if (o.status === 'Completed') {
          slots[idx].revenue += o.total;
        }
      });
      return slots;
    }

    if (timeframe === '1w') {
      // 7 days (today down to 6 days ago)
      const dayBuckets: Bucket[] = [];
      const weekdays = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
      for (let i = 6; i >= 0; i--) {
        const d = new Date(now);
        d.setDate(now.getDate() - i);
        const dayLabel = i === 0 ? 'Hôm nay' : i === 1 ? 'Hôm qua' : weekdays[d.getDay()];
        dayBuckets.push({
          label: `${dayLabel} (${d.getDate()}/${d.getMonth() + 1})`,
          revenue: 0,
          ordersCount: 0,
        });
      }
      filteredOrders.forEach((o) => {
        const d = new Date(o.createdAt);
        const diffDays = Math.floor((now.getTime() - d.getTime()) / (1000 * 60 * 60 * 24));
        const idx = 6 - diffDays;
        if (idx >= 0 && idx < 7) {
          dayBuckets[idx].ordersCount += 1;
          if (o.status === 'Completed') {
            dayBuckets[idx].revenue += o.total;
          }
        }
      });
      return dayBuckets;
    }

    if (timeframe === '1m') {
      // 4 weeks (Tuần 1 -> Tuần 4)
      const weekBuckets: Bucket[] = [
        { label: '3-4 tuần trước', revenue: 0, ordersCount: 0 },
        { label: '2-3 tuần trước', revenue: 0, ordersCount: 0 },
        { label: 'Tuần trước', revenue: 0, ordersCount: 0 },
        { label: 'Tuần này', revenue: 0, ordersCount: 0 },
      ];
      filteredOrders.forEach((o) => {
        const d = new Date(o.createdAt);
        const diffDays = Math.floor((now.getTime() - d.getTime()) / (1000 * 60 * 60 * 24));
        let idx = 3 - Math.floor(diffDays / 7.5);
        if (idx < 0) idx = 0;
        if (idx > 3) idx = 3;
        weekBuckets[idx].ordersCount += 1;
        if (o.status === 'Completed') {
          weekBuckets[idx].revenue += o.total;
        }
      });
      return weekBuckets;
    }

    // 3m, 6m, 1y, or all -> Month by Month buckets
    const numMonths = timeframe === '3m' ? 3 : timeframe === '6m' ? 6 : 12;
    const monthBuckets: Bucket[] = [];
    for (let i = numMonths - 1; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      monthBuckets.push({
        label: `Tháng ${(d.getMonth() + 1).toString().padStart(2, '0')}/${d.getFullYear() % 100}`,
        revenue: 0,
        ordersCount: 0,
      });
    }

    filteredOrders.forEach((o) => {
      const d = new Date(o.createdAt);
      const diffMonths =
        (now.getFullYear() - d.getFullYear()) * 12 + (now.getMonth() - d.getMonth());
      const idx = numMonths - 1 - diffMonths;
      if (idx >= 0 && idx < numMonths) {
        monthBuckets[idx].ordersCount += 1;
        if (o.status === 'Completed') {
          monthBuckets[idx].revenue += o.total;
        }
      }
    });

    return monthBuckets;
  }, [timeframe, filteredOrders]);

  const maxBucketRevenue = useMemo(() => {
    return Math.max(...trendBuckets.map((b) => b.revenue), 100000);
  }, [trendBuckets]);

  return (
    <PageContainer
      title="Bảng Điều Khiển Tổng Quan"
      subtitle={
        currentShop
          ? `Hiệu suất kinh doanh và vận hành chi nhánh: ${currentShop.name}`
          : 'Hiệu suất kinh doanh chuỗi cà phê, đơn hàng đa kênh và cảnh báo tồn kho toàn hệ thống'
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
          <Button variant="outline" size="sm" onClick={() => navigate('/inventory')}>
            Kiểm kê kho
          </Button>
        </div>
      }
    >
      {/* Timeframe Filter Bar */}
      <div className="bg-white rounded-lg border border-neutral-200/90 shadow-sm p-3.5 mb-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-md bg-orange-50 border border-orange-200 flex items-center justify-center text-cafe-orange">
            <Calendar className="w-4 h-4" />
          </div>
          <div>
            <span className="text-[11px] font-bold text-neutral-500 uppercase tracking-wider block">
              Kỳ thống kê dữ liệu
            </span>
            <div className="flex items-center gap-2 mt-0.5">
              <span className="text-xs font-semibold text-neutral-900 font-mono">
                {dateRangeLabel}
              </span>
              <span className="text-[11px] bg-neutral-100 text-neutral-600 px-2 py-0.5 rounded font-mono font-bold border border-neutral-200">
                {filteredOrders.length} đơn hàng
              </span>
            </div>
          </div>
        </div>

        {/* Segmented Button Group for Timeframe Selection */}
        <div className="flex items-center gap-1 bg-neutral-100 p-1 rounded-lg border border-neutral-200 overflow-x-auto max-w-full">
          {TIMEFRAME_CONFIGS.map((cfg) => {
            const isActive = timeframe === cfg.key;
            return (
              <button
                key={cfg.key}
                type="button"
                onClick={() => setTimeframe(cfg.key)}
                className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-all whitespace-nowrap ${
                  isActive
                    ? 'bg-cafe-orange text-white shadow-sm font-bold scale-[1.02]'
                    : 'text-neutral-600 hover:text-neutral-900 hover:bg-neutral-200/70'
                }`}
              >
                {cfg.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* 4 Metric KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-8">
        {/* Metric 1: Revenue */}
        <Card noPadding className="p-5 relative overflow-hidden group hover:border-cafe-orange/50 transition-all">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
              Doanh thu hoàn tất
            </span>
            <div className="w-8 h-8 rounded bg-orange-50 border border-orange-200 flex items-center justify-center text-cafe-orange group-hover:scale-105 transition-transform">
              <DollarSign className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-neutral-900 tracking-tight font-mono block">
              {loading ? '...' : `${totalCompletedRevenue.toLocaleString('vi-VN')} ₫`}
            </span>
            <div className="flex items-center justify-between mt-1 pt-1 border-t border-neutral-100 text-[11px] text-neutral-500">
              <span>
                Từ <strong className="text-neutral-700">{completedOrders.length}</strong> / {filteredOrders.length} đơn
              </span>
              <span className="font-mono text-cafe-orange font-bold" title="Giá trị trung bình mỗi đơn hoàn tất">
                AOV: {avgOrderValue.toLocaleString('vi-VN')} ₫
              </span>
            </div>
          </div>
        </Card>

        {/* Metric 2: Orders Count */}
        <Card noPadding className="p-5 relative overflow-hidden group hover:border-neutral-400 transition-all">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500 truncate" title={currentConfig.orderTitle}>
              {currentConfig.orderTitle}
            </span>
            <div className="w-8 h-8 rounded bg-neutral-100 border border-neutral-300 flex items-center justify-center text-neutral-800 group-hover:scale-105 transition-transform">
              <ShoppingBag className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-neutral-900 tracking-tight font-mono block">
              {loading ? '...' : filteredOrders.length}
            </span>
            <div className="mt-1 pt-1 border-t border-neutral-100 text-[11px] text-neutral-500 truncate">
              {timeframe === '1d' ? (
                <span>
                  <strong className="text-amber-600">{preparingCount}</strong> đang pha, <strong className="text-neutral-700">{pendingCount}</strong> chờ xử lý
                </span>
              ) : (
                <span>
                  <strong className="text-emerald-600">{completedOrders.length}</strong> hoàn tất ({filteredOrders.length > 0 ? Math.round((completedOrders.length / filteredOrders.length) * 100) : 0}%)
                </span>
              )}
            </div>
          </div>
        </Card>

        {/* Metric 3: Low Stock Warning */}
        <Card noPadding className="p-5 relative overflow-hidden group hover:border-red-300 transition-all">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
              Cảnh báo nguyên liệu
            </span>
            <div className="w-8 h-8 rounded bg-red-50 border border-red-200 flex items-center justify-center text-red-600 group-hover:scale-105 transition-transform">
              <AlertTriangle className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-red-600 tracking-tight font-mono block">
              {loading ? '...' : lowStockCount}
            </span>
            <div className="mt-1 pt-1 border-t border-neutral-100 text-[11px] text-neutral-500 truncate">
              {lowStockCount > 0 ? (
                <span className="text-red-600 font-semibold">Cần bổ sung kho chi nhánh</span>
              ) : (
                <span className="text-emerald-600 font-medium">Tồn kho định mức an toàn</span>
              )}
            </div>
          </div>
        </Card>

        {/* Metric 4: Active Shops */}
        <Card noPadding className="p-5 relative overflow-hidden group hover:border-neutral-400 transition-all">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
              Chi nhánh hoạt động
            </span>
            <div className="w-8 h-8 rounded bg-neutral-100 border border-neutral-300 flex items-center justify-center text-neutral-800 group-hover:scale-105 transition-transform">
              <Store className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <span className="text-2xl font-black text-neutral-900 tracking-tight font-mono block">
              {loading ? '...' : `${activeShopsCount} / ${shops.length}`}
            </span>
            <div className="mt-1 pt-1 border-t border-neutral-100 text-[11px] text-neutral-500 truncate" title={currentShop?.name}>
              {currentShop ? currentShop.name : 'Đang xem: Toàn hệ thống (9 quán)'}
            </div>
          </div>
        </Card>
      </div>

      {/* Visual Analytics & Trend Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
        {/* Left 2 Cols: Revenue Trend Bar Chart */}
        <div className="lg:col-span-2">
          <Card
            title="Biểu Đồ Doanh Thu & Xu Hướng"
            subtitle={`Diễn biến doanh thu thực thu theo từng mốc thời gian (${currentConfig.label})`}
            action={
              <div className="flex items-center gap-1.5 text-xs text-neutral-500">
                <BarChart2 className="w-4 h-4 text-cafe-orange" />
                <span className="font-mono font-bold text-neutral-800">
                  {totalCompletedRevenue.toLocaleString('vi-VN')} ₫
                </span>
              </div>
            }
          >
            {loading ? (
              <div className="py-16 text-center text-xs text-neutral-500">Đang cập nhật biểu đồ...</div>
            ) : trendBuckets.length === 0 || totalCompletedRevenue === 0 ? (
              <div className="py-16 text-center text-xs text-neutral-500">
                Chưa có dữ liệu doanh thu trong khoảng thời gian này.
              </div>
            ) : (
              <div>
                {/* Visual Bars Container */}
                <div className="h-56 flex items-end gap-2 pt-6 pb-2 px-1 border-b border-neutral-200">
                  {trendBuckets.map((bucket, index) => {
                    const heightPercent = Math.max(
                      Math.round((bucket.revenue / maxBucketRevenue) * 100),
                      bucket.revenue > 0 ? 8 : 2
                    );
                    return (
                      <div
                        key={index}
                        className="flex-1 flex flex-col items-center h-full justify-end group relative"
                      >
                        {/* Hover Tooltip */}
                        <div className="absolute -top-12 z-20 pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity bg-neutral-900 text-white text-[10px] px-2.5 py-1.5 rounded shadow-lg whitespace-nowrap text-center font-mono">
                          <span className="font-bold block text-orange-300">
                            {bucket.revenue.toLocaleString('vi-VN')} ₫
                          </span>
                          <span className="text-neutral-300 text-[9px]">
                            {bucket.ordersCount} đơn hàng
                          </span>
                        </div>

                        {/* Bar */}
                        <div
                          style={{ height: `${heightPercent}%` }}
                          className={`w-full max-w-[48px] rounded-t transition-all duration-300 ${
                            bucket.revenue > 0
                              ? 'bg-gradient-to-t from-orange-600 to-orange-400 group-hover:from-orange-500 group-hover:to-orange-300 shadow-sm'
                              : 'bg-neutral-200/60'
                          }`}
                        />
                      </div>
                    );
                  })}
                </div>

                {/* X-Axis Labels */}
                <div className="flex items-center gap-2 px-1 pt-2">
                  {trendBuckets.map((bucket, index) => (
                    <div
                      key={index}
                      className="flex-1 text-center text-[10px] font-medium text-neutral-600 truncate"
                      title={bucket.label}
                    >
                      {bucket.label}
                    </div>
                  ))}
                </div>

                {/* Bottom Chart Footer Legend */}
                <div className="mt-4 pt-3 border-t border-neutral-100 flex flex-wrap items-center justify-between text-xs text-neutral-500">
                  <div className="flex items-center gap-4">
                    <span className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-sm bg-cafe-orange inline-block" />
                      Doanh thu hoàn tất
                    </span>
                    <span className="flex items-center gap-1.5">
                      <span className="w-2.5 h-2.5 rounded-sm bg-neutral-200 inline-block" />
                      Không phát sinh
                    </span>
                  </div>
                  <div className="text-[11px] text-neutral-400">
                    Đỉnh kỳ: <strong className="text-neutral-700 font-mono">{maxBucketRevenue.toLocaleString('vi-VN')} ₫</strong>
                  </div>
                </div>
              </div>
            )}
          </Card>
        </div>

        {/* Right 1 Col: Channel Breakdown & Top Products */}
        <div className="space-y-6">
          {/* Channel Share Card */}
          <Card
            title="Kênh Phục Vụ"
            subtitle={`Phân bổ ${filteredOrders.length} đơn theo hình thức`}
          >
            <div className="space-y-3.5">
              {/* Stacked Progress Bar */}
              <div className="h-3 w-full bg-neutral-100 rounded-full overflow-hidden flex">
                <div
                  style={{ width: `${channelBreakdown.dineIn.percent}%` }}
                  className="bg-neutral-800 transition-all duration-500"
                  title={`Tại quán: ${channelBreakdown.dineIn.percent}%`}
                />
                <div
                  style={{ width: `${channelBreakdown.takeaway.percent}%` }}
                  className="bg-cafe-orange transition-all duration-500"
                  title={`Mang đi: ${channelBreakdown.takeaway.percent}%`}
                />
                <div
                  style={{ width: `${channelBreakdown.delivery.percent}%` }}
                  className="bg-amber-500 transition-all duration-500"
                  title={`Giao hàng: ${channelBreakdown.delivery.percent}%`}
                />
              </div>

              {/* Items List */}
              <div className="space-y-2 text-xs">
                <div className="flex items-center justify-between p-2 rounded bg-neutral-50 border border-neutral-100">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-sm bg-neutral-800" />
                    <span className="font-semibold text-neutral-900">Tại quán (Dine-in)</span>
                  </div>
                  <div className="text-right font-mono">
                    <span className="font-bold text-neutral-900">{channelBreakdown.dineIn.count}</span>
                    <span className="text-neutral-500 ml-1 text-[11px]">({channelBreakdown.dineIn.percent}%)</span>
                  </div>
                </div>

                <div className="flex items-center justify-between p-2 rounded bg-neutral-50 border border-neutral-100">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-sm bg-cafe-orange" />
                    <span className="font-semibold text-neutral-900">Mang đi (Takeaway)</span>
                  </div>
                  <div className="text-right font-mono">
                    <span className="font-bold text-neutral-900">{channelBreakdown.takeaway.count}</span>
                    <span className="text-neutral-500 ml-1 text-[11px]">({channelBreakdown.takeaway.percent}%)</span>
                  </div>
                </div>

                <div className="flex items-center justify-between p-2 rounded bg-neutral-50 border border-neutral-100">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-sm bg-amber-500" />
                    <span className="font-semibold text-neutral-900">Giao hàng (Delivery)</span>
                  </div>
                  <div className="text-right font-mono">
                    <span className="font-bold text-neutral-900">{channelBreakdown.delivery.count}</span>
                    <span className="text-neutral-500 ml-1 text-[11px]">({channelBreakdown.delivery.percent}%)</span>
                  </div>
                </div>
              </div>
            </div>
          </Card>

          {/* Top Products in Timeframe */}
          <Card
            title="Món Bán Chạy Trong Kỳ"
            subtitle="Top doanh số thực thu từ thực đơn"
          >
            {topSellingItems.length === 0 ? (
              <div className="py-6 text-center text-xs text-neutral-500">
                Chưa có dữ liệu bán món trong kỳ.
              </div>
            ) : (
              <div className="space-y-2.5">
                {topSellingItems.map((item, idx) => (
                  <div
                    key={item.name}
                    className="flex items-center justify-between text-xs p-2 rounded border border-neutral-100 hover:bg-neutral-50 transition-colors"
                  >
                    <div className="flex items-center gap-2 min-w-0 pr-2">
                      <span className="w-5 h-5 rounded-full bg-neutral-100 text-neutral-700 font-bold text-[10px] flex items-center justify-center font-mono">
                        #{idx + 1}
                      </span>
                      <span className="font-bold text-neutral-900 truncate block">
                        {item.name}
                      </span>
                    </div>
                    <div className="text-right whitespace-nowrap">
                      <span className="font-mono font-bold text-cafe-orange block text-[11px]">
                        {item.revenue.toLocaleString('vi-VN')} ₫
                      </span>
                      <span className="text-[10px] text-neutral-500 font-mono">
                        {item.quantity} ly
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Tables Bottom Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent Orders Table */}
        <div className="lg:col-span-2">
          <Card
            title={`Đơn Hàng Trong Kỳ (${currentConfig.label})`}
            subtitle="Cập nhật trực tiếp từ quầy thu ngân và ứng dụng bán hàng"
            action={
              <Button
                variant="ghost"
                size="sm"
                onClick={() => navigate('/orders')}
                icon={<ArrowRight className="w-3.5 h-3.5" />}
              >
                Tất cả đơn hàng
              </Button>
            }
            noPadding
          >
            {loading ? (
              <div className="p-6 text-center text-xs text-neutral-500">Đang tải dữ liệu từ máy chủ...</div>
            ) : filteredOrders.length === 0 ? (
              <div className="p-8 text-center text-xs text-neutral-500">
                Không tìm thấy đơn hàng nào trong khoảng thời gian {currentConfig.label.toLowerCase()} đã chọn.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs text-neutral-700">
                  <thead className="bg-neutral-50 text-[11px] font-bold uppercase tracking-wider text-neutral-600 border-b border-neutral-200">
                    <tr>
                      <th className="py-3 px-4">Mã đơn</th>
                      <th className="py-3 px-4">Thời gian</th>
                      <th className="py-3 px-4">Khách hàng</th>
                      <th className="py-3 px-4">Hình thức</th>
                      <th className="py-3 px-4">Trạng thái</th>
                      <th className="py-3 px-4 text-right">Tổng tiền</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-200">
                    {filteredOrders.slice(0, 6).map((order) => {
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

                      const orderDate = new Date(order.createdAt);
                      const formattedTime = !isNaN(orderDate.getTime())
                        ? `${orderDate.getHours().toString().padStart(2, '0')}:${orderDate
                            .getMinutes()
                            .toString()
                            .padStart(2, '0')} ${orderDate.getDate().toString().padStart(2, '0')}/${(
                            orderDate.getMonth() + 1
                          )
                            .toString()
                            .padStart(2, '0')}`
                        : '—';

                      return (
                        <tr key={order.id} className="hover:bg-neutral-50/80 transition-colors">
                          <td className="py-3 px-4 font-mono font-bold text-neutral-900">
                            {order.orderNumber}
                          </td>
                          <td className="py-3 px-4 text-neutral-500 font-mono text-[11px]">
                            {formattedTime}
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
