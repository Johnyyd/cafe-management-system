import React, { useState, useEffect } from 'react';
import {
  Star,
  Search,
  Plus,
  CheckCircle2,
  Store,
  Filter,
  RefreshCw,
  Coffee,
  ThumbsUp,
  Phone,
  Calendar,
  Trash2,
} from 'lucide-react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Button } from '../components/common/Button';
import { Modal } from '../components/common/Modal';
import { EmptyState } from '../components/common/EmptyState';
import { reviewsApi, shopsApi, menuApi } from '../api/client';
import { CustomerReview, ReviewStats, Shop, MenuItem, ReviewItem } from '../types';
import { useAppStore } from '../stores/appStore';

export const ReviewsPage: React.FC = () => {
  const currentShopId = useAppStore((state) => state.currentShopId);
  const notify = useAppStore((state) => state.notify);

  const [reviews, setReviews] = useState<CustomerReview[]>([]);
  const [stats, setStats] = useState<ReviewStats | null>(null);
  const [shops, setShops] = useState<Shop[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [refreshing, setRefreshing] = useState<boolean>(false);

  // Filters
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [selectedShopFilter, setSelectedShopFilter] = useState<string>(currentShopId || 'all');
  const [selectedRatingFilter, setSelectedRatingFilter] = useState<string>('all');
  const [selectedProductFilter, setSelectedProductFilter] = useState<string>('all');
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);

  // Create review modal
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [formData, setFormData] = useState<{
    customerName: string;
    customerPhone: string;
    shopId: string;
    orderId: string;
    shopRating: number;
    shopComment: string;
    tags: string[];
    itemReviews: ReviewItem[];
  }>({
    customerName: '',
    customerPhone: '',
    shopId: currentShopId || '',
    orderId: '',
    shopRating: 5,
    shopComment: '',
    tags: ['Không gian đẹp', 'Đồ uống xuất sắc'],
    itemReviews: [],
  });

  const availableTags = [
    'Không gian đẹp',
    'Pha chế nhanh',
    'Hương vị đậm đà',
    'Phục vụ thân thiện',
    'Đồ uống xuất sắc',
    'Wifi mạnh',
    'View đẹp check-in',
    'Giá cả hợp lý',
  ];

  // Fetch shops and menu items on mount
  useEffect(() => {
    const fetchMetadata = async () => {
      try {
        const [shopsData, menuData] = await Promise.all([
          shopsApi.getAll(),
          menuApi.getAll(),
        ]);
        setShops(shopsData);
        setMenuItems(menuData);
        if (!formData.shopId && shopsData.length > 0) {
          setFormData((prev) => ({ ...prev, shopId: shopsData[0].id }));
        }
      } catch (err) {
        console.error('Error loading metadata:', err);
      }
    };
    fetchMetadata();
  }, [currentShopId]);

  // Sync selectedShopFilter with currentShopId if changed from header
  useEffect(() => {
    if (currentShopId) {
      setSelectedShopFilter(currentShopId);
    }
  }, [currentShopId]);

  const loadData = async (isSilent = false) => {
    if (!isSilent) setLoading(true);
    else setRefreshing(true);

    try {
      const [resReviews, resStats] = await Promise.all([
        reviewsApi.getAll({
          shopId: selectedShopFilter,
          rating: selectedRatingFilter !== 'all' ? parseInt(selectedRatingFilter, 10) : undefined,
          menuItemId: selectedProductFilter,
          search: searchTerm,
          page: currentPage,
          pageSize: 12,
        }),
        reviewsApi.getStats(selectedShopFilter),
      ]);

      setReviews(resReviews.items || []);
      setTotalPages(resReviews.totalPages || 1);
      setTotalCount(resReviews.totalCount || 0);
      setStats(resStats);
    } catch (err: any) {
      console.error('Error loading reviews:', err);
      notify('error', 'Lỗi khi tải danh sách đánh giá');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [selectedShopFilter, selectedRatingFilter, selectedProductFilter, currentPage]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
    loadData();
  };

  const handleTagToggle = (tag: string) => {
    setFormData((prev) => {
      const exists = prev.tags.includes(tag);
      return {
        ...prev,
        tags: exists ? prev.tags.filter((t) => t !== tag) : [...prev.tags, tag],
      };
    });
  };

  const handleAddItemReview = (menuItemId: string) => {
    const item = menuItems.find((m) => m.id === menuItemId);
    if (!item) return;
    if (formData.itemReviews.some((r) => r.menuItemId === menuItemId)) return;

    setFormData((prev) => ({
      ...prev,
      itemReviews: [
        ...prev.itemReviews,
        {
          menuItemId: item.id,
          menuItemName: item.name,
          rating: 5,
          comment: '',
        },
      ],
    }));
  };

  const handleRemoveItemReview = (menuItemId: string) => {
    setFormData((prev) => ({
      ...prev,
      itemReviews: prev.itemReviews.filter((r) => r.menuItemId !== menuItemId),
    }));
  };

  const handleItemRatingChange = (menuItemId: string, rating: number) => {
    setFormData((prev) => ({
      ...prev,
      itemReviews: prev.itemReviews.map((r) =>
        r.menuItemId === menuItemId ? { ...r, rating } : r
      ),
    }));
  };

  const handleItemCommentChange = (menuItemId: string, comment: string) => {
    setFormData((prev) => ({
      ...prev,
      itemReviews: prev.itemReviews.map((r) =>
        r.menuItemId === menuItemId ? { ...r, comment } : r
      ),
    }));
  };

  const handleSubmitReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.customerName.trim()) {
      notify('info', 'Vui lòng nhập họ tên khách hàng');
      return;
    }
    if (!formData.shopId) {
      notify('info', 'Vui lòng chọn chi nhánh cửa hàng');
      return;
    }

    const shop = shops.find((s) => s.id === formData.shopId);
    const shopName = shop ? shop.name : 'Chi nhánh';

    setIsSubmitting(true);
    try {
      await reviewsApi.create({
        shopId: formData.shopId,
        shopName,
        orderId: formData.orderId || undefined,
        customerName: formData.customerName.trim(),
        customerPhone: formData.customerPhone.trim() || undefined,
        shopRating: formData.shopRating,
        shopComment: formData.shopComment.trim() || undefined,
        itemReviews: formData.itemReviews,
        tags: formData.tags,
      });

      notify('success', 'Gửi đánh giá trải nghiệm thành công!');
      setIsModalOpen(false);
      // Reset form
      setFormData({
        customerName: '',
        customerPhone: '',
        shopId: currentShopId || (shops[0]?.id || ''),
        orderId: '',
        shopRating: 5,
        shopComment: '',
        tags: ['Không gian đẹp', 'Đồ uống xuất sắc'],
        itemReviews: [],
      });
      loadData(true);
    } catch (err: any) {
      notify('error', err.message || 'Lỗi khi gửi đánh giá');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Helper render interactive stars
  const renderInteractiveStars = (
    currentRating: number,
    onSelect: (rating: number) => void,
    size: 'sm' | 'md' = 'md'
  ) => {
    const starSize = size === 'sm' ? 'w-4 h-4' : 'w-6 h-6';
    const textSize = size === 'sm' ? 'text-xs' : 'text-sm';
    return (
      <div className="flex items-center gap-1 shrink-0">
        <div className="flex items-center">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              onClick={() => onSelect(star)}
              className="p-0.5 focus:outline-none transition-transform hover:scale-110"
            >
              <Star
                className={`${starSize} shrink-0 ${
                  star <= currentRating
                    ? 'fill-amber-400 text-amber-500'
                    : 'text-neutral-300 hover:text-amber-300'
                }`}
              />
            </button>
          ))}
        </div>
        <span className={`ml-1.5 font-bold text-neutral-800 ${textSize} whitespace-nowrap`}>
          {currentRating}/5
        </span>
      </div>
    );
  };

  // Helper render static stars
  const renderStaticStars = (rating: number, size = 'w-4 h-4') => {
    return (
      <div className="flex items-center gap-0.5 shrink-0">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            className={`${size} shrink-0 ${
              star <= rating ? 'fill-amber-400 text-amber-500' : 'text-neutral-300'
            }`}
          />
        ))}
      </div>
    );
  };

  const maskPhone = (phone?: string) => {
    if (!phone || phone.length < 7) return phone || '';
    return phone.slice(0, 3) + '****' + phone.slice(-3);
  };

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <PageContainer
      title="Đánh Giá & Phản Hồi Khách Hàng"
      subtitle="Quản lý ý kiến đóng góp, chấm điểm chất lượng đồ uống và dịch vụ tại các chi nhánh"
      actions={
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => loadData(true)}
            disabled={refreshing}
            className="flex items-center gap-1.5"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin' : ''}`} />
            <span>Làm mới</span>
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => setIsModalOpen(true)}
            className="flex items-center gap-1.5 bg-cafe-orange text-white hover:bg-orange-600"
          >
            <Plus className="w-4 h-4" />
            <span>Gửi Đánh Giá Trải Nghiệm</span>
          </Button>
        </div>
      }
    >
      <div className="space-y-6">
        {/* STATS OVERVIEW CARDS */}
        {stats && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* Card 1: Điểm trung bình */}
            <Card className="p-5 border-neutral-200 bg-white shadow-sm flex flex-col justify-between">
              <div>
                <span className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider block">
                  Điểm Trung Bình Quán
                </span>
                <div className="flex items-baseline gap-2 mt-2">
                  <span className="text-4xl font-black text-neutral-900 tracking-tight">
                    {stats.averageShopRating.toFixed(1)}
                  </span>
                  <span className="text-sm font-semibold text-neutral-500">/ 5.0</span>
                </div>
                <div className="mt-2.5 space-y-1">
                  <div className="flex items-center gap-1.5 flex-wrap">
                    {renderStaticStars(Math.round(stats.averageShopRating), 'w-4 h-4')}
                    <span className="text-xs font-bold text-amber-600 ml-0.5">
                      {stats.averageShopRating.toFixed(1)}/5
                    </span>
                  </div>
                  <span className="text-xs text-neutral-500 font-medium block">
                    ({stats.totalReviews} lượt đánh giá)
                  </span>
                </div>
              </div>
              <div className="mt-4 pt-3 border-t border-neutral-100 flex items-center gap-1.5 text-xs text-emerald-600 font-semibold">
                <CheckCircle2 className="w-4 h-4" />
                <span>Tiêu chuẩn dịch vụ xuất sắc</span>
              </div>
            </Card>

            {/* Card 2: Tỷ lệ hài lòng */}
            <Card className="p-5 border-neutral-200 bg-white shadow-sm flex flex-col justify-between">
              <div>
                <span className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider block">
                  Tỷ Lệ Khách Hài Lòng
                </span>
                <div className="flex items-baseline gap-2 mt-2">
                  <span className="text-4xl font-black text-cafe-orange tracking-tight">
                    {stats.satisfactionRate}%
                  </span>
                </div>
                <p className="text-xs text-neutral-500 mt-2 font-medium">
                  Đánh giá đạt từ 4 đến 5 sao trên toàn hệ thống
                </p>
              </div>
              <div className="mt-4 pt-3 border-t border-neutral-100 flex items-center gap-1.5 text-xs text-neutral-700 font-semibold">
                <ThumbsUp className="w-4 h-4 text-cafe-orange" />
                <span>9/10 khách hàng sẵn sàng giới thiệu</span>
              </div>
            </Card>

            {/* Card 3: Phân bổ số sao */}
            <Card className="p-5 border-neutral-200 bg-white shadow-sm md:col-span-2 flex flex-col justify-between">
              <div>
                <span className="text-[11px] font-bold text-neutral-400 uppercase tracking-wider block mb-2">
                  Phân Bổ Xếp Hạng Đánh Giá
                </span>
                <div className="space-y-1.5">
                  {[5, 4, 3, 2, 1].map((star) => {
                    const count = stats.ratingDistribution[star.toString()] || 0;
                    const pct = stats.totalReviews > 0 ? Math.round((count / stats.totalReviews) * 100) : 0;
                    return (
                      <div key={star} className="flex items-center gap-2 text-xs">
                        <span className="w-7 font-bold text-neutral-700 flex items-center gap-0.5">
                          {star} <Star className="w-3 h-3 fill-amber-400 text-amber-500" />
                        </span>
                        <div className="flex-1 bg-neutral-100 h-2 rounded-full overflow-hidden">
                          <div
                            className={`h-full rounded-full ${
                              star >= 4 ? 'bg-amber-400' : star === 3 ? 'bg-amber-300' : 'bg-red-400'
                            }`}
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                        <span className="w-12 text-right text-[11px] font-mono text-neutral-500">
                          {count} ({pct}%)
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* TOP RATED PRODUCTS & TAGS HIGHLIGHT */}
        {stats && stats.topProducts && stats.topProducts.length > 0 && (
          <Card className="p-4 border-neutral-200 bg-white shadow-sm">
            <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded bg-orange-100 text-cafe-orange flex items-center justify-center font-bold">
                  <Coffee className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-xs font-black uppercase text-neutral-900 tracking-wider">
                    Đồ Uống Được Yêu Thích Nhất
                  </h3>
                  <span className="text-[11px] text-neutral-500">
                    Xếp hạng dựa trên phản hồi trực tiếp từ khách hàng
                  </span>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                {stats.topProducts.slice(0, 4).map((p) => (
                  <div
                    key={p.menuItemId}
                    onClick={() => {
                      setSelectedProductFilter(p.menuItemId);
                      setCurrentPage(1);
                    }}
                    className={`cursor-pointer px-3 py-1.5 rounded border text-xs flex items-center justify-between gap-2 transition-all ${
                      selectedProductFilter === p.menuItemId
                        ? 'border-cafe-orange bg-orange-50 text-cafe-orange font-bold shadow-sm'
                        : 'border-neutral-200 hover:border-neutral-300 text-neutral-800 bg-white'
                    }`}
                  >
                    <span className="truncate max-w-[180px]">{p.menuItemName}</span>
                    <span className="bg-amber-100 text-amber-800 font-bold px-1.5 py-0.5 rounded text-[10px] flex items-center gap-0.5 shrink-0 whitespace-nowrap">
                      ★ {p.averageRating}
                    </span>
                  </div>
                ))}
                {selectedProductFilter !== 'all' && (
                  <button
                    onClick={() => {
                      setSelectedProductFilter('all');
                      setCurrentPage(1);
                    }}
                    className="text-xs text-neutral-400 hover:text-red-500 font-bold underline ml-1"
                  >
                    Xóa lọc món
                  </button>
                )}
              </div>
            </div>
          </Card>
        )}

        {/* FILTER TOOLBAR */}
        <div className="bg-white p-4 rounded border border-neutral-200 shadow-sm flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
          <form onSubmit={handleSearchSubmit} className="flex-1 max-w-md relative">
            <Search className="w-4 h-4 text-neutral-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Tìm theo tên khách hàng, số điện thoại, nhận xét..."
              className="w-full pl-9 pr-4 py-2 border border-neutral-300 rounded text-xs focus:outline-none focus:border-cafe-orange"
            />
          </form>

          <div className="flex flex-wrap items-center gap-2.5">
            {/* Filter by Shop */}
            <div className="flex items-center gap-1.5 text-xs">
              <Store className="w-3.5 h-3.5 text-neutral-400" />
              <select
                value={selectedShopFilter}
                onChange={(e) => {
                  setSelectedShopFilter(e.target.value);
                  setCurrentPage(1);
                }}
                className="border border-neutral-300 rounded px-2.5 py-1.5 text-xs text-neutral-800 focus:outline-none focus:border-cafe-orange bg-white"
              >
                <option value="all">Tất cả chi nhánh ({shops.length})</option>
                {shops.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Filter by Rating */}
            <div className="flex items-center gap-1.5 text-xs">
              <Filter className="w-3.5 h-3.5 text-neutral-400" />
              <select
                value={selectedRatingFilter}
                onChange={(e) => {
                  setSelectedRatingFilter(e.target.value);
                  setCurrentPage(1);
                }}
                className="border border-neutral-300 rounded px-2.5 py-1.5 text-xs text-neutral-800 focus:outline-none focus:border-cafe-orange bg-white"
              >
                <option value="all">Tất cả xếp hạng</option>
                <option value="5">5 Sao (Xuất sắc)</option>
                <option value="4">4 Sao (Rất tốt)</option>
                <option value="3">3 Sao (Bình thường)</option>
                <option value="2">2 Sao (Cần cải thiện)</option>
                <option value="1">1 Sao (Kém)</option>
              </select>
            </div>
          </div>
        </div>

        {/* REVIEWS LIST */}
        {loading ? (
          <div className="flex flex-col items-center justify-center p-12 bg-white rounded border border-neutral-200">
            <RefreshCw className="w-8 h-8 text-cafe-orange animate-spin mb-3" />
            <span className="text-sm font-semibold text-neutral-600">Đang tải danh sách đánh giá...</span>
          </div>
        ) : reviews.length === 0 ? (
          <EmptyState
            title="Không tìm thấy đánh giá nào"
            description="Hãy thử thay đổi bộ lọc hoặc gửi phản hồi trải nghiệm đầu tiên."
            action={
              <Button
                variant="primary"
                size="sm"
                onClick={() => setIsModalOpen(true)}
                className="bg-cafe-orange text-white"
              >
                Gửi Đánh Giá Trải Nghiệm
              </Button>
            }
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {reviews.map((rev) => (
              <Card
                key={rev.id}
                className="p-5 border-neutral-200 bg-white hover:border-neutral-300 transition-shadow shadow-sm flex flex-col justify-between"
              >
                <div>
                  {/* Card Header: Customer Info & Rating */}
                  <div className="flex items-start justify-between gap-2 pb-3 border-b border-neutral-100">
                    <div className="flex items-center gap-2.5 min-w-0">
                      <div className="w-9 h-9 rounded-full bg-neutral-900 text-white font-bold text-xs flex items-center justify-center uppercase shrink-0">
                        {rev.customerName.slice(0, 2)}
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-1.5 flex-wrap">
                          <h4 className="text-xs font-bold text-neutral-900 truncate max-w-[120px] sm:max-w-[150px]">
                            {rev.customerName}
                          </h4>
                          <span className="inline-flex items-center gap-0.5 text-[9px] bg-emerald-50 text-emerald-700 border border-emerald-200 px-1.5 py-0.2 rounded font-semibold shrink-0">
                            <CheckCircle2 className="w-2.5 h-2.5" />
                            Đã mua
                          </span>
                        </div>
                        <div className="flex items-center gap-2 text-[11px] text-neutral-400 mt-0.5">
                          {rev.customerPhone && (
                            <span className="flex items-center gap-0.5 whitespace-nowrap">
                              <Phone className="w-2.5 h-2.5" />
                              {maskPhone(rev.customerPhone)}
                            </span>
                          )}
                          <span>•</span>
                          <span className="flex items-center gap-0.5 whitespace-nowrap">
                            <Calendar className="w-2.5 h-2.5" />
                            {formatDate(rev.createdAt)}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div className="flex flex-col items-end shrink-0 pl-1">
                      {renderStaticStars(rev.shopRating, 'w-3.5 h-3.5')}
                      <span className="text-[10px] font-bold text-neutral-500 mt-0.5 whitespace-nowrap">
                        {rev.shopRating}.0 / 5
                      </span>
                    </div>
                  </div>

                  {/* Shop Name Badge */}
                  <div className="mt-2.5 flex items-center gap-1 text-[11px] font-semibold text-neutral-600 bg-neutral-50 px-2 py-1 rounded">
                    <Store className="w-3 h-3 text-cafe-orange flex-shrink-0" />
                    <span className="truncate">{rev.shopName}</span>
                  </div>

                  {/* Shop Comment */}
                  {rev.shopComment && (
                    <p className="mt-2 text-xs text-neutral-800 leading-relaxed font-normal bg-neutral-50/50 p-2.5 rounded border border-neutral-100">
                      "{rev.shopComment}"
                    </p>
                  )}

                  {/* Item Reviews */}
                  {rev.itemReviews && rev.itemReviews.length > 0 && (
                    <div className="mt-3 space-y-1.5 pt-2 border-t border-dashed border-neutral-200">
                      <span className="text-[10px] font-black uppercase text-neutral-400 tracking-wider block">
                        Đồ uống đã dùng & cảm nhận:
                      </span>
                      {rev.itemReviews.map((it, idx) => (
                        <div
                          key={idx}
                          className="bg-orange-50/40 border border-orange-100/80 rounded p-2 text-xs"
                        >
                          <div className="flex items-center justify-between gap-1.5">
                            <span className="font-bold text-neutral-900 truncate max-w-[170px]">
                              {it.menuItemName}
                            </span>
                            <div className="flex items-center gap-0.5 shrink-0">
                              {renderStaticStars(it.rating, 'w-2.5 h-2.5')}
                            </div>
                          </div>
                          {it.comment && (
                            <p className="text-[11px] text-neutral-600 mt-1 italic leading-tight">
                              - {it.comment}
                            </p>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Tags footer */}
                {rev.tags && rev.tags.length > 0 && (
                  <div className="mt-3 pt-2.5 border-t border-neutral-100 flex flex-wrap gap-1">
                    {rev.tags.map((tag, tIdx) => (
                      <span
                        key={tIdx}
                        className="text-[10px] font-medium bg-neutral-100 text-neutral-600 px-2 py-0.5 rounded"
                      >
                        #{tag}
                      </span>
                    ))}
                  </div>
                )}
              </Card>
            ))}
          </div>
        )}

        {/* PAGINATION */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-neutral-200 pt-4">
            <span className="text-xs text-neutral-500 font-medium">
              Hiển thị trang {currentPage} trên tổng số {totalPages} trang ({totalCount} đánh giá)
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage <= 1}
                onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              >
                Trang trước
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage >= totalPages}
                onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              >
                Trang sau
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* CREATE REVIEW MODAL */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Gửi Đánh Giá Trải Nghiệm Khách Hàng"
      >
        <form onSubmit={handleSubmitReview} className="space-y-4 text-xs">
          {/* Customer info */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-neutral-700 font-bold mb-1">
                Họ và tên khách hàng <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                required
                value={formData.customerName}
                onChange={(e) => setFormData({ ...formData, customerName: e.target.value })}
                placeholder="Ví dụ: Nguyễn Văn An"
                className="w-full border border-neutral-300 rounded px-3 py-2 focus:outline-none focus:border-cafe-orange text-xs"
              />
            </div>
            <div>
              <label className="block text-neutral-700 font-bold mb-1">Số điện thoại</label>
              <input
                type="text"
                value={formData.customerPhone}
                onChange={(e) => setFormData({ ...formData, customerPhone: e.target.value })}
                placeholder="Ví dụ: 0908123456"
                className="w-full border border-neutral-300 rounded px-3 py-2 focus:outline-none focus:border-cafe-orange text-xs"
              />
            </div>
          </div>

          {/* Shop Selection */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-neutral-700 font-bold mb-1">
                Chi nhánh trải nghiệm <span className="text-red-500">*</span>
              </label>
              <select
                required
                value={formData.shopId}
                onChange={(e) => setFormData({ ...formData, shopId: e.target.value })}
                className="w-full border border-neutral-300 rounded px-3 py-2 focus:outline-none focus:border-cafe-orange text-xs bg-white"
              >
                {shops.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-neutral-700 font-bold mb-1">Mã đơn hàng (nếu có)</label>
              <input
                type="text"
                value={formData.orderId}
                onChange={(e) => setFormData({ ...formData, orderId: e.target.value })}
                placeholder="Ví dụ: ORD-20261018-001"
                className="w-full border border-neutral-300 rounded px-3 py-2 focus:outline-none focus:border-cafe-orange text-xs"
              />
            </div>
          </div>

          {/* Shop Rating */}
          <div className="p-3 bg-neutral-50 rounded border border-neutral-200 space-y-2">
            <label className="block text-neutral-900 font-bold">
              Đánh giá chung về không gian & chất lượng phục vụ quán:
            </label>
            {renderInteractiveStars(formData.shopRating, (r) =>
              setFormData({ ...formData, shopRating: r })
            )}
            <textarea
              rows={2}
              value={formData.shopComment}
              onChange={(e) => setFormData({ ...formData, shopComment: e.target.value })}
              placeholder="Chia sẻ cảm nhận về không gian quán, thái độ phục vụ, âm nhạc, vệ sinh..."
              className="w-full border border-neutral-300 rounded p-2 text-xs focus:outline-none focus:border-cafe-orange bg-white mt-2"
            />
          </div>

          {/* Item Ratings Section */}
          <div className="p-3 bg-orange-50/40 rounded border border-orange-200/80 space-y-3">
            <div className="flex items-center justify-between">
              <label className="block text-neutral-900 font-bold flex items-center gap-1.5">
                <Coffee className="w-4 h-4 text-cafe-orange" />
                <span>Đánh giá chi tiết đồ uống đã dùng:</span>
              </label>
              <select
                onChange={(e) => {
                  if (e.target.value) {
                    handleAddItemReview(e.target.value);
                    e.target.value = '';
                  }
                }}
                className="border border-neutral-300 rounded px-2 py-1 text-xs bg-white text-neutral-800"
              >
                <option value="">+ Chọn món để đánh giá...</option>
                {menuItems.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name}
                  </option>
                ))}
              </select>
            </div>

            {formData.itemReviews.length === 0 ? (
              <p className="text-neutral-400 italic text-[11px]">
                Chưa có món nào được chọn. Hãy chọn một món từ thực đơn phía trên để chấm điểm hương vị.
              </p>
            ) : (
              <div className="space-y-2.5">
                {formData.itemReviews.map((it) => (
                  <div
                    key={it.menuItemId}
                    className="bg-white border border-neutral-200 rounded p-2.5 space-y-1.5"
                  >
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      <span className="font-bold text-neutral-900 text-xs truncate max-w-[200px]">
                        {it.menuItemName}
                      </span>
                      <div className="flex items-center gap-2 shrink-0 self-end sm:self-auto">
                        {renderInteractiveStars(
                          it.rating,
                          (r) => handleItemRatingChange(it.menuItemId, r),
                          'sm'
                        )}
                        <button
                          type="button"
                          onClick={() => handleRemoveItemReview(it.menuItemId)}
                          className="text-neutral-400 hover:text-red-500 p-1 rounded hover:bg-neutral-100 transition-colors"
                          title="Xóa món"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                    <input
                      type="text"
                      value={it.comment || ''}
                      onChange={(e) => handleItemCommentChange(it.menuItemId, e.target.value)}
                      placeholder="Nhận xét hương vị: độ ngọt, độ béo, độ đậm cà phê..."
                      className="w-full border border-neutral-200 rounded px-2 py-1 text-xs focus:outline-none focus:border-cafe-orange"
                    />
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Quick Tags */}
          <div>
            <label className="block text-neutral-700 font-bold mb-1.5">
              Chọn nhanh đặc điểm trải nghiệm:
            </label>
            <div className="flex flex-wrap gap-1.5">
              {availableTags.map((tag) => {
                const isSelected = formData.tags.includes(tag);
                return (
                  <button
                    key={tag}
                    type="button"
                    onClick={() => handleTagToggle(tag)}
                    className={`px-2.5 py-1 rounded text-xs transition-colors ${
                      isSelected
                        ? 'bg-cafe-orange text-white font-bold'
                        : 'bg-neutral-100 text-neutral-700 hover:bg-neutral-200'
                    }`}
                  >
                    {isSelected ? '✓ ' : '+ '}
                    {tag}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Submit Actions */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-neutral-200">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setIsModalOpen(false)}
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              disabled={isSubmitting}
              className="bg-cafe-orange text-white hover:bg-orange-600"
            >
              {isSubmitting ? 'Đang gửi...' : 'Gửi Đánh Giá'}
            </Button>
          </div>
        </form>
      </Modal>
    </PageContainer>
  );
};

export default ReviewsPage;
