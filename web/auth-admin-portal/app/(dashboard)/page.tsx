import { auth } from "@/auth";
import {
  DailyLoginPoint,
  LoginActivityChart,
} from "@/components/dashboard/login-activity-chart";
import { RecentAuditLogsCard } from "@/components/dashboard/recent-audit-logs-card";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { appsApi } from "@/lib/api/apps";
import { auditLogsApi } from "@/lib/api/audit-logs";
import { AuditLogDto, ClientAppDto } from "@/lib/api/types";
import { usersApi } from "@/lib/api/users";
import { hasPermission } from "@/lib/auth/permission-rules";
import {
  AlertTriangle,
  CheckCircle2,
  Layers,
  ShieldCheck,
  UserCheck,
  Users,
} from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Dashboard | Auth Admin Portal",
  description:
    "Ringkasan operasional layanan terpusat autentikasi, aplikasi klien, pengguna SSO, dan audit keamanan",
};

interface ComputedAuditStats {
  sevenDaysAgoIso: string;
  loginSuccess24h: number;
  loginFailed24h: number;
  dailyChartData: DailyLoginPoint[];
}

function getSevenDaysAgoIso(): string {
  const now = new Date();
  const sevenDaysAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
  return sevenDaysAgo.toISOString();
}

function computeAuditStats(logs: AuditLogDto[]): ComputedAuditStats {
  const now = new Date();
  const twentyFourHoursAgoMs = now.getTime() - 24 * 60 * 60 * 1000;

  let loginSuccess24h = 0;
  let loginFailed24h = 0;

  // Siapkan bucket 7 hari terakhir (dari H-6 sampai hari ini)
  const dayBuckets: {
    key: string;
    dateLabel: string;
    successCount: number;
    failedCount: number;
  }[] = [];

  for (let i = 6; i >= 0; i--) {
    const d = new Date(now.getTime() - i * 24 * 60 * 60 * 1000);
    const key = d.toISOString().slice(0, 10);
    const dateLabel = new Intl.DateTimeFormat("id-ID", {
      day: "2-digit",
      month: "short",
    }).format(d);

    dayBuckets.push({
      key,
      dateLabel,
      successCount: 0,
      failedCount: 0,
    });
  }

  for (const log of logs) {
    const ts = new Date(log.timestampUtc);
    const tsMs = ts.getTime();
    if (Number.isNaN(tsMs)) continue;

    const ev = log.eventType;
    const isLoginSuccess = ev === "LoginSuccess";
    const isLoginFailureOrLockout =
      ev === "LoginFailed" || ev === "AccountLockedOut";

    if (tsMs >= twentyFourHoursAgoMs) {
      if (isLoginSuccess) loginSuccess24h++;
      if (isLoginFailureOrLockout) loginFailed24h++;
    }

    const dayKey = ts.toISOString().slice(0, 10);
    const bucket = dayBuckets.find((b) => b.key === dayKey);
    if (bucket) {
      if (isLoginSuccess) bucket.successCount++;
      if (isLoginFailureOrLockout) bucket.failedCount++;
    }
  }

  return {
    sevenDaysAgoIso: getSevenDaysAgoIso(),
    loginSuccess24h,
    loginFailed24h,
    dailyChartData: dayBuckets.map((b) => ({
      dateLabel: b.dateLabel,
      successCount: b.successCount,
      failedCount: b.failedCount,
    })),
  };
}

