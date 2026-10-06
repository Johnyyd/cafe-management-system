import React, { useState, useEffect } from 'react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { Input } from '../components/common/Input';
import { Select } from '../components/common/Select';
import { Modal } from '../components/common/Modal';
import { api } from '../api/client';
import { Shop } from '../types';
import { shopSchema, ShopFormData } from '../schemas';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Store, Clock, Phone, Mail, MapPin } from 'lucide-react';
import { useAppStore } from '../stores/appStore';
import { VIETNAM_PROVINCES } from '../utils/provinces';

export const ShopsPage: React.FC = () => {
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { notify } = useAppStore();

  const loadShops = async () => {
    setLoading(true);
    try {
      const data = await api.getShops();
      setShops(data);
    } catch (err: any) {
      notify('error', err.message || 'Không thể tải danh sách chi nhánh');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadShops();
  }, []);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isValid },
  } = useForm<ShopFormData>({
    resolver: zodResolver(shopSchema),
    mode: 'onBlur',
    defaultValues: {
      name: '',
      street: '',
      city: 'Hồ Chí Minh',
      state: 'Hồ Chí Minh',
      postalCode: '70000',
      country: 'Việt Nam',
      phone: '',
      email: '',
      openTime: '07:00',
      closeTime: '22:30',
      status: 'Active',
    },
  });

  const onSubmit = async (data: ShopFormData) => {
    try {
      const newShop = await api.createShop({
        name: data.name,
        address: {
          street: data.street,
          city: data.city,
          state: data.state,
          postalCode: data.postalCode,
          country: data.country,
        },
        contact: {
          phone: data.phone,
          email: data.email,
        },
        status: data.status,
        operatingHours: [
          { dayOfWeek: 1, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
          { dayOfWeek: 2, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
          { dayOfWeek: 3, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
          { dayOfWeek: 4, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
          { dayOfWeek: 5, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
          { dayOfWeek: 6, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
          { dayOfWeek: 0, openTime: data.openTime, closeTime: data.closeTime, isClosed: false },
        ],
      });
      setShops((prev) => [...prev, newShop]);
      notify('success', `Đã thêm chi nhánh "${data.name}" thành công`);
      reset();
      setIsModalOpen(false);
    } catch (err: any) {
      notify('error', err.message || 'Không thể tạo chi nhánh');
    }
  };

  return (
    <PageContainer
      title="Quản Lý Chi Nhánh & Điểm Bán"
      subtitle="Danh sách các quán cà phê trong chuỗi, vị trí, giờ hoạt động và tình trạng phục vụ"
      actions={
        <Button
          variant="primary"
          icon={<Plus className="w-4 h-4" />}
          onClick={() => setIsModalOpen(true)}
        >
          Thêm chi nhánh mới
        </Button>
      }
    >
      {loading ? (
        <div className="p-12 text-center text-xs text-neutral-500 bg-white border border-neutral-200 rounded">
          Đang tải danh sách chi nhánh từ máy chủ...
        </div>
      ) : shops.length === 0 ? (
        <div className="p-12 text-center text-xs text-neutral-500 bg-white border border-neutral-200 rounded">
          Chưa có chi nhánh nào được cấu hình trong hệ thống.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {shops.map((shop) => (
          <Card key={shop.id} noPadding className="flex flex-col justify-between">
            <div className="p-5 border-b border-neutral-200">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-2.5">
                  <div className="w-8 h-8 rounded bg-neutral-100 border border-neutral-300 flex items-center justify-center text-neutral-800">
                    <Store className="w-4 h-4" />
                  </div>
                  <div>
                    <h3 className="font-bold text-sm text-neutral-900 leading-tight">{shop.name}</h3>
                    <span className="text-[10px] text-neutral-500 font-mono block">Mã: {shop.id}</span>
                  </div>
                </div>
                <Badge
                  variant={
                    shop.status === 'Active'
                      ? 'success'
                      : shop.status === 'UnderMaintenance'
                      ? 'warning'
                      : 'danger'
                  }
                  size="sm"
                >
                  {shop.status === 'Active'
                    ? 'Đang mở cửa'
                    : shop.status === 'UnderMaintenance'
                    ? 'Bảo trì'
                    : 'Đã đóng cửa'}
                </Badge>
              </div>

              <div className="mt-4 space-y-2 text-xs text-neutral-600">
                <div className="flex items-start gap-2">
                  <MapPin className="w-3.5 h-3.5 text-neutral-400 mt-0.5 flex-shrink-0" />
                  <span>
                    {shop.address.street}, {shop.address.city}, {shop.address.country}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <Phone className="w-3.5 h-3.5 text-neutral-400 flex-shrink-0" />
                  <span className="font-mono">{shop.contact.phone}</span>
                </div>
                <div className="flex items-center gap-2">
                  <Mail className="w-3.5 h-3.5 text-neutral-400 flex-shrink-0" />
                  <span>{shop.contact.email}</span>
                </div>
              </div>
            </div>

            <div className="p-4 bg-neutral-50 flex items-center justify-between text-xs border-t border-neutral-200">
              <div className="flex items-center gap-1.5 text-neutral-600">
                <Clock className="w-3.5 h-3.5 text-cafe-orange" />
                <span className="font-medium">Giờ mở cửa:</span>
              </div>
              <span className="font-mono font-bold text-neutral-900">
                {shop.operatingHours[0]?.openTime || '07:00'} - {shop.operatingHours[0]?.closeTime || '22:30'}
              </span>
            </div>
          </Card>
        ))}
        </div>
      )}

      {/* Add Shop Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Thêm Chi Nhánh Cà Phê Mới"
        description="Điền thông tin định danh và giờ mở cửa. Dữ liệu được kiểm tra nghiêm ngặt."
        maxWidth="lg"
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Tên chi nhánh quán"
            placeholder="VD: Cafe Landmark 81 - Bình Thạnh"
            required
            {...register('name')}
            error={errors.name?.message}
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              label="Địa chỉ đường"
              placeholder="VD: 720A Điện Biên Phủ"
              required
              {...register('street')}
              error={errors.street?.message}
            />
            <Select
              label="Tỉnh / Thành phố"
              options={VIETNAM_PROVINCES.map((p) => ({ value: p.name, label: p.name }))}
              required
              {...register('city')}
              error={errors.city?.message}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Input
              label="Quận / Huyện"
              placeholder="VD: Quận 1"
              required
              {...register('state')}
              error={errors.state?.message}
            />
            <Input
              label="Mã bưu điện"
              placeholder="70000"
              required
              {...register('postalCode')}
              error={errors.postalCode?.message}
            />
            <Input
              label="Quốc gia"
              placeholder="Việt Nam"
              required
              {...register('country')}
              error={errors.country?.message}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              label="Số điện thoại liên hệ"
              placeholder="VD: +84 28 3812 3456"
              required
              {...register('phone')}
              error={errors.phone?.message}
            />
            <Input
              label="Email liên hệ chi nhánh"
              placeholder="branch@cafemanagement.com"
              type="email"
              required
              {...register('email')}
              error={errors.email?.message}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-2 border-t border-neutral-200">
            <Input
              label="Giờ mở cửa (HH:mm)"
              placeholder="07:00"
              required
              {...register('openTime')}
              error={errors.openTime?.message}
            />
            <Input
              label="Giờ đóng cửa (HH:mm)"
              placeholder="22:30"
              required
              {...register('closeTime')}
              error={errors.closeTime?.message}
            />
            <Select
              label="Trạng thái ban đầu"
              options={[
                { value: 'Active', label: 'Hoạt động (Active)' },
                { value: 'UnderMaintenance', label: 'Bảo trì (UnderMaintenance)' },
                { value: 'Closed', label: 'Đóng cửa (Closed)' },
              ]}
              {...register('status')}
              error={errors.status?.message}
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-5 border-t border-neutral-200">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsModalOpen(false)}
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={isSubmitting}
              disabled={!isValid || isSubmitting}
            >
              Lưu chi nhánh
            </Button>
          </div>
        </form>
      </Modal>
    </PageContainer>
  );
};
