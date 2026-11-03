"use client";

import { hasPermission } from "@/lib/auth/permission-rules";
import { useSession } from "next-auth/react";
import { ReactNode } from "react";

interface CanProps {
  permission: string;
  fallback?: ReactNode;
  children: ReactNode;
}

/**
 * Komponen Client untuk menyembunyikan atau menampilkan elemen UI berdasarkan izin (permission).
 * SuperAdmin memiliki izin akses ke seluruh komponen tanpa batasan.
 */
export function Can({ permission, fallback = null, children }: CanProps) {
  const { data: session } = useSession();

  const userRoles = session?.user?.roles;
  const userPermissions = session?.user?.permissions;

  const allowed = hasPermission(userPermissions, userRoles, permission);

  if (!allowed) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

