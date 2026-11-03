import "server-only";

import { apiFetch } from "./client";
import { SetUserAppRolesRequest, UserAppRolesResponse } from "./types";

export const userAppRolesApi = {
  getRoles: (
    userId: string,
    clientAppId: string,
    token: string
  ): Promise<UserAppRolesResponse> =>
    apiFetch<UserAppRolesResponse>(
      `/api/v1/admin/users/${encodeURIComponent(userId)}/apps/${encodeURIComponent(clientAppId)}/roles`,
      {
        method: "GET",
        token,
      }
    ),

  setRoles: (
    userId: string,
    clientAppId: string,
    request: SetUserAppRolesRequest,
    token: string
  ): Promise<UserAppRolesResponse> =>
    apiFetch<UserAppRolesResponse>(
      `/api/v1/admin/users/${encodeURIComponent(userId)}/apps/${encodeURIComponent(clientAppId)}/roles`,
      {
        method: "PUT",
        body: request,
        token,
      }
    ),
};

