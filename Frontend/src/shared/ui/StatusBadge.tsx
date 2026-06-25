import { clsx } from "clsx";

type StatusBadgeProps = {
  label: string;
  variant: "paid" | "partial" | "unpaid" | "draft" | "completed";
};

const variantClass: Record<StatusBadgeProps["variant"], string> = {
  paid: "status-badge--paid",
  partial: "status-badge--partial",
  unpaid: "status-badge--unpaid",
  draft: "status-badge--draft",
  completed: "status-badge--paid",
};

export function StatusBadge({ label, variant }: StatusBadgeProps) {
  return <span className={clsx("status-badge", variantClass[variant])}>{label}</span>;
}
