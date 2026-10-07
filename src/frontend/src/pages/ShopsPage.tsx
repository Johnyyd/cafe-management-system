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
import { Plus, Store, Clock, Phone, Mail, MapPin, Loader2 } from 'lucide-react';
import { useAppStore } from '../stores/appStore';
import {
  VIETNAM_PROVINCES,
  fetchOnlineProvinces,
  fetchDistrictsByProvince,
  fetchWardsByDistrict,
  getPostalCodeForProvince,
  Province,
  District,
  Ward,
} from '../utils/provinces';

export const ShopsPage: React.FC = () => {
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { notify } = useAppStore();

  // Location hierarchy state for cascading selects
  const [provinces, setProvinces] = useState<Province[]>(VIETNAM_PROVINCES);
  const [districts, setDistricts] = useState<District[]>([]);
  const [wards, setWards] = useState<Ward[]>([]);
  const [loadingDistricts, setLoadingDistricts] = useState<boolean>(false);
  const [loadingWards, setLoadingWards] = useState<boolean>(false);

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

    // Initialize provinces, districts, and wards for default province (TP. HCM, code 79)
    const initLocation = async () => {
      try {
        const pList = await fetchOnlineProvinces();
        setProvinces(pList);

        const defaultProvince =
          pList.find((p) => p.code === 79 || p.name.includes('Hồ Chí Minh')) || pList[0];

        if (defaultProvince) {
          setLoadingDistricts(true);
          const dists = await fetchDistrictsByProvince(defaultProvince.code);
          setDistricts(dists);
          setLoadingDistricts(false);

          const defaultDistrict = dists.find((d) => d.name === 'Quận 1') || dists[0];
          if (defaultDistrict) {
            setLoadingWards(true);
            const wds = await fetchWardsByDistrict(defaultDistrict.code);
            setWards(wds);
            setLoadingWards(false);
          }
        }
      } catch {
        // Fallback already embedded in helper functions
      }
    };

    initLocation();
  }, []);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isSubmitting, isValid },
  } = useForm<ShopFormData>({
    resolver: zodResolver(shopSchema),
    mode: 'onChange',
    defaultValues: {
      name: '',
      street: '',
      city: 'Thành phố Hồ Chí Minh',
      district: 'Quận 1',
      ward: 'Phường Bến Nghé',
      state: 'Quận 1',
      postalCode: '70000',
      country: 'Việt Nam',
      phone: '',
      email: '',
      openTime: '07:00',
      closeTime: '22:30',
      status: 'Active',
    },
  });

  const selectedCity = watch('city');
  const selectedDistrict = watch('district');
  const selectedWard = watch('ward');

  const handleProvinceChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const cityName = e.target.value;
    setValue('city', cityName, { shouldValidate: true });

    const prov = provinces.find((p) => p.name === cityName);
    if (!prov) return;

    // Auto-update suggested postal code
    const zip = getPostalCodeForProvince(prov.code);
    setValue('postalCode', zip, { shouldValidate: true });

    // Fetch cascading districts
    setLoadingDistricts(true);
    setDistricts([]);
    setWards([]);
    try {
      const dists = await fetchDistrictsByProvince(prov.code);
      setDistricts(dists);
      if (dists.length > 0) {
        const firstDist = dists[0];
        setValue('district', firstDist.name, { shouldValidate: true });
        setValue('state', firstDist.name);

        setLoadingWards(true);
        const wds = await fetchWardsByDistrict(firstDist.code);
        setWards(wds);
        if (wds.length > 0) {
          setValue('ward', wds[0].name, { shouldValidate: true });
        } else {
          setValue('ward', '', { shouldValidate: true });
        }
        setLoadingWards(false);
      } else {
        setValue('district', '', { shouldValidate: true });
        setValue('ward', '', { shouldValidate: true });
      }
    } catch {
      // handled
    } finally {
      setLoadingDistricts(false);
    }
  };

  const handleDistrictChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const distName = e.target.value;
    setValue('district', distName, { shouldValidate: true });
    setValue('state', distName);

    const dist = districts.find((d) => d.name === distName);
    if (!dist) {
      setWards([]);
      setValue('ward', '', { shouldValidate: true });
      return;
    }

    setLoadingWards(true);
    setWards([]);
    try {
      const wds = await fetchWardsByDistrict(dist.code);
      setWards(wds);
      if (wds.length > 0) {
        setValue('ward', wds[0].name, { shouldValidate: true });
      } else {
        setValue('ward', '', { shouldValidate: true });
      }
    } catch {
      // handled
    } finally {
      setLoadingWards(false);
    }
  };

  const handleWardChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setValue('ward', e.target.value, { shouldValidate: true });
  };

  const onSubmit = async (data: ShopFormData) => {
    try {
      const newShop = await api.createShop({
        name: data.name,
        district: data.district,
        ward: data.ward,
        address: {
          street: data.street,
          city: data.city,
          state: data.district,
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
      reset({
        name: '',
        street: '',
        city: 'Thành phố Hồ Chí Minh',
        district: 'Quận 1',
        ward: 'Phường Bến Nghé',
        state: 'Quận 1',
        postalCode: '70000',
        country: 'Việt Nam',
        phone: '',
        email: '',
        openTime: '07:00',
        closeTime: '22:30',
        status: 'Active',
      });
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
          {shops.map((shop) => {
            const addressParts = [
              shop.address.street,
              shop.address.state && shop.address.state !== shop.address.city ? shop.address.state : null,
              shop.address.city,
              shop.address.country,
            ].filter(Boolean);

            return (
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
                      <span>{addressParts.join(', ')}</span>
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
            );
          })}
        </div>
      )}

      {/* Add Shop Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Thêm Chi Nhánh Cà Phê Mới"
        description="Điền thông tin định danh và địa chỉ chi nhánh. Dữ liệu địa giới hành chính liên kết trực tiếp qua API."
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

          <Input
            label="Địa chỉ đường (Số nhà, tên đường)"
            placeholder="VD: 720A Điện Biên Phủ"
            required
            {...register('street')}
            error={errors.street?.message}
          />

          {/* Cascading Location Selects */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Select
              label="Tỉnh / Thành phố"
              value={selectedCity}
              options={provinces.map((p) => ({ value: p.name, label: p.name }))}
              required
              onChange={handleProvinceChange}
              error={errors.city?.message}
            />

            <div>
              <Select
                label="Quận / Huyện"
                value={selectedDistrict}
                options={
                  loadingDistricts
                    ? [{ value: '', label: 'Đang tải danh sách...' }]
                    : districts.length === 0
                    ? [{ value: '', label: 'Không có dữ liệu quận' }]
                    : districts.map((d) => ({ value: d.name, label: d.name }))
                }
                required
                disabled={loadingDistricts || districts.length === 0}
                onChange={handleDistrictChange}
                error={errors.district?.message}
              />
              {loadingDistricts && (
                <div className="flex items-center gap-1.5 mt-1 text-[11px] text-neutral-500">
                  <Loader2 className="w-3 h-3 animate-spin text-cafe-orange" />
                  <span>Đang tải quận huyện...</span>
                </div>
              )}
            </div>

            <div>
              <Select
                label="Phường / Xã"
                value={selectedWard || ''}
                options={
                  loadingWards
                    ? [{ value: '', label: 'Đang tải danh sách...' }]
                    : wards.length === 0
                    ? [{ value: '', label: 'Không có dữ liệu phường' }]
                    : wards.map((w) => ({ value: w.name, label: w.name }))
                }
                disabled={loadingWards || wards.length === 0}
                onChange={handleWardChange}
                error={errors.ward?.message}
              />
              {loadingWards && (
                <div className="flex items-center gap-1.5 mt-1 text-[11px] text-neutral-500">
                  <Loader2 className="w-3 h-3 animate-spin text-cafe-orange" />
                  <span>Đang tải phường xã...</span>
                </div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
