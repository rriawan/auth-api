import "server-only";

import { apiFetch } from "./client";
import {
  AdminUserDto,
  CreateUserRequest,
  MessageResponse,
  PagedResponse,
  RevokeSessionsResponse,
  UpdateUserRequest,
} from "./types";

export interface UsersQuery {
  search?: string | null;
  page?: number;
  pageSize?: number;
}

export const usersApi = {
  getAll: (
    query: UsersQuery = {},
    token: string
  ): Promise<PagedResponse<AdminUserDto>> => {
    const params = new URLSearchParams();
    if (query.search) params.set("search", query.search);
    if (query.page) params.set("page", query.page.toString());
    if (query.pageSize) params.set("pageSize", query.pageSize.toString());

    const qs = params.toString();
    const endpoint = `/api/v1/admin/users${qs ? `?${qs}` : ""}`;

    return apiFetch<PagedResponse<AdminUserDto>>(endpoint, {
      method: "GET",
      token,
    });
  },

  getById: (id: string, token: string): Promise<AdminUserDto> =>
    apiFetch<AdminUserDto>(`/api/v1/admin/users/${encodeURIComponent(id)}`, {
      method: "GET",
      token,
    }),

  create: (request: CreateUserRequest, token: string): Promise<AdminUserDto> =>
    apiFetch<AdminUserDto>("/api/v1/admin/users", {
      method: "POST",
      body: request,
      token,
    }),

  update: (
    id: string,
    request: UpdateUserRequest,
    token: string
  ): Promise<AdminUserDto> =>
    apiFetch<AdminUserDto>(`/api/v1/admin/users/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: request,
      token,
    }),

  unlock: (id: string, token: string): Promise<MessageResponse> =>
    apiFetch<MessageResponse>(
      `/api/v1/admin/users/${encodeURIComponent(id)}/unlock`,
      {
        method: "POST",
        token,
      }
    ),

  revokeSessions: (
    id: string,
    token: string
  ): Promise<RevokeSessionsResponse> =>
    apiFetch<RevokeSessionsResponse>(
      `/api/v1/admin/users/${encodeURIComponent(id)}/revoke-sessions`,
      {
        method: "POST",
        token,
      }
    ),
};

