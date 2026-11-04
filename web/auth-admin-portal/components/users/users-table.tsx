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
import { AdminUserDto, PagedResponse } from "@/lib/api/types";
import {
  AlertTriangle,
  Check,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Copy,
  Lock,
  Mail,
  Search,
  Users,
  X,
  XCircle,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import toast from "react-hot-toast";

interface UsersTableProps {
  pagedData: PagedResponse<AdminUserDto>;
  currentSearch: string;
}

function checkUserLocked(lockoutEndUtc: string | null): boolean {
  if (!lockoutEndUtc) return false;
  try {
    return new Date(lockoutEndUtc) > new Date();
  } catch {
    return false;
  }
}

export function UsersTable({ pagedData, currentSearch }: UsersTableProps) {
  const router = useRouter();
  const [searchInput, setSearchInput] = useState(currentSearch);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const { items, page, pageSize, totalCount, totalPages } = pagedData;

  const navigateWithParams = (nextSearch: string, nextPage: number) => {
    const params = new URLSearchParams();
    if (nextSearch.trim()) {
      params.set("search", nextSearch.trim());
    }
    if (nextPage > 1) {
      params.set("page", nextPage.toString());
    }
    if (pageSize !== 20) {
      params.set("pageSize", pageSize.toString());
    }
    const qs = params.toString();
    router.push(`/users${qs ? `?${qs}` : ""}`);
  };

  const handleSearchSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    navigateWithParams(searchInput, 1);
  };

  const handleClearSearch = () => {
    setSearchInput("");
    navigateWithParams("", 1);
  };

  const handleCopyUsername = (username: string) => {
    navigator.clipboard.writeText(username);
    setCopiedId(username);
    toast.success(`Username '${username}' disalin!`);
    setTimeout(() => setCopiedId(null), 2000);
  };

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
      {/* Bar Pencarian Server-Side */}
      <div className="p-4 border-b border-neutral-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-neutral-50/50 dark:bg-slate-900/40">
        <form
          onSubmit={handleSearchSubmit}
          className="flex items-center gap-2 flex-1 max-w-md"
        >
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              placeholder="Cari username, email, atau nama lengkap..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="pl-9 pr-8 h-9 text-xs bg-white dark:bg-slate-950"
            />
            {searchInput && (
              <button
                type="button"
                onClick={handleClearSearch}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                title="Bersihkan pencarian"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
          </div>
          <Button type="submit" variant="secondary" size="sm" className="h-9 text-xs">
            Cari
          </Button>
        </form>

        <div className="text-xs text-muted-foreground">
          Total <strong>{totalCount}</strong> pengguna terdaftar
          {currentSearch && (
            <span>
              {" "}
              untuk pencarian <strong>&ldquo;{currentSearch}&rdquo;</strong>
            </span>
          )}
        </div>
      </div>

      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent bg-neutral-50 dark:bg-slate-900/60 text-xs">
              <TableHead className="w-[24%]">Pengguna</TableHead>
              <TableHead className="w-[22%]">Email</TableHead>
              <TableHead className="w-[20%]">Status Akun</TableHead>
              <TableHead>Akses Aplikasi &amp; Role</TableHead>
              <TableHead className="w-[12%]">Terdaftar</TableHead>
              <TableHead className="text-right w-[10%]">Aksi</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-44 text-center">
                  <div className="flex flex-col items-center justify-center text-muted-foreground space-y-2">
                    <Users className="w-8 h-8 opacity-40" />
                    <p className="text-sm font-medium">
                      {currentSearch
                        ? "Tidak ada pengguna yang sesuai dengan kata kunci pencarian."
                        : "Belum ada pengguna yang terdaftar."}
                    </p>
                    {currentSearch && (
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={handleClearSearch}
                        className="text-xs h-8 mt-1"
                      >
                        Reset Pencarian
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              items.map((user) => {
                const locked = checkUserLocked(user.lockoutEndUtc);

                return (
                  <TableRow
                    key={user.id}
                    className="hover:bg-neutral-50/70 dark:hover:bg-slate-800/40"
                  >
                    <TableCell className="align-middle py-3.5">
                      <div className="space-y-1">
                        <div className="font-semibold text-sm text-foreground">
                          {user.fullName}
                        </div>
                        <div className="flex items-center gap-1.5">
                          <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 text-neutral-700 dark:text-neutral-300 px-1.5 py-0.5 rounded">
                            @{user.username}
                          </code>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-5 w-5 text-muted-foreground hover:text-foreground"
                            onClick={() => handleCopyUsername(user.username)}
                            title="Salin Username"
                          >
                            {copiedId === user.username ? (
                              <Check className="w-3 h-3 text-emerald-500" />
                            ) : (
                              <Copy className="w-3 h-3" />
                            )}
                          </Button>
                        </div>
                      </div>
                    </TableCell>

                    <TableCell className="align-middle">
                      <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                        <Mail className="w-3.5 h-3.5 shrink-0 opacity-70" />
                        <span className="truncate max-w-[200px] text-foreground/90">
                          {user.email}
                        </span>
                      </div>
                    </TableCell>

                    <TableCell className="align-middle">
                      <div className="flex flex-wrap items-center gap-1.5">
                        {/* Badge Aktif / Nonaktif */}
                        {user.isActive ? (
                          <Badge
                            variant="success"
                            className="gap-1 text-[11px] py-0.5"
                          >
                            <CheckCircle2 className="w-3 h-3" />
                            Aktif
                          </Badge>
                        ) : (
                          <Badge
                            variant="secondary"
                            className="gap-1 text-[11px] py-0.5 text-muted-foreground"
                          >
                            <XCircle className="w-3 h-3" />
                            Nonaktif
                          </Badge>
                        )}

                        {/* Badge Terkunci (Merah) */}
                        {locked && (
                          <Badge
                            variant="danger"
                            className="gap-1 text-[11px] py-0.5"
                          >
                            <Lock className="w-3 h-3" />
                            Terkunci
                          </Badge>
                        )}

                        {/* Badge Belum Verifikasi (Kuning) */}
                        {!user.isAccountVerified && (
                          <Badge
                            variant="warning"
                            className="gap-1 text-[11px] py-0.5"
                          >
                            <AlertTriangle className="w-3 h-3" />
                            Belum Verifikasi
                          </Badge>
                        )}
                      </div>
                    </TableCell>

                    <TableCell className="align-middle py-3">
                      {user.appAssignments.length === 0 ? (
                        <span className="text-xs text-muted-foreground/60 italic">
                          Belum ada akses aplikasi
                        </span>
                      ) : (
                        <div className="flex flex-wrap gap-1.5">
                          {user.appAssignments.slice(0, 3).map((assign) => (
                            <div
                              key={assign.clientAppId}
                              className="inline-flex items-center gap-1 text-[11px] bg-neutral-100 dark:bg-slate-800 border border-neutral-200/80 dark:border-slate-700 px-2 py-0.5 rounded"
                            >
                              <span className="font-mono font-semibold text-foreground">
                                {assign.clientId}
                              </span>
                              <span className="text-muted-foreground">:</span>
                              <span className="text-primary font-medium">
                                {assign.roles.join(", ")}
                              </span>
                            </div>
                          ))}
                          {user.appAssignments.length > 3 && (
                            <Badge variant="outline" className="text-[10px]">
                              +{user.appAssignments.length - 3} aplikasi lainnya
                            </Badge>
                          )}
                        </div>
                      )}
                    </TableCell>

                    <TableCell className="align-middle text-xs text-muted-foreground whitespace-nowrap">
                      {formatDate(user.createdAtUtc)}
                    </TableCell>

                    <TableCell className="align-middle text-right">
                      <Link href={`/users/${user.id}`}>
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-8 gap-1 text-xs"
                        >
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

        {/* Footer Pagination */}
        <div className="p-3 border-t border-neutral-200 dark:border-slate-800 text-xs text-muted-foreground flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-neutral-50/30 dark:bg-slate-900/20">
          <div>
            Menampilkan <strong>{items.length}</strong> pengguna pada halaman{" "}
            <strong>{page}</strong> dari{" "}
            <strong>{Math.max(1, totalPages)}</strong> (Total{" "}
            <strong>{totalCount}</strong> pengguna)
          </div>

          {totalPages > 1 && (
            <div className="flex items-center gap-1.5">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-8 gap-1 text-xs"
                disabled={page <= 1}
                onClick={() => navigateWithParams(currentSearch, page - 1)}
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
                onClick={() => navigateWithParams(currentSearch, page + 1)}
              >
                Berikutnya
                <ChevronRight className="w-3.5 h-3.5" />
              </Button>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
