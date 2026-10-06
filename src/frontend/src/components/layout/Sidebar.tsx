import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Store,
  Coffee,
  Boxes,
  ShoppingCart,
  Users,
  Activity,
  LogOut,
  Coffee as LogoIcon,
} from 'lucide-react';
import { useAuthStore } from '../../stores/authStore';

export const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const { logout, activeRole } = useAuthStore();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  const navigation = [
    { name: 'Tổng quan', href: '/dashboard', icon: LayoutDashboard },
    { name: 'Quán & Chi nhánh', href: '/shops', icon: Store },
    { name: 'Thực đơn đồ uống', href: '/menu', icon: Coffee },
    { name: 'Kho & Nguyên liệu', href: '/inventory', icon: Boxes },
    { name: 'Bán hàng & Thu ngân', href: '/orders', icon: ShoppingCart },
    { name: 'Nhân sự & Ca làm', href: '/staff', icon: Users },
    { name: 'Giám sát hệ thống', href: '/metrics', icon: Activity },
  ];

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
    <aside className="w-64 bg-cafe-black border-r border-neutral-800 flex flex-col flex-shrink-0 min-h-screen text-white select-none">
      {/* Brand logo */}
      <div className="h-16 flex items-center px-6 gap-3 border-b border-neutral-800 bg-black">
        <div className="w-8 h-8 rounded bg-cafe-orange flex items-center justify-center text-white font-black">
          <LogoIcon className="w-5 h-5 text-white" />
        </div>
        <div>
          <span className="text-sm font-black tracking-wider uppercase text-white block">
            Quản Lý Cà Phê
          </span>
          <span className="text-[10px] text-neutral-400 uppercase tracking-widest block font-mono">
            Hệ thống vận hành
          </span>
        </div>
      </div>

      {/* Role tag */}
      <div className="px-6 py-3 border-b border-neutral-800/80 bg-neutral-900/50 flex items-center justify-between">
        <span className="text-[10px] text-neutral-400 uppercase font-semibold">Vai trò:</span>
        <span className="text-[11px] font-bold text-cafe-orange uppercase tracking-wider bg-orange-950/60 border border-orange-800/60 px-2 py-0.5 rounded">
          {getRoleDisplayName(activeRole)}
        </span>
      </div>

      {/* Navigation menu */}
      <nav className="flex-1 px-3 py-4 space-y-1">
        {navigation.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.href}
              to={item.href}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 text-xs font-semibold uppercase tracking-wider rounded transition-colors ${
                  isActive
                    ? 'bg-cafe-orange text-white shadow-none'
                    : 'text-neutral-400 hover:text-white hover:bg-neutral-900'
                }`
              }
            >
              <Icon className="w-4 h-4 flex-shrink-0" />
              <span>{item.name}</span>
            </NavLink>
          );
        })}
      </nav>

      {/* Footer logout */}
      <div className="p-4 border-t border-neutral-800 bg-neutral-950">
        <button
          onClick={handleLogout}
          className="w-full flex items-center gap-2.5 px-3 py-2 text-xs font-semibold text-neutral-400 hover:text-red-400 hover:bg-neutral-900 rounded transition-colors"
        >
          <LogOut className="w-4 h-4" />
          <span>Đăng xuất hệ thống</span>
        </button>
      </div>
    </aside>
  );
};
