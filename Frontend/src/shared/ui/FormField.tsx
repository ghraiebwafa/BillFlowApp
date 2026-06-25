import { useState, type InputHTMLAttributes } from "react";
import type { LucideIcon } from "lucide-react";
import { Eye, EyeOff } from "lucide-react";

type FormFieldProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  error?: string;
  icon?: LucideIcon;
  showPasswordToggle?: boolean;
};

export function FormField({
  label,
  error,
  id,
  className,
  icon: Icon,
  showPasswordToggle,
  type,
  ...props
}: FormFieldProps) {
  const fieldId = id ?? props.name;
  const [visible, setVisible] = useState(false);
  const isPassword = type === "password";
  const inputType = isPassword && showPasswordToggle && visible ? "text" : type;

  return (
    <label className="block space-y-1.5 text-sm" htmlFor={fieldId}>
      <span className="font-medium text-primary">{label}</span>
      <div className="relative">
        {Icon ? (
          <Icon
            className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-secondary"
            aria-hidden
          />
        ) : null}
        <input
          id={fieldId}
          type={inputType}
          className={`w-full rounded-xl border border-muted bg-panel py-2.5 text-primary outline-none focus:border-[var(--billflow-orange)] ${
            Icon ? "pl-10" : "px-3"
          } ${showPasswordToggle && isPassword ? "pr-10" : Icon ? "pr-3" : "px-3"} ${className ?? ""}`}
          {...props}
        />
        {showPasswordToggle && isPassword ? (
          <button
            className="absolute top-1/2 right-3 -translate-y-1/2 text-secondary"
            onClick={() => setVisible((v) => !v)}
            type="button"
            aria-label={visible ? "Hide password" : "Show password"}
          >
            {visible ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
          </button>
        ) : null}
      </div>
      {error ? <span className="text-xs text-red-500">{error}</span> : null}
    </label>
  );
}
