"use server";

import { authApi } from "@/lib/api/auth";
import { ApiException } from "@/lib/api/errors";

export interface ConfirmActivationActionResult {
  success: boolean;
  message?: string;
  error?: string;
}

export async function handleConfirmActivationAction(
  token: string
): Promise<ConfirmActivationActionResult> {
  if (!token || typeof token !== "string" || !token.trim()) {
    return {
      success: false,
      error: "Token aktivasi akun tidak ditemukan pada tautan ini.",
    };
  }

  try {
    const res = await authApi.confirmActivation({ token: token.trim() });
    return {
      success: true,
      message:
        res.message ||
        "Akun Anda berhasil diaktifkan. Anda sekarang dapat masuk menggunakan akun Anda.",
    };
  } catch (error) {
    if (error instanceof ApiException) {
      return {
        success: false,
        error:
          error.problem?.detail ||
          "Token aktivasi tidak valid, telah kedaluwarsa, atau sudah pernah digunakan.",
      };
    }

    return {
      success: false,
      error:
        "Terjadi kesalahan saat memproses aktivasi akun. Silakan coba kembali.",
    };
  }
}

