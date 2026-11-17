import { create } from 'zustand';
import { User, StaffRole } from '../types';
import { useAppStore } from './appStore';

interface AuthState {
  token: string | null;
  user: User | null;
  activeRole: StaffRole;
  isAuthenticated: boolean;
  setAuth: (token: string, user: User, refreshToken?: string) => void;
  setActiveRole: (role: StaffRole) => void;
  login: (email: string, password?: string) => Promise<void>;
  logout: () => void;
}

// Backend roles arrive as either canonical names ("Barista") or upper-case
// normalized names ("BARISTA"). Map them to the single frontend role union.
const roleMap: Record<string, StaffRole> = {
  ADMIN: 'Admin',
  MANAGER: 'Manager',
  CASHIER: 'Cashier',
  BARISTA: 'Barista',
};

const toStaffRole = (role?: string | null): StaffRole =>
  roleMap[(role || '').toUpperCase()] || 'Barista';

const savedToken = localStorage.getItem('cms_token');
const savedUser = localStorage.getItem('cms_user');

let initialUser: User | null = null;
if (savedUser) {
  try {
    const parsed = JSON.parse(savedUser);
    if (parsed && typeof parsed === 'object') {
      initialUser = parsed;
      if (initialUser?.shopId) {
        useAppStore.getState().setCurrentShopId(initialUser.shopId);
      }
    }
  } catch {
    initialUser = null;
  }
}

export const useAuthStore = create<AuthState>((set) => ({
  token: savedToken,
  user: initialUser,
  activeRole: toStaffRole(initialUser?.roles?.[0]),
  isAuthenticated: !!savedToken && !!initialUser,

  setAuth: (token, user, refreshToken) => {
    if (user.shopId) {
      useAppStore.getState().setCurrentShopId(user.shopId);
    }
    localStorage.setItem('cms_token', token);
    localStorage.setItem('cms_user', JSON.stringify(user));
    if (refreshToken) {
      localStorage.setItem('cms_refresh_token', refreshToken);
    }
    set({
      token,
      user,
      isAuthenticated: true,
      activeRole: toStaffRole(user.roles[0]),
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

    // The backend returns a role list; never infer privileges from the email.
    const mappedRole = toStaffRole(data.user?.roles?.[0]);

    const userShopId = (data.user?.shopId && data.user.shopId.trim() !== '') ? data.user.shopId : undefined;

    const user: User = {
      id: typeof data.user?.id === 'string' ? data.user.id : '',
      email: data.user?.email || email,
      fullName: data.user?.fullName || 'Người dùng hệ thống',
      roles: [mappedRole],
      shopId: userShopId,
    };

    if (userShopId) {
      useAppStore.getState().setCurrentShopId(userShopId);
    }

    localStorage.setItem('cms_token', token);
    localStorage.setItem('cms_user', JSON.stringify(user));
    if (data.refreshToken) {
      localStorage.setItem('cms_refresh_token', data.refreshToken);
    }

    set({
      token,
      user,
      isAuthenticated: true,
      activeRole: mappedRole,
    });
  },

  logout: () => {
    const refreshToken = localStorage.getItem('cms_refresh_token');
    if (refreshToken) {
      // Best-effort server-side revocation; local logout must not depend on it.
      fetch('/api/v1/auth/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      }).catch(() => undefined);
    }
    localStorage.removeItem('cms_token');
    localStorage.removeItem('cms_refresh_token');
    localStorage.removeItem('cms_user');
    set({
      token: null,
      user: null,
      isAuthenticated: false,
    });
  },
}));
