"use client";

import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { ClientAppDto } from "@/lib/api/types";
import { Info, Lock, ShieldCheck } from "lucide-react";

interface AppPermissionsTabProps {
  app: ClientAppDto;
}

export function AppPermissionsTab({ app }: AppPermissionsTabProps) {
  return (
    <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
      <CardHeader>
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
              <Lock className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-semibold">
                Daftar Izin Aplikasi (Permissions)
              </CardTitle>
              <CardDescription className="text-xs">
                Katalog granular hak akses spesifik untuk modul aplikasi {app.name}.
              </CardDescription>
            </div>
          </div>
          <Badge variant="outline" className="text-xs">
            {app.permissionsCount} Izin Terdaftar
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="p-6 rounded-lg border border-dashed border-neutral-300 dark:border-slate-700 bg-neutral-50/50 dark:bg-slate-900/30 text-center space-y-3">
          <div className="w-12 h-12 rounded-full bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400 flex items-center justify-center mx-auto">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <h4 className="font-semibold text-sm text-foreground">
            Katalog Hak Akses Granular (Fase F7)
          </h4>
          <p className="text-xs text-muted-foreground max-w-lg mx-auto leading-relaxed">
            Aplikasi ini saat ini memiliki <strong>{app.permissionsCount} izin</strong> terdaftar di database.
            Fitur CRUD lengkap (tambah, ubah, dan hapus izin berpola <code>modul.aksi</code>) disediakan secara komprehensif pada <strong>Fase F7: Permissions &amp; Roles per Aplikasi</strong>.
          </p>
          <div className="inline-flex items-center gap-1.5 text-xs text-blue-600 dark:text-blue-400 font-medium bg-blue-50 dark:bg-blue-950/60 px-3 py-1 rounded-full border border-blue-200 dark:border-blue-900/60">
            <Info className="w-3.5 h-3.5" />
            <span>Fase F7 akan mengaktifkan manajemen izin interaktif.</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

