import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Star,
  Coffee,
  CheckCircle2,
  Sparkles,
  Store,
  User,
  Phone,
  MessageSquare,
  ThumbsUp,
  Tag,
  Plus,
  Trash2,
  Gift
} from 'lucide-react';
import { reviewsApi, api } from '../api/client';
import { Shop, MenuItem } from '../types';

const PRESET_TAGS = [
  'Đồ uống xuất sắc',
  'Không gian đẹp & chill',
  'Phục vụ nhanh nhẹn',
  'Nhân viên thân thiện',
  'Wifi mạnh & ổn định',
  'Điều hòa mát lạnh',
  'Bàn ghế êm làm việc tốt',
  'Sạch sẽ chu đáo',
  'Bảo vệ dắt xe nhiệt tình',
  'Giá cả hợp lý'
];

const RATING_DESCRIPTIONS: Record<number, { text: string; emoji: string; color: string }> = {
  1: { text: 'Rất thất vọng', emoji: '😞', color: 'text-red-500' },
  2: { text: 'Chưa hài lòng', emoji: '🙁', color: 'text-orange-500' },
  3: { text: 'Bình thường', emoji: '😐', color: 'text-yellow-600' },
  4: { text: 'Hài lòng', emoji: '😊', color: 'text-lime-600' },
  5: { text: 'Tuyệt vời!', emoji: '🌟', color: 'text-amber-500' },
};

interface ItemRatingState {
  menuItemId: string;
  menuItemName: string;
  rating: number;
  comment: string;
}

