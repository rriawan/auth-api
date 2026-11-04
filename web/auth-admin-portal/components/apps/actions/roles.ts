"use server";

import { revalidatePath } from "next/cache";
import { ApiException, getErrorMessage } from "@/lib/api/errors";
import { rolesApi } from "@/lib/api/roles";
import { MessageResponse, RoleDto } from "@/lib/api/types";
import { requirePermission } from "@/lib/auth/permissions";
import {
  CreateRoleFormData,
  createRoleSchema,
  SetRolePermissionsFormData,
  setRolePermissionsSchema,
  UpdateRoleFormData,
  updateRoleSchema,
} from "@/lib/validation/roles.schema";
import { ActionResult } from "./apps";

/**
 * Server Action untuk membuat role baru pada aplikasi klien.
 * Memerlukan permission 'roles.write'.
 */
export async function createRoleAction(
  appId: string,
  formData: CreateRoleFormData
): Promise<ActionResult<RoleDto>> {
  try {
    await requirePermission("roles.write");

    const parsed = createRoleSchema.safeParse(formData);
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

    const created = await rolesApi.create(appId, {
      name: parsed.data.name.trim(),
      description: parsed.data.description?.trim() || null,
      permissionIds: parsed.data.permissionIds ?? [],
    });

    revalidatePath("/apps");
    revalidatePath(`/apps/${appId}`);

    return {
      success: true,
      data: created,
    };
  } catch (error) {
    if (error instanceof ApiException && error.isConflict) {
      return {
        success: false,
        error: `Role '${formData.name.trim()}' sudah terdaftar pada aplikasi ini.`,
      };
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk memperbarui role pada aplikasi klien.
 * Memerlukan permission 'roles.write'.
 * Nama System Role bawaan tidak boleh diubah.
 */
export async function updateRoleAction(
  appId: string,
  roleId: string,
  isSystemRole: boolean,
  currentRoleName: string,
  formData: UpdateRoleFormData
): Promise<ActionResult<RoleDto>> {
  try {
    await requirePermission("roles.write");

    if (
      isSystemRole &&
      formData.name.trim().toLowerCase() !==
        currentRoleName.trim().toLowerCase()
    ) {
      return {
        success: false,
        error: "Nama System Role bawaan tidak boleh diubah.",
      };
    }

    const parsed = updateRoleSchema.safeParse(formData);
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

    const updated = await rolesApi.update(appId, roleId, {
      name: isSystemRole ? currentRoleName : parsed.data.name.trim(),
      description: parsed.data.description?.trim() || null,
    });

    revalidatePath("/apps");
    revalidatePath(`/apps/${appId}`);

    return {
      success: true,
      data: updated,
    };
  } catch (error) {
    if (error instanceof ApiException && error.isConflict) {
      return {
        success: false,
        error: `Role '${formData.name.trim()}' sudah digunakan pada aplikasi ini.`,
      };
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk menghapus role dari aplikasi klien.
 * Memerlukan permission 'roles.write'.
 * System Role bawaan tidak dapat dihapus.
 */
export async function deleteRoleAction(
  appId: string,
  roleId: string,
  isSystemRole: boolean
): Promise<ActionResult<MessageResponse>> {
  try {
    await requirePermission("roles.write");

    if (isSystemRole) {
      return {
        success: false,
        error: "System Role bawaan tidak dapat dihapus.",
      };
    }

    const res = await rolesApi.delete(appId, roleId);

    revalidatePath("/apps");
    revalidatePath(`/apps/${appId}`);

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
 * Server Action untuk memetakan daftar permission ke suatu role pada aplikasi klien.
 * Memerlukan permission 'roles.write'.
 */
export async function setRolePermissionsAction(
  appId: string,
  roleId: string,
  formData: SetRolePermissionsFormData
): Promise<ActionResult<RoleDto>> {
  try {
    await requirePermission("roles.write");

    const parsed = setRolePermissionsSchema.safeParse(formData);
    if (!parsed.success) {
      return {
        success: false,
        error: "Format daftar ID permission tidak valid.",
      };
    }

    const updated = await rolesApi.setPermissions(appId, roleId, {
      permissionIds: parsed.data.permissionIds,
    });

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

