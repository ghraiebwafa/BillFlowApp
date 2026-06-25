import type { TextareaHTMLAttributes } from "react";

type FormTextAreaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label: string;
  error?: string;
};

export function FormTextArea({ label, error, id, className, ...props }: FormTextAreaProps) {
  const fieldId = id ?? props.name;

  return (
    <label className="block space-y-1 text-sm" htmlFor={fieldId}>
      <span className="text-secondary">{label}</span>
      <textarea
        id={fieldId}
        rows={3}
        className={`w-full rounded-md border border-muted bg-panel px-3 py-2 text-primary outline-none focus:border-accent ${className ?? ""}`}
        {...props}
      />
      {error ? <span className="text-xs text-red-500">{error}</span> : null}
    </label>
  );
}
