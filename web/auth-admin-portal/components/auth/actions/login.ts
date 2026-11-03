"use server";

import { signIn } from "@/auth";
import { loginSchema } from "@/lib/validation/auth.schema";
import { AuthError } from "next-auth";

export interface LoginActionResult {
  success?: boolean;
  error?: string;
  code?: string;
}

export const handleLoginAction = async (
  formData: FormData
): Promise<LoginActionResult | void> => {
  const usernameOrEmail = formData.get("usernameOrEmail") as string;
  const password = formData.get("password") as string;
  const callbackUrl = (formData.get("callbackUrl") as string) || "/";

  const parsed = loginSchema.safeParse({ usernameOrEmail, password });
  if (!parsed.success) {
    return {
      error: "Username/Email dan password harus diisi dengan benar.",
    };
  }

  try {
    await signIn("credentials", {
      usernameOrEmail: parsed.data.usernameOrEmail,
      password: parsed.data.password,
      redirectTo: callbackUrl,
    });
    return { success: true };
  } catch (error) {
    if (error instanceof AuthError) {
      const rawError = error.cause?.err;
      const detailMessage =
        rawError instanceof Error ? rawError.message : error.message;

      let code: string = error.type;
      if (rawError && typeof rawError === "object" && "code" in rawError) {
        code = String((rawError as { code: unknown }).code);
      }

      return {
        error: detailMessage || "Username/email atau password salah.",
        code,
      };
    }
    // Lempar kembali NEXT_REDIRECT agar Next.js dapat mengeksekusi navigasi
    throw error;
  }
};
