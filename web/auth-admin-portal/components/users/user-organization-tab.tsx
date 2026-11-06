"use client";

import { Can } from "@/components/auth/can";
import {
  linkEmployeeAction,
  unlinkEmployeeAction,
} from "@/components/employees/actions/employees";
import { CommandDialog } from "@/components/employees/command-dialog";
import {
  EmployeeStatusBadge,
  isInForce,
  periodLabel,
} from "@/components/employees/format";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import type { UserOrganizationDto } from "@/lib/api/employee-types";
import { AlertCircle, Info, Link2, Link2Off } from "lucide-react";
import Link from "next/link";

interface UserOrganizationTabProps {
  userId: string;
  organization: UserOrganizationDto | null;
  fetchError: string | null;
  /** Tanggal bisnis hari ini (Asia/Jakarta, YYYY-MM-DD) untuk menentukan penempatan yang sedang berlaku. */
  today: string;
}

const NO_ROLE_CHANGE =
  "Menautkan atau melepas pegawai tidak menambah, mengubah, atau mencabut role dan akses aplikasi akun. Akses diatur terpisah pada tab Akses Aplikasi.";

/** Tab Organisasi pada detail pengguna: tautan akun–pegawai (opsional, 1:1) dan ringkasan penempatan. */
export function UserOrganizationTab({ userId, organization, fetchError, today }: UserOrganizationTabProps) {
  if (fetchError) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertDescription className="text-xs">{fetchError}</AlertDescription>
      </Alert>
    );
  }

  const detail = organization?.employee ?? null;

  if (!detail) {
    return (
      <Card>
        <CardContent className="p-6 space-y-4">
          <div className="space-y-1">
            <h3 className="text-sm font-semibold">Akun ini belum tertaut ke pegawai</h3>
            <p className="text-xs text-muted-foreground">
              Akun non-pegawai (misalnya akun layanan) tetap dapat login selama memenuhi aturan akun dan role aplikasi.
            </p>
          </div>
          <Alert>
            <Info className="h-4 w-4" />
            <AlertDescription className="text-xs">{NO_ROLE_CHANGE}</AlertDescription>
          </Alert>
          <Can permission="employees.link.write">
            <CommandDialog
              triggerLabel="Tautkan ke pegawai"
              triggerIcon={<Link2 className="w-4 h-4" />}
              triggerVariant="default"
              title="Tautkan akun ke pegawai"
              description="Satu akun hanya dapat tertaut ke satu pegawai dan sebaliknya. Pegawai yang resign atau terjadwal resign tidak dapat ditautkan."
              notice={NO_ROLE_CHANGE}
              fields={[
                { kind: "employee", name: "employeeId", label: "Pegawai" },
                { kind: "textarea", name: "reason", label: "Alasan", required: true },
              ]}
              initial={{ employeeId: "", reason: "" }}
              successMessage="Akun berhasil ditautkan ke pegawai."
              onSubmit={(v) =>
                linkEmployeeAction(userId, String(v.employeeIdVersion ?? ""), {
                  employeeId: v.employeeId,
                  reason: v.reason,
                })
              }
            />
          </Can>
        </CardContent>
      </Card>
    );
  }

  const { employee } = detail;
  // Hanya penempatan yang berlaku hari ini; yang sudah berakhir atau terjadwal tidak dianggap jabatan saat ini.
  const placements = detail.placements.filter((p) => isInForce(p.effectiveFrom, p.effectiveToExclusive, today));
  const actingAssignments = detail.actingAssignments.filter((a) => isInForce(a.effectiveFrom, a.effectiveToExclusive, today));
  const primary = placements.find((p) => p.isPrimary);

  return (
    <Card>
      <CardContent className="p-6 space-y-4">
        <div className="flex flex-wrap items-center gap-2">
          <h3 className="text-base font-semibold">{employee.employeeFullName}</h3>
          <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 px-2 py-0.5 rounded">{employee.employeeNumber}</code>
          <EmployeeStatusBadge status={employee.currentStatus} />
          {placements.length === 0 && <Badge variant="warning" className="text-xs">Belum ditempatkan</Badge>}
        </div>

        <div className="text-xs space-y-1">
          <p>
            <span className="text-muted-foreground">Jabatan utama:</span>{" "}
            {primary ? `${primary.positionCode} — ${periodLabel(primary.effectiveFrom, primary.effectiveToExclusive)}` : "—"}
          </p>
          <p>
            <span className="text-muted-foreground">Penempatan lain:</span> {Math.max(0, placements.length - (primary ? 1 : 0))}
            {" · "}
            <span className="text-muted-foreground">Plh./Plt.:</span> {actingAssignments.length}
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <Link href={`/employees/${employee.id}`} className="text-xs underline underline-offset-4 self-center">
            Buka detail pegawai
          </Link>
          <Can permission="employees.link.write">
            <CommandDialog
              triggerLabel="Lepas tautan"
              triggerIcon={<Link2Off className="w-4 h-4" />}
              triggerVariant="outline"
              title="Lepas tautan pegawai"
              description="Ditolak bila pegawai memiliki resign yang sedang berlaku atau terjadwal. Melepas tautan tidak mengaktifkan atau menonaktifkan akun."
              notice={NO_ROLE_CHANGE}
              fields={[{ kind: "textarea", name: "reason", label: "Alasan", required: true }]}
              initial={{ reason: "" }}
              submitLabel="Lepas tautan"
              successMessage="Tautan akun–pegawai dilepas."
              onSubmit={(v) => unlinkEmployeeAction(userId, employee.version, { reason: v.reason })}
            />
          </Can>
        </div>
      </CardContent>
    </Card>
  );
}
