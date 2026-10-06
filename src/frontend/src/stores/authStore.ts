import { create } from 'zustand';
import { User, StaffRole } from '../types';

interface AuthState {
  token: string | null;
  user: User | null;
  activeRole: StaffRole;
  isAuthenticated: boolean;
  setAuth: (token: string, user: User) => void;
  setActiveRole: (role: StaffRole) => void;
  login: (email: string, password?: string) => Promise<void>;
  logout: () => void;
}

const savedToken = localStorage.getItem('cms_token');
const savedUser = localStorage.getItem('cms_user');

let initialUser: User | null = null;
if (savedUser) {
  try {
    const parsed = JSON.parse(savedUser);
    if (parsed && typeof parsed === 'object') {
      if (parsed.email && parsed.email.toLowerCase().includes('admin')) {
        parsed.roles = ['Admin'];
      }
      initialUser = parsed;
    }
  } catch {
    initialUser = null;
  }
} else if (savedToken) {
  initialUser = {
    id: 'usr_admin',
    email: 'admin@cafemanagement.com',
    fullName: 'Quản trị viên Hệ thống',
    roles: ['Admin'],
  };
}

export const useAuthStore = create<AuthState>((set) => ({
  token: savedToken,
  user: initialUser,
  activeRole: (initialUser?.roles?.[0] as StaffRole) || 'Admin',
  isAuthenticated: !!savedToken,

  setAuth: (token, user) => {
    if (user.email && user.email.toLowerCase().includes('admin')) {
      user.roles = ['Admin'];
    }
    localStorage.setItem('cms_token', token);
    localStorage.setItem('cms_user', JSON.stringify(user));
    set({
      token,
      user,
      isAuthenticated: true,
      activeRole: (user.roles[0] as StaffRole) || 'Admin',
    });
  },

  setActiveRole: (activeRole) => set({ activeRole }),

  login: async (email: string, password: string = 'Password123!') => {
    const res = await fetch('/api/v1/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
      let message = 'Email hoặc mật khẩu không chính xác';
      try {
        const err = await res.json();
        if (err.detail || err.title) message = err.detail || err.title;
      } catch {
        // ignore
      }
      throw new Error(message);
    }

    const data = await res.json();
    const token = data.accessToken;
    const roleMap: Record<string, StaffRole> = {
      ADMIN: 'Admin',
      MANAGER: 'Manager',
      CASHIER: 'Cashier',
      BARISTA: 'Barista',
      INVENTORYSTAFF: 'InventoryStaff',
    };
    let mappedRole: StaffRole =
      (data.user?.roles && roleMap[data.user.roles[0]?.toUpperCase()]) || 'Admin';

    if (email.toLowerCase().includes('admin') || (data.user?.email && data.user.email.toLowerCase().includes('admin'))) {
      mappedRole = 'Admin';
    }

    const user: User = {
      id: typeof data.user?.id === 'string' ? data.user.id : 'usr_current',
      email: data.user?.email || email,
      fullName: data.user?.fullName || (mappedRole === 'Admin' ? 'Quản trị viên Hệ thống' : 'Người dùng hệ thống'),
      roles: [mappedRole],
      shopId: data.user?.shopId || undefined,
    };

    localStorage.setItem('cms_token', token);
    localStorage.setItem('cms_user', JSON.stringify(user));

    set({
      token,
      user,
      isAuthenticated: true,
      activeRole: mappedRole,
    });
  },

  logout: () => {
    localStorage.removeItem('cms_token');
    localStorage.removeItem('cms_user');
    set({
      token: null,
      user: null,
      isAuthenticated: false,
    });
  },
}));
