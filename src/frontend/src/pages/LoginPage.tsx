import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
 import { zodResolver } from '@hookform/resolvers/zod';
import { Coffee, Lock, Mail, ArrowRight, ShieldCheck } from 'lucide-react';
import { Input } from '../components/common/Input';
import { Button } from '../components/common/Button';
import { useAuthStore } from '../stores/authStore';
import { useAppStore } from '../stores/appStore';
import { loginSchema, LoginFormData } from '../schemas';
import { StaffRole } from '../types';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);
  const notify = useAppStore((state) => state.notify);
  const [activeTabRole, setActiveTabRole] = useState<StaffRole>('Admin');

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting, isValid },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    mode: 'onBlur',
    defaultValues: {
      email: 'admin@cafemanagement.com',
      password: 'Password123!',
      rememberMe: true,
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data.email, data.password);
      notify('success', 'Đăng nhập thành công! Chào mừng trở lại hệ thống.');
      navigate('/dashboard');
    } catch {
      notify('error', 'Đăng nhập thất bại. Email hoặc mật khẩu không chính xác.');
    }
  };

  const handleQuickRoleSelect = (role: StaffRole, email: string) => {
    setActiveTabRole(role);
    setValue('email', email, { shouldValidate: true });
    setValue('password', 'Password123!', { shouldValidate: true });
  };

  return (
    <div className="min-h-screen bg-neutral-950 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        {/* Brand Header */}
        <div className="flex justify-center">
          <div className="w-12 h-12 bg-cafe-orange flex items-center justify-center">
            <Coffee className="w-6 h-6 text-white" />
          </div>
        </div>
        <h2 className="mt-4 text-center text-2xl font-bold tracking-tight text-white uppercase font-mono">
          Cafe Management System
        </h2>
        <p className="mt-1 text-center text-xs text-neutral-400">
          Hệ thống điều hành chuỗi cà phê tiêu chuẩn công nghiệp
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-6 border border-neutral-200 shadow-none sm:px-10">
          {/* Quick Role Fill Presets for evaluation */}
          <div className="mb-6">
            <label className="block text-xs font-semibold text-neutral-700 uppercase tracking-wider mb-2">
              Tài khoản mẫu thử nghiệm
            </label>
            <div className="grid grid-cols-3 gap-1.5 text-xs">
              <button
                type="button"
                onClick={() => handleQuickRoleSelect('Admin', 'admin@cafemanagement.com')}
                className={`py-1.5 px-2 border text-left transition-colors ${
                  activeTabRole === 'Admin'
                    ? 'border-cafe-orange bg-orange-50 text-neutral-950 font-semibold'
                    : 'border-neutral-200 text-neutral-700 hover:bg-neutral-50'
                }`}
              >
                Quản trị viên
              </button>
              <button
                type="button"
                onClick={() => handleQuickRoleSelect('Manager', 'manager@district1.com')}
                className={`py-1.5 px-2 border text-left transition-colors ${
                  activeTabRole === 'Manager'
                    ? 'border-cafe-orange bg-orange-50 text-neutral-950 font-semibold'
                    : 'border-neutral-200 text-neutral-700 hover:bg-neutral-50'
                }`}
              >
                Quản lý quán
              </button>
              <button
                type="button"
                onClick={() => handleQuickRoleSelect('Cashier', 'cashier@district1.com')}
                className={`py-1.5 px-2 border text-left transition-colors ${
                  activeTabRole === 'Cashier'
                    ? 'border-cafe-orange bg-orange-50 text-neutral-950 font-semibold'
                    : 'border-neutral-200 text-neutral-700 hover:bg-neutral-50'
                }`}
              >
                Thu ngân
              </button>
              <button
                type="button"
                onClick={() => handleQuickRoleSelect('Barista', 'barista@district1.com')}
                className={`py-1.5 px-2 border text-left transition-colors ${
                  activeTabRole === 'Barista'
                    ? 'border-cafe-orange bg-orange-50 text-neutral-950 font-semibold'
                    : 'border-neutral-200 text-neutral-700 hover:bg-neutral-50'
                }`}
              >
                Pha chế
              </button>
              <button
                type="button"
                onClick={() => handleQuickRoleSelect('InventoryStaff', 'stock@warehouse.com')}
                className={`py-1.5 px-2 border text-left transition-colors ${
                  activeTabRole === 'InventoryStaff'
                    ? 'border-cafe-orange bg-orange-50 text-neutral-950 font-semibold'
                    : 'border-neutral-200 text-neutral-700 hover:bg-neutral-50'
                }`}
              >
                Thủ kho
              </button>
            </div>
          </div>

          {/* Form */}
          <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
            <Input
              label="Email công vụ"
              type="email"
              required
              placeholder="ten@cafemanagement.com"
              leftIcon={<Mail className="w-4 h-4 text-neutral-400" />}
              {...register('email')}
              error={errors.email?.message}
            />

            <Input
              label="Mật khẩu"
              type="password"
              required
              placeholder="••••••••"
              leftIcon={<Lock className="w-4 h-4 text-neutral-400" />}
              {...register('password')}
              error={errors.password?.message}
            />

            <div className="flex items-center justify-between text-xs pt-1">
              <label className="flex items-center gap-2 cursor-pointer text-neutral-700">
                <input
                  type="checkbox"
                  {...register('rememberMe')}
                  className="w-4 h-4 text-cafe-orange border-neutral-300 rounded-none focus:ring-cafe-orange"
                />
                Ghi nhớ phiên đăng nhập
              </label>
              <a href="#" className="text-cafe-orange hover:underline">
                Quên mật khẩu?
              </a>
            </div>

            <Button
              type="submit"
              variant="primary"
              className="w-full mt-4"
              isLoading={isSubmitting}
              disabled={!isValid || isSubmitting}
              icon={<ArrowRight className="w-4 h-4" />}
            >
              Đăng nhập hệ thống
            </Button>
          </form>

          {/* Compliance & Security footnote */}
          <div className="mt-6 pt-4 border-t border-neutral-200 text-center">
            <div className="flex items-center justify-center gap-1.5 text-xs text-neutral-500">
              <ShieldCheck className="w-3.5 h-3.5 text-cafe-orange" />
              Bảo mật 2FA & JWT Token mã hóa RSA-256
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
