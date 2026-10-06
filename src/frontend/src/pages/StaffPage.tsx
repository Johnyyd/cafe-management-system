import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Search, Mail, Phone, Building2, UserX, UserCheck } from 'lucide-react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Button } from '../components/common/Button';
import { Input } from '../components/common/Input';
import { Select } from '../components/common/Select';
import { Badge } from '../components/common/Badge';
import { Modal } from '../components/common/Modal';
import { EmptyState } from '../components/common/EmptyState';
import { useAppStore } from '../stores/appStore';
import { staffApi, shopsApi } from '../api/client';
import { staffSchema, StaffFormData } from '../schemas';
import { StaffMember, Shop, StaffRole } from '../types';

export const StaffPage: React.FC = () => {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [shops, setShops] = useState<Shop[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [shopFilter, setShopFilter] = useState<string>('all');
  const addNotification = useAppStore((state) => state.addNotification);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isValid },
  } = useForm<StaffFormData>({
    resolver: zodResolver(staffSchema),
    mode: 'onBlur',
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      role: 'Cashier',
      shopId: '',
    },
  });

  const loadData = async () => {
    setLoading(true);
    try {
      const [staffData, shopsData] = await Promise.all([
        staffApi.getAll(),
        shopsApi.getAll(),
      ]);
      setStaff(staffData);
      setShops(shopsData);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi tải dữ liệu',
        message: 'Không thể tải danh sách nhân viên.',
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreateStaff = async (data: StaffFormData) => {
    try {
      const newStaff = await staffApi.create({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        role: data.role,
        shopId: data.shopId,
        status: 'Active',
        contact: {
          phone: data.phone,
          email: data.email,
        },
      });
      setStaff((prev) => [newStaff, ...prev]);
      setIsModalOpen(false);
      reset();
      addNotification({
        type: 'success',
        title: 'Thêm nhân viên thành công',
        message: `Đã cấp tài khoản cho ${data.firstName} ${data.lastName} (${data.role}).`,
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi tạo nhân viên',
        message: 'Không thể lưu nhân viên mới. Vui lòng thử lại.',
      });
    }
  };

  const handleToggleStatus = async (member: StaffMember) => {
    try {
      const nextStatus = member.status === 'Active' ? 'Terminated' : 'Active';
      const updated = await staffApi.update(member.id, { status: nextStatus });
      setStaff((prev) => prev.map((s) => (s.id === member.id ? updated : s)));
      addNotification({
        type: 'info',
        title: 'Cập nhật trạng thái',
        message: `Đã ${nextStatus === 'Active' ? 'kích hoạt' : 'khóa'} tài khoản ${member.firstName} ${member.lastName}.`,
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi cập nhật',
        message: 'Không thể đổi trạng thái tài khoản.',
      });
    }
  };

  const filteredStaff = staff.filter((s) => {
    const fullName = `${s.firstName} ${s.lastName}`.toLowerCase();
    const matchesSearch =
      fullName.includes(searchTerm.toLowerCase()) ||
      s.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (s.contact?.phone && s.contact.phone.includes(searchTerm));
    const matchesRole = roleFilter === 'all' || s.role === roleFilter;
    const matchesShop = shopFilter === 'all' || s.shopId === shopFilter;
    return matchesSearch && matchesRole && matchesShop;
  });

  const getRoleBadgeVariant = (role: StaffRole) => {
    switch (role) {
      case 'Admin':
        return 'primary';
      case 'Manager':
        return 'neutral';
      case 'Cashier':
        return 'success';
      case 'Barista':
        return 'warning';
      case 'InventoryStaff':
        return 'neutral';
      default:
        return 'neutral';
    }
  };

  return (
    <PageContainer
      title="Quản Lý Nhân Sự & Phân Quyền"
      subtitle="Danh sách nhân viên, gán chi nhánh và vai trò bảo mật truy cập"
      actions={
        <Button
          variant="primary"
          icon={<Plus className="w-4 h-4" />}
          onClick={() => {
            reset();
            setIsModalOpen(true);
          }}
        >
          Thêm nhân viên
        </Button>
      }
    >
      <div className="space-y-6">

      {/* Filter and Search Bar */}
      <Card>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Input
            placeholder="Tìm theo tên, email hoặc SĐT..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            icon={<Search className="w-4 h-4 text-neutral-400" />}
          />
          <Select
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
            options={[
              { value: 'all', label: 'Tất cả vai trò' },
              { value: 'Admin', label: 'Admin (Quản trị hệ thống)' },
              { value: 'Manager', label: 'Manager (Quản lý cửa hàng)' },
              { value: 'Cashier', label: 'Cashier (Thu ngân)' },
              { value: 'Barista', label: 'Barista (Pha chế)' },
              { value: 'InventoryStaff', label: 'InventoryStaff (Kho)' },
            ]}
          />
          <Select
            value={shopFilter}
            onChange={(e) => setShopFilter(e.target.value)}
            options={[
              { value: 'all', label: 'Tất cả chi nhánh' },
              ...shops.map((s) => ({ value: s.id, label: s.name })),
            ]}
          />
        </div>
      </Card>

      {/* Staff Table */}
      <Card noPadding>
        {loading ? (
          <div className="p-8 text-center text-sm text-neutral-500">Đang tải danh sách nhân viên...</div>
        ) : filteredStaff.length === 0 ? (
          <EmptyState
            title="Không tìm thấy nhân viên"
            description="Hãy thử thay đổi điều kiện tìm kiếm hoặc thêm nhân viên mới vào hệ thống."
            action={
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setSearchTerm('');
                  setRoleFilter('all');
                  setShopFilter('all');
                }}
              >
                Xóa bộ lọc
              </Button>
            }
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-neutral-700">
              <thead className="bg-neutral-50 text-xs font-semibold text-neutral-600 border-b border-neutral-200">
                <tr>
                  <th className="py-3 px-4">Nhân viên</th>
                  <th className="py-3 px-4">Liên hệ</th>
                  <th className="py-3 px-4">Vai trò</th>
                  <th className="py-3 px-4">Chi nhánh</th>
                  <th className="py-3 px-4">Trạng thái</th>
                  <th className="py-3 px-4 text-right">Hành động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-200">
                {filteredStaff.map((member) => (
                  <tr key={member.id} className="hover:bg-neutral-50">
                    <td className="py-3 px-4">
                      <div className="font-medium text-neutral-900">{member.firstName} {member.lastName}</div>
                      <div className="text-xs text-neutral-400 font-mono">ID: {member.id}</div>
                    </td>
                    <td className="py-3 px-4">
                      <div className="flex items-center gap-1.5 text-xs text-neutral-600">
                        <Mail className="w-3.5 h-3.5 text-neutral-400" />
                        {member.email}
                      </div>
                      <div className="flex items-center gap-1.5 text-xs text-neutral-600 mt-0.5">
                        <Phone className="w-3.5 h-3.5 text-neutral-400" />
                        {member.contact?.phone || 'N/A'}
                      </div>
                    </td>
                    <td className="py-3 px-4">
                      <Badge variant={getRoleBadgeVariant(member.role)}>
                        {member.role}
                      </Badge>
                    </td>
                    <td className="py-3 px-4">
                      <div className="flex items-center gap-1.5 text-xs text-neutral-700">
                        <Building2 className="w-3.5 h-3.5 text-neutral-400" />
                        {shops.find((s) => s.id === member.shopId)?.name || 'Chưa gán'}
                      </div>
                    </td>
                    <td className="py-3 px-4">
                      <Badge variant={member.status === 'Active' ? 'success' : 'error'}>
                        {member.status === 'Active' ? 'Đang hoạt động' : 'Đã khóa'}
                      </Badge>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleToggleStatus(member)}
                        icon={member.status === 'Active' ? <UserX className="w-3.5 h-3.5 text-red-600" /> : <UserCheck className="w-3.5 h-3.5 text-green-600" />}
                      >
                        {member.status === 'Active' ? 'Khóa' : 'Kích hoạt'}
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Add Staff Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Thêm Nhân Viên Mới"
        description="Điền thông tin định danh và phân bổ chi nhánh công tác."
      >
        <form onSubmit={handleSubmit(handleCreateStaff)} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="Họ đệm"
              required
              placeholder="Nguyễn"
              {...register('firstName')}
              error={errors.firstName?.message}
            />
            <Input
              label="Tên"
              required
              placeholder="Văn An"
              {...register('lastName')}
              error={errors.lastName?.message}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="Email"
              type="email"
              required
              placeholder="nhanvien@cafe.vn"
              {...register('email')}
              error={errors.email?.message}
            />
            <Input
              label="Số điện thoại"
              required
              placeholder="0901234567"
              {...register('phone')}
              error={errors.phone?.message}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="Vai trò (Role)"
              required
              {...register('role')}
              error={errors.role?.message}
              options={[
                { value: 'Admin', label: 'Admin (Quản trị viên)' },
                { value: 'Manager', label: 'Manager (Quản lý chi nhánh)' },
                { value: 'Cashier', label: 'Cashier (Thu ngân)' },
                { value: 'Barista', label: 'Barista (Pha chế)' },
                { value: 'InventoryStaff', label: 'InventoryStaff (Thủ kho)' },
              ]}
            />
            <Select
              label="Chi nhánh làm việc"
              required
              {...register('shopId')}
              error={errors.shopId?.message}
              options={[
                { value: '', label: '-- Chọn chi nhánh --' },
                ...shops.map((s) => ({ value: s.id, label: s.name })),
              ]}
            />
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-neutral-200">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsModalOpen(false)}
            >
              Hủy
            </Button>
            <Button
              type="submit"
              variant="primary"
              loading={isSubmitting}
              disabled={!isValid || isSubmitting}
            >
              Tạo tài khoản
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  </PageContainer>
  );
};

