import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Store, UserCircle, LogOut } from 'lucide-react';
import { useAuthStore } from '../../stores/authStore';
import { useAppStore } from '../../stores/appStore';
import { api } from '../../api/client';
import { Shop } from '../../types';

export const Header: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const { currentShopId, setCurrentShopId } = useAppStore();
  const [shops, setShops] = useState<Shop[]>([]);

  useEffect(() => {
    api.getShops().then((res) => {
      setShops(res);
      if (res.length > 0 && (!currentShopId || !res.find((s) => s.id === currentShopId))) {
        setCurrentShopId(res[0].id);
      }
    }).catch(() => {});
  }, [currentShopId, setCurrentShopId]);

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  const getRoleDisplayName = (role?: string): string => {
    switch (role?.toUpperCase()) {
      case 'ADMIN': return 'Quản trị viên';
      case 'MANAGER': return 'Quản lý';
      case 'CASHIER': return 'Thu ngân';
      case 'BARISTA': return 'Pha chế';
      case 'INVENTORYSTAFF': return 'Thủ kho';
      default: return role || 'Quản trị viên';
    }
  };

  return (
    <header className="h-16 bg-white border-b border-neutral-200 px-6 flex items-center justify-between select-none">
      {/* Left: Active shop branch selection */}
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2 px-3 py-1.5 bg-neutral-100 border border-neutral-300 rounded text-xs font-medium text-neutral-800">
          <Store className="w-4 h-4 text-cafe-orange" />
          <span className="font-semibold text-neutral-500 uppercase text-[10px]">Chi nhánh:</span>
          <select
            value={currentShopId}
            onChange={(e) => setCurrentShopId(e.target.value)}
            className="bg-transparent font-bold text-neutral-900 focus:outline-none cursor-pointer"
          >
            {shops.map((shop) => (
              <option key={shop.id} value={shop.id}>
                {shop.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Right: User Profile & Quick Logout */}
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2 text-neutral-800">
          <UserCircle className="w-6 h-6 text-neutral-700" />
          <div className="text-left hidden sm:block">
            <div className="flex items-center gap-1.5">
              <span className="text-xs font-bold leading-tight text-neutral-900">
                {user?.fullName || 'Quản trị viên Hệ thống'}
              </span>
              <span className="px-2 py-0.5 text-[10px] font-semibold bg-neutral-900 text-white rounded">
                {getRoleDisplayName(user?.roles?.[0])}
              </span>
            </div>
            <span className="text-[10px] text-neutral-500 block leading-tight font-mono">
              {user?.email || 'admin@cafemanagement.com'}
            </span>
          </div>
        </div>
        <button
          onClick={handleLogout}
          title="Đăng xuất hệ thống"
          className="p-1.5 text-neutral-500 hover:text-red-600 hover:bg-neutral-100 rounded transition-colors ml-2"
        >
          <LogOut className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
};
