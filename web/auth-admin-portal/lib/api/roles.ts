import "server-only";

import { apiFetch } from "./client";
import {
  CreateRoleRequest,
  MessageResponse,
  RoleDto,
  SetRolePermissionsRequest,
  UpdateRoleRequest,
} from "./types";

export const rolesApi = {
  getAll: (appId: string, token?: string): Promise<RoleDto[]> =>
    apiFetch<RoleDto[]>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/roles`,
      {
        method: "GET",
        token,
      }
    ),

  getById: (
    appId: string,
    roleId: string,
    token?: string
  ): Promise<RoleDto> =>
    apiFetch<RoleDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/roles/${encodeURIComponent(roleId)}`,
      {
        method: "GET",
        token,
      }
    ),

  create: (
    appId: string,
    request: CreateRoleRequest,
    token?: string
  ): Promise<RoleDto> =>
    apiFetch<RoleDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/roles`,
      {
        method: "POST",
        body: request,
        token,
      }
    ),

  update: (
    appId: string,
    roleId: string,
    request: UpdateRoleRequest,
    token?: string
  ): Promise<RoleDto> =>
    apiFetch<RoleDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/roles/${encodeURIComponent(roleId)}`,
      {
        method: "PUT",
        body: request,
        token,
      }
    ),

  delete: (
    appId: string,
    roleId: string,
    token?: string
  ): Promise<MessageResponse> =>
    apiFetch<MessageResponse>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/roles/${encodeURIComponent(roleId)}`,
      {
        method: "DELETE",
        token,
      }
    ),

  setPermissions: (
    appId: string,
    roleId: string,
    request: SetRolePermissionsRequest,
    token?: string
  ): Promise<RoleDto> =>
    apiFetch<RoleDto>(
      `/api/v1/admin/apps/${encodeURIComponent(appId)}/roles/${encodeURIComponent(roleId)}/permissions`,
      {
        method: "PUT",
        body: request,
        token,
      }
    ),
};
