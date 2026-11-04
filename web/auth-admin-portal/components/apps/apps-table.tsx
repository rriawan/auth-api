"use client";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ClientAppDto } from "@/lib/api/types";
import {
  Check,
  CheckCircle2,
  ChevronRight,
  Copy,
  Layers,
  Search,
  Shield,
  XCircle,
} from "lucide-react";
import Link from "next/link";
import { useMemo, useState } from "react";
import toast from "react-hot-toast";

interface AppsTableProps {
  initialApps: ClientAppDto[];
}

export function AppsTable({ initialApps }: AppsTableProps) {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "inactive">("all");
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const handleCopyClientId = (clientId: string) => {
    navigator.clipboard.writeText(clientId);
    setCopiedId(clientId);
    toast.success(`Client ID '${clientId}' disalin!`);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const filteredApps = useMemo(() => {
    return initialApps.filter((app) => {
      const q = search.toLowerCase().trim();
      const matchQuery =
        !q ||
        app.name.toLowerCase().includes(q) ||
        app.clientId.toLowerCase().includes(q) ||
        (app.description && app.description.toLowerCase().includes(q));

      const matchStatus =
        statusFilter === "all" ||
        (statusFilter === "active" && app.isActive) ||
        (statusFilter === "inactive" && !app.isActive);

      return matchQuery && matchStatus;
    });
  }, [initialApps, search, statusFilter]);

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        year: "numeric",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
      {/* Filter & Pencarian Bar */}
      <div className="p-4 border-b border-neutral-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-neutral-50/50 dark:bg-slate-900/40">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <Input
            placeholder="Cari nama atau client id..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 h-9 text-xs bg-white dark:bg-slate-950"
          />
        </div>

        <div className="flex items-center gap-1.5 self-start sm:self-auto">
          <Button
            variant={statusFilter === "all" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("all")}
            className="text-xs h-8"
          >
            Semua ({initialApps.length})
          </Button>
          <Button
            variant={statusFilter === "active" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("active")}
            className="text-xs h-8"
          >
            Aktif ({initialApps.filter((a) => a.isActive).length})
          </Button>
          <Button
            variant={statusFilter === "inactive" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("inactive")}
            className="text-xs h-8"
          >
            Nonaktif ({initialApps.filter((a) => !a.isActive).length})
          </Button>
        </div>
      </div>

      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent bg-neutral-50 dark:bg-slate-900/60 text-xs">
              <TableHead className="w-[30%]">Aplikasi</TableHead>
              <TableHead>Client ID</TableHead>
              <TableHead>API Key Prefix</TableHead>
              <TableHead>Statistik Hak Akses</TableHead>
              <TableHead className="text-center">Status</TableHead>
              <TableHead>Dibuat</TableHead>
              <TableHead className="text-right">Aksi</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredApps.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="h-40 text-center">
                  <div className="flex flex-col items-center justify-center text-muted-foreground space-y-2">
                    <Layers className="w-8 h-8 opacity-40" />
                    <p className="text-sm font-medium">Tidak ada aplikasi yang sesuai.</p>
                    {search && (
                      <p className="text-xs">
                        Coba gunakan kata kunci pencarian yang lain atau reset filter.
                      </p>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              filteredApps.map((app) => {
                const isSystemApp = app.clientId.toLowerCase() === "auth-admin";
                return (
                  <TableRow key={app.id} className="hover:bg-neutral-50/70 dark:hover:bg-slate-800/40">
                    <TableCell className="align-top py-3.5">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-sm text-foreground">
                            {app.name}
                          </span>
                          {isSystemApp && (
                            <Badge
                              variant="outline"
                              className="text-[10px] px-1.5 py-0 border-blue-300 dark:border-blue-700 bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 gap-1"
                            >
                              <Shield className="w-2.5 h-2.5" />
                              Sistem
                            </Badge>
                          )}
                        </div>
                        {app.description ? (
                          <p className="text-xs text-muted-foreground line-clamp-1">
                            {app.description}
                          </p>
                        ) : (
                          <span className="text-xs text-muted-foreground/60 italic">
                            Tanpa deskripsi
                          </span>
                        )}
                      </div>
                    </TableCell>

                    <TableCell className="align-middle">
                      <div className="flex items-center gap-1.5">
                        <code className="text-xs font-mono font-medium bg-neutral-100 dark:bg-slate-800 text-neutral-800 dark:text-neutral-200 px-2 py-0.5 rounded">
                          {app.clientId}
                        </code>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6 text-muted-foreground hover:text-foreground"
                          onClick={() => handleCopyClientId(app.clientId)}
                          title="Salin Client ID"
                        >
                          {copiedId === app.clientId ? (
                            <Check className="w-3 h-3 text-emerald-500" />
                          ) : (
                            <Copy className="w-3 h-3" />
                          )}
                        </Button>
                      </div>
                    </TableCell>

                    <TableCell className="align-middle">
                      <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 text-neutral-600 dark:text-neutral-400 px-2 py-0.5 rounded">
                        {app.apiKeyPrefix}...
                      </code>
                    </TableCell>

                    <TableCell className="align-middle">
                      <div className="flex flex-wrap gap-1 text-[11px]">
                        <span className="bg-neutral-100 dark:bg-slate-800 px-1.5 py-0.5 rounded text-neutral-700 dark:text-neutral-300">
                          {app.rolesCount} Role
                        </span>
                        <span className="bg-neutral-100 dark:bg-slate-800 px-1.5 py-0.5 rounded text-neutral-700 dark:text-neutral-300">
                          {app.permissionsCount} Izin
                        </span>
                        <span className="bg-neutral-100 dark:bg-slate-800 px-1.5 py-0.5 rounded text-neutral-700 dark:text-neutral-300">
                          {app.assignedUsersCount} Pengguna
                        </span>
                      </div>
                    </TableCell>

                    <TableCell className="align-middle text-center">
                      {app.isActive ? (
                        <Badge variant="success" className="gap-1 text-[11px] py-0.5">
                          <CheckCircle2 className="w-3 h-3" />
                          Aktif
                        </Badge>
                      ) : (
                        <Badge variant="secondary" className="gap-1 text-[11px] py-0.5 text-muted-foreground">
                          <XCircle className="w-3 h-3" />
                          Nonaktif
                        </Badge>
                      )}
                    </TableCell>

                    <TableCell className="align-middle text-xs text-muted-foreground whitespace-nowrap">
                      {formatDate(app.createdAtUtc)}
                    </TableCell>

                    <TableCell className="align-middle text-right">
                      <Link href={`/apps/${app.id}`}>
                        <Button variant="outline" size="sm" className="h-8 gap-1 text-xs">
                          <span>Kelola</span>
                          <ChevronRight className="w-3.5 h-3.5" />
                        </Button>
                      </Link>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>

        <div className="p-3 border-t border-neutral-200 dark:border-slate-800 text-xs text-muted-foreground flex justify-between items-center bg-neutral-50/30 dark:bg-slate-900/20">
          <span>
            Menampilkan <strong>{filteredApps.length}</strong> dari{" "}
            <strong>{initialApps.length}</strong> aplikasi terdaftar
          </span>
        </div>
      </CardContent>
    </Card>
  );
}

