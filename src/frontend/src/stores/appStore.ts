import { create } from 'zustand';

interface AppNotification {
  id: string;
  type: 'success' | 'error' | 'info';
  message: string;
}

interface AppState {
  currentShopId: string;
  setCurrentShopId: (id: string) => void;
  notifications: AppNotification[];
  notify: (type: 'success' | 'error' | 'info', message: string) => void;
  addNotification: (params: { type: 'success' | 'error' | 'info'; title?: string; message: string }) => void;
  dismissNotification: (id: string) => void;
}

const savedShopId = localStorage.getItem('cms_current_shop_id') || '';

export const useAppStore = create<AppState>((set) => ({
  currentShopId: savedShopId,
  setCurrentShopId: (currentShopId) => {
    localStorage.setItem('cms_current_shop_id', currentShopId);
    set({ currentShopId });
  },
  notifications: [],
  notify: (type, message) => {
    const id = Date.now().toString();
    set((state) => ({
      notifications: [...state.notifications, { id, type, message }],
    }));
    setTimeout(() => {
      set((state) => ({
        notifications: state.notifications.filter((n) => n.id !== id),
      }));
    }, 4000);
  },
  addNotification: ({ type, message }) => {
    const id = Date.now().toString();
    set((state) => ({
      notifications: [...state.notifications, { id, type, message }],
    }));
    setTimeout(() => {
      set((state) => ({
        notifications: state.notifications.filter((n) => n.id !== id),
      }));
    }, 4000);
  },
  dismissNotification: (id) =>
    set((state) => ({
      notifications: state.notifications.filter((n) => n.id !== id),
    })),
}));
