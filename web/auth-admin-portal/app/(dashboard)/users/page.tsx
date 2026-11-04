import { Can } from "@/components/auth/can";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { CreateUserDialog } from "@/components/users/create-user-dialog";
import { UsersTable } from "@/components/users/users-table";
import { getErrorMessage } from "@/lib/api/errors";
import { AdminUserDto, PagedResponse } from "@/lib/api/types";
import { usersApi } from "@/lib/api/users";
import { AlertCircle, Users } from "lucide-react";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Manajemen Pengguna | Auth Admin Portal",
  description:
    "Kelola akun pengguna terpusat (Single User Pool), status keamanan, dan hak akses aplikasi",
};

interface UsersPageProps {
  searchParams: Promise<{
    search?: string;
    page?: string;
    pageSize?: string;
  }>;
}

export default async function UsersPage({ searchParams }: UsersPageProps) {
  const resolvedParams = await searchParams;
  const search = resolvedParams.search?.trim() || "";
  const page = Math.max(1, parseInt(resolvedParams.page || "1", 10) || 1);
  const pageSize = Math.min(
    100,
    Math.max(1, parseInt(resolvedParams.pageSize || "20", 10) || 20)
  );

  let pagedData: PagedResponse<AdminUserDto> = {
    items: [],
    page,
    pageSize,
    totalCount: 0,
    totalPages: 0,
  };
  let fetchError: string | null = null;

  try {
    pagedData = await usersApi.getAll({
      search: search || null,
      page,
      pageSize,
    });
  } catch (err) {
    fetchError = getErrorMessage(err);
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
            <BreadcrumbPage>Pengguna</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header Halaman */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
            <Users className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Manajemen Pengguna SSO
            </h1>
            <p className="text-xs text-muted-foreground mt-0.5">
              Kelola identitas akun pengguna terpusat (Single User Pool) dan
              penugasan hak akses lintas aplikasi.
            </p>
          </div>
        </div>

        <Can permission="users.write">
          <CreateUserDialog />
        </Can>
      </div>

      {/* Konten Utama */}
      {fetchError ? (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">{fetchError}</AlertDescription>
        </Alert>
      ) : (
        <UsersTable pagedData={pagedData} currentSearch={search} />
      )}
    </div>
  );
}

