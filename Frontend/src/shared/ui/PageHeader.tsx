import { Link } from "react-router-dom";
import { ChevronLeft } from "lucide-react";

type PageHeaderProps = {
  title: string;
  subtitle?: string;
  backTo?: string;
};

export function PageHeader({ title, subtitle, backTo }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div className="page-header-row">
        {backTo ? (
          <Link to={backTo} className="page-header-back" aria-label="Back">
            <ChevronLeft className="h-5 w-5" strokeWidth={2} />
          </Link>
        ) : (
          <span className="page-header-back-placeholder" aria-hidden />
        )}
        <h2 className="page-header-title">{title}</h2>
        <span className="page-header-back-placeholder" aria-hidden />
      </div>
      {subtitle ? <p className="page-header-subtitle">{subtitle}</p> : null}
    </header>
  );
}
