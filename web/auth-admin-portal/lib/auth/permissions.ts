import "server-only";

import { auth } from "@/auth";
import { redirect } from "next/navigation";
import { hasPermission, isSuperAdmin } from "./permission-rules";

export { hasPermission, isSuperAdmin };

/**
 * Helper untuk Server Components dan Server Actions guna memverifikasi permission.
 * Melempar error atau mengarahkan ke halaman login jika akses tidak sah.
 */
export async function requirePermission(permission: string) {
  const session = await auth();
  if (!session?.user) {
    redirect("/auth/login");
  }

  const allowed = hasPermission(
    session.user.permissions,
    session.user.roles,
    permission
  );

  if (!allowed) {
    throw new Error(
      `Akses ditolak. Dibutuhkan izin '${permission}' untuk melakukan tindakan ini.`
    );
  }

  return session;
}
