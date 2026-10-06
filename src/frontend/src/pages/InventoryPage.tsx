import React, { useState, useEffect } from 'react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Badge } from '../components/common/Badge';
import { Button } from '../components/common/Button';
import { Input } from '../components/common/Input';
import { Select } from '../components/common/Select';
import { Modal } from '../components/common/Modal';
import { api } from '../api/client';
import { InventoryItem } from '../types';
import {
  inventoryItemSchema,
  InventoryItemFormData,
  stockAdjustSchema,
  StockAdjustFormData,
} from '../schemas';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Boxes, SlidersHorizontal, ArrowUpDown } from 'lucide-react';
import { useAppStore } from '../stores/appStore';

export const InventoryPage: React.FC = () => {
  const [inventory, setInventory] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isAddItemModalOpen, setIsAddItemModalOpen] = useState(false);
  const [adjustingItem, setAdjustingItem] = useState<InventoryItem | null>(null);
  const [onlyLowStock, setOnlyLowStock] = useState(false);
  const { notify } = useAppStore();

  const loadInventory = async () => {
    setLoading(true);
    try {
      const data = await api.getInventory();
      setInventory(data);
    } catch (err: any) {
      notify('error', err.message || 'Không thể tải danh sách tồn kho');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadInventory();
  }, []);

  const filteredInventory = onlyLowStock
    ? inventory.filter((item) => item.currentStock <= item.reorderLevel)
    : inventory;

  // Add Item Form
  const {
    register: registerAdd,
    handleSubmit: handleSubmitAdd,
    reset: resetAdd,
    formState: { errors: errorsAdd, isSubmitting: isSubmittingAdd, isValid: isValidAdd },
  } = useForm<InventoryItemFormData>({
    resolver: zodResolver(inventoryItemSchema),
    mode: 'onBlur',
    defaultValues: {
      itemName: '',
      unit: 'kg',
      currentStock: 10,
      reorderLevel: 5,
      supplierName: '',
    },
  });

  // Adjust Stock Form
  const {
    register: registerAdjust,
    handleSubmit: handleSubmitAdjust,
    reset: resetAdjust,
    formState: { errors: errorsAdjust, isSubmitting: isSubmittingAdjust, isValid: isValidAdjust },
  } = useForm<StockAdjustFormData>({
    resolver: zodResolver(stockAdjustSchema),
    mode: 'onBlur',
    defaultValues: {
      amount: 5,
      type: 'Add',
      reason: 'Nhập hàng từ nhà cung cấp',
    },
  });

  const onAddItemSubmit = async (data: InventoryItemFormData) => {
    try {
      const newItem = await api.createInventoryItem({
        itemName: data.itemName,
        unit: data.unit,
        currentStock: data.currentStock,
        reorderLevel: data.reorderLevel,
      });
      setInventory((prev) => [...prev, newItem]);
      notify('success', `Đã thêm nguyên liệu "${data.itemName}" vào kho`);
      resetAdd();
      setIsAddItemModalOpen(false);
    } catch (err: any) {
      notify('error', err.message || 'Lỗi khi thêm nguyên liệu vào kho');
    }
  };

  const onAdjustSubmit = async (data: StockAdjustFormData) => {
    if (!adjustingItem) return;
    try {
      const delta = data.type === 'Deduct' ? -Math.abs(data.amount) : Math.abs(data.amount);
      const updated = await api.adjustStock(adjustingItem.id, delta, data.reason);
      setInventory((prev) =>
        prev.map((item) => (item.id === adjustingItem.id ? { ...item, currentStock: updated.currentStock } : item))
      );
      notify('success', `Đã cập nhật số lượng tồn cho "${adjustingItem.itemName}"`);
      resetAdjust();
      setAdjustingItem(null);
    } catch (err: any) {
      notify('error', err.message || 'Lỗi khi cập nhật kho');
    }
  };

  return (
    <PageContainer
      title="Quản Lý Kho & Nguyên Liệu"
      subtitle="Giám sát định mức tiêu hao, cảnh báo sắp hết hàng và thực hiện kiểm kê nhập/xuất kho"
      actions={
        <div className="flex items-center gap-2">
          <Button
            variant={onlyLowStock ? 'secondary' : 'outline'}
            size="sm"
            icon={<SlidersHorizontal className="w-3.5 h-3.5" />}
            onClick={() => setOnlyLowStock(!onlyLowStock)}
          >
            {onlyLowStock ? 'Hiện tất cả kho' : 'Lọc nguyên liệu thiếu'}
          </Button>
          <Button
            variant="primary"
            size="sm"
            icon={<Plus className="w-4 h-4" />}
            onClick={() => setIsAddItemModalOpen(true)}
          >
            Thêm nguyên liệu
          </Button>
        </div>
      }
    >
      <Card noPadding>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50 text-[11px] font-bold uppercase tracking-wider text-neutral-600">
                <th className="py-3.5 px-5">Tên nguyên liệu</th>
                <th className="py-3.5 px-5">Đơn vị</th>
                <th className="py-3.5 px-5">Tồn kho hiện tại</th>
                <th className="py-3.5 px-5">Ngưỡng cảnh báo</th>
                <th className="py-3.5 px-5">Tình trạng</th>
                <th className="py-3.5 px-5 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200 text-xs">
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-neutral-500">
                    Đang tải dữ liệu tồn kho từ máy chủ...
                  </td>
                </tr>
              ) : filteredInventory.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-neutral-500">
                    {onlyLowStock ? 'Không có mặt hàng nào chạm ngưỡng cảnh báo.' : 'Kho hàng chưa có nguyên liệu nào.'}
                  </td>
                </tr>
              ) : (
                filteredInventory.map((item) => {
                  const isLow = item.currentStock <= item.reorderLevel;

                  return (
                    <tr key={item.id} className="hover:bg-neutral-50/80 transition-colors">
                      <td className="py-3.5 px-5">
                        <div className="flex items-center gap-2.5">
                          <div className="w-7 h-7 rounded bg-neutral-100 border border-neutral-300 flex items-center justify-center text-neutral-700">
                            <Boxes className="w-3.5 h-3.5" />
                          </div>
                          <div>
                            <span className="font-bold text-neutral-900 block">{item.itemName}</span>
                            <span className="text-[10px] text-neutral-500 font-mono">Mã: {item.id}</span>
                          </div>
                        </div>
                      </td>
                      <td className="py-3.5 px-5 font-mono text-neutral-700">{item.unit}</td>
                      <td className="py-3.5 px-5 font-mono font-bold text-sm text-neutral-900">
                        {item.currentStock}
                      </td>
                      <td className="py-3.5 px-5 font-mono text-neutral-500">{item.reorderLevel}</td>
                      <td className="py-3.5 px-5">
                        <Badge variant={isLow ? 'danger' : 'success'} size="sm">
                          {isLow ? 'Sắp hết hàng' : 'Đủ định mức'}
                        </Badge>
                      </td>
                      <td className="py-3.5 px-5 text-right">
                        <Button
                          variant="outline"
                          size="sm"
                          icon={<ArrowUpDown className="w-3 h-3" />}
                          onClick={() => {
                            setAdjustingItem(item);
                            resetAdjust({ amount: 5, type: 'Add', reason: 'Nhập hàng thêm' });
                          }}
                        >
                          Điều chỉnh tồn
                        </Button>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Adjust Stock Modal */}
      <Modal
        isOpen={!!adjustingItem}
        onClose={() => setAdjustingItem(null)}
        title={`Điều Chỉnh Kho: ${adjustingItem?.itemName || ''}`}
        description="Ghi nhận số lượng thực tế nhập thêm hoặc hao hụt trong quá trình pha chế."
      >
        <form onSubmit={handleSubmitAdjust(onAdjustSubmit)} className="space-y-4">
          <div className="p-3 bg-neutral-100 rounded border border-neutral-200 text-xs flex justify-between items-center">
            <span className="text-neutral-600 font-medium">Tồn kho hiện tại:</span>
            <span className="font-mono font-black text-sm text-neutral-900">
              {adjustingItem?.currentStock} {adjustingItem?.unit}
            </span>
          </div>

          <Select
            label="Loại điều chỉnh"
            options={[
              { value: 'Add', label: 'Cộng thêm (+) Nhập hàng mới' },
              { value: 'Deduct', label: 'Trừ bớt (-) Hao hụt, hỏng hóc' },
            ]}
            {...registerAdjust('type')}
            error={errorsAdjust.type?.message}
          />

          <Input
            label={`Số lượng (${adjustingItem?.unit || 'đơn vị'})`}
            type="number"
            step="any"
            placeholder="5"
            required
            {...registerAdjust('amount')}
            error={errorsAdjust.amount?.message}
          />

          <Input
            label="Lý do điều chỉnh kho"
            placeholder="VD: Nhập thêm từ nhà cung cấp Phương Nam"
            required
            {...registerAdjust('reason')}
            error={errorsAdjust.reason?.message}
          />

          <div className="flex items-center justify-end gap-3 pt-5 border-t border-neutral-200">
            <Button
              type="button"
              variant="outline"
              onClick={() => setAdjustingItem(null)}
            >
              Hủy
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={isSubmittingAdjust}
              disabled={!isValidAdjust || isSubmittingAdjust}
            >
              Lưu thay đổi tồn kho
            </Button>
          </div>
        </form>
      </Modal>

      {/* Add Inventory Item Modal */}
      <Modal
        isOpen={isAddItemModalOpen}
        onClose={() => setIsAddItemModalOpen(false)}
        title="Thêm Nguyên Liệu Vào Kho"
        description="Khai báo tên hàng, đơn vị tính và định mức tồn kho tối thiểu."
      >
        <form onSubmit={handleSubmitAdd(onAddItemSubmit)} className="space-y-4">
          <Input
            label="Tên nguyên vật liệu"
            placeholder="VD: Cà phê Arabica Cầu Đất"
            required
            {...registerAdd('itemName')}
            error={errorsAdd.itemName?.message}
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Select
              label="Đơn vị tính"
              options={[
                { value: 'kg', label: 'Kilogram (kg)' },
                { value: 'lít', label: 'Lít (l)' },
                { value: 'hộp (1kg)', label: 'Hộp (1kg)' },
                { value: 'gói', label: 'Gói' },
                { value: 'cái', label: 'Cái / Chiếc' },
              ]}
              {...registerAdd('unit')}
              error={errorsAdd.unit?.message}
            />

            <Input
              label="Số lượng ban đầu"
              type="number"
              step="any"
              placeholder="10"
              required
              {...registerAdd('currentStock')}
              error={errorsAdd.currentStock?.message}
            />
          </div>

          <Input
            label="Ngưỡng cảnh báo hết hàng (Reorder Level)"
            type="number"
            step="any"
            placeholder="5"
            required
            {...registerAdd('reorderLevel')}
            error={errorsAdd.reorderLevel?.message}
            helperText="Khi tồn kho bằng hoặc thấp hơn mức này, hệ thống sẽ bật cảnh báo đỏ"
          />

          <div className="flex items-center justify-end gap-3 pt-5 border-t border-neutral-200">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsAddItemModalOpen(false)}
            >
              Hủy
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={isSubmittingAdd}
              disabled={!isValidAdd || isSubmittingAdd}
            >
              Khai báo nguyên liệu
            </Button>
          </div>
        </form>
      </Modal>
    </PageContainer>
  );
};
