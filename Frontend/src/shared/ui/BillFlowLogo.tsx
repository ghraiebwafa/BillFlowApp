import { clsx } from "clsx";
import { BILLFLOW_LOGO } from "../brand/assets";

type BillFlowLogoProps = {
  className?: string;
  /** full = horizontal lockup; compact = smaller for headers */
  size?: "splash" | "auth" | "header" | "compact";
};

const sizeClass: Record<NonNullable<BillFlowLogoProps["size"]>, string> = {
  splash: "billflow-logo--splash",
  auth: "billflow-logo--auth",
  header: "billflow-logo--header",
  compact: "billflow-logo--compact",
};

export function BillFlowLogo({ className, size = "auth" }: BillFlowLogoProps) {
  return (
    <img
      src={BILLFLOW_LOGO}
      alt="BillFlow"
      className={clsx("billflow-logo", sizeClass[size], className)}
    />
  );
}
