import React from 'react';

export interface BadgeProps {
  children: React.ReactNode;
  variant?: 'orange' | 'black' | 'neutral' | 'success' | 'warning' | 'danger' | 'primary' | 'error';
  size?: 'sm' | 'md';
}

export const Badge: React.FC<BadgeProps> = ({
  children,
  variant = 'neutral',
  size = 'md',
}) => {
  const sizeClasses = {
    sm: 'px-1.5 py-0.5 text-[10px]',
    md: 'px-2 py-0.5 text-xs',
  }[size];

  const variantClasses = {
    orange: 'bg-orange-50 text-cafe-orange-dark border border-orange-200',
    primary: 'bg-orange-50 text-cafe-orange-dark border border-orange-200',
    black: 'bg-neutral-900 text-white border border-neutral-900',
    neutral: 'bg-neutral-100 text-neutral-800 border border-neutral-300',
    success: 'bg-emerald-50 text-emerald-800 border border-emerald-200',
    warning: 'bg-amber-50 text-amber-800 border border-amber-200',
    danger: 'bg-red-50 text-red-800 border border-red-200',
    error: 'bg-red-50 text-red-800 border border-red-200',
  }[variant];

  return (
    <span
      className={`inline-flex items-center font-medium rounded uppercase tracking-wider ${sizeClasses} ${variantClasses}`}
    >
      {children}
    </span>
  );
};
