import "server-only";

import { apiFetch } from "./client";
import {
  AuthTokenResponse,
  ChangePasswordRequest,
  ConfirmAccountActivationRequest,
  ForgotPasswordRequest,
  JwksResponse,
  LoginRequest,
  LogoutRequest,
  MessageResponse,
  RefreshTokenRequest,
  RequestAccountActivationRequest,
  ResetPasswordRequest,
  UserPermissionsResponse,
  UserProfileSummaryDto,
  VerifyTokenRequest,
  VerifyTokenResponse,
} from "./types";

export const authApi = {
  login: (request: LoginRequest): Promise<AuthTokenResponse> =>
    apiFetch<AuthTokenResponse>("/api/v1/auth/login", {
      method: "POST",
      body: request,
      token: null,
    }),

  refresh: (request: RefreshTokenRequest): Promise<AuthTokenResponse> =>
    apiFetch<AuthTokenResponse>("/api/v1/auth/refresh", {
      method: "POST",
      body: request,
      token: null,
    }),

  logout: (request?: LogoutRequest, token?: string): Promise<MessageResponse> =>
    apiFetch<MessageResponse>("/api/v1/auth/logout", {
      method: "POST",
      body: request || {},
      token,
    }),

  requestActivation: (
    request: RequestAccountActivationRequest
  ): Promise<MessageResponse> =>
    apiFetch<MessageResponse>("/api/v1/auth/activate-account/request", {
      method: "POST",
      body: request,
      token: null,
    }),

  confirmActivation: (
    request: ConfirmAccountActivationRequest
  ): Promise<MessageResponse> =>
    apiFetch<MessageResponse>("/api/v1/auth/activate-account/confirm", {
      method: "POST",
      body: request,
      token: null,
    }),

  forgotPassword: (request: ForgotPasswordRequest): Promise<MessageResponse> =>
    apiFetch<MessageResponse>("/api/v1/auth/forgot-password", {
      method: "POST",
      body: request,
      token: null,
    }),

  resetPassword: (request: ResetPasswordRequest): Promise<MessageResponse> =>
    apiFetch<MessageResponse>("/api/v1/auth/reset-password", {
      method: "POST",
      body: request,
      token: null,
    }),

  getMe: (token?: string): Promise<UserProfileSummaryDto> =>
    apiFetch<UserProfileSummaryDto>("/api/v1/auth/me", {
      method: "GET",
      token,
    }),

  changePassword: (
    request: ChangePasswordRequest,
    token?: string
  ): Promise<MessageResponse> =>
    apiFetch<MessageResponse>("/api/v1/auth/change-password", {
      method: "POST",
      body: request,
      token,
    }),

  verifyToken: (request: VerifyTokenRequest): Promise<VerifyTokenResponse> =>
    apiFetch<VerifyTokenResponse>("/api/v1/auth/verify", {
      method: "POST",
      body: request,
    }),

  getUserPermissions: (
    userId: string,
    clientId: string,
    token: string
  ): Promise<UserPermissionsResponse> =>
    apiFetch<UserPermissionsResponse>(
      `/api/v1/auth/permissions?userId=${encodeURIComponent(userId)}&clientId=${encodeURIComponent(clientId)}`,
      {
        method: "GET",
        token,
      }
    ),

  getJwks: (): Promise<JwksResponse> =>
    apiFetch<JwksResponse>("/.well-known/jwks.json", {
      method: "GET",
    }),
};

