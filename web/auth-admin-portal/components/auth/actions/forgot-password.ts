"use server";

import { authApi } from "@/lib/api/auth";
import { forgotPasswordSchema } from "@/lib/validation/auth.schema";

export interface ForgotPasswordActionResult {
  success: boolean;
  message?: string;
  error?: string;
}

export async function handleForgotPasswordAction(
  formData: FormData
): Promise<ForgotPasswordActionResult> {
  const emailOrUsername = formData.get("emailOrUsername") as string;
  const parsed = forgotPasswordSchema.safeParse({ emailOrUsername });

  if (!parsed.success) {
    return {
      success: false,
      error: "Username atau email wajib diisi dengan benar.",
    };
  }

  try {
    await authApi.forgotPassword({
      emailOrUsername: parsed.data.emailOrUsername,
    });
  } catch {
    // Sesuai Section 8.4: Selalu kembalikan respon sukses generik untuk mencegah enumerasi akun
  }

  return {
    success: true,
    message:
      "Jika akun terdaftar di sistem, instruksi reset password telah dikirimkan ke kontak terdaftar Anda.",
  };
}
