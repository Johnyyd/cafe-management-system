import React, { useState, useEffect, useMemo } from 'react';
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
  Calendar as CalendarIcon,
  Clock,
  Play,
  CheckCircle2,
  XCircle,
  Users,
  CalendarDays,
  Store,
  RefreshCw,
  ChevronLeft,
  ChevronRight,
  Edit,
  LayoutGrid,
  List,
  CalendarCheck,
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
import { useAuthStore } from '../stores/authStore';
import { staffApi, shopsApi, shiftsApi } from '../api/client';
import { staffSchema, StaffFormData } from '../schemas';
import { StaffMember, Shop, StaffRole, Shift, ShiftStatus } from '../types';

export const StaffPage: React.FC = () => {
  const { activeRole, user } = useAuthStore();
  const { currentShopId, setCurrentShopId, addNotification } = useAppStore();
  const isManagerOrAdmin = activeRole === 'Admin' || activeRole === 'Manager';

  // State tabs: Nếu là nhân viên thường thì mặc định và chỉ được xem tab ca làm việc
  const [activeTab, setActiveTab] = useState<'staff' | 'shifts'>(
    isManagerOrAdmin ? 'shifts' : 'shifts'
  );

  // View mode for shifts: 'calendar' (Thứ 2 - CN) or 'list' (Bảng danh sách)
  const [shiftViewMode, setShiftViewMode] = useState<'calendar' | 'list'>('calendar');

  // Week offset: 0 = tuần này, 1 = tuần tới (+1), 2 = tuần tới (+2), -1 = tuần trước
  const [weekOffset, setWeekOffset] = useState<number>(0);

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
  const [shopFilter, setShopFilter] = useState<string>(() => currentShopId || 'all');

  // Modals & Forms for Shifts
  const [isCreateShiftModalOpen, setIsCreateShiftModalOpen] = useState<boolean>(false);
  const [isCreatingShift, setIsCreatingShift] = useState<boolean>(false);
  const [editingShift, setEditingShift] = useState<Shift | null>(null);
  const [isUpdatingShift, setIsUpdatingShift] = useState<boolean>(false);

  // Create shift form state
  const [shiftStaffId, setShiftStaffId] = useState<string>('');
  const [shiftShopId, setShiftShopId] = useState<string>('');
  const [shiftDate, setShiftDate] = useState<string>(() => new Date().toISOString().split('T')[0]);
  const [shiftPreset, setShiftPreset] = useState<'morning' | 'afternoon' | 'evening' | 'custom'>('morning');
  const [shiftStartTime, setShiftStartTime] = useState<string>('06:30');
  const [shiftEndTime, setShiftEndTime] = useState<string>('14:30');
  const [shiftNotes, setShiftNotes] = useState<string>('Ca sáng: Mở quán, kiểm tra máy pha, phục vụ');

  // Edit shift form state
  const [editStaffId, setEditStaffId] = useState<string>('');
  const [editDate, setEditDate] = useState<string>('');
  const [editStartTime, setEditStartTime] = useState<string>('06:30');
  const [editEndTime, setEditEndTime] = useState<string>('14:30');
  const [editNotes, setEditNotes] = useState<string>('');

  // Filters for Shifts
  const [shiftSearchStaffId, setShiftSearchStaffId] = useState<string>('all');
  const [shiftFilterShopId, setShiftFilterShopId] = useState<string>(() => currentShopId || 'all');
  const [shiftFilterStatus, setShiftFilterStatus] = useState<string>('all');

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
        message: 'Không thể tải danh sách nhân viên hoặc ca làm việc.',
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
      addNotification({
        type: 'success',
        title: 'Đã cập nhật',
        message: 'Danh sách ca làm việc đã được đồng bộ mới nhất.',
      });
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

  // Xác định chi nhánh làm việc đang chọn (đồng bộ theo header currentShopId hoặc shop phân công của user)
  const currentBranchShop = useMemo(() => {
    const targetShopId =
      shiftFilterShopId !== 'all'
        ? shiftFilterShopId
        : (currentShopId || user?.shopId || shops[0]?.id);
    return shops.find((s) => s.id === targetShopId) || shops[0] || null;
  }, [shiftFilterShopId, currentShopId, user?.shopId, shops]);

  // Đồng bộ shop filter với currentShopId từ Header
  useEffect(() => {
    if (currentShopId) {
      setShiftFilterShopId(currentShopId);
      setShopFilter(currentShopId);
    } else if (shops.length > 0) {
      setShiftFilterShopId(shops[0].id);
      setShopFilter(shops[0].id);
      setCurrentShopId(shops[0].id);
    }
  }, [currentShopId, shops]);

  // Tính 7 ngày trong tuần từ Thứ Hai đến Chủ Nhật theo weekOffset
  const weekDays = useMemo(() => {
    const now = new Date();
    const currentDay = now.getDay();
    // Monday = 1, Sunday = 0
    const distanceToMonday = currentDay === 0 ? -6 : 1 - currentDay;
    const monday = new Date(now);
    monday.setDate(now.getDate() + distanceToMonday + weekOffset * 7);
    monday.setHours(0, 0, 0, 0);

    const days = [];
    for (let i = 0; i < 7; i++) {
      const d = new Date(monday);
      d.setDate(monday.getDate() + i);
      days.push(d);
    }
    return days;
  }, [weekOffset]);

  const toLocalDateKey = (date: Date) => {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  };

  const getShiftDateKey = (isoString: string) => {
    try {
      const d = new Date(isoString);
      return toLocalDateKey(d);
    } catch {
      return '';
    }
  };

  // Ràng buộc phạm vi xếp lịch: Chỉ trong tuần hiện tại và 2 tuần kế tiếp (tổng cộng 3 tuần)
  const { minShiftDate, maxShiftDate } = useMemo(() => {
    const now = new Date();
    const currentDay = now.getDay();
    const distanceToMonday = currentDay === 0 ? -6 : 1 - currentDay;
    const mondayCurrent = new Date(now);
    mondayCurrent.setDate(now.getDate() + distanceToMonday);
    mondayCurrent.setHours(0, 0, 0, 0);

    const sundayPlus2 = new Date(mondayCurrent);
    sundayPlus2.setDate(mondayCurrent.getDate() + 20); // 21 ngày: Thứ 2 tuần hiện tại -> Chủ Nhật tuần +2

    return {
      minShiftDate: toLocalDateKey(mondayCurrent),
      maxShiftDate: toLocalDateKey(sundayPlus2),
    };
  }, []);

  // Kiểm tra ca đang xem có phải ca thuộc các tuần trước hay không (chế độ chỉ xem)
  const isEditingShiftPast = useMemo(() => {
    if (!editingShift) return false;
    const shiftDate = getShiftDateKey(editingShift.startTime);
    return shiftDate < minShiftDate;
  }, [editingShift, minShiftDate]);

  // Preset ca làm việc
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
        setShiftNotes('Ca tối: Dọn dẹp quầy bar, vệ sinh thiết bị');
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

  // Assign Shop
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

  // Shift: Create modal open
  const handleOpenCreateShift = (prefilledDate?: string, prefilledStaffId?: string) => {
    const targetShopId =
      shiftFilterShopId !== 'all'
        ? shiftFilterShopId
        : (currentShopId || currentBranchShop?.id || shops[0]?.id || '');
    const shopStaffList = staff.filter((s) => s.shopId === targetShopId);
    const defaultStaff = prefilledStaffId
      ? staff.find((s) => s.id === prefilledStaffId)
      : (shopStaffList[0] || staff[0]);
    setShiftStaffId(defaultStaff?.id || '');
    setShiftShopId(targetShopId);
    setShiftDate(prefilledDate || new Date().toISOString().split('T')[0]);
    handleShiftPresetChange('morning');
    setIsCreateShiftModalOpen(true);
  };

  // Shift: Create submit
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

    if (shiftDate < minShiftDate || shiftDate > maxShiftDate) {
      addNotification({
        type: 'error',
        title: 'Ngoài phạm vi xếp lịch',
        message: 'Chỉ được phép xếp lịch trong tuần hiện tại và 2 tuần kế tiếp.',
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

  // Shift: Edit open
  const handleOpenEditShift = (shift: Shift) => {
    setEditingShift(shift);
    setEditStaffId(shift.staffId);
    const startDate = new Date(shift.startTime);
    const endDate = new Date(shift.endTime);

    setEditDate(toLocalDateKey(startDate));
    setEditStartTime(
      `${String(startDate.getHours()).padStart(2, '0')}:${String(startDate.getMinutes()).padStart(2, '0')}`
    );
    setEditEndTime(
      `${String(endDate.getHours()).padStart(2, '0')}:${String(endDate.getMinutes()).padStart(2, '0')}`
    );
    setEditNotes(shift.notes || '');
  };

  // Shift: Edit submit
  const handleUpdateShiftSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingShift || !editStaffId || !editDate || !editStartTime || !editEndTime) return;

    if (editDate < minShiftDate || editDate > maxShiftDate) {
      addNotification({
        type: 'error',
        title: 'Ngoài phạm vi xếp lịch',
        message: 'Chỉ được phép xếp lịch trong tuần hiện tại và 2 tuần kế tiếp.',
      });
      return;
    }

    setIsUpdatingShift(true);
    try {
      const startDateTime = new Date(`${editDate}T${editStartTime}:00`).toISOString();
      const endDateTime = new Date(`${editDate}T${editEndTime}:00`).toISOString();

      await shiftsApi.update(editingShift.id, {
        staffId: editStaffId,
        startTime: startDateTime,
        endTime: endDateTime,
        notes: editNotes,
      });

      setShifts((prev) =>
        prev.map((s) =>
          s.id === editingShift.id
            ? {
                ...s,
                staffId: editStaffId,
                startTime: startDateTime,
                endTime: endDateTime,
                notes: editNotes,
              }
            : s
        )
      );

      addNotification({
        type: 'success',
        title: 'Cập nhật lịch ca thành công',
        message: 'Lịch trực ca làm việc đã được điều chỉnh và lưu lại.',
      });
      setEditingShift(null);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi cập nhật ca làm',
        message: 'Không thể lưu thay đổi ca làm việc. Vui lòng thử lại.',
      });
    } finally {
      setIsUpdatingShift(false);
    }
  };

  // Status transitions
  const handleStartShift = async (shiftId: string) => {
    try {
      await shiftsApi.start(shiftId);
      setShifts((prev) =>
        prev.map((s) => (s.id === shiftId ? { ...s, status: 'InProgress' } : s))
      );
      if (editingShift?.id === shiftId) {
        setEditingShift((prev) => (prev ? { ...prev, status: 'InProgress' } : null));
      }
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
      if (editingShift?.id === shiftId) {
        setEditingShift((prev) => (prev ? { ...prev, status: 'Completed' } : null));
      }
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
      await shiftsApi.cancel(shiftId, 'Quản lý hủy ca');
      setShifts((prev) =>
        prev.map((s) => (s.id === shiftId ? { ...s, status: 'Cancelled' } : s))
      );
      if (editingShift?.id === shiftId) {
        setEditingShift((prev) => (prev ? { ...prev, status: 'Cancelled' } : null));
      }
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
      if (editingShift?.id === shiftId) {
        setEditingShift(null);
      }
      addNotification({
        type: 'success',
        title: 'Đã xóa ca',
        message: 'Ca làm việc đã được xóa khỏi lịch trực.',
      });
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi',
        message: 'Không thể xóa ca làm việc.',
      });
    }
  };

  // Filter staff
  const filteredStaff = useMemo(() => {
    return staff.filter((s) => {
      const fullName = `${s.firstName} ${s.lastName}`.toLowerCase();
      const matchesSearch =
        fullName.includes(searchTerm.toLowerCase()) ||
        s.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (s.contact?.phone && s.contact.phone.includes(searchTerm));
      const matchesRole = roleFilter === 'all' || s.role === roleFilter;
      const matchesShop = shopFilter === 'all' || s.shopId === shopFilter;
      return matchesSearch && matchesRole && matchesShop;
    });
  }, [staff, searchTerm, roleFilter, shopFilter]);

  // Filter shifts
  const filteredShifts = useMemo(() => {
    return shifts.filter((sh) => {
      const matchesStaff = shiftSearchStaffId === 'all' || sh.staffId === shiftSearchStaffId;
      const matchesShop = shiftFilterShopId === 'all' || sh.shopId === shiftFilterShopId;
      const matchesStatus = shiftFilterStatus === 'all' || sh.status === shiftFilterStatus;
      return matchesStaff && matchesShop && matchesStatus;
    });
  }, [shifts, shiftSearchStaffId, shiftFilterShopId, shiftFilterStatus]);

  // Nhóm ca làm theo ngày trong tuần (cho Weekly Calendar View)
  const shiftsByDateKey = useMemo(() => {
    const map: Record<string, Shift[]> = {};
    for (const sh of filteredShifts) {
      const dateKey = getShiftDateKey(sh.startTime);
      if (!map[dateKey]) map[dateKey] = [];
      map[dateKey].push(sh);
    }
    // Sắp xếp ca trong ngày theo giờ bắt đầu
    for (const k in map) {
      map[k].sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime());
    }
    return map;
  }, [filteredShifts]);

  const getRoleBadgeVariant = (role: StaffRole) => {
    switch (role) {
      case 'Admin': return 'primary';
      case 'Manager': return 'neutral';
      case 'Cashier': return 'success';
      case 'Barista': return 'warning';
      case 'InventoryStaff': return 'neutral';
      default: return 'neutral';
    }
  };

  const getShiftBadgeVariant = (status: ShiftStatus) => {
    switch (status) {
      case 'Scheduled': return 'warning';
      case 'InProgress': return 'primary';
      case 'Completed': return 'success';
      case 'Cancelled': return 'error';
      default: return 'neutral';
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

  const todayKey = toLocalDateKey(new Date());

  const getShiftTypeTag = (startTime: string) => {
    try {
      const h = new Date(startTime).getHours();
      if (h < 12) return { label: 'Ca sáng', color: 'bg-neutral-100 text-neutral-700 border-neutral-200' };
      if (h < 17) return { label: 'Ca chiều', color: 'bg-neutral-100 text-neutral-700 border-neutral-200' };
      return { label: 'Ca tối', color: 'bg-neutral-100 text-neutral-700 border-neutral-200' };
    } catch {
      return { label: 'Ca làm', color: 'bg-neutral-100 text-neutral-700 border-neutral-200' };
    }
  };

  // Tiêu đề dải ngày tuần
  const weekRangeTitle = useMemo(() => {
    if (weekDays.length < 7) return '';
    const start = weekDays[0];
    const end = weekDays[6];
    const startStr = `${start.getDate()}/${start.getMonth() + 1}`;
    const endStr = `${end.getDate()}/${end.getMonth() + 1}/${end.getFullYear()}`;
    return `${startStr} - ${endStr}`;
  }, [weekDays]);

  return (
    <PageContainer
      title="Quản Lý Nhân Sự & Ca Làm Việc"
      subtitle={
        isManagerOrAdmin
          ? 'Quản lý hồ sơ nhân viên, phân bổ chi nhánh công tác và xếp lịch trực ca tuần từ Thứ 2 đến Chủ Nhật'
          : `Lịch trực ca làm việc tuần của chi nhánh: ${currentBranchShop?.name || 'Chi nhánh của bạn'}`
      }
      actions={
        <div className="flex items-center gap-2">
          {activeTab === 'staff' && isManagerOrAdmin && (
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
          )}

          {activeTab === 'shifts' && (
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                icon={<RefreshCw className={`w-3.5 h-3.5 ${loadingShifts ? 'animate-spin' : ''}`} />}
                onClick={loadShiftsOnly}
                disabled={loadingShifts}
              >
                Đồng bộ
              </Button>
              {isManagerOrAdmin && (
                <Button
                  variant="primary"
                  icon={<CalendarDays className="w-4 h-4" />}
                  onClick={() => handleOpenCreateShift()}
                >
                  Xếp ca làm mới
                </Button>
              )}
            </div>
          )}
        </div>
      }
    >
      <div className="space-y-6">
        {/* Banner chi nhánh cho nhân viên hoặc quản lý */}
        {currentBranchShop && (
          <div className="bg-neutral-50 border border-neutral-200 rounded-lg px-4 py-2.5 flex items-center justify-between text-xs text-neutral-600">
            <div className="flex items-center gap-2">
              <Building2 className="w-4 h-4 text-neutral-400 flex-shrink-0" />
              <span>
                Chi nhánh:{' '}
                <strong className="font-semibold text-neutral-900">{currentBranchShop.name}</strong>
              </span>
            </div>
            <span className="font-mono text-neutral-500">
              {currentBranchShop.address?.city || 'Hồ Chí Minh'}
            </span>
          </div>
        )}

        {/* Navigation Tabs (Chỉ hiển thị Tab Nhân sự nếu là Quản lý / Admin) */}
        {isManagerOrAdmin ? (
          <div className="flex border-b border-neutral-200 gap-4">
            <button
              onClick={() => setActiveTab('shifts')}
              className={`flex items-center gap-2 py-3 px-1 font-medium text-sm border-b-2 transition-colors ${
                activeTab === 'shifts'
                  ? 'border-primary-600 text-neutral-900 font-semibold'
                  : 'border-transparent text-neutral-500 hover:text-neutral-700 hover:border-neutral-300'
              }`}
            >
              <CalendarCheck className="w-4 h-4 text-neutral-500" />
              <span>Lịch làm việc</span>
              <span className="px-1.5 py-0.2 text-xs rounded bg-neutral-100 text-neutral-600">
                {filteredShifts.length}
              </span>
            </button>

            <button
              onClick={() => setActiveTab('staff')}
              className={`flex items-center gap-2 py-3 px-1 font-medium text-sm border-b-2 transition-colors ${
                activeTab === 'staff'
                  ? 'border-primary-600 text-neutral-900 font-semibold'
                  : 'border-transparent text-neutral-500 hover:text-neutral-700 hover:border-neutral-300'
              }`}
            >
              <Users className="w-4 h-4 text-neutral-500" />
              <span>Nhân sự</span>
              <span className="px-1.5 py-0.2 text-xs rounded bg-neutral-100 text-neutral-600">
                {filteredStaff.length}
              </span>
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-2 text-sm font-semibold text-neutral-800 pb-2 border-b border-neutral-200">
            <CalendarCheck className="w-4 h-4 text-neutral-500" />
            <span>Lịch trực ca làm việc</span>
          </div>
        )}

        {/* ========================================================= */}
        {/* TAB 1: BẢNG XẾP LỊCH TUẦN & CA LÀM VIỆC                   */}
        {/* ========================================================= */}
        {activeTab === 'shifts' && (
          <div className="space-y-4">
            {/* Thanh điều hướng tuần (Week Navigator) */}
            <Card>
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
                {/* Bộ chọn và tiến/lùi tuần */}
                <div className="flex items-center gap-3">
                  <div className="inline-flex rounded-md border border-neutral-200 bg-white shadow-sm overflow-hidden text-xs">
                    <button
                      type="button"
                      onClick={() => setWeekOffset((prev) => prev - 1)}
                      className="px-2.5 py-1.5 font-medium border-r border-neutral-200 flex items-center gap-1 transition-colors text-neutral-600 hover:bg-neutral-50"
                      title="Xem tuần trước đó (Chế độ chỉ xem)"
                    >
                      <ChevronLeft className="w-4 h-4" />
                      <span>Trước</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => setWeekOffset(0)}
                      className={`px-3 py-1.5 font-medium transition-colors ${
                        weekOffset === 0
                          ? 'bg-neutral-900 text-white font-semibold'
                          : 'text-neutral-600 hover:bg-neutral-50'
                      }`}
                    >
                      Tuần này
                    </button>
                    <button
                      type="button"
                      onClick={() => setWeekOffset(1)}
                      className={`px-3 py-1.5 font-medium border-l border-neutral-200 transition-colors ${
                        weekOffset === 1
                          ? 'bg-neutral-900 text-white font-semibold'
                          : 'text-neutral-600 hover:bg-neutral-50'
                      }`}
                    >
                      Tuần sau (+1)
                    </button>
                    <button
                      type="button"
                      onClick={() => setWeekOffset(2)}
                      className={`px-3 py-1.5 font-medium border-l border-neutral-200 transition-colors ${
                        weekOffset === 2
                          ? 'bg-neutral-900 text-white font-semibold'
                          : 'text-neutral-600 hover:bg-neutral-50'
                      }`}
                    >
                      Tuần sau nữa (+2)
                    </button>
                    <button
                      type="button"
                      disabled={weekOffset >= 2}
                      onClick={() => setWeekOffset((prev) => Math.min(2, prev + 1))}
                      className={`px-2.5 py-1.5 font-medium border-l border-neutral-200 flex items-center gap-1 transition-colors ${
                        weekOffset >= 2
                          ? 'text-neutral-300 cursor-not-allowed bg-neutral-50'
                          : 'text-neutral-600 hover:bg-neutral-50'
                      }`}
                      title="Tuần sau (Giới hạn tối đa 2 tuần kế tiếp)"
                    >
                      <span>Sau</span>
                      <ChevronRight className="w-4 h-4" />
                    </button>
                  </div>

                  <div className="flex items-baseline gap-2">
                    <span className="text-sm font-semibold text-neutral-900">
                      {weekRangeTitle}
                    </span>
                    {weekOffset < 0 ? (
                      <span className="text-xs text-neutral-500 font-medium">
                        ({Math.abs(weekOffset)} tuần trước - Chỉ xem)
                      </span>
                    ) : weekOffset === 1 ? (
                      <span className="text-xs text-neutral-500 font-medium">(Kế tiếp)</span>
                    ) : weekOffset === 2 ? (
                      <span className="text-xs text-neutral-500 font-medium">(Sau 2 tuần)</span>
                    ) : null}
                  </div>
                </div>

                {/* Chế độ xem: Lịch tuần (Grid) vs Danh sách (List) & Bộ lọc */}
                <div className="flex items-center gap-2">
                  <div className="inline-flex rounded-md border border-neutral-200 p-0.5 bg-neutral-100">
                    <button
                      type="button"
                      onClick={() => setShiftViewMode('calendar')}
                      className={`flex items-center gap-1.5 px-3 py-1 text-xs font-medium rounded ${
                        shiftViewMode === 'calendar'
                          ? 'bg-white text-neutral-900 shadow-sm'
                          : 'text-neutral-500 hover:text-neutral-800'
                      }`}
                    >
                      <LayoutGrid className="w-3.5 h-3.5" />
                      <span>Lịch tuần</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => setShiftViewMode('list')}
                      className={`flex items-center gap-1.5 px-3 py-1 text-xs font-medium rounded ${
                        shiftViewMode === 'list'
                          ? 'bg-white text-neutral-900 shadow-sm'
                          : 'text-neutral-500 hover:text-neutral-800'
                      }`}
                    >
                      <List className="w-3.5 h-3.5" />
                      <span>Danh sách</span>
                    </button>
                  </div>

                  {activeRole === 'Admin' && (
                    <Select
                      value={shiftFilterShopId}
                      onChange={(e) => {
                        const val = e.target.value;
                        setShiftFilterShopId(val);
                        if (val !== 'all') {
                          setCurrentShopId(val);
                          setShopFilter(val);
                        }
                      }}
                      options={[
                        ...shops.map((s) => ({ value: s.id, label: s.name })),
                        { value: 'all', label: 'Tất cả chi nhánh' },
                      ]}
                    />
                  )}

                  <Select
                    value={shiftSearchStaffId}
                    onChange={(e) => setShiftSearchStaffId(e.target.value)}
                    options={[
                      { value: 'all', label: 'Tất cả nhân sự' },
                      ...(shiftFilterShopId === 'all'
                        ? staff
                        : staff.filter((s) => s.shopId === shiftFilterShopId)
                      ).map((s) => ({
                        value: s.id,
                        label: `${s.firstName} ${s.lastName} (${s.role})`,
                      })),
                    ]}
                  />

                  <Select
                    value={shiftFilterStatus}
                    onChange={(e) => setShiftFilterStatus(e.target.value)}
                    options={[
                      { value: 'all', label: 'Tất cả trạng thái' },
                      { value: 'Scheduled', label: 'Đã lên lịch' },
                      { value: 'InProgress', label: 'Đang diễn ra' },
                      { value: 'Completed', label: 'Đã hoàn thành' },
                      { value: 'Cancelled', label: 'Đã hủy' },
                    ]}
                  />
                </div>
              </div>
            </Card>

            {/* CHẾ ĐỘ 1: BẢNG LỊCH TUẦN TỪ THỨ 2 ĐẾN CHỦ NHẬT (WEEKLY GRID) */}
            {shiftViewMode === 'calendar' && (
              <div className="grid grid-cols-1 md:grid-cols-7 gap-2.5">
                {weekDays.map((dayDate) => {
                  const dateKey = toLocalDateKey(dayDate);
                  const isToday = dateKey === todayKey;
                  const isPastDay = dateKey < todayKey || weekOffset < 0;
                  const dayShifts = shiftsByDateKey[dateKey] || [];
                  const dayNames = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
                  const dayName = dayNames[dayDate.getDay()];

                  return (
                    <div
                      key={dateKey}
                      className={`flex flex-col rounded-lg border bg-white ${
                        isToday ? 'border-neutral-400' : 'border-neutral-200'
                      }`}
                    >
                      {/* Cột Header ngày */}
                      <div
                        className={`p-2 border-b flex items-center justify-between ${
                          isToday ? 'bg-neutral-100 border-neutral-200' : 'bg-neutral-50/70 border-neutral-200'
                        }`}
                      >
                        <div>
                          <div className="flex items-center gap-1.5">
                            <span className="text-xs font-semibold text-neutral-900">{dayName}</span>
                            {isToday && (
                              <span className="text-[10px] font-medium text-neutral-600">
                                (Hôm nay)
                              </span>
                            )}
                          </div>
                          <span className="text-[11px] text-neutral-500 block">
                            {dayDate.getDate()} thg {dayDate.getMonth() + 1}
                          </span>
                        </div>

                        {/* Nút xếp ca nhanh cho ngày này (chỉ dành cho ngày hiện tại hoặc tương lai) */}
                        {isManagerOrAdmin && !isPastDay && (
                          <button
                            type="button"
                            onClick={() => handleOpenCreateShift(dateKey)}
                            className="p-1 rounded text-neutral-400 hover:text-neutral-700 hover:bg-neutral-200/60 transition-colors"
                            title={`Xếp ca làm việc cho ngày ${dayName} (${dateKey})`}
                          >
                            <Plus className="w-3.5 h-3.5" />
                          </button>
                        )}
                      </div>

                      {/* Danh sách thẻ ca trong ngày */}
                      <div className="p-2 flex-1 space-y-2 min-h-[140px] bg-neutral-50/30">
                        {dayShifts.length === 0 ? (
                          <div className="h-full flex flex-col items-center justify-center text-center p-3 text-neutral-400">
                            <span className="text-xs">Trống</span>
                            {isManagerOrAdmin && !isPastDay && (
                              <button
                                type="button"
                                onClick={() => handleOpenCreateShift(dateKey)}
                                className="text-xs text-neutral-600 hover:text-neutral-900 hover:underline mt-1 flex items-center gap-0.5"
                              >
                                <Plus className="w-3 h-3" /> Thêm ca
                              </button>
                            )}
                          </div>
                        ) : (
                          dayShifts.map((sh) => {
                            const shiftStaff = staff.find((s) => s.id === sh.staffId);
                            const tag = getShiftTypeTag(sh.startTime);
                            const isMyShift =
                              user?.email &&
                              shiftStaff?.email.toLowerCase() === user.email.toLowerCase();

                            return (
                              <div
                                key={sh.id}
                                onClick={() => handleOpenEditShift(sh)}
                                className="p-2.5 rounded border border-neutral-200 bg-white cursor-pointer hover:border-neutral-300 transition-colors"
                              >
                                {/* Tag ca và giờ */}
                                <div className="flex items-center justify-between text-[11px] mb-1.5">
                                  <span className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-neutral-100 text-neutral-600">
                                    {tag.label}
                                  </span>
                                  <span className="font-mono text-xs font-medium text-neutral-700">
                                    {formatShiftTime(sh.startTime)} - {formatShiftTime(sh.endTime)}
                                  </span>
                                </div>

                                {/* Tên nhân viên */}
                                <div className="font-medium text-xs text-neutral-900 truncate">
                                  {shiftStaff
                                    ? `${shiftStaff.firstName} ${shiftStaff.lastName}`
                                    : 'Nhân sự'}
                                  {isMyShift && (
                                    <span className="ml-1 text-[10px] text-neutral-500 font-normal">
                                      (Bạn)
                                    </span>
                                  )}
                                </div>

                                {/* Vai trò & Trạng thái */}
                                <div className="flex items-center justify-between mt-1 text-[10px] text-neutral-500">
                                  <span>{shiftStaff?.role || ''}</span>
                                  <span>
                                    {sh.status === 'InProgress'
                                      ? 'Đang trực'
                                      : sh.status === 'Completed'
                                      ? 'Đã xong'
                                      : sh.status === 'Cancelled'
                                      ? 'Đã hủy'
                                      : 'Đã lên lịch'}
                                  </span>
                                </div>

                                {sh.notes && (
                                  <div className="text-[10px] text-neutral-400 truncate mt-1 border-t border-neutral-100 pt-1">
                                    {sh.notes}
                                  </div>
                                )}
                              </div>
                            );
                          })
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}

            {/* CHẾ ĐỘ 2: BẢNG DANH SÁCH (TABLE LIST VIEW) */}
            {shiftViewMode === 'list' && (
              <Card noPadding>
                {filteredShifts.length === 0 ? (
                  <EmptyState
                    title="Chưa có ca làm việc nào"
                    description="Hãy bấm vào nút Xếp ca làm mới để thêm ca làm việc cho nhân viên."
                    action={
                      isManagerOrAdmin && (
                        <Button
                          variant="primary"
                          size="sm"
                          icon={<Plus className="w-4 h-4" />}
                          onClick={() => handleOpenCreateShift()}
                        >
                          Xếp ca làm việc ngay
                        </Button>
                      )
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
                          <th className="py-3 px-4 text-right">Thao tác</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-neutral-200">
                        {filteredShifts.map((sh) => {
                          const shiftStaff = staff.find((s) => s.id === sh.staffId);
                          const shiftShop = shops.find((s) => s.id === sh.shopId);
                          const isMyShift =
                            user?.email &&
                            shiftStaff?.email.toLowerCase() === user.email.toLowerCase();

                          return (
                            <tr key={sh.id} className="hover:bg-neutral-50 transition-colors">
                              <td className="py-3 px-4">
                                <div className="font-semibold text-neutral-900">
                                  {shiftStaff
                                    ? `${shiftStaff.firstName} ${shiftStaff.lastName}`
                                    : 'Nhân sự không xác định'}
                                  {isMyShift && (
                                    <span className="ml-1.5 text-[10px] px-1.5 py-0.5 bg-primary-100 text-primary-800 rounded font-normal">
                                      Ca của bạn
                                    </span>
                                  )}
                                </div>
                                <div className="flex items-center gap-2 mt-0.5">
                                  {shiftStaff && (
                                    <Badge variant={getRoleBadgeVariant(shiftStaff.role)}>
                                      {shiftStaff.role}
                                    </Badge>
                                  )}
                                  <span className="text-xs text-neutral-400 font-mono">
                                    ID: {sh.staffId.slice(-6)}
                                  </span>
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
                                  <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => handleOpenEditShift(sh)}
                                    icon={<Edit className="w-3.5 h-3.5" />}
                                  >
                                    Chi tiết
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
            )}
          </div>
        )}

        {/* ========================================================= */}
        {/* TAB 2: NHÂN SỰ & PHÂN QUYỀN (CHỈ DÀNH CHO MANAGER / ADMIN) */}
        {/* ========================================================= */}
        {activeTab === 'staff' && isManagerOrAdmin && (
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
                {activeRole === 'Admin' ? (
                  <Select
                    value={shopFilter}
                    onChange={(e) => {
                      const val = e.target.value;
                      setShopFilter(val);
                      if (val !== 'all') {
                        setCurrentShopId(val);
                        setShiftFilterShopId(val);
                      }
                    }}
                    options={[
                      ...shops.map((s) => ({ value: s.id, label: s.name })),
                      { value: 'all', label: 'Tất cả chi nhánh' },
                    ]}
                  />
                ) : (
                  <div className="p-2.5 rounded border border-neutral-200 bg-neutral-50 text-xs font-medium text-neutral-700 flex items-center gap-2">
                    <Building2 className="w-4 h-4 text-primary-600" />
                    <span>Chi nhánh: {currentBranchShop?.name || 'Chi nhánh của bạn'}</span>
                  </div>
                )}
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
                                  onClick={() => handleOpenCreateShift(undefined, member.id)}
                                  title="Xếp ca làm việc cho nhân viên này"
                                  icon={<CalendarIcon className="w-3.5 h-3.5 text-primary-600" />}
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
        {/* MODAL: XẾP CA LÀM VIỆC MỚI                                */}
        {/* ========================================================= */}
        <Modal
          isOpen={isCreateShiftModalOpen}
          onClose={() => setIsCreateShiftModalOpen(false)}
          title="Xếp Ca Làm Việc Mới"
          description="Lên lịch trực ca làm việc cụ thể cho từng nhân viên theo chi nhánh (hỗ trợ xếp lịch trước 2 tuần)."
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
                min={minShiftDate}
                max={maxShiftDate}
                value={shiftDate}
                onChange={(e) => setShiftDate(e.target.value)}
              />
              <p className="text-[11px] text-neutral-500 mt-1">
                Chỉ được xếp lịch trong tuần hiện tại và tối đa 2 tuần kế tiếp.
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Khung ca làm việc:
              </label>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('morning')}
                  className={`p-2.5 rounded-lg border text-left transition-colors ${
                    shiftPreset === 'morning'
                      ? 'border-neutral-900 bg-neutral-50 text-neutral-900 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">Ca Sáng</div>
                  <div className="text-neutral-500 font-mono mt-0.5">06:30 - 14:30 (8h)</div>
                </button>
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('afternoon')}
                  className={`p-2.5 rounded-lg border text-left transition-colors ${
                    shiftPreset === 'afternoon'
                      ? 'border-neutral-900 bg-neutral-50 text-neutral-900 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">Ca Chiều</div>
                  <div className="text-neutral-500 font-mono mt-0.5">14:30 - 22:30 (8h)</div>
                </button>
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('evening')}
                  className={`p-2.5 rounded-lg border text-left transition-colors ${
                    shiftPreset === 'evening'
                      ? 'border-neutral-900 bg-neutral-50 text-neutral-900 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">Ca Tối</div>
                  <div className="text-neutral-500 font-mono mt-0.5">18:00 - 23:00 (5h)</div>
                </button>
                <button
                  type="button"
                  onClick={() => handleShiftPresetChange('custom')}
                  className={`p-2.5 rounded-lg border text-left transition-colors ${
                    shiftPreset === 'custom'
                      ? 'border-neutral-900 bg-neutral-50 text-neutral-900 font-semibold'
                      : 'border-neutral-200 hover:border-neutral-300 text-neutral-700'
                  }`}
                >
                  <div className="font-medium">Tùy Chỉnh</div>
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
        {/* MODAL: CẬP NHẬT & CHI TIẾT CA LÀM VIỆC                    */}
        {/* ========================================================= */}
        <Modal
          isOpen={!!editingShift}
          onClose={() => setEditingShift(null)}
          title="Chi Tiết & Cập Nhật Lịch Ca Làm"
          description="Điều chỉnh nhân viên, ngày trực hoặc khung giờ ca làm việc hiện tại."
        >
          {editingShift && (
            <div className="space-y-4">
              {/* Thông tin tóm tắt */}
              <div className="p-3 bg-neutral-50 rounded-lg border border-neutral-200 flex items-center justify-between">
                <div>
                  <div className="text-xs text-neutral-500 font-mono">Mã ca: {editingShift.id}</div>
                  <div className="text-sm font-bold text-neutral-900 mt-0.5">
                    {shops.find((s) => s.id === editingShift.shopId)?.name || 'Chi nhánh'}
                  </div>
                </div>
                <Badge variant={getShiftBadgeVariant(editingShift.status)}>
                  {editingShift.status === 'Scheduled'
                    ? 'Đã lên lịch'
                    : editingShift.status === 'InProgress'
                    ? 'Đang làm việc'
                    : editingShift.status === 'Completed'
                    ? 'Đã hoàn thành'
                    : 'Đã hủy'}
                </Badge>
              </div>

              {/* Form chỉnh sửa (chỉ dành cho quản lý/admin VÀ đối với ca thuộc tuần hiện tại hoặc tương lai) */}
              {isManagerOrAdmin && !isEditingShiftPast ? (
                <form onSubmit={handleUpdateShiftSubmit} className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-neutral-700 mb-1">
                      Nhân viên trực ca:
                    </label>
                    <Select
                      value={editStaffId}
                      onChange={(e) => setEditStaffId(e.target.value)}
                      options={staff.map((s) => ({
                        value: s.id,
                        label: `${s.firstName} ${s.lastName} - ${s.role}`,
                      }))}
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-neutral-700 mb-1">
                      Ngày làm việc:
                    </label>
                    <Input
                      type="date"
                      required
                      min={minShiftDate}
                      max={maxShiftDate}
                      value={editDate}
                      onChange={(e) => setEditDate(e.target.value)}
                    />
                    <p className="text-[11px] text-neutral-500 mt-1">
                      Chỉ được xếp lịch trong tuần hiện tại và tối đa 2 tuần kế tiếp.
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <Input
                      label="Giờ bắt đầu"
                      type="time"
                      required
                      value={editStartTime}
                      onChange={(e) => setEditStartTime(e.target.value)}
                    />
                    <Input
                      label="Giờ kết thúc"
                      type="time"
                      required
                      value={editEndTime}
                      onChange={(e) => setEditEndTime(e.target.value)}
                    />
                  </div>

                  <div>
                    <Input
                      label="Ghi chú nhiệm vụ ca"
                      value={editNotes}
                      onChange={(e) => setEditNotes(e.target.value)}
                    />
                  </div>

                  <div className="flex items-center justify-between pt-3 border-t border-neutral-200">
                    <div className="flex items-center gap-1.5">
                      {editingShift.status === 'Scheduled' && (
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => handleStartShift(editingShift.id)}
                          icon={<Play className="w-3.5 h-3.5" />}
                        >
                          Bắt đầu ca
                        </Button>
                      )}
                      {editingShift.status === 'InProgress' && (
                        <Button
                          type="button"
                          variant="primary"
                          size="sm"
                          onClick={() => handleCompleteShift(editingShift.id)}
                          icon={<CheckCircle2 className="w-3.5 h-3.5" />}
                        >
                          Hoàn thành
                        </Button>
                      )}
                      {editingShift.status !== 'Cancelled' && editingShift.status !== 'Completed' && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => handleCancelShift(editingShift.id)}
                          icon={<XCircle className="w-3.5 h-3.5" />}
                        >
                          Hủy ca
                        </Button>
                      )}
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        onClick={() => handleDeleteShift(editingShift.id)}
                        icon={<Trash2 className="w-3.5 h-3.5" />}
                      >
                        Xóa
                      </Button>
                    </div>

                    <div className="flex items-center gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => setEditingShift(null)}
                      >
                        Đóng
                      </Button>
                      <Button
                        type="submit"
                        variant="primary"
                        loading={isUpdatingShift}
                        icon={<Edit className="w-3.5 h-3.5" />}
                      >
                        Lưu cập nhật
                      </Button>
                    </div>
                  </div>
                </form>
              ) : (
                /* Chế độ xem: dành cho nhân viên thường HOẶC ca làm việc thuộc các tuần trước */
                <div className="space-y-3">
                  {isEditingShiftPast && (
                    <div className="p-2.5 bg-neutral-50 rounded-lg border border-neutral-200 text-xs text-neutral-600">
                      Đây là ca làm việc thuộc lịch sử tuần trước. Bạn chỉ có thể xem, không thể chỉnh sửa hoặc xóa.
                    </div>
                  )}
                  <div className="text-sm text-neutral-700">
                    <p>
                      <strong>Nhân viên:</strong>{' '}
                      {staff.find((s) => s.id === editingShift.staffId)?.firstName}{' '}
                      {staff.find((s) => s.id === editingShift.staffId)?.lastName}
                    </p>
                    <p className="mt-1">
                      <strong>Thời gian:</strong> {formatShiftDate(editingShift.startTime)} (
                      {formatShiftTime(editingShift.startTime)} - {formatShiftTime(editingShift.endTime)})
                    </p>
                    <p className="mt-1">
                      <strong>Nhiệm vụ:</strong> {editingShift.notes || 'Không có ghi chú'}
                    </p>
                  </div>

                  {/* Nhân viên có thể tự bấm bắt đầu / xong ca của mình (chỉ đối với ca hiện tại/tương lai) */}
                  {!isEditingShiftPast &&
                    user?.email &&
                    staff.find((s) => s.id === editingShift.staffId)?.email.toLowerCase() ===
                      user.email.toLowerCase() && (
                      <div className="flex gap-2 pt-3 border-t border-neutral-200">
                        {editingShift.status === 'Scheduled' && (
                          <Button
                            variant="primary"
                            size="sm"
                            onClick={() => handleStartShift(editingShift.id)}
                            icon={<Play className="w-3.5 h-3.5" />}
                          >
                            Bắt đầu ca của tôi
                          </Button>
                        )}
                        {editingShift.status === 'InProgress' && (
                          <Button
                            variant="primary"
                            size="sm"
                            onClick={() => handleCompleteShift(editingShift.id)}
                            icon={<CheckCircle2 className="w-3.5 h-3.5" />}
                          >
                            Xác nhận hoàn thành ca
                          </Button>
                        )}
                      </div>
                    )}

                  <div className="flex justify-end pt-3 border-t border-neutral-200">
                    <Button variant="outline" onClick={() => setEditingShift(null)}>
                      Đóng
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </Modal>

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
                Vai trò: <span className="font-medium text-neutral-800">{assigningMember?.role}</span> | Email:{' '}
                {assigningMember?.email}
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
      </div>
    </PageContainer>
  );
};
