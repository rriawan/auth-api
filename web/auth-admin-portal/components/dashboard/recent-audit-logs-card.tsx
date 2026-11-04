"use client";

import { getAuditEventLabel } from "@/components/audit-logs/audit-log-constants";
import { AuditLogDetailDialog } from "@/components/audit-logs/audit-log-detail-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { AuditLogDto } from "@/lib/api/types";
import {
  ArrowRight,
  CheckCircle2,
  Eye,
  History,
  XCircle,
} from "lucide-react";
import Link from "next/link";
import { useState } from "react";

interface RecentAuditLogsCardProps {
  logs: AuditLogDto[];
}

export function RecentAuditLogsCard({ logs }: RecentAuditLogsCardProps) {
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        hour: "2-digit",
        minute: "2-digit",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <>
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        <CardHeader className="border-b border-neutral-200 dark:border-slate-800">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <History className="w-4 h-4" />
              </div>
              <div>
                <CardTitle className="text-base font-semibold">
                  Aktivitas Keamanan Terbaru
                </CardTitle>
                <CardDescription className="text-xs">
                  10 rekaman log audit terakhir dari seluruh aplikasi terdaftar.
                </CardDescription>
              </div>
            </div>

            <Link href="/audit-logs">
              <Button variant="outline" size="sm" className="h-8 gap-1.5 text-xs">
                <span>Lihat Semua Log</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Button>
            </Link>
          </div>
        </CardHeader>

        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent bg-neutral-50 dark:bg-slate-900/60 text-xs">
                <TableHead className="w-[18%]">Waktu</TableHead>
                <TableHead className="w-[28%]">Aktivitas</TableHead>
                <TableHead className="w-[18%]">Pengguna</TableHead>
                <TableHead className="w-[15%]">Aplikasi</TableHead>
                <TableHead className="text-center w-[12%]">Status</TableHead>
                <TableHead className="text-right w-[9%]">Aksi</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {logs.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="h-32 text-center text-xs text-muted-foreground">
                    Belum ada aktivitas audit yang tercatat.
                  </TableCell>
                </TableRow>
              ) : (
                logs.map((log) => (
                  <TableRow
                    key={log.id}
                    className="hover:bg-neutral-50/70 dark:hover:bg-slate-800/40"
                  >
                    <TableCell className="align-middle py-2.5 text-xs text-muted-foreground whitespace-nowrap">
                      {formatDate(log.timestampUtc)}
                    </TableCell>

                    <TableCell className="align-middle py-2.5">
                      <div className="space-y-0.5">
                        <code className="text-xs font-mono font-semibold text-foreground">
                          {log.eventType}
                        </code>
                        <div className="text-[11px] text-muted-foreground">
                          {getAuditEventLabel(log.eventType)}
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="align-middle py-2.5 text-xs">
                      {log.username ? (
                        <span className="font-medium text-foreground">
                          @{log.username}
                        </span>
                      ) : (
                        <span className="text-muted-foreground italic">
                          Sistem / Anonim
                        </span>
                      )}
                    </TableCell>

                    <TableCell className="align-middle py-2.5">
                      {log.clientId ? (
                        <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 px-1.5 py-0.5 rounded">
                          {log.clientId}
                        </code>
                      ) : (
                        <span className="text-xs text-muted-foreground">-</span>
                      )}
                    </TableCell>

                    <TableCell className="align-middle py-2.5 text-center">
                      {log.isSuccess ? (
                        <Badge
                          variant="success"
                          className="gap-1 text-[10px] py-0.5"
                        >
                          <CheckCircle2 className="w-3 h-3" />
                          Berhasil
                        </Badge>
                      ) : (
                        <Badge
                          variant="danger"
                          className="gap-1 text-[10px] py-0.5"
                        >
                          <XCircle className="w-3 h-3" />
                          Gagal
                        </Badge>
                      )}
                    </TableCell>

                    <TableCell className="align-middle py-2.5 text-right">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7 text-muted-foreground hover:text-foreground"
                        onClick={() => setSelectedLog(log)}
                        title="Lihat Detail JSON"
                      >
                        <Eye className="w-3.5 h-3.5" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <AuditLogDetailDialog
        log={selectedLog}
        onClose={() => setSelectedLog(null)}
      />
    </>
  );
}

