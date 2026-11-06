import { formatDate, periodLabel } from "@/components/employees/format";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { SelfOrganizationProfileDto } from "@/lib/api/employee-types";
import { AlertCircle, Info } from "lucide-react";

interface ProfileOrganizationProps {
  profile: SelfOrganizationProfileDto | null;
  error: string | null;
}

const TYPE_LABEL: Record<string, string> = {
  DEFINITIVE: "Definitif",
  PLH: "Plh. (Pelaksana Harian)",
  PLT: "Plt. (Pelaksana Tugas)",
};

/** Ringkasan organisasi akun sendiri (hanya baca). Profil ini referensi, bukan bukti kewenangan. */
export function ProfileOrganization({ profile, error }: ProfileOrganizationProps) {
  if (error) {
    return (
      <Alert variant="destructive">
        <AlertCircle className="h-4 w-4" />
        <AlertDescription className="text-xs">{error}</AlertDescription>
      </Alert>
    );
  }

  if (!profile?.employee) {
    return (
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription className="text-xs">
          Akun Anda belum tertaut ke data pegawai, sehingga tidak ada penempatan yang ditampilkan.
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">
            {profile.employee.employeeFullName}{" "}
            <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 px-2 py-0.5 rounded">{profile.employee.employeeNumber}</code>
          </CardTitle>
        </CardHeader>
        <CardContent className="text-xs text-muted-foreground">
          Berlaku per {formatDate(profile.asOf)} ({profile.businessTimeZone}). Data ini hanya referensi; hak akses ditentukan oleh role aplikasi.
        </CardContent>
      </Card>

      {profile.assignments.length === 0 ? (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertDescription className="text-xs">Belum ada penempatan yang berlaku hari ini.</AlertDescription>
        </Alert>
      ) : (
        profile.assignments.map((a) => (
          <Card key={a.id}>
            <CardContent className="p-5 space-y-2 text-xs">
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-sm font-semibold">{a.position.name}</span>
                <Badge variant={a.type === "DEFINITIVE" ? "info" : "warning"} className="text-xs">{TYPE_LABEL[a.type] ?? a.type}</Badge>
                {a.isPrimary && <Badge variant="success" className="text-xs">Utama</Badge>}
                {a.position.isUnitHead && <Badge variant="secondary" className="text-xs">Pimpinan unit</Badge>}
              </div>
              <p>
                <span className="text-muted-foreground">Kantor:</span> {a.office.code} — {a.office.name} ({a.office.classification})
              </p>
              <p>
                <span className="text-muted-foreground">Unit:</span> {a.orgUnit.name}
              </p>
              <p>
                <span className="text-muted-foreground">Periode:</span> {periodLabel(a.effectiveFrom, a.effectiveToExclusive)}
              </p>
              {a.formalSupervisor && (
                <p>
                  <span className="text-muted-foreground">Atasan formal (posisi):</span> {a.formalSupervisor.positionName}
                  {a.formalSupervisor.isVacant && <Badge variant="warning" className="text-xs ml-2">Vacant</Badge>}
                </p>
              )}
            </CardContent>
          </Card>
        ))
      )}
    </div>
  );
}
