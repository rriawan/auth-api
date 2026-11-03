/**
 * Mengecek apakah user memiliki role 'SuperAdmin' (case-insensitive).
 * SuperAdmin memiliki seluruh permission admin secara otomatis.
 */
export function isSuperAdmin(roles?: string[]): boolean {
  if (!roles || !Array.isArray(roles)) {
    return false;
  }
  return roles.some((r) => r.toLowerCase() === "superadmin");
}

/**
 * Memeriksa apakah user memiliki permission tertentu atau merupakan SuperAdmin.
 */
export function hasPermission(
  userPermissions?: string[],
  userRoles?: string[],
  requiredPermission?: string
): boolean {
  if (!requiredPermission) {
    return true;
  }
  if (isSuperAdmin(userRoles)) {
    return true;
  }
  if (!userPermissions || !Array.isArray(userPermissions)) {
    return false;
  }
  return userPermissions.includes(requiredPermission);
}

