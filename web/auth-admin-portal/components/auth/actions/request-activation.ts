"use server";

import { authApi } from "@/lib/api/auth";
import { requestActivationSchema } from "@/lib/validation/auth.schema";

export interface RequestActivationActionResult {
  success: boolean;
  message?: string;
  error?: string;
}

export async function handleRequestActivationAction(
  formData: FormData
): Promise<RequestActivationActionResult> {
  const emailOrUsername = formData.get("emailOrUsername") as string;
  const parsed = requestActivationSchema.safeParse({ emailOrUsername });

  if (!parsed.success) {
    return {
      success: false,
      error: "Username atau email wajib diisi dengan benar.",
    };
  }

  try {
    await authApi.requestActivation({
      emailOrUsername: parsed.data.emailOrUsername,
    });
  } catch {
    // Sesuai Section 8.4: Selalu kembalikan pesan sukses generik untuk mencegah enumerasi akun
  }

  return {
    success: true,
    message:
      "Jika akun Anda terdaftar dan belum diverifikasi, tautan aktivasi telah dikirimkan ke kontak Anda.",
  };
}

