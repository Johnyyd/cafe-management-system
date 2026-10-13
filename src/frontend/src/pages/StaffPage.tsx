import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Plus,
  Search,
  Mail,
  Phone,
  Building2,
  UserX,
  UserCheck,
  Trash2,
  Calendar,
  Clock,
  Play,
  CheckCircle2,
  XCircle,
  Users,
  CalendarDays,
  Store,
  RefreshCw,
} from 'lucide-react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Button } from '../components/common/Button';
import { Input } from '../components/common/Input';
import { Select } from '../components/common/Select';
import { Badge } from '../components/common/Badge';
import { Modal } from '../components/common/Modal';
import { EmptyState } from '../components/common/EmptyState';
import { useAppStore } from '../stores/appStore';
import { staffApi, shopsApi, shiftsApi } from '../api/client';
import { staffSchema, StaffFormData } from '../schemas';
import { StaffMember, Shop, StaffRole, Shift, ShiftStatus } from '../types';

export const StaffPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'staff' | 'shifts'>('staff');
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [shops, setShops] = useState<Shop[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadingShifts, setLoadingShifts] = useState<boolean>(false);

  // Modals for Staff
  const [isAddStaffModalOpen, setIsAddStaffModalOpen] = useState<boolean>(false);
  const [deletingMember, setDeletingMember] = useState<StaffMember | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);
  const [assigningMember, setAssigningMember] = useState<StaffMember | null>(null);
  const [selectedAssignShopId, setSelectedAssignShopId] = useState<string>('');
  const [isPrimaryAssignment, setIsPrimaryAssignment] = useState<boolean>(true);
  const [isAssigning, setIsAssigning] = useState<boolean>(false);

  // Filters for Staff
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [shopFilter, setShopFilter] = useState<string>('all');

  // Modals & Forms for Shifts
  const [isCreateShiftModalOpen, setIsCreateShiftModalOpen] = useState<boolean>(false);
  const [isCreatingShift, setIsCreatingShift] = useState<boolean>(false);
  const [deletingShiftId, setDeletingShiftId] = useState<string | null>(null);

  // Shift form state
  const [shiftStaffId, setShiftStaffId] = useState<string>('');
  const [shiftShopId, setShiftShopId] = useState<string>('');
  const [shiftDate, setShiftDate] = useState<string>(() => new Date().toISOString().split('T')[0]);
  const [shiftPreset, setShiftPreset] = useState<'morning' | 'afternoon' | 'evening' | 'custom'>('morning');
  const [shiftStartTime, setShiftStartTime] = useState<string>('06:30');
  const [shiftEndTime, setShiftEndTime] = useState<string>('14:30');
  const [shiftNotes, setShiftNotes] = useState<string>('Pha chế & Phục vụ chính');

  // Filters for Shifts
  const [shiftSearchStaffId, setShiftSearchStaffId] = useState<string>('all');
  const [shiftFilterShopId, setShiftFilterShopId] = useState<string>('all');
  const [shiftFilterStatus, setShiftFilterStatus] = useState<string>('all');
  const [shiftFilterDate, setShiftFilterDate] = useState<string>('');

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
      const [staffData, shopsData, shiftsData] = await Promise.all([
        staffApi.getAll(),
        shopsApi.getAll(),
        shiftsApi.getAll().catch(() => []),
      ]);
      setStaff(staffData);
      setShops(shopsData);
      setShifts(shiftsData);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi tải dữ liệu',
        message: 'Không thể tải danh sách nhân viên hoặc chi nhánh.',
      });
    } finally {
      setLoading(false);
    }
  };

  const loadShiftsOnly = async () => {
    setLoadingShifts(true);
    try {
      const shiftsData = await shiftsApi.getAll();
      setShifts(shiftsData);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi tải ca làm việc',
        message: 'Không thể làm mới danh sách ca làm việc.',
      });
    } finally {
      setLoadingShifts(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  // When shift preset changes
  const handleShiftPresetChange = (preset: 'morning' | 'afternoon' | 'evening' | 'custom') => {
    setShiftPreset(preset);
    switch (preset) {
      case 'morning':
        setShiftStartTime('06:30');
        setShiftEndTime('14:30');
        setShiftNotes('Ca sáng: Mở quán, kiểm tra máy pha, phục vụ');
        break;
      case 'afternoon':
        setShiftStartTime('14:30');
        setShiftEndTime('22:30');
        setShiftNotes('Ca chiều: Cao điểm tối, kiểm kê doanh thu, chốt ca');
        break;
      case 'evening':
        setShiftStartTime('18:00');
        setShiftEndTime('23:00');
        setShiftNotes('Ca tối tăng cường: Dọn dẹp quầy bar, vệ sinh thiết bị');
        break;
      case 'custom':
        break;
    }
  };

  // Staff CRUD Handlers
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
      setIsAddStaffModalOpen(false);
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

  const handleDeleteMember = async () => {
    if (!deletingMember) return;
    setIsDeleting(true);
    try {
      await staffApi.delete(deletingMember.id);
      setStaff((prev) => prev.filter((s) => s.id !== deletingMember.id));
      addNotification({
        type: 'success',
        title: 'Đã xóa nhân viên',
        message: `Đã xóa nhân viên ${deletingMember.firstName} ${deletingMember.lastName} khỏi hệ thống.`,
      });
      setDeletingMember(null);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi xóa nhân viên',
        message: 'Không thể xóa nhân viên khỏi hệ thống.',
      });
    } finally {
      setIsDeleting(false);
    }
  };

  // Assign Shop Handler
  const openAssignShopModal = (member: StaffMember) => {
    setAssigningMember(member);
    setSelectedAssignShopId(member.shopId || shops[0]?.id || '');
    setIsPrimaryAssignment(true);
  };

  const handleSaveShopAssignment = async () => {
    if (!assigningMember || !selectedAssignShopId) return;
    setIsAssigning(true);
    try {
      await staffApi.assignShop(assigningMember.id, selectedAssignShopId, isPrimaryAssignment);
      setStaff((prev) =>
        prev.map((s) =>
          s.id === assigningMember.id ? { ...s, shopId: selectedAssignShopId } : s
        )
      );
      const targetShop = shops.find((s) => s.id === selectedAssignShopId);
      addNotification({
        type: 'success',
        title: 'Gán chi nhánh thành công',
        message: `Đã phân công ${assigningMember.firstName} ${assigningMember.lastName} làm việc tại ${targetShop?.name || 'chi nhánh đã chọn'}.`,
      });
      setAssigningMember(null);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi gán chi nhánh',
        message: 'Không thể lưu phân công chi nhánh. Vui lòng thử lại.',
      });
    } finally {
      setIsAssigning(false);
    }
  };

  // Shift Handlers
  const handleOpenCreateShift = (prefilledStaffId?: string) => {
    const defaultStaff = prefilledStaffId
      ? staff.find((s) => s.id === prefilledStaffId)
      : staff[0];
    setShiftStaffId(defaultStaff?.id || '');
    setShiftShopId(defaultStaff?.shopId || shops[0]?.id || '');
    setShiftDate(new Date().toISOString().split('T')[0]);
    handleShiftPresetChange('morning');
    setIsCreateShiftModalOpen(true);
  };

  const handleCreateShiftSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!shiftStaffId || !shiftShopId || !shiftDate || !shiftStartTime || !shiftEndTime) {
      addNotification({
        type: 'info',
        title: 'Thiếu thông tin ca làm',
        message: 'Vui lòng chọn nhân viên, chi nhánh và khung giờ làm việc.',
      });
      return;
    }

    setIsCreatingShift(true);
    try {
      const startDateTime = new Date(`${shiftDate}T${shiftStartTime}:00`).toISOString();
      const endDateTime = new Date(`${shiftDate}T${shiftEndTime}:00`).toISOString();

      const newShift = await shiftsApi.create({
        staffId: shiftStaffId,
        shopId: shiftShopId,
        startTime: startDateTime,
        endTime: endDateTime,
        notes: shiftNotes,
      });

      setShifts((prev) => [newShift, ...prev]);
      setIsCreateShiftModalOpen(false);
      const assignedStaff = staff.find((s) => s.id === shiftStaffId);
      addNotification({
        type: 'success',
        title: 'Xếp ca làm việc thành công',
        message: `Đã xếp ca (${shiftStartTime} - ${shiftEndTime}) cho ${assignedStaff?.firstName} ${assignedStaff?.lastName}.`,
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi xếp ca làm việc',
        message: 'Không thể lưu ca làm việc. Vui lòng kiểm tra lại thời gian.',
      });
    } finally {
      setIsCreatingShift(false);
    }
  };

  const handleStartShift = async (shiftId: string) => {
    try {
      await shiftsApi.start(shiftId);
      setShifts((prev) =>
        prev.map((s) => (s.id === shiftId ? { ...s, status: 'InProgress' } : s))
      );
      addNotification({
        type: 'info',
        title: 'Bắt đầu ca làm',
        message: 'Ca làm việc đã được chuyển sang trạng thái Đang làm việc.',
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi',
        message: 'Không thể chuyển trạng thái bắt đầu ca.',
      });
    }
  };

  const handleCompleteShift = async (shiftId: string) => {
    try {
      await shiftsApi.complete(shiftId);
      setShifts((prev) =>
        prev.map((s) => (s.id === shiftId ? { ...s, status: 'Completed' } : s))
      );
      addNotification({
        type: 'success',
        title: 'Hoàn thành ca làm việc',
        message: 'Đã hoàn thành và ghi nhận giờ công cho nhân viên.',
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi',
        message: 'Không thể ghi nhận hoàn thành ca.',
      });
    }
  };

  const handleCancelShift = async (shiftId: string) => {
    try {
      await shiftsApi.cancel(shiftId, 'Quản lý hủy phân công');
      setShifts((prev) =>
        prev.map((s) => (s.id === shiftId ? { ...s, status: 'Cancelled' } : s))
      );
      addNotification({
        type: 'info',
        title: 'Đã hủy ca làm việc',
        message: 'Ca làm việc đã được hủy bỏ.',
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi',
        message: 'Không thể hủy ca làm việc.',
      });
    }
  };

  const handleDeleteShift = async (shiftId: string) => {
    try {
      await shiftsApi.delete(shiftId);
      setShifts((prev) => prev.filter((s) => s.id !== shiftId));
      addNotification({
        type: 'success',
        title: 'Đã xóa ca',
        message: 'Ca làm việc đã được xóa khỏi lịch trực.',
      });
      setDeletingShiftId(null);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi',
        message: 'Không thể xóa ca làm việc.',
      });
    }
  };

  // Filtered Staff
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

  // Filtered Shifts
  const filteredShifts = shifts.filter((sh) => {
    const matchesStaff = shiftSearchStaffId === 'all' || sh.staffId === shiftSearchStaffId;
    const matchesShop = shiftFilterShopId === 'all' || sh.shopId === shiftFilterShopId;
    const matchesStatus = shiftFilterStatus === 'all' || sh.status === shiftFilterStatus;
    const matchesDate =
      !shiftFilterDate ||
      (sh.startTime && sh.startTime.startsWith(shiftFilterDate));
    return matchesStaff && matchesShop && matchesStatus && matchesDate;
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

  const getShiftBadgeVariant = (status: ShiftStatus) => {
    switch (status) {
      case 'Scheduled':
        return 'warning';
      case 'InProgress':
        return 'primary';
      case 'Completed':
        return 'success';
      case 'Cancelled':
        return 'error';
      default:
        return 'neutral';
    }
  };

  const formatShiftTime = (isoString: string) => {
    try {
      const d = new Date(isoString);
      return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    } catch {
      return isoString;
    }
  };

  const formatShiftDate = (isoString: string) => {
    try {
      const d = new Date(isoString);
      return d.toLocaleDateString('vi-VN', {
        weekday: 'short',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return isoString;
    }
  };

  return (
    <PageContainer
      title="Quản Lý Nhân Sự & Ca Làm Việc"
      subtitle="Quản lý hồ sơ nhân viên, phân bổ chi nhánh công tác và xếp lịch trực ca làm việc"
      actions={
        <div className="flex items-center gap-2">
          {activeTab === 'staff' ? (
            <Button
              variant="primary"
              icon={<Plus className="w-4 h-4" />}
              onClick={() => {
                reset();
                setIsAddStaffModalOpen(true);
              }}
            >
              Thêm nhân viên
            </Button>
          ) : (
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                icon={<RefreshCw className={`w-3.5 h-3.5 ${loadingShifts ? 'animate-spin' : ''}`} />}
                onClick={loadShiftsOnly}
                disabled={loadingShifts}
              >
                Làm mới
              </Button>
              <Button
                variant="primary"
                icon={<CalendarDays className="w-4 h-4" />}
                onClick={() => handleOpenCreateShift()}
              >
                Xếp ca làm mới
              </Button>
            </div>
          )}
        </div>
      }
    >
      <div className="space-y-6">
        {/* Navigation Tabs */}
        <div className="flex border-b border-neutral-200 gap-2">
          <button
            onClick={() => setActiveTab('staff')}
            className={`flex items-center gap-2 px-5 py-3 font-medium text-sm border-b-2 transition-all ${
              activeTab === 'staff'
                ? 'border-primary-600 text-primary-700 bg-primary-50/50'
                : 'border-transparent text-neutral-500 hover:text-neutral-800 hover:border-neutral-300'
            }`}
          >
            <Users className="w-4 h-4" />
            <span>Nhân sự & Phân quyền</span>
            <span className="ml-1 px-2 py-0.5 text-xs rounded-full bg-neutral-100 text-neutral-700 font-semibold">
              {staff.length}
            </span>
          </button>

          <button
            onClick={() => setActiveTab('shifts')}
            className={`flex items-center gap-2 px-5 py-3 font-medium text-sm border-b-2 transition-all ${
              activeTab === 'shifts'
                ? 'border-primary-600 text-primary-700 bg-primary-50/50'
                : 'border-transparent text-neutral-500 hover:text-neutral-800 hover:border-neutral-300'
            }`}
          >
            <Clock className="w-4 h-4" />
            <span>Xếp ca & Lịch làm việc</span>
            <span className="ml-1 px-2 py-0.5 text-xs rounded-full bg-neutral-100 text-neutral-700 font-semibold">
              {shifts.length}
            </span>
          </button>
        </div>

        {/* ========================================================= */}
        {/* TAB 1: NHÂN SỰ & PHÂN QUYỀN                                */}
        {/* ========================================================= */}
        {activeTab === 'staff' && (
          <div className="space-y-6">
            {/* Quick Stat Summary */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Tổng nhân sự</p>
                    <p className="text-2xl font-bold text-neutral-900 mt-1">{staff.length}</p>
                  </div>
                  <div className="p-3 bg-primary-50 rounded-lg text-primary-600">
                    <Users className="w-6 h-6" />
                  </div>
                </div>
              </Card>
              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Đang hoạt động</p>
                    <p className="text-2xl font-bold text-emerald-600 mt-1">
                      {staff.filter((s) => s.status === 'Active').length}
                    </p>
                  </div>
                  <div className="p-3 bg-emerald-50 rounded-lg text-emerald-600">
                    <UserCheck className="w-6 h-6" />
                  </div>
                </div>
              </Card>
              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Đã gán chi nhánh</p>
                    <p className="text-2xl font-bold text-amber-600 mt-1">
                      {staff.filter((s) => s.shopId && s.shopId.trim() !== '').length}
                    </p>
                  </div>
                  <div className="p-3 bg-amber-50 rounded-lg text-amber-600">
                    <Store className="w-6 h-6" />
                  </div>
                </div>
              </Card>
            </div>

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
                        <th className="py-3 px-4">Chi nhánh công tác</th>
                        <th className="py-3 px-4">Trạng thái</th>
                        <th className="py-3 px-4 text-right">Hành động</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-neutral-200">
                      {filteredStaff.map((member) => {
                        const assignedShop = shops.find((s) => s.id === member.shopId);
                        return (
                          <tr key={member.id} className="hover:bg-neutral-50 transition-colors">
                            <td className="py-3 px-4">
                              <div className="font-semibold text-neutral-900">
                                {member.firstName} {member.lastName}
                              </div>
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
                              <Badge variant={getRoleBadgeVariant(member.role)}>{member.role}</Badge>
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center gap-2">
                                <div className="flex items-center gap-1.5 text-xs font-medium text-neutral-800">
                                  <Building2 className="w-3.5 h-3.5 text-primary-600" />
                                  {assignedShop ? (
                                    <span>{assignedShop.name}</span>
                                  ) : (
                                    <span className="text-amber-600 font-normal italic">Chưa gán chi nhánh</span>
                                  )}
                                </div>
                                <button
                                  type="button"
                                  onClick={() => openAssignShopModal(member)}
                                  className="text-xs text-primary-600 hover:text-primary-800 underline px-1 py-0.5 rounded hover:bg-primary-50 transition-colors"
                                  title="Đổi hoặc gán chi nhánh làm việc"
                                >
                                  {assignedShop ? 'Đổi' : 'Gán ngay'}
                                </button>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <Badge variant={member.status === 'Active' ? 'success' : 'error'}>
                                {member.status === 'Active' ? 'Đang hoạt động' : 'Đã khóa'}
                              </Badge>
                            </td>
                            <td className="py-3 px-4 text-right">
                              <div className="flex items-center justify-end gap-1.5">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleOpenCreateShift(member.id)}
                                  title="Xếp ca làm việc cho nhân viên này"
                                  icon={<Calendar className="w-3.5 h-3.5 text-primary-600" />}
                                >
                                  Xếp ca
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleToggleStatus(member)}
                                  icon={
                                    member.status === 'Active' ? (
                                      <UserX className="w-3.5 h-3.5 text-amber-600" />
                                    ) : (
                                      <UserCheck className="w-3.5 h-3.5 text-green-600" />
                                    )
                                  }
                                >
                                  {member.status === 'Active' ? 'Khóa' : 'Mở'}
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="text-red-600 hover:text-red-700 hover:bg-red-50 p-1.5 border border-transparent hover:border-red-200"
                                  icon={<Trash2 className="w-3.5 h-3.5 text-red-500" />}
                                  onClick={() => setDeletingMember(member)}
                                >
                                  Xóa
                                </Button>
                              </div>
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
        )}

        {/* ========================================================= */}
        {/* TAB 2: XẾP CA & LỊCH LÀM VIỆC                             */}
        {/* ========================================================= */}
        {activeTab === 'shifts' && (
          <div className="space-y-6">
            {/* Shifts Stats Summary */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Tổng số ca</p>
                    <p className="text-2xl font-bold text-neutral-900 mt-1">{shifts.length}</p>
                  </div>
                  <div className="p-3 bg-neutral-100 rounded-lg text-neutral-700">
                    <Calendar className="w-5 h-5" />
                  </div>
                </div>
              </Card>

              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Đã lên lịch</p>
                    <p className="text-2xl font-bold text-amber-600 mt-1">
                      {shifts.filter((s) => s.status === 'Scheduled').length}
                    </p>
                  </div>
                  <div className="p-3 bg-amber-50 rounded-lg text-amber-600">
                    <Clock className="w-5 h-5" />
                  </div>
                </div>
              </Card>

              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Đang diễn ra</p>
                    <p className="text-2xl font-bold text-primary-600 mt-1">
                      {shifts.filter((s) => s.status === 'InProgress').length}
                    </p>
                  </div>
                  <div className="p-3 bg-primary-50 rounded-lg text-primary-600 animate-pulse">
                    <Play className="w-5 h-5" />
                  </div>
                </div>
              </Card>

              <Card>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-medium text-neutral-500 uppercase tracking-wider">Đã hoàn thành</p>
                    <p className="text-2xl font-bold text-emerald-600 mt-1">
                      {shifts.filter((s) => s.status === 'Completed').length}
                    </p>
                  </div>
                  <div className="p-3 bg-emerald-50 rounded-lg text-emerald-600">
                    <CheckCircle2 className="w-5 h-5" />
                  </div>
                </div>
              </Card>
            </div>

            {/* Filter Bar for Shifts */}
            <Card>
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
                <Select
                  value={shiftSearchStaffId}
                  onChange={(e) => setShiftSearchStaffId(e.target.value)}
                  options={[
                    { value: 'all', label: 'Tất cả nhân viên' },
                    ...staff.map((s) => ({
                      value: s.id,
                      label: `${s.firstName} ${s.lastName} (${s.role})`,
                    })),
                  ]}
                />
                <Select
                  value={shiftFilterShopId}
                  onChange={(e) => setShiftFilterShopId(e.target.value)}
                  options={[
                    { value: 'all', label: 'Tất cả chi nhánh' },
                    ...shops.map((s) => ({ value: s.id, label: s.name })),
                  ]}
                />
                <Select
                  value={shiftFilterStatus}
                  onChange={(e) => setShiftFilterStatus(e.target.value)}
                  options={[
                    { value: 'all', label: 'Tất cả trạng thái ca' },
                    { value: 'Scheduled', label: 'Đã lên lịch' },
                    { value: 'InProgress', label: 'Đang làm việc' },
                    { value: 'Completed', label: 'Đã hoàn thành' },
                    { value: 'Cancelled', label: 'Đã hủy' },
                  ]}
                />
                <Input
                  type="date"
                  value={shiftFilterDate}
                  onChange={(e) => setShiftFilterDate(e.target.value)}
                />
              </div>
            </Card>

            {/* Shifts Table */}
            <Card noPadding>
              {loading || loadingShifts ? (
                <div className="p-8 text-center text-sm text-neutral-500">Đang tải danh sách ca làm việc...</div>
              ) : filteredShifts.length === 0 ? (
                <EmptyState
                  title="Chưa có ca làm việc nào phù hợp"
                  description="Hãy thêm mới ca làm việc cho nhân viên hoặc thay đổi bộ lọc ngày/chi nhánh."
                  action={
                    <Button
                      variant="primary"
                      size="sm"
                      icon={<Plus className="w-4 h-4" />}
                      onClick={() => handleOpenCreateShift()}
                    >
                      Xếp ca làm việc ngay
                    </Button>
                  }
                />
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm text-neutral-700">
                    <thead className="bg-neutral-50 text-xs font-semibold text-neutral-600 border-b border-neutral-200">
                      <tr>
                        <th className="py-3 px-4">Nhân viên trực ca</th>
                        <th className="py-3 px-4">Chi nhánh</th>
                        <th className="py-3 px-4">Thời gian ca làm</th>
                        <th className="py-3 px-4">Ghi chú phân công</th>
                        <th className="py-3 px-4">Trạng thái</th>
                        <th className="py-3 px-4 text-right">Thao tác ca</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-neutral-200">
                      {filteredShifts.map((sh) => {
                        const shiftStaff = staff.find((s) => s.id === sh.staffId);
                        const shiftShop = shops.find((s) => s.id === sh.shopId);
                        return (
                          <tr key={sh.id} className="hover:bg-neutral-50 transition-colors">
                            <td className="py-3 px-4">
                              <div className="font-semibold text-neutral-900">
                                {shiftStaff ? `${shiftStaff.firstName} ${shiftStaff.lastName}` : 'Nhân sự không xác định'}
                              </div>
                              <div className="flex items-center gap-2 mt-0.5">
                                {shiftStaff && (
                                  <Badge variant={getRoleBadgeVariant(shiftStaff.role)}>
                                    {shiftStaff.role}
                                  </Badge>
                                )}
                                <span className="text-xs text-neutral-400 font-mono">ID: {sh.staffId.slice(-6)}</span>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="flex items-center gap-1.5 text-xs text-neutral-800 font-medium">
                                <Building2 className="w-3.5 h-3.5 text-neutral-400" />
                                {shiftShop?.name || 'Chi nhánh mặc định'}
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <div className="text-xs font-semibold text-neutral-900">
                                {formatShiftDate(sh.startTime)}
                              </div>
                              <div className="flex items-center gap-1.5 text-xs text-neutral-600 mt-0.5">
                                <Clock className="w-3.5 h-3.5 text-primary-600" />
                                <span className="font-mono font-medium">
                                  {formatShiftTime(sh.startTime)} - {formatShiftTime(sh.endTime)}
                                </span>
                              </div>
                            </td>
                            <td className="py-3 px-4">
                              <span className="text-xs text-neutral-600 italic">
                                {sh.notes || 'Không có ghi chú'}
                              </span>
                            </td>
                            <td className="py-3 px-4">
                              <Badge variant={getShiftBadgeVariant(sh.status)}>
                                {sh.status === 'Scheduled'
                                  ? 'Đã lên lịch'
                                  : sh.status === 'InProgress'
                                  ? 'Đang làm việc'
                                  : sh.status === 'Completed'
                                  ? 'Đã hoàn thành'
                                  : 'Đã hủy'}
                              </Badge>
                            </td>
                            <td className="py-3 px-4 text-right">
                              <div className="flex items-center justify-end gap-1.5">
                                {sh.status === 'Scheduled' && (
                                  <>
                                    <Button
                                      variant="outline"
                                      size="sm"
                                      onClick={() => handleStartShift(sh.id)}
                                      icon={<Play className="w-3.5 h-3.5 text-emerald-600" />}
                                    >
                                      Bắt đầu
                                    </Button>
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      onClick={() => handleCancelShift(sh.id)}
                                      icon={<XCircle className="w-3.5 h-3.5 text-amber-600" />}
                                    >
                                      Hủy
                                    </Button>
                                  </>
                                )}

                                {sh.status === 'InProgress' && (
                                  <>
                                    <Button
                                      variant="primary"
                                      size="sm"
                                      onClick={() => handleCompleteShift(sh.id)}
                                      icon={<CheckCircle2 className="w-3.5 h-3.5" />}
                                    >
                                      Xong ca
                                    </Button>
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      onClick={() => handleCancelShift(sh.id)}
                                      icon={<XCircle className="w-3.5 h-3.5 text-amber-600" />}
                                    >
                                      Hủy
                                    </Button>
                                  </>
                                )}

                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="text-red-600 hover:text-red-700 hover:bg-red-50 p-1.5 border border-transparent hover:border-red-200"
                                  icon={<Trash2 className="w-3.5 h-3.5 text-red-500" />}
                                  onClick={() => setDeletingShiftId(sh.id)}
                                  title="Xóa ca làm việc này"
                                >
                                  Xóa
                                </Button>
                              </div>
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
        )}

        {/* ========================================================= */}
        {/* MODAL: GÁN CHI NHÁNH CHO NHÂN VIÊN                        */}
        {/* ========================================================= */}
        <Modal
          isOpen={!!assigningMember}
          onClose={() => setAssigningMember(null)}
          title="Phân Bổ Chi Nhánh Công Tác"
          description={`Chỉ định chi nhánh quán nơi nhân viên ${assigningMember?.firstName} ${assigningMember?.lastName} sẽ làm việc.`}
        >
          <div className="space-y-4">
            <div className="p-3 bg-neutral-50 rounded-lg border border-neutral-200">
              <div className="text-sm font-semibold text-neutral-900">
                {assigningMember?.firstName} {assigningMember?.lastName}
              </div>
              <div className="text-xs text-neutral-500 mt-0.5">
                Vai trò: <span className="font-medium text-neutral-800">{assigningMember?.role}</span> | Email: {assigningMember?.email}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-1">
                Chọn chi nhánh công tác:
              </label>
              <Select
                value={selectedAssignShopId}
                onChange={(e) => setSelectedAssignShopId(e.target.value)}
                options={shops.map((s) => ({ value: s.id, label: s.name }))}
              />
            </div>

            <div className="flex items-center gap-2 pt-1">
              <input
                type="checkbox"
                id="isPrimaryCheckbox"
                checked={isPrimaryAssignment}
                onChange={(e) => setIsPrimaryAssignment(e.target.checked)}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500"
              />
              <label htmlFor="isPrimaryCheckbox" className="text-sm text-neutral-700 cursor-pointer">
                Đặt làm chi nhánh làm việc chính (Primary Shop)
              </label>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t border-neutral-200">
              <Button
                variant="outline"
                onClick={() => setAssigningMember(null)}
                disabled={isAssigning}
              >
                Hủy
              </Button>
              <Button
                variant="primary"
                onClick={handleSaveShopAssignment}
                loading={isAssigning}
                icon={<Building2 className="w-4 h-4" />}
              >
                Lưu phân công chi nhánh
              </Button>
            </div>
          </div>
        </Modal>

        {/* ========================================================= */}
        {/* MODAL: XẾP CA LÀM VIỆC MỚI                                */}
        {/* ========================================================= */}
        <Modal
          isOpen={isCreateShiftModalOpen}
          onClose={() => setIsCreateShiftModalOpen(false)}
          title="Xếp Ca Làm Việc Mới"
          description="Lên lịch trực ca làm việc cụ thể cho từng nhân viên theo chi nhánh."
        >
          <form onSubmit={handleCreateShiftSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-1">
                Chọn nhân viên trực ca:
              </label>
              <Select
                value={shiftStaffId}
                onChange={(e) => {
                  setShiftStaffId(e.target.value);
                  const selected = staff.find((s) => s.id === e.target.value);
                  if (selected?.shopId) {
                    setShiftShopId(selected.shopId);
                  }
                }}
                options={[
                  { value: '', label: '-- Chọn nhân viên --' },
                  ...staff.map((s) => ({
                    value: s.id,
                    label: `${s.firstName} ${s.lastName} - ${s.role} (${shops.find((sh) => sh.id === s.shopId)?.name || 'Chưa có quán'})`,
                  })),
                ]}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-1">
                Chi nhánh làm việc:
              </label>
              <Select
                value={shiftShopId}
                onChange={(e) => setShiftShopId(e.target.value)}
                options={[
                  { value: '', label: '-- Chọn chi nhánh --' },
                  ...shops.map((s) => ({ value: s.id, label: s.name })),
                ]}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-1">
                Ngày làm việc:
              </label>
              <Input
                type="date"
                required
                value={shiftDate}
                onChange={(e) => setShiftDate(e.target.value)}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Khung ca làm việc chuẩn:
              </label>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('morning')}
                  className={`p-2.5 rounded-lg border text-left transition-all ${
                    shiftPreset === 'morning'
                      ? 'border-primary-600 bg-primary-50 text-primary-800 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">☀️ Ca Sáng</div>
                  <div className="text-neutral-500 font-mono mt-0.5">06:30 - 14:30 (8h)</div>
                </button>
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('afternoon')}
                  className={`p-2.5 rounded-lg border text-left transition-all ${
                    shiftPreset === 'afternoon'
                      ? 'border-primary-600 bg-primary-50 text-primary-800 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">🌤️ Ca Chiều</div>
                  <div className="text-neutral-500 font-mono mt-0.5">14:30 - 22:30 (8h)</div>
                </button>
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('evening')}
                  className={`p-2.5 rounded-lg border text-left transition-all ${
                    shiftPreset === 'evening'
                      ? 'border-primary-600 bg-primary-50 text-primary-800 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">🌙 Ca Tối Tăng Cường</div>
                  <div className="text-neutral-500 font-mono mt-0.5">18:00 - 23:00 (5h)</div>
                </button>
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('custom')}
                  className={`p-2.5 rounded-lg border text-left transition-all ${
                    shiftPreset === 'custom'
                      ? 'border-primary-600 bg-primary-50 text-primary-800 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">⚙️ Tùy Chỉnh Giờ</div>
                  <div className="text-neutral-500 mt-0.5">Tự nhập giờ vào/ra</div>
                </button>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Giờ bắt đầu"
                type="time"
                required
                value={shiftStartTime}
                onChange={(e) => {
                  setShiftStartTime(e.target.value);
                  setShiftPreset('custom');
                }}
              />
              <Input
                label="Giờ kết thúc"
                type="time"
                required
                value={shiftEndTime}
                onChange={(e) => {
                  setShiftEndTime(e.target.value);
                  setShiftPreset('custom');
                }}
              />
            </div>

            <div>
              <Input
                label="Ghi chú nhiệm vụ ca"
                placeholder="Ví dụ: Pha chế chính quầy bar, thu ngân mở ca..."
                value={shiftNotes}
                onChange={(e) => setShiftNotes(e.target.value)}
              />
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t border-neutral-200">
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsCreateShiftModalOpen(false)}
                disabled={isCreatingShift}
              >
                Hủy
              </Button>
              <Button
                type="submit"
                variant="primary"
                loading={isCreatingShift}
                icon={<CalendarDays className="w-4 h-4" />}
              >
                Lưu ca làm việc
              </Button>
            </div>
          </form>
        </Modal>

        {/* ========================================================= */}
        {/* MODAL: THÊM NHÂN VIÊN MỚI                                 */}
        {/* ========================================================= */}
        <Modal
          isOpen={isAddStaffModalOpen}
          onClose={() => setIsAddStaffModalOpen(false)}
          title="Thêm Nhân Viên Mới"
          description="Điền thông tin định danh và phân bổ chi nhánh công tác ban đầu."
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
                onClick={() => setIsAddStaffModalOpen(false)}
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

        {/* ========================================================= */}
        {/* MODAL: XÓA NHÂN VIÊN                                      */}
        {/* ========================================================= */}
        <Modal
          isOpen={!!deletingMember}
          onClose={() => setDeletingMember(null)}
          title="Xác nhận xóa nhân viên"
          description="Thao tác này sẽ xóa hồ sơ nhân viên khỏi danh sách nhân sự của hệ thống."
        >
          <div className="space-y-4">
            <p className="text-sm text-neutral-700">
              Bạn có chắc chắn muốn xóa nhân viên{' '}
              <strong className="text-neutral-900">
                {deletingMember?.firstName} {deletingMember?.lastName}
              </strong>{' '}
              (Mã:{' '}
              <code className="text-xs bg-neutral-100 px-1 py-0.5 rounded font-mono">
                {deletingMember?.id}
              </code>
              )?
            </p>
            <div className="flex justify-end gap-2 pt-3 border-t border-neutral-200">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setDeletingMember(null)}
                disabled={isDeleting}
              >
                Hủy
              </Button>
              <Button
                variant="danger"
                size="sm"
                onClick={handleDeleteMember}
                loading={isDeleting}
                icon={<Trash2 className="w-3.5 h-3.5" />}
              >
                Xác nhận xóa
              </Button>
            </div>
          </div>
        </Modal>

        {/* ========================================================= */}
        {/* MODAL: XÓA CA LÀM VIỆC                                    */}
        {/* ========================================================= */}
        <Modal
          isOpen={!!deletingShiftId}
          onClose={() => setDeletingShiftId(null)}
          title="Xóa ca làm việc"
          description="Thao tác này sẽ xóa ca làm việc đã chọn khỏi hệ thống."
        >
          <div className="space-y-4">
            <p className="text-sm text-neutral-700">
              Bạn có chắc chắn muốn xóa ca làm việc này không? Lịch trực sẽ không còn hiển thị trên bảng.
            </p>
            <div className="flex justify-end gap-2 pt-3 border-t border-neutral-200">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setDeletingShiftId(null)}
              >
                Hủy
              </Button>
              <Button
                variant="danger"
                size="sm"
                onClick={() => deletingShiftId && handleDeleteShift(deletingShiftId)}
                icon={<Trash2 className="w-3.5 h-3.5" />}
              >
                Xác nhận xóa
              </Button>
            </div>
          </div>
        </Modal>
      </div>
    </PageContainer>
  );
};
