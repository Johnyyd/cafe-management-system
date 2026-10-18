import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '../components/layout/PageContainer';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { Select } from '../components/common/Select';
import { api } from '../api/client';
import { Order, OrderStatus, Shop } from '../types';
import { useAppStore } from '../stores/appStore';
import { useAuthStore } from '../stores/authStore';
import {
  Clock,
  CheckCircle2,
  Bell,
  RefreshCw,
  Flame,
  CheckSquare,
  Square,
  Volume2,
  VolumeX,
  ShoppingCart,
  Maximize2,
  Minimize2,
} from 'lucide-react';

export const KdsPage: React.FC = () => {
  const navigate = useNavigate();
  const { user, activeRole } = useAuthStore();
  const isAdmin = activeRole === 'Admin';
  const { currentShopId, setCurrentShopId, notify } = useAppStore();
  const [orders, setOrders] = useState<Order[]>([]);
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<'all_active' | 'Pending' | 'Preparing' | 'Ready' | 'Completed'>('all_active');
  const [soundEnabled, setSoundEnabled] = useState<boolean>(true);
  const [completedItems, setCompletedItems] = useState<Record<string, boolean>>({});
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false);
  const prevOrderCountRef = useRef<number>(0);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().then(() => setIsFullscreen(true)).catch(() => {});
    } else {
      document.exitFullscreen().then(() => setIsFullscreen(false)).catch(() => {});
    }
  };

  // Sound beep via Web Audio API
  const playNotificationSound = () => {
    if (!soundEnabled) return;
    try {
      const audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(587.33, audioCtx.currentTime); // D5
      osc.frequency.setValueAtTime(880, audioCtx.currentTime + 0.1); // A5
      gain.gain.setValueAtTime(0.2, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.35);
      osc.connect(gain);
      gain.connect(audioCtx.destination);
      osc.start();
      osc.stop(audioCtx.currentTime + 0.35);
    } catch {
      // AudioContext may be restricted by autoplay policy
    }
  };

  const loadData = async (targetShopId?: string, isAutoRefresh = false) => {
    if (!isAutoRefresh) setLoading(true);
    try {
      const [ordersData, shopsData] = await Promise.all([
        api.getOrders(targetShopId),
        shops.length > 0 ? Promise.resolve(shops) : api.getShops()
      ]);

      if (shops.length === 0) setShops(shopsData);

      // Check if new pending orders arrived
      const currentActiveCount = ordersData.filter(o => o.status === 'Pending' || o.status === 'Confirmed').length;
      if (isAutoRefresh && currentActiveCount > prevOrderCountRef.current) {
        playNotificationSound();
        notify('info', 'Quầy bar có đơn gọi món mới!');
      }
      prevOrderCountRef.current = currentActiveCount;

      setOrders(ordersData);
    } catch (err: any) {
      if (!isAutoRefresh) {
        notify('error', err.message || 'Không thể tải danh sách pha chế.');
      }
    } finally {
      if (!isAutoRefresh) setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAdmin && user?.shopId && currentShopId !== user.shopId) {
      setCurrentShopId(user.shopId);
    }
  }, [isAdmin, user?.shopId, currentShopId]);

  useEffect(() => {
    const effectiveShopId = (!isAdmin && user?.shopId) ? user.shopId : currentShopId;
    loadData(effectiveShopId);
  }, [currentShopId, isAdmin, user?.shopId]);

  // Auto-refresh poll every 10 seconds for real-time KDS experience
  useEffect(() => {
    const timer = setInterval(() => {
      loadData(currentShopId, true);
    }, 10000);
    return () => clearInterval(timer);
  }, [currentShopId]);

  // Update Status handler
  const handleUpdateStatus = async (orderId: string, newStatus: OrderStatus) => {
    setUpdatingId(orderId);
    try {
      await api.updateOrderStatus(orderId, newStatus);
      setOrders(prev => prev.map(o => o.id === orderId ? { ...o, status: newStatus } : o));
      const statusLabels: Record<OrderStatus, string> = {
        Pending: 'Chờ xử lý',
        Confirmed: 'Đã nhận đơn',
        Preparing: 'Đang pha chế',
        Ready: 'Đã pha xong (Sẵn sàng giao)',
        Completed: 'Đã hoàn tất',
        Cancelled: 'Đã hủy'
      };
      notify('success', `Đã chuyển đơn sang: ${statusLabels[newStatus]}`);
    } catch (err: any) {
      notify('error', err.message || 'Lỗi khi cập nhật trạng thái pha chế.');
    } finally {
      setUpdatingId(null);
    }
  };

  // Toggle item checked state in barista ticket
  const toggleItemCheck = (orderId: string, itemIdx: number) => {
    const key = `${orderId}-${itemIdx}`;
    setCompletedItems(prev => ({ ...prev, [key]: !prev[key] }));
  };


  // Filtered orders
  const displayedOrders = useMemo(() => {
    let list = currentShopId ? orders.filter(o => o.shopId === currentShopId) : orders;

    if (activeFilter === 'all_active') {
      list = list.filter(o => o.status === 'Pending' || o.status === 'Confirmed' || o.status === 'Preparing' || o.status === 'Ready');
    } else if (activeFilter === 'Pending') {
      list = list.filter(o => o.status === 'Pending' || o.status === 'Confirmed');
    } else {
      list = list.filter(o => o.status === activeFilter);
    }

    return list;
  }, [orders, currentShopId, activeFilter]);

  // Aggregate Batch Queue (Món cần pha nhiều nhất)
  const batchSummary = useMemo(() => {
    const activeOrders = orders.filter(o =>
      (!currentShopId || o.shopId === currentShopId) &&
      (o.status === 'Pending' || o.status === 'Confirmed' || o.status === 'Preparing')
    );

    const counts: Record<string, { name: string; quantity: number }> = {};
    for (const ord of activeOrders) {
      for (const it of ord.items) {
        if (!counts[it.name]) {
          counts[it.name] = { name: it.name, quantity: 0 };
        }
        counts[it.name].quantity += it.quantity;
      }
    }

    return Object.values(counts)
      .sort((a, b) => b.quantity - a.quantity)
      .slice(0, 6);
  }, [orders, currentShopId]);

  // Counts for tabs
  const tabCounts = useMemo(() => {
    const shopFiltered = currentShopId ? orders.filter(o => o.shopId === currentShopId) : orders;
    return {
      all_active: shopFiltered.filter(o => o.status === 'Pending' || o.status === 'Confirmed' || o.status === 'Preparing' || o.status === 'Ready').length,
      Pending: shopFiltered.filter(o => o.status === 'Pending' || o.status === 'Confirmed').length,
      Preparing: shopFiltered.filter(o => o.status === 'Preparing').length,
      Ready: shopFiltered.filter(o => o.status === 'Ready').length,
      Completed: shopFiltered.filter(o => o.status === 'Completed').length,
    };
  }, [orders, currentShopId]);

  // Helper: Elapsed minutes calculation
  const getElapsedMinutes = (dateStr: string) => {
    const diffMs = Date.now() - new Date(dateStr).getTime();
    return Math.max(0, Math.floor(diffMs / 60000));
  };

  return (
    <PageContainer
      title="Quầy Pha Chế (KDS)"
      subtitle={`Đang xử lý ${tabCounts.all_active} đơn hàng tại quầy bar`}
      actions={
        <div className="flex items-center gap-2">
          {/* Shop Switcher: ONLY for Admin */}
          {isAdmin && shops.length > 0 && (
            <div className="w-56">
              <Select
                value={currentShopId || shops[0]?.id || ''}
                onChange={(e) => setCurrentShopId(e.target.value)}
                options={shops.map((s) => ({ value: s.id, label: s.name }))}
              />
            </div>
          )}

          {/* Sound Toggle */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSoundEnabled(!soundEnabled)}
            icon={soundEnabled ? <Volume2 className="w-4 h-4 text-emerald-600" /> : <VolumeX className="w-4 h-4 text-neutral-400" />}
            title={soundEnabled ? 'Chuông báo đơn: Bật' : 'Chuông báo đơn: Tắt'}
          >
            {soundEnabled ? 'Chuông: Bật' : 'Chuông: Tắt'}
          </Button>

          {/* Fullscreen Toggle */}
          <Button
            variant="outline"
            size="sm"
            onClick={toggleFullscreen}
            icon={isFullscreen ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
            title={isFullscreen ? 'Thu nhỏ' : 'Toàn màn hình'}
          >
            {isFullscreen ? 'Thu nhỏ' : 'Toàn màn hình'}
          </Button>

          {/* Switch to POS for Managers/Admins */}
          {(activeRole === 'Admin' || activeRole === 'Manager') && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/orders')}
              icon={<ShoppingCart className="w-4 h-4 text-cafe-orange" />}
            >
              Bán hàng (POS)
            </Button>
          )}

          {/* Refresh Button */}
          <Button
            variant="secondary"
            size="sm"
            onClick={() => loadData(currentShopId)}
            disabled={loading}
            icon={<RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />}
          >
            Làm mới
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        {/* Compact All-Day Batch Summary */}
        {batchSummary.length > 0 && (
          <div className="flex items-center gap-3 px-3.5 py-2 bg-white rounded-lg border border-neutral-200 shadow-xs text-xs">
            <span className="font-bold text-neutral-500 uppercase text-[10px] tracking-wider shrink-0">
              Tổng hợp cần pha:
            </span>
            <div className="flex items-center gap-2 overflow-x-auto py-0.5">
              {batchSummary.map((b) => (
                <span
                  key={b.name}
                  className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded bg-neutral-100 border border-neutral-200 text-neutral-900 font-medium text-xs whitespace-nowrap"
                >
                  <span>{b.name}</span>
                  <span className="font-mono font-bold text-cafe-orange">×{b.quantity}</span>
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Filter Navigation Tabs */}
        <div className="flex items-center justify-between gap-2 border-b border-neutral-200 pb-2">
          <div className="flex items-center gap-1 overflow-x-auto">
            <button
              onClick={() => setActiveFilter('all_active')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                activeFilter === 'all_active'
                  ? 'bg-neutral-900 text-white shadow-xs'
                  : 'text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900'
              }`}
            >
              <span>Tất cả đang pha</span>
              <span className={`px-1.5 py-0.5 rounded text-[11px] font-mono ${activeFilter === 'all_active' ? 'bg-neutral-800 text-neutral-100' : 'bg-neutral-100 text-neutral-700'}`}>
                {tabCounts.all_active}
              </span>
            </button>

            <button
              onClick={() => setActiveFilter('Pending')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                activeFilter === 'Pending'
                  ? 'bg-amber-600 text-white shadow-xs'
                  : 'text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900'
              }`}
            >
              <span>Chờ pha chế</span>
              <span className={`px-1.5 py-0.5 rounded text-[11px] font-mono ${activeFilter === 'Pending' ? 'bg-amber-700 text-white' : 'bg-neutral-100 text-neutral-700'}`}>
                {tabCounts.Pending}
              </span>
            </button>

            <button
              onClick={() => setActiveFilter('Preparing')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                activeFilter === 'Preparing'
                  ? 'bg-cafe-orange text-white shadow-xs'
                  : 'text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900'
              }`}
            >
              <span>Đang pha chế</span>
              <span className={`px-1.5 py-0.5 rounded text-[11px] font-mono ${activeFilter === 'Preparing' ? 'bg-amber-700 text-white' : 'bg-neutral-100 text-neutral-700'}`}>
                {tabCounts.Preparing}
              </span>
            </button>

            <button
              onClick={() => setActiveFilter('Ready')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                activeFilter === 'Ready'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900'
              }`}
            >
              <span>Chờ trả món</span>
              <span className={`px-1.5 py-0.5 rounded text-[11px] font-mono ${activeFilter === 'Ready' ? 'bg-emerald-700 text-white' : 'bg-neutral-100 text-neutral-700'}`}>
                {tabCounts.Ready}
              </span>
            </button>

            <button
              onClick={() => setActiveFilter('Completed')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 ${
                activeFilter === 'Completed'
                  ? 'bg-neutral-800 text-white shadow-xs'
                  : 'text-neutral-500 hover:bg-neutral-100 hover:text-neutral-900'
              }`}
            >
              <span>Đã hoàn tất</span>
              <span className="px-1.5 py-0.5 rounded text-[11px] font-mono bg-neutral-100 text-neutral-700">
                {tabCounts.Completed}
              </span>
            </button>
          </div>

          <span className="text-[11px] text-neutral-400 font-mono hidden md:inline">
            Tự động làm mới 10s
          </span>
        </div>

        {/* Ticket Cards Grid */}
        {loading ? (
          <div className="p-16 text-center text-neutral-500 text-sm">
            <RefreshCw className="w-6 h-6 mx-auto mb-2 animate-spin text-cafe-orange" />
            Đang tải dữ liệu...
          </div>
        ) : displayedOrders.length === 0 ? (
          <div className="py-12 px-6 text-center bg-white rounded-xl border border-neutral-200">
            <CheckCircle2 className="w-8 h-8 mx-auto mb-2 text-emerald-500" />
            <h3 className="text-sm font-semibold text-neutral-800 mb-0.5">
              {activeFilter === 'all_active' ? 'Quầy bar không có đơn chờ pha' : 'Không có đơn hàng nào'}
            </h3>
            <p className="text-xs text-neutral-400">
              Đơn hàng mới từ thu ngân sẽ tự động hiển thị tại đây.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3.5">
            {displayedOrders.map((order) => {
              const elapsedMin = getElapsedMinutes(order.createdAt);
              const isUrgent = elapsedMin >= 10;
              const isWarning = elapsedMin >= 5 && elapsedMin < 10;

              // Border and Badge colors by status
              const statusConfig = {
                Pending: {
                  badge: <Badge variant="neutral" size="sm">Chờ pha</Badge>,
                  cardBorder: 'border-neutral-200 hover:border-neutral-300',
                  headerBg: 'bg-neutral-50'
                },
                Confirmed: {
                  badge: <Badge variant="orange" size="sm">Đã nhận</Badge>,
                  cardBorder: 'border-amber-300 hover:border-amber-400',
                  headerBg: 'bg-amber-50/50'
                },
                Preparing: {
                  badge: <Badge variant="warning" size="sm">Đang pha</Badge>,
                  cardBorder: 'border-amber-400 shadow-xs',
                  headerBg: 'bg-orange-50/50'
                },
                Ready: {
                  badge: <Badge variant="success" size="sm">Chờ trả</Badge>,
                  cardBorder: 'border-emerald-300 shadow-xs',
                  headerBg: 'bg-emerald-50/50'
                },
                Completed: {
                  badge: <Badge variant="neutral" size="sm">Hoàn tất</Badge>,
                  cardBorder: 'border-neutral-200 opacity-70',
                  headerBg: 'bg-neutral-50'
                },
                Cancelled: {
                  badge: <Badge variant="danger" size="sm">Đã hủy</Badge>,
                  cardBorder: 'border-red-200 opacity-60',
                  headerBg: 'bg-red-50/40'
                }
              }[order.status] || {
                badge: <Badge variant="neutral" size="sm">{order.status}</Badge>,
                cardBorder: 'border-neutral-200',
                headerBg: 'bg-neutral-50'
              };

              return (
                <div
                  key={order.id}
                  className={`bg-white rounded-xl border flex flex-col justify-between overflow-hidden transition-all ${statusConfig.cardBorder}`}
                >
                  {/* Card Header */}
                  <div className={`px-3.5 py-2.5 border-b border-neutral-100 flex items-start justify-between ${statusConfig.headerBg}`}>
                    <div>
                      <div className="flex items-center gap-1.5">
                        <span className="font-mono font-bold text-xs tracking-tight text-neutral-900">
                          {order.orderNumber}
                        </span>
                        {statusConfig.badge}
                      </div>

                      <div className="flex items-center gap-1.5 text-xs text-neutral-600 mt-1">
                        <span className="font-medium truncate max-w-[120px]">{order.customerInfo.name}</span>
                        {order.customerInfo.tableNumber && (
                          <span className="font-semibold text-cafe-orange bg-orange-50 px-1.5 py-0.5 rounded text-[11px] font-mono border border-orange-200/50">
                            {order.customerInfo.tableNumber}
                          </span>
                        )}
                        {order.type === 'Takeaway' && (
                          <span className="font-medium text-blue-700 bg-blue-50 px-1.5 py-0.5 rounded text-[10px] border border-blue-200/50">
                            Mang đi
                          </span>
                        )}
                        {order.type === 'Delivery' && (
                          <span className="font-medium text-purple-700 bg-purple-50 px-1.5 py-0.5 rounded text-[10px] border border-purple-200/50">
                            Giao hàng
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Timer badge */}
                    <div
                      className={`flex items-center gap-1 text-[11px] font-mono font-semibold px-2 py-0.5 rounded ${
                        isUrgent
                          ? 'bg-red-600 text-white'
                          : isWarning
                          ? 'bg-amber-100 text-amber-900'
                          : 'bg-neutral-100 text-neutral-600'
                      }`}
                      title={`Đã tạo cách đây ${elapsedMin} phút`}
                    >
                      <Clock className="w-3 h-3" />
                      <span>{elapsedMin}m</span>
                    </div>
                  </div>

                  {/* Card Items List (Checklist for Barista) */}
                  <div className="p-3 space-y-2 flex-1 divide-y divide-neutral-100">
                    {(!order.items || order.items.length === 0) ? (
                      <div className="py-4 text-center">
                        <span className="text-xs text-neutral-400">Đơn gọi món tại quầy</span>
                      </div>
                    ) : (
                      order.items.map((item, idx) => {
                        const itemKey = `${order.id}-${idx}`;
                        const isChecked = !!completedItems[itemKey];

                        return (
                          <div
                            key={idx}
                            onClick={() => toggleItemCheck(order.id, idx)}
                            className={`pt-2 first:pt-0 flex items-start gap-2 cursor-pointer select-none transition-opacity ${
                              isChecked ? 'opacity-35 line-through' : 'opacity-100'
                            }`}
                          >
                            <button
                              type="button"
                              className="mt-0.5 text-neutral-400 hover:text-cafe-orange focus:outline-none"
                            >
                              {isChecked ? (
                                <CheckSquare className="w-4 h-4 text-emerald-600" />
                              ) : (
                                <Square className="w-4 h-4 text-neutral-300" />
                              )}
                            </button>

                            <div className="flex-1 min-w-0">
                              <div className="flex items-center justify-between gap-1">
                                <span className="font-semibold text-xs text-neutral-900 leading-tight">
                                  {item.name}
                                </span>
                                <span className="font-mono font-bold text-xs text-neutral-900 bg-neutral-100 px-1.5 py-0.5 rounded shrink-0">
                                  ×{item.quantity}
                                </span>
                              </div>

                              {item.specialInstructions && item.specialInstructions !== 'Tiêu chuẩn' && (
                                <div className="mt-0.5 text-[11px] text-amber-800 font-medium">
                                  • {item.specialInstructions}
                                </div>
                              )}
                            </div>
                          </div>
                        );
                      })
                    )}
                  </div>

                  {/* Card Actions */}
                  <div className="p-2.5 bg-neutral-50/60 border-t border-neutral-100">
                    {(order.status === 'Pending' || order.status === 'Confirmed') && (
                      <Button
                        variant="primary"
                        size="sm"
                        className="w-full py-2 font-medium bg-amber-600 hover:bg-amber-700 text-white"
                        disabled={updatingId === order.id}
                        onClick={() => handleUpdateStatus(order.id, 'Preparing')}
                        icon={<Flame className="w-3.5 h-3.5" />}
                      >
                        {updatingId === order.id ? 'Đang cập nhật...' : 'Bắt đầu pha'}
                      </Button>
                    )}

                    {order.status === 'Preparing' && (
                      <Button
                        variant="primary"
                        size="sm"
                        className="w-full py-2 font-medium bg-cafe-orange hover:bg-orange-700 text-white"
                        disabled={updatingId === order.id}
                        onClick={() => handleUpdateStatus(order.id, 'Ready')}
                        icon={<Bell className="w-3.5 h-3.5" />}
                      >
                        {updatingId === order.id ? 'Đang cập nhật...' : 'Báo xong món'}
                      </Button>
                    )}

                    {order.status === 'Ready' && (
                      <Button
                        variant="primary"
                        size="sm"
                        className="w-full py-2 font-medium bg-emerald-600 hover:bg-emerald-700 text-white"
                        disabled={updatingId === order.id}
                        onClick={() => handleUpdateStatus(order.id, 'Completed')}
                        icon={<CheckCircle2 className="w-3.5 h-3.5" />}
                      >
                        {updatingId === order.id ? 'Đang cập nhật...' : 'Đã giao khách'}
                      </Button>
                    )}

                    {order.status === 'Completed' && (
                      <div className="text-center py-1 text-xs font-medium text-emerald-700 flex items-center justify-center gap-1">
                        <CheckCircle2 className="w-3.5 h-3.5" /> Đã phục vụ
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </PageContainer>
  );
};
