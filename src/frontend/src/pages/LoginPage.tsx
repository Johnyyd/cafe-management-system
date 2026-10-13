import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Coffee, Lock, Mail, ArrowRight } from 'lucide-react';
import { Input } from '../components/common/Input';
import { Button } from '../components/common/Button';
import { useAuthStore } from '../stores/authStore';
import { useAppStore } from '../stores/appStore';
import { loginSchema, LoginFormData } from '../schemas';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);
  const notify = useAppStore((state) => state.notify);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting, isValid },
  } = useForm<LoginFormData>({

    resolver: zodResolver(loginSchema),
    mode: 'onBlur',
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data.email, data.password);
      notify('success', 'Đăng nhập thành công! Chào mừng trở lại hệ thống.');
      const currentRole = useAuthStore.getState().activeRole;
      if (currentRole === 'Cashier' || currentRole === 'Barista') {
        navigate('/orders');
      } else if (currentRole === 'InventoryStaff') {
        navigate('/inventory');
      } else {
        navigate('/dashboard');
      }
    } catch {
      notify('error', 'Đăng nhập thất bại. Email hoặc mật khẩu không chính xác.');
    }
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
          {/* Form */}
          <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>

            <Input
              label="Email"
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
              Đăng nhập
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
};