export default async function DashboardPage() {
  const session = await auth();
  const userPermissions = session?.user?.permissions;
  const userRoles = session?.user?.roles;

  const canReadApps = hasPermission(userPermissions, userRoles, "apps.read");
  const canReadUsers = hasPermission(userPermissions, userRoles, "users.read");
  const canReadAuditLogs = hasPermission(
    userPermissions,
    userRoles,
    "audit-logs.read"
  );

  let apps: ClientAppDto[] = [];
  let totalUsers = 0;
  let auditLogs: AuditLogDto[] = [];

  const tasks: Promise<void>[] = [];

  if (canReadApps) {
    tasks.push(
      appsApi
        .getAll()
        .then((res) => {
          apps = res;
        })
        .catch(() => {
          apps = [];
        })
    );
  }

  if (canReadUsers) {
    tasks.push(
      usersApi
        .getAll({ page: 1, pageSize: 1 })
        .then((res) => {
          totalUsers = res.totalCount;
        })
        .catch(() => {
          totalUsers = 0;
        })
    );
  }

  if (canReadAuditLogs) {
    tasks.push(
      auditLogsApi
        .getAll({
          page: 1,
          pageSize: 100,
          dateFromUtc: getSevenDaysAgoIso(),
        })
        .then((res) => {
          auditLogs = res.items;
        })
        .catch(() => {
          auditLogs = [];
        })
    );
  }

  await Promise.all(tasks);

  const activeAppsCount = apps.filter((a) => a.isActive).length;
  const auditStats = computeAuditStats(auditLogs);
  const recentLogs = auditLogs.slice(0, 10);

  return (
    <div className="space-y-6">
      {/* Header Selamat Datang */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">
            Dashboard Administrasi
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Selamat datang kembali,{" "}
            <strong className="text-foreground">
              {session?.user?.fullName || session?.user?.username || "Admin"}
            </strong>{" "}
            — Pantau kesehatan layanan autentikasi dan hak akses secara
            terpusat.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          {userRoles?.map((role) => (
            <Badge
              key={role}
              variant="outline"
              className="text-xs border-blue-300 dark:border-blue-700 bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 gap-1"
            >
              <ShieldCheck className="w-3.5 h-3.5" />
              {role}
            </Badge>
          ))}
        </div>
      </div>

      {/* Grid Kartu Statistik (Hanya tampil jika admin punya permission terkait) */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {canReadApps && (
          <Link href="/apps">
            <Card className="border border-neutral-200 dark:border-slate-800 hover:border-primary/60 transition-colors cursor-pointer shadow-sm h-full">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                  Aplikasi Terdaftar
                </CardTitle>
                <div className="w-8 h-8 rounded-lg bg-blue-500/10 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <Layers className="w-4 h-4" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-foreground">
                  {apps.length}
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  <strong>{activeAppsCount}</strong> aktif •{" "}
                  <strong>{apps.length - activeAppsCount}</strong> nonaktif
                </p>
              </CardContent>
            </Card>
          </Link>
        )}

        {canReadUsers && (
          <Link href="/users">
            <Card className="border border-neutral-200 dark:border-slate-800 hover:border-primary/60 transition-colors cursor-pointer shadow-sm h-full">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                  Total Pengguna SSO
                </CardTitle>
                <div className="w-8 h-8 rounded-lg bg-purple-500/10 text-purple-600 dark:text-purple-400 flex items-center justify-center">
                  <Users className="w-4 h-4" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-foreground">
                  {totalUsers}
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  Identitas terdaftar di Single User Pool
                </p>
              </CardContent>
            </Card>
          </Link>
        )}

        {canReadAuditLogs && (
          <>
            <Link href="/audit-logs?eventType=LoginSuccess">
              <Card className="border border-neutral-200 dark:border-slate-800 hover:border-emerald-500/60 transition-colors cursor-pointer shadow-sm h-full">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    Login Berhasil (24 Jam)
                  </CardTitle>
                  <div className="w-8 h-8 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
                    <CheckCircle2 className="w-4 h-4" />
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold text-emerald-600 dark:text-emerald-400">
                    {auditStats.loginSuccess24h}
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">
                    Autentikasi sukses 24 jam terakhir
                  </p>
                </CardContent>
              </Card>
            </Link>

            <Link href="/audit-logs?isSuccess=false">
              <Card className="border border-neutral-200 dark:border-slate-800 hover:border-red-500/60 transition-colors cursor-pointer shadow-sm h-full">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    Login Gagal (24 Jam)
                  </CardTitle>
                  <div className="w-8 h-8 rounded-lg bg-red-500/10 text-red-600 dark:text-red-400 flex items-center justify-center">
                    <AlertTriangle className="w-4 h-4" />
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold text-red-600 dark:text-red-400">
                    {auditStats.loginFailed24h}
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">
                    Percobaan gagal / akun terkunci 24 jam
                  </p>
                </CardContent>
              </Card>
            </Link>
          </>
        )}

        {/* Kartu Profil selalu tampil apabila admin memiliki akses terbatas */}
        {(!canReadApps || !canReadUsers || !canReadAuditLogs) && (
          <Link href="/profile">
            <Card className="border border-neutral-200 dark:border-slate-800 hover:border-primary/60 transition-colors cursor-pointer shadow-sm h-full">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                  Profil &amp; Keamanan Saya
                </CardTitle>
                <div className="w-8 h-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
                  <UserCheck className="w-4 h-4" />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-lg font-bold text-foreground truncate">
                  @{session?.user?.username}
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  Lihat hak akses &amp; ganti password
                </p>
              </CardContent>
            </Card>
          </Link>
        )}
      </div>

      {/* Grafik Login 7 Hari Terakhir & Tabel Aktivitas Terbaru (Memerlukan audit-logs.read) */}
      {canReadAuditLogs && (
        <div className="space-y-6">
          <LoginActivityChart data={auditStats.dailyChartData} />
          <RecentAuditLogsCard logs={recentLogs} />
        </div>
      )}
    </div>
  );
}
