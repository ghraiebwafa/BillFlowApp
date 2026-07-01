import { X, CheckCircle2, AlertCircle, Info } from "lucide-react";
import type { Toast, ToastVariant } from "./toast-store";
import { useToastStore } from "./toast-store";

function ToastIcon({ variant }: { variant: ToastVariant }) {
  const className = "h-5 w-5 shrink-0";
  switch (variant) {
    case "success":
      return <CheckCircle2 className={className} aria-hidden />;
    case "error":
      return <AlertCircle className={className} aria-hidden />;
    default:
      return <Info className={className} aria-hidden />;
  }
}

function ToastItem({ toast }: { toast: Toast }) {
  const dismiss = useToastStore((s) => s.dismiss);

  return (
    <div className={`toast toast--${toast.variant}`} role="status" aria-live="polite">
      <ToastIcon variant={toast.variant} />
      <p className="toast-message">{toast.message}</p>
      <button
        className="toast-dismiss"
        onClick={() => dismiss(toast.id)}
        type="button"
        aria-label="Dismiss"
      >
        <X className="h-4 w-4" aria-hidden />
      </button>
    </div>
  );
}

export function ToastHost() {
  const toasts = useToastStore((s) => s.toasts);

  if (toasts.length === 0) return null;

  return (
    <div className="toast-host" aria-label="Notifications">
      {toasts.map((item) => (
        <ToastItem key={item.id} toast={item} />
      ))}
    </div>
  );
}
