import React from 'react';
import { PackageOpen } from 'lucide-react';
import { Button } from './Button';

export interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
  action?: React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  actionLabel,
  onAction,
  action,
}) => {
  return (
    <div className="flex flex-col items-center justify-center text-center p-12 border border-dashed border-neutral-300 rounded bg-neutral-50/50">
      <div className="p-3 bg-neutral-100 rounded text-neutral-500 mb-3 border border-neutral-200">
        {icon || <PackageOpen className="w-8 h-8" />}
      </div>
      <h4 className="text-sm font-bold text-neutral-900 uppercase tracking-wider">{title}</h4>
      <p className="text-xs text-neutral-500 max-w-sm mt-1 mb-4">{description}</p>
      {action ? (
        action
      ) : actionLabel && onAction ? (
        <Button variant="outline" size="sm" onClick={onAction}>
          {actionLabel}
        </Button>
      ) : null}
    </div>
  );
};

