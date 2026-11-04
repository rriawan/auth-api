import "server-only";

import { apiFetch } from "./client";
import { AuditLogDto, AuditLogFilterQuery, PagedResponse } from "./types";

export const auditLogsApi = {
  getAll: (
    query: AuditLogFilterQuery = {},
    token?: string
  ): Promise<PagedResponse<AuditLogDto>> => {
    const params = new URLSearchParams();
    if (query.userId) params.set("userId", query.userId);
    if (query.clientId) params.set("clientId", query.clientId);
    if (query.eventType) params.set("eventType", query.eventType);
    if (query.isSuccess !== undefined && query.isSuccess !== null) {
      params.set("isSuccess", query.isSuccess.toString());
    }
    if (query.dateFromUtc) params.set("dateFrom", query.dateFromUtc);
    if (query.dateToUtc) params.set("dateTo", query.dateToUtc);
    if (query.page) params.set("page", query.page.toString());
    if (query.pageSize) params.set("pageSize", query.pageSize.toString());

    const qs = params.toString();
    const endpoint = `/api/v1/admin/audit-logs${qs ? `?${qs}` : ""}`;

    return apiFetch<PagedResponse<AuditLogDto>>(endpoint, {
      method: "GET",
      token,
    });
  },
};
