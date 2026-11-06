import { LifecyclePanel } from "@/components/employees/lifecycle-panel";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { requirePermission } from "@/lib/auth/permissions";
import { UserRoundX } from "lucide-react";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Lifecycle Akun | Auth Admin Portal",
  description: "Nonaktifkan akun pegawai yang resign-nya sudah berlaku",
};

export default async function LifecyclePage() {
  // Halaman operasional: tolak lebih awal bagi yang tidak berhak (backend tetap menegakkan hak akses).
  await requirePermission("employees.lifecycle.write");

  return (
    <div className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink href="/employees">Pegawai</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Lifecycle akun</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex items-center gap-3">
        <div className="w-11 h-11 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
          <UserRoundX className="w-5 h-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Lifecycle Akun Pegawai</h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Nonaktifkan akun pegawai yang resign-nya sudah berlaku dan cabut sesinya. Mulailah dengan pemeriksaan.
          </p>
        </div>
      </div>

      <LifecyclePanel />
    </div>
  );
}
