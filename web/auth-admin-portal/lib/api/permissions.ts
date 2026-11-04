import "server-only";

import { apiFetch } from "./client";
import {
  CreatePermissionRequest,
  MessageResponse,
  PermissionDto,
  UpdatePermissionRequest,
} from "./types";

export const permissionsApi = {
  getAll: (appId: string, token?: string): Promise<PermissionDto[]> =>
    apiFetch<PermissionDto[]>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/permissions`,
      {
        method: "GET",
        token,
      }
    ),

  getById: (
    appId: string,
    permissionId: string,
    token?: string
  ): Promise<PermissionDto> =>
    apiFetch<PermissionDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/permissions/${encodeURIComponent(permissionId)}`,
      {
        method: "GET",
        token,
      }
    ),

  create: (
    appId: string,
    request: CreatePermissionRequest,
    token?: string
  ): Promise<PermissionDto> =>
    apiFetch<PermissionDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/permissions`,
      {
        method: "POST",
        body: request,
        token,
      }
    ),

  update: (
    appId: string,
    permissionId: string,
    request: UpdatePermissionRequest,
    token?: string
  ): Promise<PermissionDto> =>
    apiFetch<PermissionDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/permissions/${encodeURIComponent(permissionId)}`,
      {
        method: "PUT",
        body: request,
        token,
      }
    ),

  delete: (
    appId: string,
    permissionId: string,
    token?: string
  ): Promise<MessageResponse> =>
    apiFetch<MessageResponse>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/permissions/${encodeURIComponent(permissionId)}`,
      {
        method: "DELETE",
        token,
      }
    ),
};
