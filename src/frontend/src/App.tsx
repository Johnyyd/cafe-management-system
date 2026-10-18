import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Sidebar } from './components/layout/Sidebar';
import { Header } from './components/layout/Header';
import { NotificationToast } from './components/layout/NotificationToast';
import { DashboardPage } from './pages/DashboardPage';
import { ShopsPage } from './pages/ShopsPage';
import { MenuPage } from './pages/MenuPage';
import { InventoryPage } from './pages/InventoryPage';
import { OrdersPage } from './pages/OrdersPage';
import { StaffPage } from './pages/StaffPage';
import { MetricsPage } from './pages/MetricsPage';
import { ReviewsPage } from './pages/ReviewsPage';
import { LoginPage } from './pages/LoginPage';
import { PublicFeedbackPage } from './pages/PublicFeedbackPage';
import { KdsPage } from './pages/KdsPage';
import { useAuthStore } from './stores/authStore';

const getDefaultRouteForRole = (role?: string): string => {
  switch (role) {
    case 'Cashier':
      return '/orders';
    case 'Barista':
      return '/kds';
    case 'InventoryStaff':
      return '/inventory';
    default:
      return '/dashboard';
  }
};

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
};

const RoleRouteGuard: React.FC<{ allowedRoles: string[]; children: React.ReactNode }> = ({ allowedRoles, children }) => {
  const activeRole = useAuthStore((state) => state.activeRole);
  if (!allowedRoles.includes(activeRole)) {
    return <Navigate to={getDefaultRouteForRole(activeRole)} replace />;
  }
  return <>{children}</>;
};

const AppLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
    <div className="flex h-screen bg-neutral-100 overflow-hidden font-sans">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        <Header />
        {children}
      </div>
    </div>
  );
};

export const App: React.FC = () => {
  const { isAuthenticated, activeRole } = useAuthStore();

  return (
    <BrowserRouter>
      <NotificationToast />
      <Routes>
        {/* Public Customer Feedback & Review Routes (No Login Required) */}
        <Route path="/feedback" element={<PublicFeedbackPage />} />
        <Route path="/review" element={<PublicFeedbackPage />} />

        {/* Authentication */}
        <Route path="/login" element={<LoginPage />} />

        {/* Protected Dashboard and Management Routes */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin', 'Manager']}>
                <AppLayout>
                  <DashboardPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/shops"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin']}>
                <AppLayout>
                  <ShopsPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/menu"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin']}>
                <AppLayout>
                  <MenuPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/inventory"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin', 'Manager', 'InventoryStaff']}>
                <AppLayout>
                  <InventoryPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/orders"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin', 'Manager', 'Cashier']}>
                <AppLayout>
                  <OrdersPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/kds"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin', 'Manager', 'Barista']}>
                <AppLayout>
                  <KdsPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/staff"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin', 'Manager', 'Cashier', 'Barista', 'InventoryStaff']}>
                <AppLayout>
                  <StaffPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/metrics"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin']}>
                <AppLayout>
                  <MetricsPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />
        <Route
          path="/reviews"
          element={
            <ProtectedRoute>
              <RoleRouteGuard allowedRoles={['Admin', 'Manager', 'Cashier', 'Barista', 'InventoryStaff']}>
                <AppLayout>
                  <ReviewsPage />
                </AppLayout>
              </RoleRouteGuard>
            </ProtectedRoute>
          }
        />

        {/* Catch-all route */}
        <Route
          path="*"
          element={<Navigate to={isAuthenticated ? getDefaultRouteForRole(activeRole) : '/login'} replace />}
        />
      </Routes>
    </BrowserRouter>
  );
};


export default App;
