import React from 'react';

export interface DialogConfig {
  title: string;
  message: string;
  type: 'info' | 'warning' | 'error' | 'confirm';
  onConfirm?: () => void;
}

interface DialogModalProps {
  dialog: DialogConfig | null;
  onClose: () => void;
}

export const DialogModal: React.FC<DialogModalProps> = ({ dialog, onClose }) => {
  if (!dialog) return null;

  const headerColor =
    dialog.type === 'error'
      ? 'bg-[#DC2626]'
      : dialog.type === 'warning'
        ? 'bg-[#D97706]'
        : dialog.type === 'confirm'
          ? 'bg-[#1E293B]'
          : 'bg-[#1B365D]';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-md overflow-hidden rounded border border-[#CBD5E1] bg-white shadow-xl">
        <div className={`${headerColor} px-5 py-3 text-sm font-bold text-white`}>
          {dialog.title}
        </div>
        <div className="px-5 py-5 text-sm leading-relaxed text-[#334155] whitespace-pre-line">
          {dialog.message}
        </div>
        <div className="flex justify-end gap-2 border-t border-[#E2E8F0] bg-[#F8FAFC] px-5 py-3">
          {dialog.type === 'confirm' ? (
            <>
              <button
                type="button"
                onClick={onClose}
                className="cursor-pointer rounded bg-[#94A3B8] px-4 py-1.5 text-xs font-bold text-white hover:bg-[#64748B]"
              >
                Não
              </button>
              <button
                type="button"
                onClick={() => {
                  const cb = dialog.onConfirm;
                  onClose();
                  cb?.();
                }}
                className="cursor-pointer rounded bg-[#DC2626] px-4 py-1.5 text-xs font-bold text-white hover:bg-[#B91C1C]"
              >
                Sim
              </button>
            </>
          ) : (
            <button
              type="button"
              onClick={onClose}
              className="cursor-pointer rounded bg-[#2563EB] px-5 py-1.5 text-xs font-bold text-white hover:bg-[#1D4ED8]"
            >
              OK
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
