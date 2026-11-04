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
import { Info, Shield, Users } from "lucide-react";

interface AppRolesTabProps {
  app: ClientAppDto;
}

export function AppRolesTab({ app }: AppRolesTabProps) {
  return (
    <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
      <CardHeader>
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
              <Users className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-semibold">
                Daftar Peran Aplikasi (Roles)
              </CardTitle>
              <CardDescription className="text-xs">
                Grup hak akses dan pemetaan izin pengguna untuk aplikasi {app.name}.
              </CardDescription>
            </div>
          </div>
          <Badge variant="outline" className="text-xs">
            {app.rolesCount} Peran Terdaftar
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="p-6 rounded-lg border border-dashed border-neutral-300 dark:border-slate-700 bg-neutral-50/50 dark:bg-slate-900/30 text-center space-y-3">
          <div className="w-12 h-12 rounded-full bg-purple-50 dark:bg-purple-950/40 text-purple-600 dark:text-purple-400 flex items-center justify-center mx-auto">
            <Shield className="w-6 h-6" />
          </div>
          <h4 className="font-semibold text-sm text-foreground">
            Manajemen Peran &amp; Pemetaan Izin (Fase F7)
          </h4>
          <p className="text-xs text-muted-foreground max-w-lg mx-auto leading-relaxed">
            Aplikasi ini saat ini memiliki <strong>{app.rolesCount} peran</strong> terdaftar dan ditugaskan ke{" "}
            <strong>{app.assignedUsersCount} pengguna</strong>.
            Fitur CRUD pembuatan peran baru, proteksi System Roles, serta dialog pemetaan izin berbentuk checklist akan diimplementasikan secara komprehensif pada <strong>Fase F7: Permissions &amp; Roles per Aplikasi</strong>.
          </p>
          <div className="inline-flex items-center gap-1.5 text-xs text-purple-600 dark:text-purple-400 font-medium bg-purple-50 dark:bg-purple-950/60 px-3 py-1 rounded-full border border-purple-200 dark:border-purple-900/60">
            <Info className="w-3.5 h-3.5" />
            <span>Fase F7 akan mengaktifkan manajemen peran interaktif.</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

