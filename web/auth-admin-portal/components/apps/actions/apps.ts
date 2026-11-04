"use server";

import { revalidatePath } from "next/cache";
import { appsApi } from "@/lib/api/apps";
import { ApiException, getErrorMessage } from "@/lib/api/errors";
import {
  ClientAppDto,
  ClientAppWithRawKeyResponse,
} from "@/lib/api/types";
import { requirePermission } from "@/lib/auth/permissions";
import {
  createClientAppSchema,
  CreateClientAppFormData,
  updateClientAppSchema,
  UpdateClientAppFormData,
} from "@/lib/validation/apps.schema";

export interface ActionResult<T> {
  success: boolean;
  data?: T;
  error?: string;
  fieldErrors?: Record<string, string>;
}

/**
 * Server Action untuk mendaftarkan aplikasi klien baru ke auth-api.
 * Memerlukan permission 'apps.write'.
 */
export async function createAppAction(
  formData: CreateClientAppFormData
): Promise<ActionResult<ClientAppWithRawKeyResponse>> {
  try {
    await requirePermission("apps.write");

    const parsed = createClientAppSchema.safeParse(formData);
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

    const response = await appsApi.create({
      clientId: parsed.data.clientId.toLowerCase().trim(),
      name: parsed.data.name.trim(),
      description: parsed.data.description?.trim() || null,
      allowedOrigins: parsed.data.allowedOrigins?.trim() || null,
      isActive: parsed.data.isActive ?? true,
    });

    revalidatePath("/apps");
    return {
      success: true,
      data: response,
    };
  } catch (error) {
    if (error instanceof ApiException) {
      if (error.isConflict) {
        return {
          success: false,
          error: `Client ID '${formData.clientId}' sudah digunakan oleh aplikasi lain.`,
        };
      }
    }
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

/**
 * Server Action untuk memperbarui informasi aplikasi klien.
 * Memerlukan permission 'apps.write'.
 * Aplikasi sistem 'auth-admin' tidak boleh dinonaktifkan.
 */
export async function updateAppAction(
  id: string,
  clientId: string,
  formData: UpdateClientAppFormData
): Promise<ActionResult<ClientAppDto>> {
  try {
    await requirePermission("apps.write");

    if (clientId.toLowerCase() === "auth-admin" && !formData.isActive) {
      return {
        success: false,
        error: "Aplikasi sistem 'auth-admin' tidak boleh dinonaktifkan.",
      };
    }

    const parsed = updateClientAppSchema.safeParse(formData);
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

    const updated = await appsApi.update(id, {
      name: parsed.data.name.trim(),
      description: parsed.data.description?.trim() || null,
      allowedOrigins: parsed.data.allowedOrigins?.trim() || null,
      isActive: parsed.data.isActive,
    });

    revalidatePath("/apps");
    revalidatePath(`/apps/${id}`);

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
 * Server Action untuk merotasi API Key aplikasi klien.
 * Memerlukan permission 'apps.rotate-key'.
 * Mengembalikan rawApiKey baru yang wajib segera disalin oleh administrator.
 */
export async function rotateApiKeyAction(
  id: string
): Promise<ActionResult<ClientAppWithRawKeyResponse>> {
  try {
    await requirePermission("apps.rotate-key");

    const response = await appsApi.rotateKey(id);

    revalidatePath("/apps");
    revalidatePath(`/apps/${id}`);

    return {
      success: true,
      data: response,
    };
  } catch (error) {
    return {
      success: false,
      error: getErrorMessage(error),
    };
  }
}

