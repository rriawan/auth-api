"use server";

import { signOut } from "@/auth";
import { authApi } from "@/lib/api/auth";
import { decode } from "next-auth/jwt";
import { cookies } from "next/headers";

export type LogoutResponse = { success: true } | { error: string };

export async function doLogout(): Promise<LogoutResponse> {
  try {
    const cookieStore = await cookies();
    const sessionCookie =
      cookieStore.get("__Secure-authjs.session-token") ||
      cookieStore.get("authjs.session-token");

    if (sessionCookie?.value) {
      try {
        const token = await decode({
          token: sessionCookie.value,
          secret: process.env.AUTH_SECRET!,
          salt: sessionCookie.name,
        });

        if (token?.refreshToken) {
          // Cabut refresh token di backend agar tercatat di audit log
          await authApi.logout(
            { refreshToken: token.refreshToken as string },
            (token.accessToken as string) || undefined
          );
        }
      } catch {
        // Abaikan jika token gagal didecode
      }
    }
  } catch {
    // Abaikan error jaringan saat logout
  }

  await signOut({ redirectTo: "/auth/login" });
  return { success: true };
}
