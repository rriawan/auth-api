import "server-only";

import { decode } from "next-auth/jwt";
import { cookies } from "next/headers";

/**
 * Membaca accessToken dari cookie sesi terenkripsi NextAuth secara server-only.
 * Token ini tidak pernah diekspos ke objek session di client component.
 */
export async function getServerAccessToken(): Promise<string | null> {
  try {
    const cookieStore = await cookies();
    const sessionCookie =
      cookieStore.get("__Secure-authjs.session-token") ||
      cookieStore.get("authjs.session-token");

    if (!sessionCookie?.value) {
      return null;
    }

    const token = await decode({
      token: sessionCookie.value,
      secret: process.env.AUTH_SECRET!,
      salt: sessionCookie.name,
    });

    return (token?.accessToken as string) || null;
  } catch {
    return null;
  }
}

