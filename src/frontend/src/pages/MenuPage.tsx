import React, { useState, useEffect } from 'react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { Input } from '../components/common/Input';
import { Select } from '../components/common/Select';
import { Modal } from '../components/common/Modal';
import { api } from '../api/client';
import { MenuItem } from '../types';
import { menuItemSchema, MenuItemFormData } from '../schemas';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Coffee, AlertCircle, CheckCircle } from 'lucide-react';
import { useAppStore } from '../stores/appStore';

export const MenuPage: React.FC = () => {
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedCategory, setSelectedCategory] = useState<string>('Tất cả');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const { notify } = useAppStore();

  const loadMenu = async () => {
    setLoading(true);
    try {
      const data = await api.getMenuItems();
      setMenuItems(data);
    } catch (err: any) {
      notify('error', err.message || 'Không thể tải danh sách thực đơn');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMenu();
  }, []);

  const categories = ['Tất cả', 'Cà phê', 'Trà & Trái Cây', 'Bánh Mì & Tráng Miệng'];

  const filteredItems =
    selectedCategory === 'Tất cả'
      ? menuItems
      : menuItems.filter((item) => item.category === selectedCategory);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isValid },
  } = useForm<MenuItemFormData>({
    resolver: zodResolver(menuItemSchema),
    mode: 'onBlur',
    defaultValues: {
      name: '',
      description: '',
      category: 'Cà phê',
      price: 45000,
      ingredientsText: '',
      allergensText: '',
      isAvailable: true,
    },
  });

  const toggleAvailability = (id: string) => {
    setMenuItems((prev) =>
      prev.map((item) => {
        if (item.id === id) {
          const updated = !item.availability.isAvailable;
          notify(
            updated ? 'success' : 'info',
            `${item.name}: ${updated ? 'Đã bật phục vụ' : 'Đã tạm ngưng phục vụ'}`
          );
          return {
            ...item,
            availability: { isAvailable: updated },
          };
        }
        return item;
      })
    );
  };

  const onSubmit = async (data: MenuItemFormData) => {
    try {
      const ingredients = data.ingredientsText.split(',').map((s) => s.trim()).filter(Boolean);
      const allergens = data.allergensText ? data.allergensText.split(',').map((s) => s.trim()).filter(Boolean) : [];

      const newItem = await api.createMenuItem({
        name: data.name,
        description: data.description,
        category: data.category,
        price: { amount: data.price, currency: 'VND' },
        ingredients,
        allergens,
        availability: { isAvailable: data.isAvailable },
      });

      setMenuItems((prev) => [...prev, newItem]);
      notify('success', `Đã thêm món "${data.name}" vào thực đơn`);
      reset();
      setIsModalOpen(false);
    } catch (err: any) {
      notify('error', err.message || 'Lỗi khi tạo món');
    }
  };

  return (
    <PageContainer
      title="Quản Lý Thực Đơn (Menu)"
      subtitle="Thiết lập danh mục món ăn, công thức pha chế, giá niêm yết và tình trạng phục vụ tại quầy"
      actions={
        <Button
          variant="primary"
          icon={<Plus className="w-4 h-4" />}
          onClick={() => setIsModalOpen(true)}
        >
          Thêm món mới
        </Button>
      }
    >
      {/* Category filter tabs */}
      <div className="flex items-center gap-2 mb-6 border-b border-neutral-200 pb-3 overflow-x-auto">
        {categories.map((cat) => (
          <button
            key={cat}
            onClick={() => setSelectedCategory(cat)}
            className={`px-3.5 py-1.5 text-xs font-bold uppercase tracking-wider rounded transition-colors whitespace-nowrap ${
              selectedCategory === cat
                ? 'bg-neutral-900 text-white'
                : 'text-neutral-600 hover:text-neutral-900 hover:bg-neutral-100'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Menu Grid */}
      {loading ? (
        <div className="p-12 text-center text-xs text-neutral-500 bg-white border border-neutral-200 rounded">
          Đang tải thực đơn từ máy chủ...
        </div>
      ) : filteredItems.length === 0 ? (
        <div className="p-12 text-center text-xs text-neutral-500 bg-white border border-neutral-200 rounded">
          Không có món nào trong danh mục này.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredItems.map((item) => (
          <Card key={item.id} noPadding className="flex flex-col justify-between">
            <div className="p-5">
              <div className="flex items-start justify-between gap-3 mb-2">
                <div className="flex items-center gap-2">
                  <div className="w-7 h-7 rounded bg-orange-50 border border-orange-200 flex items-center justify-center text-cafe-orange">
                    <Coffee className="w-4 h-4" />
                  </div>
                  <h3 className="font-bold text-sm text-neutral-900">{item.name}</h3>
                </div>
                <Badge variant={item.availability.isAvailable ? 'success' : 'danger'} size="sm">
                  {item.availability.isAvailable ? 'Sẵn sàng' : 'Hết món'}
                </Badge>
              </div>

              <p className="text-xs text-neutral-600 line-clamp-2 mb-3">{item.description}</p>

              <div className="flex items-center justify-between py-2 border-t border-b border-neutral-100 my-3">
                <span className="text-xs font-bold text-neutral-500 uppercase tracking-wider">Đơn giá</span>
                <span className="font-mono font-black text-base text-cafe-orange">
                  {item.price.amount.toLocaleString('vi-VN')} ₫
                </span>
              </div>

              {/* Ingredients tag cloud */}
              <div className="space-y-1.5">
                <span className="text-[10px] font-bold text-neutral-400 uppercase tracking-wider block">
                  Thành phần pha chế:
                </span>
                <div className="flex flex-wrap gap-1">
                  {item.ingredients.map((ing, idx) => (
                    <span
                      key={idx}
                      className="inline-flex items-center text-[10px] bg-neutral-100 text-neutral-700 px-1.5 py-0.5 rounded border border-neutral-200"
                    >
                      {ing}
                    </span>
                  ))}
                </div>
              </div>

              {/* Allergens warning */}
              {item.allergens.length > 0 && (
                <div className="mt-3 flex items-center gap-1.5 text-[10px] text-amber-800 bg-amber-50 p-1.5 rounded border border-amber-200">
                  <AlertCircle className="w-3 h-3 flex-shrink-0" />
                  <span>Dị ứng: {item.allergens.join(', ')}</span>
                </div>
              )}
            </div>

            {/* Availability toggle bar */}
            <div className="px-5 py-3 bg-neutral-50 border-t border-neutral-200 flex items-center justify-between">
              <span className="text-xs text-neutral-500 font-medium">Trạng thái bán hàng:</span>
              <button
                onClick={() => toggleAvailability(item.id)}
                className={`flex items-center gap-1.5 text-xs font-bold px-2.5 py-1 rounded transition-colors ${
                  item.availability.isAvailable
                    ? 'bg-neutral-900 text-white hover:bg-neutral-800'
                    : 'bg-emerald-600 text-white hover:bg-emerald-700'
                }`}
              >
                {item.availability.isAvailable ? (
                  <>
                    <span>Tạm dừng</span>
                  </>
                ) : (
                  <>
                    <CheckCircle className="w-3.5 h-3.5" />
                    <span>Mở bán lại</span>
                  </>
                )}
              </button>
            </div>
          </Card>
        ))}
        </div>
      )}

      {/* Add Menu Item Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Thêm Món Mới Vào Thực Đơn"
        description="Định giá tiền, công thức nguyên liệu và danh mục phục vụ."
        maxWidth="lg"
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Tên món"
            placeholder="VD: Cà phê Muối Cố Đô"
            required
            {...register('name')}
            error={errors.name?.message}
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Select
              label="Danh mục phục vụ"
              options={[
                { value: 'Cà phê', label: 'Cà phê' },
                { value: 'Trà & Trái Cây', label: 'Trà & Trái Cây' },
                { value: 'Bánh Mì & Tráng Miệng', label: 'Bánh Mì & Tráng Miệng' },
                { value: 'Đồ uống đá xay', label: 'Đồ uống đá xay' },
              ]}
              {...register('category')}
              error={errors.category?.message}
            />

            <Input
              label="Đơn giá bán (VND)"
              type="number"
              placeholder="45000"
              required
              {...register('price')}
              error={errors.price?.message}
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-neutral-700 mb-1.5">
              Mô tả hương vị <span className="text-cafe-orange">*</span>
            </label>
            <textarea
              rows={2}
              className={`block w-full text-sm rounded border bg-white px-3 py-2 text-neutral-900 placeholder-neutral-400 focus:outline-none focus:ring-1 ${
                errors.description
                  ? 'border-red-500 focus:border-red-500 focus:ring-red-500 bg-red-50/20'
                  : 'border-neutral-300 focus:border-cafe-orange focus:ring-cafe-orange'
              }`}
              placeholder="VD: Lớp kem mặn béo ngậy hòa quyện cùng vị cà phê đắng êm dịu..."
              {...register('description')}
            />
            {errors.description && (
              <p className="mt-1 text-xs font-medium text-red-600 flex items-center gap-1">
                <span>•</span> {errors.description.message}
              </p>
            )}
          </div>

          <Input
            label="Nguyên liệu (phân tách bằng dấu phẩy)"
            placeholder="VD: Cà phê Robusta, Kem béo thực vật, Muối hồng Himalaya, Sữa đặc"
            required
            {...register('ingredientsText')}
            error={errors.ingredientsText?.message}
            helperText="Nhập danh sách nguyên liệu để nhân viên quầy kiểm tra công thức"
          />

          <Input
            label="Cảnh báo dị ứng (nếu có, phân tách bằng dấu phẩy)"
            placeholder="VD: Sữa, Đậu phộng, Gluten"
            {...register('allergensText')}
            error={errors.allergensText?.message}
          />

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
              Thêm món vào Menu
            </Button>
          </div>
        </form>
      </Modal>
    </PageContainer>
  );
};
