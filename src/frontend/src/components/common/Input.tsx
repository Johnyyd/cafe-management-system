import React, { forwardRef } from 'react';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  icon?: React.ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, leftIcon, rightIcon, icon, className = '', id, required, ...props }, ref) => {
    const inputId = id || (label ? label.toLowerCase().replace(/\s+/g, '-') : undefined);
    const displayLeftIcon = leftIcon || icon;

    return (
      <div className="w-full">
        {label && (
          <label htmlFor={inputId} className="block text-xs font-semibold uppercase tracking-wider text-neutral-700 mb-1.5">
            {label} {required && <span className="text-cafe-orange">*</span>}
          </label>
        )}
        <div className="relative rounded">
          {displayLeftIcon && (
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-neutral-500">
              {displayLeftIcon}
            </div>
          )}
          <input
            ref={ref}
            id={inputId}
            className={`block w-full text-sm rounded border bg-white px-3 py-2 text-neutral-900 placeholder-neutral-400 transition-colors focus:outline-none focus:ring-1 ${
              displayLeftIcon ? 'pl-9' : ''
            } ${rightIcon ? 'pr-9' : ''} ${
              error
                ? 'border-red-500 focus:border-red-500 focus:ring-red-500 bg-red-50/20'
                : 'border-neutral-300 focus:border-cafe-orange focus:ring-cafe-orange'
            } disabled:bg-neutral-100 disabled:cursor-not-allowed ${className}`}
            {...props}
          />
          {rightIcon && (
            <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none text-neutral-500">
              {rightIcon}
            </div>
          )}
        </div>
        {error ? (
          <p className="mt-1 text-xs font-medium text-red-600 flex items-center gap-1">
            <span>•</span> {error}
          </p>
        ) : helperText ? (
          <p className="mt-1 text-xs text-neutral-500">{helperText}</p>
        ) : null}
      </div>
    );
  }
);

Input.displayName = 'Input';
