"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { AuditLogDto, ClientAppDto, PagedResponse } from "@/lib/api/types";
import {
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Eye,
  Filter,
  History,
  RotateCcw,
  XCircle,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import {
  AUDIT_EVENT_TYPES,
  getAuditEventLabel,
} from "./audit-log-constants";
import { AuditLogDetailDialog } from "./audit-log-detail-dialog";

export interface CurrentAuditLogFilters {
  userId: string;
  clientId: string;
  eventType: string;
  isSuccess: string; // "" | "true" | "false"
  dateFrom: string; // YYYY-MM-DD
  dateTo: string; // YYYY-MM-DD
}

interface AuditLogsTableProps {
  pagedData: PagedResponse<AuditLogDto>;
  currentFilters: CurrentAuditLogFilters;
  apps: ClientAppDto[];
}

export function AuditLogsTable({
  pagedData,
  currentFilters,
  apps,
}: AuditLogsTableProps) {
  const router = useRouter();

  // State Filter
  const [userId, setUserId] = useState(currentFilters.userId);
  const [clientId, setClientId] = useState(currentFilters.clientId);
  const [eventType, setEventType] = useState(currentFilters.eventType);
  const [isSuccess, setIsSuccess] = useState(currentFilters.isSuccess);
  const [dateFrom, setDateFrom] = useState(currentFilters.dateFrom);
  const [dateTo, setDateTo] = useState(currentFilters.dateTo);

  // State Dialog Detail
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);

  const { items, page, pageSize, totalCount, totalPages } = pagedData;

  const navigateWithFilters = (
    filters: CurrentAuditLogFilters,
    nextPage: number
  ) => {
    const params = new URLSearchParams();
    if (filters.userId.trim()) params.set("userId", filters.userId.trim());
    if (filters.clientId.trim())
      params.set("clientId", filters.clientId.trim());
    if (filters.eventType.trim())
      params.set("eventType", filters.eventType.trim());
    if (filters.isSuccess === "true" || filters.isSuccess === "false") {
      params.set("isSuccess", filters.isSuccess);
    }
    if (filters.dateFrom.trim())
      params.set("dateFrom", filters.dateFrom.trim());
    if (filters.dateTo.trim()) params.set("dateTo", filters.dateTo.trim());
    if (nextPage > 1) params.set("page", nextPage.toString());
    if (pageSize !== 25) params.set("pageSize", pageSize.toString());

    const qs = params.toString();
    router.push(`/audit-logs${qs ? `?${qs}` : ""}`);
  };

  const handleApplyFilter = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    navigateWithFilters(
      {
        userId,
        clientId,
        eventType,
        isSuccess,
        dateFrom,
        dateTo,
      },
      1
    );
  };

  const handleResetFilters = () => {
    const empty: CurrentAuditLogFilters = {
      userId: "",
      clientId: "",
      eventType: "",
      isSuccess: "",
      dateFrom: "",
      dateTo: "",
    };
    setUserId("");
    setClientId("");
    setEventType("");
    setIsSuccess("");
    setDateFrom("");
    setDateTo("");
    navigateWithFilters(empty, 1);
  };

  const hasActiveFilters = Boolean(
    currentFilters.userId ||
      currentFilters.clientId ||
      currentFilters.eventType ||
      currentFilters.isSuccess ||
      currentFilters.dateFrom ||
      currentFilters.dateTo
  );

  const formatTimestamp = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <>
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        {/* Form Filter Komprehensif */}
        <form
          onSubmit={handleApplyFilter}
          className="p-4 border-b border-neutral-200 dark:border-slate-800 bg-neutral-50/50 dark:bg-slate-900/40 space-y-4"
        >
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3">
            {/* Filter Jenis Event */}
            <div className="space-y-1">
              <Label
                htmlFor="filter-eventType"
                className="text-[11px] font-semibold text-muted-foreground"
              >
                Jenis Aktivitas
              </Label>
              <select
                id="filter-eventType"
                value={eventType}
                onChange={(e) => setEventType(e.target.value)}
                className="w-full h-9 rounded-md border border-input bg-white dark:bg-slate-950 px-2.5 text-xs focus:outline-none focus:ring-2 focus:ring-primary/30"
              >
                <option value="">Semua Event</option>
                {AUDIT_EVENT_TYPES.map((ev) => (
                  <option key={ev.value} value={ev.value}>
                    {ev.value} — {ev.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Filter Aplikasi (ClientId) */}
            <div className="space-y-1">
              <Label
                htmlFor="filter-clientId"
                className="text-[11px] font-semibold text-muted-foreground"
              >
                Aplikasi (Client ID)
              </Label>
              {apps.length > 0 ? (
                <select
                  id="filter-clientId"
                  value={clientId}
                  onChange={(e) => setClientId(e.target.value)}
                  className="w-full h-9 rounded-md border border-input bg-white dark:bg-slate-950 px-2.5 text-xs focus:outline-none focus:ring-2 focus:ring-primary/30"
                >
                  <option value="">Semua Aplikasi</option>
                  {apps.map((app) => (
                    <option key={app.id} value={app.clientId}>
                      {app.clientId} ({app.name})
                    </option>
                  ))}
                </select>
              ) : (
                <Input
                  id="filter-clientId"
                  placeholder="contoh: auth-admin"
                  value={clientId}
                  onChange={(e) => setClientId(e.target.value)}
                  className="h-9 text-xs bg-white dark:bg-slate-950"
                />
              )}
            </div>

            {/* Filter Status */}
            <div className="space-y-1">
              <Label
                htmlFor="filter-isSuccess"
                className="text-[11px] font-semibold text-muted-foreground"
              >
                Status Eksekusi
              </Label>
              <select
                id="filter-isSuccess"
                value={isSuccess}
                onChange={(e) => setIsSuccess(e.target.value)}
                className="w-full h-9 rounded-md border border-input bg-white dark:bg-slate-950 px-2.5 text-xs focus:outline-none focus:ring-2 focus:ring-primary/30"
              >
                <option value="">Semua Status</option>
                <option value="true">Berhasil (Success)</option>
                <option value="false">Gagal (Failed)</option>
              </select>
            </div>

            {/* Filter User ID (UUID) */}
            <div className="space-y-1">
              <Label
                htmlFor="filter-userId"
                className="text-[11px] font-semibold text-muted-foreground"
              >
                User ID (UUID)
              </Label>
              <Input
                id="filter-userId"
                placeholder="UUID pengguna..."
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                className="h-9 text-xs font-mono bg-white dark:bg-slate-950"
              />
            </div>

            {/* Filter Tanggal Dari */}
            <div className="space-y-1">
              <Label
                htmlFor="filter-dateFrom"
                className="text-[11px] font-semibold text-muted-foreground"
              >
                Dari Tanggal
              </Label>
              <Input
                id="filter-dateFrom"
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="h-9 text-xs bg-white dark:bg-slate-950"
              />
            </div>

            {/* Filter Tanggal Sampai */}
            <div className="space-y-1">
              <Label
                htmlFor="filter-dateTo"
                className="text-[11px] font-semibold text-muted-foreground"
              >
                Sampai Tanggal
              </Label>
              <Input
                id="filter-dateTo"
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="h-9 text-xs bg-white dark:bg-slate-950"
              />
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2 pt-1">
            <div className="text-xs text-muted-foreground">
              Menemukan <strong>{totalCount}</strong> rekaman audit keamanan
              {hasActiveFilters && " (filter aktif)"}
            </div>

            <div className="flex items-center gap-2">
              {hasActiveFilters && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={handleResetFilters}
                  className="h-8 gap-1.5 text-xs text-muted-foreground hover:text-foreground"
                >
                  <RotateCcw className="w-3.5 h-3.5" />
                  Reset Filter
                </Button>
              )}
              <Button
                type="submit"
                size="sm"
                className="h-8 gap-1.5 text-xs"
              >
                <Filter className="w-3.5 h-3.5" />
                Terapkan Filter
              </Button>
            </div>
          </div>
        </form>

        {/* Tabel Audit Logs */}
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent bg-neutral-50 dark:bg-slate-900/60 text-xs">
                <TableHead className="w-[17%]">Waktu</TableHead>
                <TableHead className="w-[26%]">Jenis Aktivitas</TableHead>
                <TableHead className="w-[16%]">Pengguna</TableHead>
                <TableHead className="w-[14%]">Aplikasi</TableHead>
                <TableHead className="w-[12%]">Alamat IP</TableHead>
                <TableHead className="text-center w-[9%]">Status</TableHead>
                <TableHead className="text-right w-[6%]">Detail</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="h-44 text-center">
                    <div className="flex flex-col items-center justify-center text-muted-foreground space-y-2">
                      <History className="w-8 h-8 opacity-40" />
                      <p className="text-sm font-medium">
                        Tidak ada rekaman audit log yang sesuai dengan filter.
                      </p>
                      {hasActiveFilters && (
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={handleResetFilters}
                          className="text-xs h-8 mt-1"
                        >
                          Reset Semua Filter
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ) : (
                items.map((log) => (
                  <TableRow
                    key={log.id}
                    className="hover:bg-neutral-50/70 dark:hover:bg-slate-800/40"
                  >
                    <TableCell className="align-middle py-3 text-xs text-muted-foreground whitespace-nowrap">
                      {formatTimestamp(log.timestampUtc)}
                    </TableCell>

                    <TableCell className="align-middle py-3">
                      <div className="space-y-0.5">
                        <code className="text-xs font-mono font-semibold text-foreground">
                          {log.eventType}
                        </code>
                        <div className="text-[11px] text-muted-foreground">
                          {getAuditEventLabel(log.eventType)}
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="align-middle py-3">
                      {log.username ? (
                        <button
                          type="button"
                          onClick={() => {
                            if (log.userId) {
                              setUserId(log.userId);
                              navigateWithFilters(
                                {
                                  ...currentFilters,
                                  userId: log.userId,
                                },
                                1
                              );
                            }
                          }}
                          className="text-left group"
                          title="Klik untuk memfilter berdasarkan pengguna ini"
                        >
                          <span className="text-xs font-medium text-foreground group-hover:text-primary group-hover:underline">
                            @{log.username}
                          </span>
                        </button>
                      ) : (
                        <span className="text-xs text-muted-foreground italic">
                          Sistem / Anonim
                        </span>
                      )}
                    </TableCell>

                    <TableCell className="align-middle py-3">
                      {log.clientId ? (
                        <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 text-neutral-700 dark:text-neutral-300 px-2 py-0.5 rounded">
                          {log.clientId}
                        </code>
                      ) : (
                        <span className="text-xs text-muted-foreground">-</span>
                      )}
                    </TableCell>

                    <TableCell className="align-middle py-3 font-mono text-xs text-muted-foreground">
                      {log.ipAddress || "-"}
                    </TableCell>

                    <TableCell className="align-middle py-3 text-center">
                      {log.isSuccess ? (
                        <Badge
                          variant="success"
                          className="gap-1 text-[11px] py-0.5"
                        >
                          <CheckCircle2 className="w-3 h-3" />
                          Berhasil
                        </Badge>
                      ) : (
                        <Badge
                          variant="danger"
                          className="gap-1 text-[11px] py-0.5"
                        >
                          <XCircle className="w-3 h-3" />
                          Gagal
                        </Badge>
                      )}
                    </TableCell>

                    <TableCell className="align-middle py-3 text-right">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8 gap-1 text-xs"
                        onClick={() => setSelectedLog(log)}
                      >
                        <Eye className="w-3.5 h-3.5" />
                        <span>Detail</span>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>

          {/* Footer Pagination */}
          <div className="p-3 border-t border-neutral-200 dark:border-slate-800 text-xs text-muted-foreground flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-neutral-50/30 dark:bg-slate-900/20">
            <div>
              Menampilkan <strong>{items.length}</strong> log pada halaman{" "}
              <strong>{page}</strong> dari{" "}
              <strong>{Math.max(1, totalPages)}</strong> (Total{" "}
              <strong>{totalCount}</strong> rekaman)
            </div>

            {totalPages > 1 && (
              <div className="flex items-center gap-1.5">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-8 gap-1 text-xs"
                  disabled={page <= 1}
                  onClick={() =>
                    navigateWithFilters(currentFilters, page - 1)
                  }
                >
                  <ChevronLeft className="w-3.5 h-3.5" />
                  Sebelumnya
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-8 gap-1 text-xs"
                  disabled={page >= totalPages}
                  onClick={() =>
                    navigateWithFilters(currentFilters, page + 1)
                  }
                >
                  Berikutnya
                  <ChevronRight className="w-3.5 h-3.5" />
                </Button>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Dialog Detail Audit Log */}
      <AuditLogDetailDialog
        log={selectedLog}
        onClose={() => setSelectedLog(null)}
      />
    </>
  );
}

