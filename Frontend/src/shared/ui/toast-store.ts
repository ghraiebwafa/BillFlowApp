import { create } from "zustand";

export type ToastVariant = "success" | "error" | "info";

export type Toast = {
  id: string;
  message: string;
  variant: ToastVariant;
};

type ToastState = {
  toasts: Toast[];
  push: (message: string, variant?: ToastVariant) => void;
  dismiss: (id: string) => void;
};

const DEFAULT_DURATION_MS = 4_500;
const timers = new Map<string, ReturnType<typeof setTimeout>>();

function scheduleDismiss(id: string, dismiss: (id: string) => void) {
  const existing = timers.get(id);
  if (existing) clearTimeout(existing);

  const timer = setTimeout(() => {
    timers.delete(id);
    dismiss(id);
  }, DEFAULT_DURATION_MS);

  timers.set(id, timer);
}

export const useToastStore = create<ToastState>((set, get) => ({
  toasts: [],

  push: (message, variant = "info") => {
    const id = crypto.randomUUID();
    set((state) => ({ toasts: [...state.toasts, { id, message, variant }] }));
    scheduleDismiss(id, get().dismiss);
  },

  dismiss: (id) => {
    const timer = timers.get(id);
    if (timer) {
      clearTimeout(timer);
      timers.delete(id);
    }

    set((state) => ({ toasts: state.toasts.filter((t) => t.id !== id) }));
  },
}));

export function toast(message: string, variant: ToastVariant = "info") {
  useToastStore.getState().push(message, variant);
}
