import { AppApiKeyTab } from "@/components/apps/app-api-key-tab";
import { AppInfoTab } from "@/components/apps/app-info-tab";
import { AppPermissionsTab } from "@/components/apps/app-permissions-tab";
import { AppRolesTab } from "@/components/apps/app-roles-tab";
import { Badge } from "@/components/ui/badge";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { appsApi } from "@/lib/api/apps";
import { ClientAppDto } from "@/lib/api/types";
import {
  AppWindow,
  ArrowLeft,
  CheckCircle2,
  Info,
  KeyRound,
  Lock,
  Shield,
  ShieldAlert,
  Users,
  XCircle,
} from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

interface AppDetailPageProps {
  params: Promise<{ appId: string }>;
}

export async function generateMetadata({
  params,
}: AppDetailPageProps): Promise<Metadata> {
  const { appId } = await params;
  try {
    const app = await appsApi.getById(appId);
    return {
      title: `${app.name} (${app.clientId}) | Auth Admin Portal`,
      description: `Kelola konfigurasi, API key, dan hak akses aplikasi ${app.name}`,
    };
  } catch {
    return {
      title: "Detail Aplikasi | Auth Admin Portal",
    };
  }
}

export default async function AppDetailPage({ params }: AppDetailPageProps) {
  const { appId } = await params;

  let app: ClientAppDto | null = null;
  let fetchError: string | null = null;

  try {
    app = await appsApi.getById(appId);
  } catch (err) {
    fetchError =
      err instanceof Error
        ? err.message
        : "Aplikasi klien tidak ditemukan atau gagal dimuat.";
  }

  if (fetchError || !app) {
    return (
      <div className="space-y-6">
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbLink href="/apps">Aplikasi</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>Detail</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>

        <Card className="border-red-200 dark:border-red-900/50 bg-red-50/20 dark:bg-red-950/20">
          <CardContent className="p-8 text-center space-y-4">
            <ShieldAlert className="w-12 h-12 text-red-500 mx-auto" />
            <h2 className="text-lg font-semibold text-red-800 dark:text-red-300">
              Aplikasi Klien Tidak Ditemukan
            </h2>
            <p className="text-xs text-muted-foreground max-w-md mx-auto">
              {fetchError ||
                "Aplikasi yang Anda cari mungkin telah dihapus atau ID tidak valid."}
            </p>
            <Link href="/apps">
              <Button variant="outline" className="gap-2 text-xs">
                <ArrowLeft className="w-4 h-4" />
                Kembali ke Daftar Aplikasi
              </Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  const isSystemApp = app.clientId.toLowerCase() === "auth-admin";

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="space-y-6">
      {/* Breadcrumb Navigasi */}
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink href="/apps">Aplikasi</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{app.name}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header Detail Aplikasi */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link href="/apps">
            <Button
              variant="outline"
              size="icon"
              className="h-9 w-9 shrink-0 text-muted-foreground hover:text-foreground"
              title="Kembali ke Daftar Aplikasi"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </Link>
          <div className="w-12 h-12 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
            <AppWindow className="w-6 h-6" />
          </div>
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                {app.name}
              </h1>
              <code className="text-xs font-mono font-medium bg-neutral-100 dark:bg-slate-800 text-neutral-800 dark:text-neutral-200 px-2 py-0.5 rounded">
                {app.clientId}
              </code>
              {isSystemApp && (
                <Badge
                  variant="outline"
                  className="text-xs border-blue-300 dark:border-blue-700 bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 gap-1"
                >
                  <Shield className="w-3 h-3" />
                  Aplikasi Sistem
                </Badge>
              )}
              {app.isActive ? (
                <Badge variant="success" className="gap-1 text-xs">
                  <CheckCircle2 className="w-3 h-3" />
                  Aktif
                </Badge>
              ) : (
                <Badge variant="secondary" className="gap-1 text-xs text-muted-foreground">
                  <XCircle className="w-3 h-3" />
                  Nonaktif
                </Badge>
              )}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Didaftarkan pada {formatDate(app.createdAtUtc)}
              {app.updatedAtUtc && ` • Diperbarui pada ${formatDate(app.updatedAtUtc)}`}
            </p>
          </div>
        </div>
      </div>

      {/* Tab Navigasi Detail Aplikasi */}
      <Tabs defaultValue="info" className="space-y-4">
        <TabsList className="bg-neutral-100 dark:bg-slate-800/80 p-1">
          <TabsTrigger value="info" className="gap-2 text-xs">
            <Info className="w-3.5 h-3.5" />
            Informasi
          </TabsTrigger>
          <TabsTrigger value="api-key" className="gap-2 text-xs">
            <KeyRound className="w-3.5 h-3.5" />
            API Key &amp; Integrasi
          </TabsTrigger>
          <TabsTrigger value="permissions" className="gap-2 text-xs">
            <Lock className="w-3.5 h-3.5" />
            Permissions ({app.permissionsCount})
          </TabsTrigger>
          <TabsTrigger value="roles" className="gap-2 text-xs">
            <Users className="w-3.5 h-3.5" />
            Roles ({app.rolesCount})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="info">
          <AppInfoTab app={app} />
        </TabsContent>

        <TabsContent value="api-key">
          <AppApiKeyTab app={app} />
        </TabsContent>

        <TabsContent value="permissions">
          <AppPermissionsTab app={app} />
        </TabsContent>

        <TabsContent value="roles">
          <AppRolesTab app={app} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

