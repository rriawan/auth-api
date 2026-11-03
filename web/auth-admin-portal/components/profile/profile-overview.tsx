"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { UserProfileSummaryDto } from "@/lib/api/types";
import { isSuperAdmin } from "@/lib/auth/permission-rules";
import {
  AppWindow,
  Check,
  CheckCircle,
  Copy,
  Key,
  Mail,
  Shield,
  ShieldAlert,
  User,
} from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";

interface ProfileOverviewProps {
  user: UserProfileSummaryDto;
}

export default function ProfileOverview({ user }: ProfileOverviewProps) {
  const [copiedId, setCopiedId] = useState(false);
  const superAdmin = isSuperAdmin(user.roles);

  const handleCopyId = () => {
    navigator.clipboard.writeText(user.id);
    setCopiedId(true);
    toast.success("User ID berhasil disalin ke clipboard!");
    setTimeout(() => setCopiedId(false), 2000);
  };

  return (
    <div className="space-y-6">
      {/* Kartu Informasi Akun */}
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        <CardHeader>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 rounded-2xl bg-primary/10 text-primary flex items-center justify-center font-bold text-xl uppercase">
                {user.fullName ? user.fullName[0] : user.username[0]}
              </div>
              <div>
                <CardTitle className="text-xl font-semibold">
                  {user.fullName || user.username}
                </CardTitle>
                <CardDescription className="text-sm flex items-center gap-2 mt-1">
                  <span>@{user.username}</span>
                  <span>•</span>
                  <span>{user.email}</span>
                </CardDescription>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <Badge variant="success" className="gap-1.5 py-1">
                <CheckCircle className="w-3.5 h-3.5" />
                Aktif
              </Badge>
              {user.isAccountVerified && (
                <Badge variant="info" className="gap-1.5 py-1">
                  <ShieldCheckIcon className="w-3.5 h-3.5" />
                  Terverifikasi
                </Badge>
              )}
            </div>
          </div>
        </CardHeader>

        <CardContent className="pt-2">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* User ID */}
            <div className="p-3.5 rounded-xl bg-neutral-50 dark:bg-slate-900/40 border border-neutral-200/80 dark:border-slate-800 flex items-center justify-between">
              <div className="space-y-0.5">
                <span className="text-xs text-neutral-500 font-medium">User ID (Global)</span>
                <p className="text-xs font-mono font-medium text-neutral-900 dark:text-neutral-100">
                  {user.id}
                </p>
              </div>
              <Button
                variant="ghost"
                size="icon"
                onClick={handleCopyId}
                className="h-8 w-8 text-neutral-500 hover:text-neutral-900 dark:hover:text-neutral-100"
              >
                {copiedId ? (
                  <Check className="w-4 h-4 text-emerald-600" />
                ) : (
                  <Copy className="w-4 h-4" />
                )}
              </Button>
            </div>

            {/* Client App */}
            <div className="p-3.5 rounded-xl bg-neutral-50 dark:bg-slate-900/40 border border-neutral-200/80 dark:border-slate-800 flex items-center justify-between">
              <div className="space-y-0.5">
                <span className="text-xs text-neutral-500 font-medium">Konteks Aplikasi Terhubung</span>
                <p className="text-sm font-semibold text-neutral-900 dark:text-neutral-100 flex items-center gap-1.5">
                  <AppWindow className="w-4 h-4 text-primary" />
                  {user.clientId || "auth-admin"}
                </p>
              </div>
              <Badge variant="outline" className="text-xs font-mono">
                BFF Admin
              </Badge>
            </div>

            {/* Username */}
            <div className="p-3.5 rounded-xl bg-neutral-50 dark:bg-slate-900/40 border border-neutral-200/80 dark:border-slate-800 space-y-0.5">
              <span className="text-xs text-neutral-500 font-medium">Username Login</span>
              <p className="text-sm font-medium text-neutral-900 dark:text-neutral-100 flex items-center gap-1.5">
                <User className="w-4 h-4 text-neutral-500" />
                {user.username}
              </p>
            </div>

            {/* Email */}
            <div className="p-3.5 rounded-xl bg-neutral-50 dark:bg-slate-900/40 border border-neutral-200/80 dark:border-slate-800 space-y-0.5">
              <span className="text-xs text-neutral-500 font-medium">Alamat Email</span>
              <p className="text-sm font-medium text-neutral-900 dark:text-neutral-100 flex items-center gap-1.5">
                <Mail className="w-4 h-4 text-neutral-500" />
                {user.email}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Kartu Peran (Roles) & Izin (Permissions) */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Roles Card */}
        <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
          <CardHeader>
            <div className="flex items-center gap-2.5">
              <Shield className="w-5 h-5 text-primary" />
              <CardTitle className="text-base font-semibold">
                Peran yang Diberikan (Roles)
              </CardTitle>
            </div>
            <CardDescription className="text-xs">
              Role yang ditetapkan untuk akun Anda pada aplikasi <code>{user.clientId || "auth-admin"}</code>.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {user.roles && user.roles.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {user.roles.map((role) => (
                  <Badge
                    key={role}
                    variant={role.toLowerCase() === "superadmin" ? "default" : "info"}
                    className="text-xs py-1 px-3"
                  >
                    {role}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="text-xs text-neutral-500 italic">
                Tidak ada peran khusus yang ditetapkan.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Permissions Card */}
        <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <Key className="w-5 h-5 text-primary" />
                <CardTitle className="text-base font-semibold">
                  Izin Efektif (Permissions)
                </CardTitle>
              </div>
              {superAdmin && (
                <Badge variant="warning" className="text-xs">
                  Full SuperAdmin Access
                </Badge>
              )}
            </div>
            <CardDescription className="text-xs">
              Hak akses granular yang dapat dijalankan pada portal ini.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {superAdmin ? (
              <div className="space-y-3">
                <div className="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900 text-xs text-amber-800 dark:text-amber-200 flex items-start gap-2">
                  <ShieldAlert className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>
                    Sebagai <strong>SuperAdmin</strong>, Anda memiliki hak istimewa penuh untuk mengelola seluruh data aplikasi, user, role, permission, dan audit log tanpa batasan izin individual.
                  </span>
                </div>
                {user.permissions && user.permissions.length > 0 && (
                  <div className="flex flex-wrap gap-1.5 pt-1">
                    {user.permissions.map((perm) => (
                      <Badge
                        key={perm}
                        variant="outline"
                        className="text-[11px] font-mono py-0.5 px-2 bg-neutral-50 dark:bg-slate-900"
                      >
                        {perm}
                      </Badge>
                    ))}
                  </div>
                )}
              </div>
            ) : user.permissions && user.permissions.length > 0 ? (
              <div className="flex flex-wrap gap-1.5">
                {user.permissions.map((perm) => (
                  <Badge
                    key={perm}
                    variant="outline"
                    className="text-[11px] font-mono py-0.5 px-2 bg-neutral-50 dark:bg-slate-900"
                  >
                    {perm}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="text-xs text-neutral-500 italic">
                Tidak ada izin granular khusus.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function ShieldCheckIcon(props: React.ComponentProps<"svg">) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  );
}

