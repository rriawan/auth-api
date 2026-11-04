import "server-only";

import { apiFetch } from "./client";
import {
  ClientAppDto,
  ClientAppWithRawKeyResponse,
  CreateClientAppRequest,
  UpdateClientAppRequest,
} from "./types";

export const appsApi = {
  getAll: (token?: string): Promise<ClientAppDto[]> =>
    apiFetch<ClientAppDto[]>("/api/v1/admin/apps", {
      method: "GET",
      token,
    }),

  getById: (id: string, token?: string): Promise<ClientAppDto> =>
    apiFetch<ClientAppDto>(`/api/v1/admin/apps/${encodeURIComponent(id)}`, {
      method: "GET",
      token,
    }),

  create: (
    request: CreateClientAppRequest,
    token?: string
  ): Promise<ClientAppWithRawKeyResponse> =>
    apiFetch<ClientAppWithRawKeyResponse>("/api/v1/admin/apps", {
      method: "POST",
      body: request,
      token,
    }),

  update: (
    id: string,
    request: UpdateClientAppRequest,
    token?: string
  ): Promise<ClientAppDto> =>
    apiFetch<ClientAppDto>(`/api/v1/admin/apps/${encodeURIComponent(id)}`, {
      method: "PUT",
      body: request,
      token,
    }),

  rotateKey: (
    id: string,
    token?: string
  ): Promise<ClientAppWithRawKeyResponse> =>
    apiFetch<ClientAppWithRawKeyResponse>(
      `/api/v1/admin/apps/${encodeURIComponent(id)}/rotate-key`,
      {
        method: "POST",
        token,
      }
    ),
};

