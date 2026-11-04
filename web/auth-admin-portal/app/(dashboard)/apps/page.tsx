import { Can } from "@/components/auth/can";
import { CreateAppDialog } from "@/components/apps/create-app-dialog";
import { AppsTable } from "@/components/apps/apps-table";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Card, CardContent } from "@/components/ui/card";
import { appsApi } from "@/lib/api/apps";
import { ClientAppDto } from "@/lib/api/types";
import { AppWindow, ShieldAlert } from "lucide-react";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Manajemen Aplikasi (ClientApps) | Auth Admin Portal",
  description: "Daftar dan kelola aplikasi klien yang terintegrasi dengan auth-api",
};

export default async function AppsPage() {
  let apps: ClientAppDto[] = [];
  let fetchError: string | null = null;

  try {
    apps = await appsApi.getAll();
  } catch (err) {
    fetchError =
      err instanceof Error
        ? err.message
        : "Gagal memuat daftar aplikasi dari server.";
  }

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
            <BreadcrumbPage>Manajemen Akses</BreadcrumbPage>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Aplikasi</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header Halaman */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-12 h-12 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
            <AppWindow className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Manajemen Aplikasi (ClientApps)
            </h1>
            <p className="text-sm text-muted-foreground mt-0.5">
              Kelola sistem dan aplikasi konsumen yang terintegrasi dengan layanan autentikasi terpusat.
            </p>
          </div>
        </div>

        <div>
          <Can permission="apps.write">
            <CreateAppDialog />
          </Can>
        </div>
      </div>

      {/* Konten Utama / Penanganan Error */}
      {fetchError ? (
        <Card className="border-red-200 dark:border-red-900/50 bg-red-50/30 dark:bg-red-950/20">
          <CardContent className="p-6 text-center space-y-3">
            <ShieldAlert className="w-10 h-10 text-red-500 mx-auto" />
            <h3 className="font-semibold text-red-700 dark:text-red-400">
              Gagal Memuat Data Aplikasi
            </h3>
            <p className="text-xs text-muted-foreground max-w-md mx-auto">
              {fetchError}
            </p>
          </CardContent>
        </Card>
      ) : (
        <AppsTable initialApps={apps} />
      )}
    </div>
  );
}

