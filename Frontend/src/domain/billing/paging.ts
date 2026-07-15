import { z } from "zod";

export type PagedResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export function pagedSchema<T extends z.ZodTypeAny>(itemSchema: T) {
  return z.object({
    items: z.array(itemSchema),
    totalCount: z.number().int(),
    page: z.number().int(),
    pageSize: z.number().int(),
  });
}

export function buildPageQuery(params: {
  search?: string;
  status?: number | string;
  statuses?: Array<number | string>;
  page?: number;
  pageSize?: number;
  includeArchived?: boolean;
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
  if (params.includeArchived !== undefined) {
    query.set("includeArchived", String(params.includeArchived));
  }
  const serialized = query.toString();
  return serialized ? `?${serialized}` : "";
}

export function totalPages(totalCount: number, pageSize: number): number {
  if (pageSize <= 0) return 0;
  return Math.max(1, Math.ceil(totalCount / pageSize));
}
