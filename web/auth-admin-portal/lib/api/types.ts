// =============================================================================
// RFC 9457 Problem Details
// =============================================================================

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  timestampUtc?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface MessageResponse {
  message: string;
}

// =============================================================================
// Auth DTOs (AuthDtos.cs)
// =============================================================================

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
  clientId: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
  clientId?: string | null;
}

export interface LogoutRequest {
  refreshToken?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface RequestAccountActivationRequest {
  emailOrUsername: string;
}

export interface ConfirmAccountActivationRequest {
  token: string;
}

export interface ForgotPasswordRequest {
  emailOrUsername: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface VerifyTokenRequest {
  accessToken: string;
  requiredPermission?: string | null;
  requiredRole?: string | null;
}

export interface VerifyTokenResponse {
  isValid: boolean;
  isAuthorized: boolean;
  userId?: string | null;
  username?: string | null;
  email?: string | null;
  fullName?: string | null;
  clientId: string;
  expiresAtUtc?: string | null;
  roles: string[];
  permissions: string[];
  reason?: string | null;
}

export interface UserPermissionsResponse {
  userId: string;
  username: string;
  email: string;
  clientId: string;
  isUserActive: boolean;
  roles: string[];
  permissions: string[];
  fetchedAtUtc: string;
}

export interface UserProfileSummaryDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  clientId: string;
  isAccountVerified: boolean;
  roles: string[];
  permissions: string[];
}

export interface AuthTokenResponse {
  accessToken: string;
  tokenType: string;
  expiresInSeconds: number;
  expiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserProfileSummaryDto;
}

// =============================================================================
// Admin: ClientApps DTOs (AdminDtos.cs)
// =============================================================================

export interface CreateClientAppRequest {
  clientId: string;
  name: string;
  description?: string | null;
  allowedOrigins?: string | null;
  isActive?: boolean;
}

export interface UpdateClientAppRequest {
  name: string;
  description?: string | null;
  allowedOrigins?: string | null;
  isActive: boolean;
}

export interface ClientAppDto {
  id: string;
  clientId: string;
  name: string;
  description: string | null;
  apiKeyPrefix: string;
  allowedOrigins: string | null;
  isActive: boolean;
  rolesCount: number;
  permissionsCount: number;
  assignedUsersCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface ClientAppWithRawKeyResponse {
  app: ClientAppDto;
  rawApiKey: string;
  notice: string;
}

// =============================================================================
// Admin: User Management DTOs
// =============================================================================

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  fullName: string;
  isActive?: boolean;
  isAccountVerified?: boolean;
}

export interface UpdateUserRequest {
  email: string;
  fullName: string;
  isActive: boolean;
  isAccountVerified: boolean;
}

export interface UserAppAssignmentSummaryDto {
  clientAppId: string;
  clientId: string;
  appName: string;
  roles: string[];
}

export interface AdminUserDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  isActive: boolean;
  isAccountVerified: boolean;
  failedLoginCount: number;
  lockoutEndUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  appAssignments: UserAppAssignmentSummaryDto[];
}

export interface RevokeSessionsResponse {
  userId: string;
  revokedSessionsCount: number;
  message: string;
}

// =============================================================================
// Admin: Permissions & Roles DTOs
// =============================================================================

export interface CreatePermissionRequest {
  code: string;
  description?: string | null;
}

export interface UpdatePermissionRequest {
  code: string;
  description?: string | null;
}

export interface PermissionDto {
  id: string;
  clientAppId: string;
  code: string;
  description: string | null;
  createdAtUtc: string;
}

export interface CreateRoleRequest {
  name: string;
  description?: string | null;
  permissionIds?: string[] | null;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string | null;
}

export interface SetRolePermissionsRequest {
  permissionIds: string[];
}

export interface RoleDto {
  id: string;
  clientAppId: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  createdAtUtc: string;
  permissions: PermissionDto[];
}

// =============================================================================
// Admin: User Access Assignment per ClientApp DTOs
// =============================================================================

export interface SetUserAppRolesRequest {
  roleIds: string[];
}

export interface UserAppRolesResponse {
  userId: string;
  username: string;
  clientAppId: string;
  clientId: string;
  assignedRoles: RoleDto[];
  effectivePermissionCodes: string[];
}

// =============================================================================
// Admin: Audit Logs DTOs
// =============================================================================

export interface AuditLogFilterQuery {
  userId?: string | null;
  clientId?: string | null;
  eventType?: string | null;
  isSuccess?: boolean | null;
  dateFromUtc?: string | null;
  dateToUtc?: string | null;
  page?: number;
  pageSize?: number;
}

export interface AuditLogDto {
  id: string;
  timestampUtc: string;
  userId: string | null;
  username: string | null;
  clientAppId: string | null;
  clientId: string | null;
  eventType: string;
  isSuccess: boolean;
  ipAddress: string | null;
  userAgent: string | null;
  detailJson: string | null;
}

// =============================================================================
// Discovery: JWKS DTOs
// =============================================================================

export interface JwkKey {
  kty: string;
  use: string;
  alg: string;
  kid: string;
  n: string;
  e: string;
}

export interface JwksResponse {
  keys: JwkKey[];
}

