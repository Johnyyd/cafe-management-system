import React from 'react';

export interface CardProps {
  children: React.ReactNode;
  title?: string;
  subtitle?: string;
  action?: React.ReactNode;
  className?: string;
  noPadding?: boolean;
}

export const Card: React.FC<CardProps> = ({
  children,
  title,
  subtitle,
  action,
  className = '',
  noPadding = false,
}) => {
  return (
    <div className={`bg-white border border-neutral-200 rounded shadow-none ${className}`}>
      {(title || action) && (
        <div className="px-5 py-4 border-b border-neutral-200 flex items-center justify-between">
          <div>
            {title && <h3 className="text-sm font-bold uppercase tracking-wider text-neutral-900">{title}</h3>}
            {subtitle && <p className="text-xs text-neutral-500 mt-0.5">{subtitle}</p>}
          </div>
          {action && <div>{action}</div>}
        </div>
      )}
      <div className={noPadding ? '' : 'p-5'}>{children}</div>
    </div>
  );
};
