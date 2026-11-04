"use server";

import { revalidatePath } from "next/cache";
import { ApiException, getErrorMessage } from "@/lib/api/errors";
import { permissionsApi } from "@/lib/api/permissions";
import { MessageResponse, PermissionDto } from "@/lib/api/types";
import { requirePermission } from "@/lib/auth/permissions";
import {
  CreatePermissionFormData,
  createPermissionSchema,
  UpdatePermissionFormData,
  updatePermissionSchema,
} from "@/lib/validation/permissions.schema";
import { ActionResult } from "./apps";

/**
 * Server Action untuk membuat permission baru pada aplikasi klien.
 * Memerlukan permission 'permissions.write'.
 */
export async function createPermissionAction(
  appId: string,
  formData: CreatePermissionFormData
): Promise<ActionResult<PermissionDto>> {
  try {
    await requirePermission("permissions.write");

    const normalizedData = {
      ...formData,
      code: formData.code.toLowerCase().trim(),
    };

    const parsed = createPermissionSchema.safeParse(normalizedData);
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

    const created = await permissionsApi.create(appId, {
      code: parsed.data.code,
      description: parsed.data.description?.trim() || null,
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
        error: `Kode permission '${formData.code.toLowerCase().trim()}' sudah terdaftar pada aplikasi ini.`,
      };
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk memperbarui permission pada aplikasi klien.
 * Memerlukan permission 'permissions.write'.
 */
export async function updatePermissionAction(
  appId: string,
  permissionId: string,
  formData: UpdatePermissionFormData
): Promise<ActionResult<PermissionDto>> {
  try {
    await requirePermission("permissions.write");

    const normalizedData = {
      ...formData,
      code: formData.code.toLowerCase().trim(),
    };

    const parsed = updatePermissionSchema.safeParse(normalizedData);
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

    const updated = await permissionsApi.update(appId, permissionId, {
      code: parsed.data.code,
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
        error: `Kode permission '${formData.code.toLowerCase().trim()}' sudah digunakan pada aplikasi ini.`,
      };
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk menghapus permission dari aplikasi klien.
 * Memerlukan permission 'permissions.write'.
 */
export async function deletePermissionAction(
  appId: string,
  permissionId: string
): Promise<ActionResult<MessageResponse>> {
  try {
    await requirePermission("permissions.write");

    const res = await permissionsApi.delete(appId, permissionId);

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

