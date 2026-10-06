import React from 'react';
import { CheckCircle2, AlertCircle, Info, X } from 'lucide-react';
import { useAppStore } from '../../stores/appStore';

export const NotificationToast: React.FC = () => {
  const { notifications, dismissNotification } = useAppStore();

  if (notifications.length === 0) return null;

  return (
    <div className="fixed bottom-5 right-5 z-50 flex flex-col gap-2 max-w-sm w-full">
      {notifications.map((n) => {
        const icon = {
          success: <CheckCircle2 className="w-4 h-4 text-emerald-600 flex-shrink-0" />,
          error: <AlertCircle className="w-4 h-4 text-red-600 flex-shrink-0" />,
          info: <Info className="w-4 h-4 text-neutral-800 flex-shrink-0" />,
        }[n.type];

        const bg = {
          success: 'bg-emerald-50 border-emerald-300 text-emerald-950',
          error: 'bg-red-50 border-red-300 text-red-950',
          info: 'bg-neutral-100 border-neutral-300 text-neutral-950',
        }[n.type];

        return (
          <div
            key={n.id}
            className={`flex items-start justify-between p-3 rounded border shadow-lg ${bg}`}
          >
            <div className="flex items-center gap-2.5">
              {icon}
              <span className="text-xs font-semibold">{n.message}</span>
            </div>
            <button
              onClick={() => dismissNotification(n.id)}
              className="text-neutral-500 hover:text-neutral-900 p-0.5 ml-2"
            >
              <X className="w-3.5 h-3.5" />
            </button>
          </div>
        );
      })}
    </div>
  );
};
