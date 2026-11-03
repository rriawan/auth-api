"use server";

import { authApi } from "@/lib/api/auth";
import { ApiException } from "@/lib/api/errors";
import { resetPasswordSchema } from "@/lib/validation/auth.schema";

export interface ResetPasswordActionResult {
  success: boolean;
  message?: string;
  error?: string;
}

export async function handleResetPassword(
  formData: FormData
): Promise<ResetPasswordActionResult> {
  const token = (formData.get("token") as string) || "";
  const newPassword = (formData.get("newPassword") as string) || "";
  const confirmPassword = (formData.get("confirmPassword") as string) || "";

  const parsed = resetPasswordSchema.safeParse({
    token,
    newPassword,
    confirmPassword,
  });

  if (!parsed.success) {
    const firstError = Object.values(
      parsed.error.flatten().fieldErrors
    )[0]?.[0];
    return {
      success: false,
      error:
        firstError ||
        "Data reset password tidak valid. Pastikan password minimal 8 karakter dan konfirmasi cocok.",
    };
  }

  try {
    const res = await authApi.resetPassword({
      token: parsed.data.token,
      newPassword: parsed.data.newPassword,
    });

    return {
      success: true,
      message:
        res.message ||
        "Password Anda berhasil diperbarui. Silakan login menggunakan password baru Anda.",
    };
  } catch (error) {
    if (error instanceof ApiException) {
      const detail =
        error.problem?.detail ||
        "Tautan reset password tidak valid atau sudah kedaluwarsa. Silakan ajukan permohonan baru.";
      return {
        success: false,
        error: detail,
      };
    }

    return {
      success: false,
      error:
        "Terjadi kesalahan saat memproses reset password. Silakan coba kembali.",
    };
  }
}
