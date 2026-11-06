import { Can } from "@/components/auth/can";
import { CreateEmployeeDialog } from "@/components/employees/create-employee-dialog";
import { EmployeesTable } from "@/components/employees/employees-table";
import { todayJakarta } from "@/components/employees/format";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { employeesApi } from "@/lib/api/employees";
import type { EmployeePageDto } from "@/lib/api/employee-types";
import { getErrorMessage } from "@/lib/api/errors";
import { AlertCircle, UsersRound } from "lucide-react";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Pegawai | Auth Admin Portal",
  description: "Master pegawai, penempatan jabatan, dan riwayatnya",
};

interface EmployeesPageProps {
  searchParams: Promise<{ search?: string; page?: string; pageSize?: string }>;
}

export default async function EmployeesPage({ searchParams }: EmployeesPageProps) {
  const params = await searchParams;
  const search = params.search?.trim() || "";
  const page = Math.min(1_000_000, Math.max(1, parseInt(params.page || "1", 10) || 1));
  const pageSize = Math.min(100, Math.max(1, parseInt(params.pageSize || "20", 10) || 20));

  let data: EmployeePageDto = { items: [], page, pageSize, totalCount: 0 };
  let fetchError: string | null = null;
  try {
    data = await employeesApi.list({ search: search || null, page, pageSize });
  } catch (err) {
    fetchError = getErrorMessage(err);
  }

  return (
    <div className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Pegawai</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
            <UsersRound className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Master Pegawai</h1>
            <p className="text-xs text-muted-foreground mt-0.5">
              Data kepegawaian bersifat referensi. Perubahan penempatan tidak mengubah role atau akses akun.
            </p>
          </div>
        </div>
        <Can permission="employees.write">
          <CreateEmployeeDialog today={todayJakarta()} />
        </Can>
      </div>

      {fetchError ? (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">{fetchError}</AlertDescription>
        </Alert>
      ) : (
        <EmployeesTable pagedData={data} currentSearch={search} />
      )}
    </div>
  );
}