export const PublicFeedbackPage: React.FC = () => {
  const [searchParams] = useSearchParams();

  // Query parameters from QR Code
  const qOrderId = searchParams.get('orderId') || '';
  const qShopId = searchParams.get('shopId') || '';
  const qShopName = searchParams.get('shopName') || '';
  const qCustomerName = searchParams.get('customerName') || '';
  const qItemsParam = searchParams.get('items') || '';

  // Form State
  const [shops, setShops] = useState<Shop[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [selectedShopId, setSelectedShopId] = useState<string>(qShopId);
  const [selectedShopName, setSelectedShopName] = useState<string>(qShopName);
  const [orderNumber, setOrderNumber] = useState<string>(qOrderId);
  const [customerName, setCustomerName] = useState<string>(qCustomerName);
  const [customerPhone, setCustomerPhone] = useState<string>('');
  
  // Rating State
  const [shopRating, setShopRating] = useState<number>(5);
  const [hoverRating, setHoverRating] = useState<number>(0);
  const [shopComment, setShopComment] = useState<string>('');
  const [selectedTags, setSelectedTags] = useState<string[]>(['Đồ uống xuất sắc', 'Nhân viên thân thiện']);

  // Item Reviews
  const [itemReviews, setItemReviews] = useState<ItemRatingState[]>([]);
  const [showItemPicker, setShowItemPicker] = useState<boolean>(false);

  // Status State
  const [loadingContext, setLoadingContext] = useState<boolean>(true);
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string>('');

  // 1. Initial Load: Load Shops & Menu, parse Query params
  useEffect(() => {
    let isMounted = true;

    async function initData() {
      setLoadingContext(true);
      try {
        const [shopsRes, menuRes] = await Promise.all([
          api.getShops().catch(() => []),
          api.getMenuItems().catch(() => [])
        ]);

        if (!isMounted) return;
        setShops(shopsRes);
        setMenuItems(menuRes);

        // Pre-parse items from query param if available
        let initialItems: ItemRatingState[] = [];
        if (qItemsParam) {
          try {
            const parsed = JSON.parse(decodeURIComponent(qItemsParam));
            if (Array.isArray(parsed)) {
              initialItems = parsed.map((it: any) => ({
                menuItemId: it.menuItemId || '',
                menuItemName: it.menuItemName || it.name || 'Món đồ uống',
                rating: 5,
                comment: ''
              }));
            }
          } catch {
            // ignore
          }
        }

        // If orderId is provided, try looking up from backend
        if (qOrderId) {
          try {
            const orderInfo = await reviewsApi.getOrderContext(qOrderId);
            if (orderInfo && isMounted) {
              if (orderInfo.shopId && !selectedShopId) {
                setSelectedShopId(orderInfo.shopId);
              }
              if (orderInfo.shopName && !selectedShopName) {
                setSelectedShopName(orderInfo.shopName);
              }
              if (orderInfo.customerName && !customerName) {
                setCustomerName(orderInfo.customerName);
              }
              if (orderInfo.customerPhone && !customerPhone) {
                setCustomerPhone(orderInfo.customerPhone);
              }
              if (orderInfo.orderNumber) {
                setOrderNumber(orderInfo.orderNumber);
              }
              if (Array.isArray(orderInfo.items) && orderInfo.items.length > 0 && initialItems.length === 0) {
                initialItems = orderInfo.items.map((it) => ({
                  menuItemId: it.menuItemId,
                  menuItemName: it.menuItemName,
                  rating: 5,
                  comment: ''
                }));
              }
            }
          } catch {
            // fallback gracefully
          }
        }

        // Fallback shop name if not filled
        if (qShopId && !selectedShopName && shopsRes.length > 0) {
          const matched = shopsRes.find((s) => s.id === qShopId);
          if (matched) setSelectedShopName(matched.name);
        } else if (!selectedShopId && shopsRes.length > 0) {
          setSelectedShopId(shopsRes[0].id);
          setSelectedShopName(shopsRes[0].name);
        }

        // If still no items and we have menu, pick 2 popular items as suggestions
        if (initialItems.length === 0 && menuRes.length > 0) {
          initialItems = menuRes.slice(0, 2).map((m) => ({
            menuItemId: m.id,
            menuItemName: m.name,
            rating: 5,
            comment: ''
          }));
        }

        if (isMounted) {
          setItemReviews(initialItems);
        }
      } catch (err) {
        console.error('Lỗi khởi tạo dữ liệu đánh giá:', err);
      } finally {
        if (isMounted) setLoadingContext(false);
      }
    }

    initData();
    return () => {
      isMounted = false;
    };
  }, [qOrderId, qShopId]);

  // Handle Shop Change
  const handleShopChange = (newShopId: string) => {
    setSelectedShopId(newShopId);
    const target = shops.find((s) => s.id === newShopId);
    if (target) {
      setSelectedShopName(target.name);
    }
  };

  // Toggle Tags
  const toggleTag = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    );
  };

  // Update Item Rating
  const updateItemRating = (index: number, rating: number) => {
    setItemReviews((prev) => {
      const copy = [...prev];
      copy[index] = { ...copy[index], rating };
      return copy;
    });
  };

  // Update Item Comment
  const updateItemComment = (index: number, comment: string) => {
    setItemReviews((prev) => {
      const copy = [...prev];
      copy[index] = { ...copy[index], comment };
      return copy;
    });
  };

  // Remove Item
  const removeItemReview = (index: number) => {
    setItemReviews((prev) => prev.filter((_, i) => i !== index));
  };

  // Add Item From Menu
  const addItemReview = (item: MenuItem) => {
    if (itemReviews.some((i) => i.menuItemId === item.id)) {
      setShowItemPicker(false);
      return;
    }
    setItemReviews((prev) => [
      ...prev,
      {
        menuItemId: item.id,
        menuItemName: item.name,
        rating: 5,
        comment: ''
      }
    ]);
    setShowItemPicker(false);
  };

  // Submit Review
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    if (!selectedShopId) {
      setErrorMessage('Vui lòng chọn chi nhánh bạn vừa ghé thăm.');
      return;
    }

    if (!customerName.trim()) {
      setErrorMessage('Vui lòng nhập tên của bạn để chúng tôi tiện xưng hô.');
      return;
    }

    setSubmitting(true);
    try {
      await reviewsApi.create({
        shopId: selectedShopId,
        shopName: selectedShopName || 'The Coffee House',
        orderId: orderNumber || undefined,
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim() || undefined,
        shopRating,
        shopComment: shopComment.trim() || undefined,
        itemReviews: itemReviews.map((it) => ({
          menuItemId: it.menuItemId,
          menuItemName: it.menuItemName,
          rating: it.rating,
          comment: it.comment.trim() || undefined
        })),
        tags: selectedTags
      });

      setIsSuccess(true);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể gửi đánh giá, vui lòng thử lại.');
    } finally {
      setSubmitting(false);
    }
  };

  // Loading Screen
  if (loadingContext) {
    return (
      <div className="min-h-screen bg-stone-100 flex flex-col items-center justify-center p-4">
        <div className="flex flex-col items-center gap-3">
          <div className="w-12 h-12 bg-orange-600 rounded-2xl flex items-center justify-center text-white shadow-lg shadow-orange-600/30 animate-pulse">
            <Coffee className="w-6 h-6" />
          </div>
          <p className="text-sm font-semibold text-neutral-700 animate-pulse">
            Đang tải thông tin đơn hàng & chi nhánh...
          </p>
        </div>
      </div>
    );
  }

  // Success Confirmation Screen
  if (isSuccess) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-amber-50/60 via-orange-50/30 to-stone-100 flex items-center justify-center p-4">
        <div className="w-full max-w-md bg-white rounded-3xl shadow-xl shadow-orange-950/5 border border-orange-100 p-8 text-center animate-in fade-in zoom-in-95 duration-300">
          <div className="w-20 h-20 bg-gradient-to-tr from-amber-500 to-orange-500 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg shadow-orange-500/25">
            <CheckCircle2 className="w-10 h-10 text-white" />
          </div>

          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-orange-100 text-orange-800 mb-3">
            <Sparkles className="w-3.5 h-3.5" /> Ghi nhận đánh giá thành công
          </span>

          <h2 className="text-2xl font-bold text-neutral-900 mb-2">
            Cảm Ơn Bạn Đã Đóng Góp Ý Kiến!
          </h2>

          <p className="text-sm text-neutral-600 mb-6 leading-relaxed">
            Mỗi nhận xét của bạn là động lực lớn để <strong className="text-neutral-800">{selectedShopName || 'Quán'}</strong> ngày càng nâng cao chất lượng đồ uống và dịch vụ.
          </p>

          {/* Reward Voucher Box */}
          <div className="bg-gradient-to-r from-amber-50 to-orange-50 rounded-2xl p-4 border border-orange-200/80 mb-6 text-left relative overflow-hidden">
            <div className="absolute -right-3 -bottom-3 text-orange-200/40">
              <Gift className="w-20 h-20" />
            </div>
            <div className="relative z-10">
              <div className="flex items-center gap-2 text-xs font-bold text-orange-700 uppercase tracking-wide mb-1">
                <Gift className="w-4 h-4 text-orange-600" /> Món quà tri ân từ quán
              </div>
              <div className="text-sm font-semibold text-neutral-900">
                Mã giảm 15% cho lần ghé thăm tiếp theo:
              </div>
              <div className="mt-2 inline-block bg-white px-3 py-1.5 rounded-lg border border-dashed border-orange-400 font-mono font-bold text-orange-600 text-base tracking-wider select-all shadow-sm">
                CAFE-THANKYOU15
              </div>
              <p className="text-[11px] text-neutral-500 mt-1.5">
                *Đưa mã này cho thu ngân khi thanh toán để áp dụng ưu đãi.
              </p>
            </div>
          </div>

          <div className="space-y-2">
            <button
              onClick={() => {
                setIsSuccess(false);
                setShopComment('');
                setShopRating(5);
              }}
              className="w-full py-3 px-4 bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white font-medium text-sm rounded-xl shadow-md shadow-orange-600/20 transition-all"
            >
              Gửi Đánh Giá Khác
            </button>
            <a
              href="/"
              className="block w-full py-2.5 text-xs font-semibold text-neutral-500 hover:text-neutral-800 transition-colors"
            >
              Về Trang Quản Lý Quán
            </a>
          </div>
        </div>
      </div>
    );
  }

  const activeRating = hoverRating || shopRating;
  const ratingMeta = RATING_DESCRIPTIONS[activeRating] || RATING_DESCRIPTIONS[5];

  return (
    <div className="min-h-screen bg-stone-100 flex flex-col justify-between selection:bg-orange-200 selection:text-orange-900 font-sans">
      {/* Top Banner / Hero */}
      <header className="relative bg-gradient-to-br from-neutral-900 via-stone-900 to-neutral-950 text-white pt-8 pb-14 px-4 overflow-hidden shadow-md">
        {/* Ambient Glows */}
        <div className="absolute top-0 right-0 w-80 h-80 bg-orange-600/20 rounded-full blur-3xl pointer-events-none -mr-20 -mt-20"></div>
        <div className="absolute bottom-0 left-0 w-64 h-64 bg-amber-500/15 rounded-full blur-2xl pointer-events-none -ml-16 -mb-16"></div>

        <div className="max-w-xl mx-auto relative z-10 text-center">
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-white/10 backdrop-blur-md border border-white/15 text-orange-300 text-xs font-medium mb-3 shadow-inner">
            <Coffee className="w-3.5 h-3.5" />
            <span>Đánh Giá Trải Nghiệm Khách Hàng</span>
          </div>

          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-white mb-2">
            {selectedShopName || 'Hương Vị Cafe Việt'}
          </h1>

          <p className="text-xs sm:text-sm text-neutral-300 max-w-md mx-auto leading-relaxed">
            Ý kiến của bạn là kim chỉ nam giúp chúng tôi hoàn thiện mỗi tách cà phê và cung cách phục vụ mỗi ngày.
          </p>

          {orderNumber && (
            <div className="inline-flex items-center gap-2 mt-4 px-3 py-1 bg-white/10 backdrop-blur rounded-lg border border-white/20 font-mono text-xs text-amber-200">
              <span className="text-neutral-400">Đơn hàng:</span>
              <strong className="text-white tracking-wide">{orderNumber}</strong>
            </div>
          )}
        </div>
      </header>

      {/* Main Feedback Form Container */}
      <main className="max-w-xl mx-auto w-full px-4 -mt-8 mb-12 relative z-20">
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Card 1: Shop & Overall Rating */}
          <section className="bg-white rounded-3xl p-6 shadow-xl shadow-stone-950/5 border border-stone-200/80">
            <div className="flex items-center justify-between pb-4 mb-4 border-b border-neutral-100">
              <div className="flex items-center gap-2 text-neutral-800 font-bold text-base">
                <Store className="w-5 h-5 text-orange-600" />
                <span>Chi Nhánh & Đánh Giá Chung</span>
              </div>
              <span className="text-xs font-medium text-neutral-400">Bước 1/3</span>
            </div>

            {/* Shop Selector */}
            <div className="mb-6">
              <label className="block text-xs font-semibold uppercase tracking-wider text-neutral-600 mb-1.5">
                Chi nhánh bạn đã thưởng thức *
              </label>
              <select
                value={selectedShopId}
                onChange={(e) => handleShopChange(e.target.value)}
                className="w-full px-3.5 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50 text-neutral-800 text-sm font-medium focus:ring-2 focus:ring-orange-500 focus:bg-white focus:outline-none transition-all"
              >
                {shops.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name} ({s.address.city || 'TP.HCM'})
                  </option>
                ))}
              </select>
            </div>

            {/* Interactive 5-Star Rating */}
            <div className="bg-gradient-to-b from-amber-50/50 to-orange-50/30 rounded-2xl p-5 border border-orange-100/80 text-center mb-6">
              <div className="text-xs font-bold uppercase tracking-wider text-neutral-500 mb-2">
                Trải nghiệm tổng quan của bạn
              </div>

              <div className="flex items-center justify-center gap-2 sm:gap-3 my-2">
                {[1, 2, 3, 4, 5].map((star) => {
                  const isFilled = star <= activeRating;
                  return (
                    <button
                      key={star}
                      type="button"
                      onClick={() => setShopRating(star)}
                      onMouseEnter={() => setHoverRating(star)}
                      onMouseLeave={() => setHoverRating(0)}
                      className="p-1 sm:p-2 transition-transform hover:scale-125 focus:outline-none"
                      aria-label={`${star} sao`}
                    >
                      <Star
                        className={`w-9 h-9 sm:w-10 sm:h-10 transition-colors ${
                          isFilled
                            ? 'text-amber-400 fill-amber-400 filter drop-shadow-sm'
                            : 'text-neutral-300 hover:text-amber-200'
                        }`}
                      />
                    </button>
                  );
                })}
              </div>

              <div className="flex items-center justify-center gap-1.5 text-sm font-bold mt-1">
                <span className="text-xl">{ratingMeta.emoji}</span>
                <span className={ratingMeta.color}>{ratingMeta.text} ({activeRating}/5 sao)</span>
              </div>
            </div>

            {/* Quick Impression Tags */}
            <div className="mb-6">
              <label className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-neutral-600 mb-2.5">
                <Tag className="w-3.5 h-3.5 text-orange-600" />
                <span>Bạn ấn tượng với điều gì nhất? (Chọn nhiều)</span>
              </label>

              <div className="flex flex-wrap gap-2">
                {PRESET_TAGS.map((tag) => {
                  const isSelected = selectedTags.includes(tag);
                  return (
                    <button
                      key={tag}
                      type="button"
                      onClick={() => toggleTag(tag)}
                      className={`text-xs font-medium px-3 py-1.5 rounded-full transition-all border ${
                        isSelected
                          ? 'bg-orange-600 text-white border-orange-600 shadow-sm shadow-orange-600/20'
                          : 'bg-neutral-50 text-neutral-700 border-neutral-200 hover:bg-neutral-100'
                      }`}
                    >
                      {tag} {isSelected && '✓'}
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Overall Comment */}
            <div>
              <label className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-neutral-600 mb-1.5">
                <MessageSquare className="w-3.5 h-3.5 text-orange-600" />
                <span>Nhận xét hoặc góp ý gửi tới quán</span>
              </label>
              <textarea
                value={shopComment}
                onChange={(e) => setShopComment(e.target.value)}
                placeholder="Hãy chia sẻ thêm về không gian, âm nhạc, sự chu đáo của nhân viên..."
                rows={3}
                className="w-full px-3.5 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50 text-neutral-800 text-sm focus:ring-2 focus:ring-orange-500 focus:bg-white focus:outline-none transition-all resize-none"
              />
            </div>
          </section>

          {/* Card 2: Drink / Product Reviews */}
          <section className="bg-white rounded-3xl p-6 shadow-xl shadow-stone-950/5 border border-stone-200/80">
            <div className="flex items-center justify-between pb-4 mb-4 border-b border-neutral-100">
              <div className="flex items-center gap-2 text-neutral-800 font-bold text-base">
                <Coffee className="w-5 h-5 text-orange-600" />
                <span>Chấm Điểm Đồ Uống & Món Ăn</span>
              </div>
              <span className="text-xs font-medium text-neutral-400">Bước 2/3</span>
            </div>

            <p className="text-xs text-neutral-500 mb-4">
              Đánh giá chi tiết từng món giúp các Barista điều chỉnh công thức pha chế vừa khẩu vị của bạn nhất.
            </p>

            {/* List of items */}
            {itemReviews.length === 0 ? (
              <div className="p-6 text-center bg-neutral-50 rounded-2xl border border-dashed border-neutral-200">
                <Coffee className="w-8 h-8 text-neutral-400 mx-auto mb-2" />
                <p className="text-xs text-neutral-500">Chưa có món nào được chọn để chấm điểm.</p>
              </div>
            ) : (
              <div className="space-y-3 mb-4">
                {itemReviews.map((item, idx) => (
                  <div
                    key={`${item.menuItemId}-${idx}`}
                    className="p-4 rounded-2xl bg-stone-50 border border-stone-200/70 hover:border-orange-200 transition-colors"
                  >
                    <div className="flex items-start justify-between gap-2 mb-2">
                      <div>
                        <h4 className="font-bold text-sm text-neutral-900">{item.menuItemName}</h4>
                        <span className="text-[11px] text-neutral-500">Món bạn vừa thưởng thức</span>
                      </div>
                      <button
                        type="button"
                        onClick={() => removeItemReview(idx)}
                        className="text-neutral-400 hover:text-red-500 p-1"
                        title="Bỏ món này"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>

                    {/* Star Rating for Item */}
                    <div className="flex items-center gap-1.5 mb-2.5">
                      {[1, 2, 3, 4, 5].map((star) => (
                        <button
                          key={star}
                          type="button"
                          onClick={() => updateItemRating(idx, star)}
                          className="focus:outline-none p-0.5"
                        >
                          <Star
                            className={`w-5 h-5 ${
                              star <= item.rating
                                ? 'text-amber-400 fill-amber-400'
                                : 'text-neutral-300'
                            }`}
                          />
                        </button>
                      ))}
                      <span className="text-xs font-semibold text-neutral-600 ml-2">
                        {item.rating}/5 sao
                      </span>
                    </div>

                    {/* Quick input comment for item */}
                    <input
                      type="text"
                      value={item.comment}
                      onChange={(e) => updateItemComment(idx, e.target.value)}
                      placeholder="Cảm nhận vị (ngọt vừa, béo ngậy, đậm đà, v.v.)..."
                      className="w-full px-3 py-1.5 text-xs rounded-lg border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-1 focus:ring-orange-500"
                    />
                  </div>
                ))}
              </div>
            )}

            {/* Add more item button */}
            {!showItemPicker ? (
              <button
                type="button"
                onClick={() => setShowItemPicker(true)}
                className="w-full py-2.5 px-3 rounded-xl border border-dashed border-orange-300 text-orange-700 bg-orange-50/50 hover:bg-orange-50 flex items-center justify-center gap-1.5 text-xs font-semibold transition-colors"
              >
                <Plus className="w-4 h-4" /> Thêm món đồ uống khác từ Menu
              </button>
            ) : (
              <div className="p-3 bg-neutral-50 rounded-2xl border border-neutral-200 mt-2">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs font-bold text-neutral-700">Chọn món từ thực đơn quán:</span>
                  <button
                    type="button"
                    onClick={() => setShowItemPicker(false)}
                    className="text-xs text-neutral-500 hover:text-neutral-800 font-medium"
                  >
                    Đóng
                  </button>
                </div>
                <div className="max-h-48 overflow-y-auto divide-y divide-neutral-200 text-xs">
                  {menuItems.map((m) => (
                    <button
                      key={m.id}
                      type="button"
                      onClick={() => addItemReview(m)}
                      className="w-full text-left py-2 px-1 flex items-center justify-between hover:text-orange-600 group"
                    >
                      <span className="font-medium text-neutral-800 group-hover:text-orange-600">
                        {m.name}
                      </span>
                      <span className="text-neutral-400 font-mono text-[11px]">
                        {m.price.amount.toLocaleString('vi-VN')} ₫
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </section>

          {/* Card 3: Customer Information */}
          <section className="bg-white rounded-3xl p-6 shadow-xl shadow-stone-950/5 border border-stone-200/80">
            <div className="flex items-center justify-between pb-4 mb-4 border-b border-neutral-100">
              <div className="flex items-center gap-2 text-neutral-800 font-bold text-base">
                <User className="w-5 h-5 text-orange-600" />
                <span>Thông Tin Người Đánh Giá</span>
              </div>
              <span className="text-xs font-medium text-neutral-400">Bước 3/3</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-neutral-600 mb-1.5">
                  Họ và tên của bạn *
                </label>
                <div className="relative">
                  <User className="w-4 h-4 text-neutral-400 absolute left-3 top-3" />
                  <input
                    type="text"
                    required
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                    placeholder="Ví dụ: Hoàng Long"
                    className="w-full pl-9 pr-3.5 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50 text-neutral-800 text-sm focus:ring-2 focus:ring-orange-500 focus:bg-white focus:outline-none transition-all font-medium"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-neutral-600 mb-1.5">
                  Số điện thoại (Nhận ưu đãi)
                </label>
                <div className="relative">
                  <Phone className="w-4 h-4 text-neutral-400 absolute left-3 top-3" />
                  <input
                    type="tel"
                    value={customerPhone}
                    onChange={(e) => setCustomerPhone(e.target.value)}
                    placeholder="09xx xxx xxx"
                    className="w-full pl-9 pr-3.5 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50 text-neutral-800 text-sm focus:ring-2 focus:ring-orange-500 focus:bg-white focus:outline-none transition-all font-medium font-mono"
                  />
                </div>
              </div>
            </div>

            <p className="text-[11px] text-neutral-500 mt-3 flex items-center gap-1">
              <Gift className="w-3.5 h-3.5 text-orange-500 shrink-0" />
              <span>Nhập số điện thoại để nhận mã giảm giá 15% cho hóa đơn tiếp theo qua tin nhắn/mã voucher.</span>
            </p>
          </section>

          {/* Error Message */}
          {errorMessage && (
            <div className="p-4 bg-red-50 text-red-700 text-xs font-medium rounded-2xl border border-red-200 flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-red-500 shrink-0"></span>
              {errorMessage}
            </div>
          )}

          {/* Submit Action Button */}
          <div className="pt-2">
            <button
              type="submit"
              disabled={submitting}
              className="w-full py-4 px-6 bg-gradient-to-r from-orange-600 via-amber-600 to-orange-700 hover:from-orange-500 hover:to-orange-600 active:scale-[0.99] disabled:opacity-50 text-white font-bold text-base rounded-2xl shadow-xl shadow-orange-600/30 flex items-center justify-center gap-2 transition-all"
            >
              {submitting ? (
                <>
                  <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                  <span>Đang gửi đánh giá của bạn...</span>
                </>
              ) : (
                <>
                  <ThumbsUp className="w-5 h-5" />
                  <span>Gửi Đánh Giá Ngay</span>
                </>
              )}
            </button>
            <p className="text-center text-[11px] text-neutral-400 mt-3">
              Thông tin đánh giá được bảo mật và truyền trực tiếp tới đội ngũ quản lý quán.
            </p>
          </div>
        </form>
      </main>

      {/* Footer */}
      <footer className="text-center py-6 text-xs text-neutral-500 border-t border-neutral-200/60 bg-white">
        <p className="font-semibold text-neutral-700">Hệ Thống Quản Lý Cafe & Trải Nghiệm Khách Hàng</p>
        <p className="text-neutral-400 text-[11px] mt-0.5">Powered by Cafe Management Platform</p>
      </footer>
    </div>
  );
};
