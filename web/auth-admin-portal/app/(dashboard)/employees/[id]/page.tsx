import { EmployeeActions } from "@/components/employees/employee-actions";
import { EmployeeDetailView } from "@/components/employees/employee-detail-view";
import { EmployeeStatusBadge, isInForce, todayJakarta } from "@/components/employees/format";
import { Alert, AlertDescription } from "@/components/ui/alert";
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
import { employeesApi } from "@/lib/api/employees";
import type { EmployeeDetailDto, EmployeeHistoryDto } from "@/lib/api/employee-types";
import { getErrorMessage } from "@/lib/api/errors";
import { AlertCircle, ArrowLeft, Info, ShieldAlert } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Detail Pegawai | Auth Admin Portal",
};

interface EmployeeDetailPageProps {
  params: Promise<{ id: string }>;
}

export default async function EmployeeDetailPage({ params }: EmployeeDetailPageProps) {
  const { id } = await params;

  let detail: EmployeeDetailDto | null = null;
  let history: EmployeeHistoryDto | null = null;
  let historyError: string | null = null;
  let fetchError: string | null = null;

  try {
    detail = await employeesApi.get(id);
  } catch (err) {
    fetchError = getErrorMessage(err);
  }

  if (detail) {
    try {
      history = await employeesApi.history(id);
    } catch (err) {
      historyError = getErrorMessage(err);
    }
  }

  if (!detail) {
    return (
      <Card className="border-red-200 dark:border-red-900/50 bg-red-50/20 dark:bg-red-950/20">
        <CardContent className="p-8 text-center space-y-4">
          <ShieldAlert className="w-12 h-12 text-red-500 mx-auto" />
          <h2 className="text-lg font-semibold text-red-800 dark:text-red-300">Pegawai Tidak Ditemukan</h2>
          <p className="text-xs text-muted-foreground max-w-md mx-auto">
            {fetchError || "Pegawai yang Anda cari tidak ditemukan atau ID tidak valid."}
          </p>
          <Link href="/employees">
            <Button variant="outline" className="gap-2 text-xs">
              <ArrowLeft className="w-4 h-4" /> Kembali ke Daftar Pegawai
            </Button>
          </Link>
        </CardContent>
      </Card>
    );
  }

  const { employee } = detail;
  const today = todayJakarta();
  const unplaced = !detail.placements.some((p) => isInForce(p.effectiveFrom, p.effectiveToExclusive, today));

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
            <BreadcrumbPage>{employee.employeeFullName}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap items-center gap-2">
          <Link href="/employees">
            <Button variant="outline" size="icon" className="h-9 w-9" title="Kembali ke daftar pegawai">
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </Link>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">{employee.employeeFullName}</h1>
          <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 px-2 py-0.5 rounded">{employee.employeeNumber}</code>
          <EmployeeStatusBadge status={employee.currentStatus} />
          {employee.linkedUserId ? (
            <Link href={`/users/${employee.linkedUserId}`}>
              <Badge variant="info" className="text-xs cursor-pointer">Akun tertaut</Badge>
            </Link>
          ) : (
            <Badge variant="secondary" className="text-xs">Belum ada akun</Badge>
          )}
          {unplaced && <Badge variant="warning" className="text-xs">Belum ditempatkan</Badge>}
        </div>

        <Alert>
          <Info className="h-4 w-4" />
          <AlertDescription className="text-xs">
            Profil kepegawaian adalah data referensi. Mutasi, promosi, Plh./Plt., dan resign tidak mengubah role atau permission akun secara otomatis.
          </AlertDescription>
        </Alert>

        {employee.currentStatus !== "Active" && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription className="text-xs">
              Pegawai tidak berstatus aktif hari ini. Penempatan baru memerlukan status aktif sepanjang periodenya.
            </AlertDescription>
          </Alert>
        )}

        <EmployeeActions detail={detail} today={today} />
      </div>

      <EmployeeDetailView detail={detail} history={history} historyError={historyError} today={today} />
    </div>
  );
}
