import { useTranslation } from "react-i18next";
import { totalPages } from "../../domain/billing/paging";

type PaginationBarProps = {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
};

export function PaginationBar({
  page,
  pageSize,
  totalCount,
  onPageChange,
  disabled = false,
}: PaginationBarProps) {
  const { t } = useTranslation();
  if (totalCount <= 0) return null;

  const pages = totalPages(totalCount, pageSize);
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div className="pagination-bar">
      <p className="pagination-summary text-sm text-secondary">
        {t("common.pagination.showing", { from, to, total: totalCount })}
      </p>
      {pages > 1 ? (
        <div className="pagination-controls">
          <button
            className="btn-secondary"
            type="button"
            disabled={disabled || page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            {t("common.pagination.previous")}
          </button>
          <span className="text-sm text-secondary">
            {t("common.pagination.pageOf", { page, pages })}
          </span>
          <button
            className="btn-secondary"
            type="button"
            disabled={disabled || page >= pages}
            onClick={() => onPageChange(page + 1)}
          >
            {t("common.pagination.next")}
          </button>
        </div>
      ) : null}
    </div>
  );
}
