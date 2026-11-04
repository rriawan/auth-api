import {
  AuditLogsTable,
  CurrentAuditLogFilters,
} from "@/components/audit-logs/audit-logs-table";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { appsApi } from "@/lib/api/apps";
import { auditLogsApi } from "@/lib/api/audit-logs";
import { getErrorMessage } from "@/lib/api/errors";
import { AuditLogDto, ClientAppDto, PagedResponse } from "@/lib/api/types";
import { AlertCircle, History } from "lucide-react";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Audit Logs Keamanan | Auth Admin Portal",
  description:
    "Pantau riwayat autentikasi, kegagalan login, rotasi kunci, dan perubahan hak akses secara terpusat",
};

interface AuditLogsPageProps {
  searchParams: Promise<{
    userId?: string;
    clientId?: string;
    eventType?: string;
    isSuccess?: string;
    dateFrom?: string;
    dateTo?: string;
    page?: string;
    pageSize?: string;
  }>;
}

export default async function AuditLogsPage({
  searchParams,
}: AuditLogsPageProps) {
  const resolved = await searchParams;

  const userId = resolved.userId?.trim() || "";
  const clientId = resolved.clientId?.trim() || "";
  const eventType = resolved.eventType?.trim() || "";
  const isSuccessParam = resolved.isSuccess?.trim() || "";
  const dateFrom = resolved.dateFrom?.trim() || "";
  const dateTo = resolved.dateTo?.trim() || "";
  const page = Math.max(1, parseInt(resolved.page || "1", 10) || 1);
  const pageSize = Math.min(
    100,
    Math.max(1, parseInt(resolved.pageSize || "25", 10) || 25)
  );

  let isSuccessFilter: boolean | null = null;
  if (isSuccessParam === "true") isSuccessFilter = true;
  if (isSuccessParam === "false") isSuccessFilter = false;

  const dateFromUtc = dateFrom ? `${dateFrom}T00:00:00.000Z` : null;
  const dateToUtc = dateTo ? `${dateTo}T23:59:59.999Z` : null;

  let pagedData: PagedResponse<AuditLogDto> = {
    items: [],
    page,
    pageSize,
    totalCount: 0,
    totalPages: 0,
  };
  let fetchError: string | null = null;
  let apps: ClientAppDto[] = [];

  const [logsResult, appsResult] = await Promise.allSettled([
    auditLogsApi.getAll({
      userId: userId || null,
      clientId: clientId || null,
      eventType: eventType || null,
      isSuccess: isSuccessFilter,
      dateFromUtc,
      dateToUtc,
      page,
      pageSize,
    }),
    appsApi.getAll(),
  ]);

  if (logsResult.status === "fulfilled") {
    pagedData = logsResult.value;
  } else {
    fetchError = getErrorMessage(logsResult.reason);
  }

  if (appsResult.status === "fulfilled") {
    apps = appsResult.value;
  }

  const currentFilters: CurrentAuditLogFilters = {
    userId,
    clientId,
    eventType,
    isSuccess: isSuccessParam,
    dateFrom,
    dateTo,
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
            <BreadcrumbPage>Audit Logs</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header Halaman */}
      <div className="flex items-center gap-3">
        <div className="w-11 h-11 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
          <History className="w-5 h-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">
            Audit Logs Keamanan
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Rekaman aktivitas autentikasi, kegagalan login, lockout, dan mutasi
            administrasi lintas aplikasi.
          </p>
        </div>
      </div>

      {fetchError ? (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">{fetchError}</AlertDescription>
        </Alert>
      ) : (
        <AuditLogsTable
          pagedData={pagedData}
          currentFilters={currentFilters}
          apps={apps}
        />
      )}
    </div>
  );
}

