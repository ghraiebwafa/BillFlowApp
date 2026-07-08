export type PagedResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export function buildPageQuery(params: {
  search?: string;
  status?: number | string;
  statuses?: Array<number | string>;
  page?: number;
  pageSize?: number;
}): string {
  const query = new URLSearchParams();
  if (params.search?.trim()) query.set("search", params.search.trim());
  if (params.status !== undefined && params.status !== null && params.status !== "") {
    query.set("status", String(params.status));
  }
  for (const status of params.statuses ?? []) {
    query.append("statuses", String(status));
  }
  if (params.page !== undefined) query.set("page", String(params.page));
  if (params.pageSize !== undefined) query.set("pageSize", String(params.pageSize));
  const serialized = query.toString();
  return serialized ? `?${serialized}` : "";
}
