"use server";

import { signOut } from "@/auth";
import { authApi } from "@/lib/api/auth";
import { ApiException } from "@/lib/api/errors";
import { changePasswordSchema } from "@/lib/validation/auth.schema";

export interface ChangePasswordActionResult {
  success: boolean;
  message?: string;
  error?: string;
  shouldRelogin?: boolean;
}

export async function handleChangePasswordAction(
  formData: FormData
): Promise<ChangePasswordActionResult> {
  const currentPassword = (formData.get("currentPassword") as string) || "";
  const newPassword = (formData.get("newPassword") as string) || "";
  const confirmNewPassword =
    (formData.get("confirmNewPassword") as string) || "";

  const parsed = changePasswordSchema.safeParse({
    currentPassword,
    newPassword,
    confirmNewPassword,
  });

  if (!parsed.success) {
    const firstError = Object.values(
      parsed.error.flatten().fieldErrors
    )[0]?.[0];
    return {
      success: false,
      error:
        firstError ||
        "Data formulir tidak valid. Pastikan password baru minimal 8 karakter dan konfirmasi cocok.",
    };
  }

  try {
    const res = await authApi.changePassword({
      currentPassword: parsed.data.currentPassword,
      newPassword: parsed.data.newPassword,
    });

    // Backend secara otomatis mencabut seluruh sesi user setelah ganti password.
    // Lakukan signOut agar cookie sesi NextAuth lokal juga dibersihkan.
    try {
      await signOut({ redirect: false });
    } catch {
      // Abaikan internal redirect NextAuth di Server Action
    }

    return {
      success: true,
      message:
        res.message ||
        "Password berhasil diubah. Seluruh sesi Anda telah dicabut demi keamanan. Silakan masuk kembali dengan password baru.",
      shouldRelogin: true,
    };
  } catch (error) {
    if (error instanceof ApiException) {
      return {
        success: false,
        error:
          error.problem?.detail ||
          "Gagal mengubah password. Pastikan password saat ini sudah benar.",
      };
    }

    return {
      success: false,
      error:
        "Terjadi kesalahan saat memproses penggantian password. Silakan coba kembali.",
    };
  }
}

