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
import type { EmployeePageDto } from "@/lib/api/employee-types";
import { ChevronLeft, ChevronRight, Search, UserRound, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { EmployeeStatusBadge } from "./format";

interface EmployeesTableProps {
  pagedData: EmployeePageDto;
  currentSearch: string;
}

export function EmployeesTable({ pagedData, currentSearch }: EmployeesTableProps) {
  const router = useRouter();
  const [searchInput, setSearchInput] = useState(currentSearch);
  const { items, page, pageSize, totalCount } = pagedData;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const go = (search: string, nextPage: number) => {
    const params = new URLSearchParams();
    if (search.trim()) params.set("search", search.trim());
    if (nextPage > 1) params.set("page", nextPage.toString());
    if (pageSize !== 20) params.set("pageSize", pageSize.toString());
    const qs = params.toString();
    router.push(`/employees${qs ? `?${qs}` : ""}`);
  };

  return (
    <Card>
      <CardContent className="p-0">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 border-b">
          <form
            onSubmit={(e) => {
              e.preventDefault();
              go(searchInput, 1);
            }}
            className="relative w-full sm:max-w-sm"
          >
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="Cari NIP atau nama pegawai"
              className="pl-9 pr-9 text-xs"
            />
            {searchInput && (
              <button
                type="button"
                onClick={() => {
                  setSearchInput("");
                  go("", 1);
                }}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                aria-label="Hapus pencarian"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </form>
          <p className="text-xs text-muted-foreground">{totalCount} pegawai</p>
        </div>

        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="text-xs">NIP</TableHead>
              <TableHead className="text-xs">Nama resmi</TableHead>
              <TableHead className="text-xs">Status hari ini</TableHead>
              <TableHead className="text-xs">Akun</TableHead>
              <TableHead className="text-xs text-right">Aksi</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-10 text-xs text-muted-foreground">
                  <UserRound className="w-8 h-8 mx-auto mb-2 opacity-40" />
                  {currentSearch ? "Tidak ada pegawai yang cocok dengan pencarian." : "Belum ada pegawai terdaftar."}
                </TableCell>
              </TableRow>
            ) : (
              items.map((e) => (
                <TableRow key={e.id}>
                  <TableCell className="font-mono text-xs font-semibold">{e.employeeNumber}</TableCell>
                  <TableCell className="text-xs">{e.employeeFullName}</TableCell>
                  <TableCell>
                    <EmployeeStatusBadge status={e.currentStatus} />
                  </TableCell>
                  <TableCell>
                    {e.linkedUserId ? (
                      <Link href={`/users/${e.linkedUserId}`}>
                        <Badge variant="info" className="text-xs cursor-pointer">Tertaut</Badge>
                      </Link>
                    ) : (
                      <Badge variant="secondary" className="text-xs">Belum ada akun</Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <Link href={`/employees/${e.id}`}>
                      <Button variant="outline" size="sm" className="text-xs">Detail</Button>
                    </Link>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>

        {totalPages > 1 && (
          <div className="flex items-center justify-between p-4 border-t">
            <p className="text-xs text-muted-foreground">
              Halaman {page} dari {totalPages}
            </p>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => go(currentSearch, page - 1)} className="gap-1 text-xs">
                <ChevronLeft className="w-4 h-4" /> Sebelumnya
              </Button>
              <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => go(currentSearch, page + 1)} className="gap-1 text-xs">
                Berikutnya <ChevronRight className="w-4 h-4" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
