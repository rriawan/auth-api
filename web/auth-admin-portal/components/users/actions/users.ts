"use server";

import { revalidatePath } from "next/cache";
import { ActionResult } from "@/components/apps/actions/apps";
import { ApiException, getErrorMessage } from "@/lib/api/errors";
import { rolesApi } from "@/lib/api/roles";
import {
  AdminUserDto,
  RevokeSessionsResponse,
  RoleDto,
  UserAppRolesResponse,
} from "@/lib/api/types";
import { userAppRolesApi } from "@/lib/api/user-app-roles";
import { usersApi } from "@/lib/api/users";
import { requirePermission } from "@/lib/auth/permissions";
import {
  SetUserAppRolesFormData,
  setUserAppRolesSchema,
} from "@/lib/validation/roles.schema";
import {
  CreateUserFormData,
  createUserSchema,
  UpdateUserFormData,
  updateUserSchema,
} from "@/lib/validation/users.schema";

export interface UserAppAccessDetail {
  availableRoles: RoleDto[];
  userAccess: UserAppRolesResponse;
}

/**
 * Server Action untuk mendaftarkan pengguna (User) baru oleh administrator.
 * Memerlukan permission 'users.write'.
 */
export async function createUserAction(
  formData: CreateUserFormData
): Promise<ActionResult<AdminUserDto>> {
  try {
    await requirePermission("users.write");

    const parsed = createUserSchema.safeParse(formData);
    if (!parsed.success) {
      const fieldErrors: Record<string, string> = {};
      for (const issue of parsed.error.issues) {
        const field = issue.path[0];
        if (field && !fieldErrors[field.toString()]) {
          fieldErrors[field.toString()] = issue.message;
        }
      }
      return {
        success: false,
        error: "Validasi formulir gagal. Silakan periksa kolom isian.",
        fieldErrors,
      };
    }

    const created = await usersApi.create({
      username: parsed.data.username.trim(),
      email: parsed.data.email.trim(),
      fullName: parsed.data.fullName.trim(),
      password: parsed.data.password,
      isActive: parsed.data.isActive ?? true,
      isAccountVerified: parsed.data.isAccountVerified ?? true,
    });

    revalidatePath("/users");

    return {
      success: true,
      data: created,
    };
  } catch (error) {
    if (error instanceof ApiException && error.isConflict) {
      return {
        success: false,
        error:
          error.problem.detail ||
          "Username atau Email sudah digunakan oleh pengguna lain.",
      };
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk memperbarui profil & status pengguna.
 * Memerlukan permission 'users.write'.
 */
export async function updateUserAction(
  userId: string,
  formData: UpdateUserFormData
): Promise<ActionResult<AdminUserDto>> {
  try {
    await requirePermission("users.write");

    const parsed = updateUserSchema.safeParse(formData);
    if (!parsed.success) {
      const fieldErrors: Record<string, string> = {};
      for (const issue of parsed.error.issues) {
        const field = issue.path[0];
        if (field && !fieldErrors[field.toString()]) {
          fieldErrors[field.toString()] = issue.message;
        }
      }
      return {
        success: false,
        error: "Validasi formulir gagal. Silakan periksa kolom isian.",
        fieldErrors,
      };
    }

    const updated = await usersApi.update(userId, {
      email: parsed.data.email.trim(),
      fullName: parsed.data.fullName.trim(),
      isActive: parsed.data.isActive,
      isAccountVerified: parsed.data.isAccountVerified,
    });

    revalidatePath("/users");
    revalidatePath(`/users/${userId}`);

    return {
      success: true,
      data: updated,
    };
  } catch (error) {
    if (error instanceof ApiException && error.isConflict) {
      return {
        success: false,
        error:
          error.problem.detail ||
          "Email sudah digunakan oleh pengguna lain.",
      };
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk membuka kunci akun pengguna (reset failedLoginCount & lockoutEndUtc).
 * Memerlukan permission 'users.unlock'.
 */
export async function unlockUserAction(
  userId: string
): Promise<ActionResult<AdminUserDto>> {
  try {
    await requirePermission("users.unlock");

    const updated = await usersApi.unlock(userId);

    revalidatePath("/users");
    revalidatePath(`/users/${userId}`);

    return {
      success: true,
      data: updated,
    };
  } catch (error) {
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk mencabut seluruh sesi refresh token aktif milik pengguna (Paksa Logout).
 * Memerlukan permission 'users.revoke-sessions'.
 */
export async function revokeUserSessionsAction(
  userId: string
): Promise<ActionResult<RevokeSessionsResponse>> {
  try {
    await requirePermission("users.revoke-sessions");

    const res = await usersApi.revokeSessions(userId);

    revalidatePath("/users");
    revalidatePath(`/users/${userId}`);

    return {
      success: true,
      data: res,
    };
  } catch (error) {
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk mengambil daftar role yang tersedia di suatu aplikasi
 * beserta role yang saat ini ditugaskan ke pengguna tersebut.
 * Memerlukan permission 'users.read'.
 */
export async function getUserAppAccessDetailAction(
  userId: string,
  appId: string
): Promise<ActionResult<UserAppAccessDetail>> {
  try {
    await requirePermission("users.read");

    const [availableRoles, userAccess] = await Promise.all([
      rolesApi.getAll(appId),
      userAppRolesApi.getRoles(userId, appId),
    ]);

    return {
      success: true,
      data: {
        availableRoles,
        userAccess,
      },
    };
  } catch (error) {
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk menyimpan penugasan role pengguna pada suatu aplikasi klien.
 * Memerlukan permission 'users.write'.
 */
export async function setUserAppRolesAction(
  userId: string,
  appId: string,
  formData: SetUserAppRolesFormData
): Promise<ActionResult<UserAppRolesResponse>> {
  try {
    await requirePermission("users.write");

    const parsed = setUserAppRolesSchema.safeParse(formData);
    if (!parsed.success) {
      return {
        success: false,
        error: "Format daftar ID role tidak valid.",
      };
    }

    const updated = await userAppRolesApi.setRoles(userId, appId, {
      roleIds: parsed.data.roleIds,
    });

    revalidatePath("/users");
    revalidatePath(`/users/${userId}`);
    revalidatePath("/apps");
    revalidatePath(`/apps/${appId}`);

    return {
      success: true,
      data: updated,
    };
  } catch (error) {
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}
