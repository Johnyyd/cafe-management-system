import React from 'react';

export interface PageContainerProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  children: React.ReactNode;
}

export const PageContainer: React.FC<PageContainerProps> = ({
  title,
  subtitle,
  actions,
  children,
}) => {
  return (
    <div className="flex-1 flex flex-col overflow-y-auto">
      {/* Page Title & Action Bar */}
      <div className="bg-white border-b border-neutral-200 px-8 py-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-xl font-black text-neutral-900 tracking-tight uppercase">{title}</h1>
          {subtitle && <p className="text-xs text-neutral-500 mt-1 font-medium">{subtitle}</p>}
        </div>
        {actions && <div className="flex items-center gap-2.5">{actions}</div>}
      </div>

      {/* Main Content Body */}
      <main className="flex-1 p-8 bg-neutral-50/60 max-w-7xl w-full mx-auto">{children}</main>
    </div>
  );
};
