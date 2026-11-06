import { formatDate, todayJakarta } from "@/components/employees/format";
import { CreateOfficeDialog } from "@/components/organization/org-dialogs";
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
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { ClassificationDto, OfficePageDto } from "@/lib/api/employee-types";
import { getErrorMessage } from "@/lib/api/errors";
import { organizationApi } from "@/lib/api/organization";
import { AlertCircle, Building2 } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Kantor | Auth Admin Portal",
  description: "Master kantor, klasifikasi, dan struktur organisasi per tanggal efektif",
};

interface OfficesPageProps {
  searchParams: Promise<{ search?: string; classification?: string; asOf?: string; page?: string }>;
}

export default async function OfficesPage({ searchParams }: OfficesPageProps) {
  const params = await searchParams;
  const search = params.search?.trim() || "";
  const classification = params.classification?.trim() || "";
  const asOf = /^\d{4}-\d{2}-\d{2}$/.test(params.asOf ?? "") ? params.asOf! : "";
  const page = Math.min(1_000_000, Math.max(1, parseInt(params.page || "1", 10) || 1));

  let data: OfficePageDto = { items: [], page, pageSize: 20, totalCount: 0, asOf: asOf };
  let classifications: ClassificationDto[] = [];
  let fetchError: string | null = null;
  try {
    [data, classifications] = await Promise.all([
      organizationApi.offices({ search: search || null, classification: classification || null, asOf: asOf || null, page, pageSize: 20 }),
      organizationApi.classifications(),
    ]);
  } catch (err) {
    fetchError = getErrorMessage(err);
  }

  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
  const link = (nextPage: number) => {
    const qs = new URLSearchParams();
    if (search) qs.set("search", search);
    if (classification) qs.set("classification", classification);
    if (asOf) qs.set("asOf", asOf);
    if (nextPage > 1) qs.set("page", String(nextPage));
    return `/organization/offices${qs.size ? `?${qs}` : ""}`;
  };

  return (
    <div className="space-y-6">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Kantor</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div className="flex items-center gap-3">
        <div className="w-11 h-11 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
          <Building2 className="w-5 h-5" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Master Kantor</h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Nama dan klasifikasi kantor berlaku menurut versi efektif pada tanggal yang dipilih.
          </p>
        </div>
      </div>
        <CreateOfficeDialog classifications={classifications} today={todayJakarta()} />
      </div>

      {fetchError ? (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">{fetchError}</AlertDescription>
        </Alert>
      ) : (
        <Card>
          <CardContent className="p-0">
            <form method="get" className="flex flex-col sm:flex-row gap-3 p-4 border-b">
              <Input name="search" defaultValue={search} placeholder="Cari kode atau nama kantor" className="text-xs sm:max-w-xs" />
              <select name="classification" defaultValue={classification} className="h-9 rounded-md border border-input bg-background px-3 text-xs">
                <option value="">Semua klasifikasi</option>
                {classifications.map((c) => (
                  <option key={c.code} value={c.code}>
                    {c.displayName} ({c.code})
                  </option>
                ))}
              </select>
              <Input name="asOf" type="date" defaultValue={asOf} aria-label="Berlaku pada tanggal" className="text-xs sm:max-w-[11rem]" />
              <Button type="submit" variant="outline" className="text-xs">Terapkan</Button>
            </form>

            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="text-xs">Kode</TableHead>
                  <TableHead className="text-xs">Nama</TableHead>
                  <TableHead className="text-xs">Klasifikasi</TableHead>
                  <TableHead className="text-xs">Status</TableHead>
                  <TableHead className="text-xs text-right">Aksi</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center py-10 text-xs text-muted-foreground">
                      Tidak ada kantor yang cocok.
                    </TableCell>
                  </TableRow>
                ) : (
                  data.items.map((o) => (
                    <TableRow key={o.id}>
                      <TableCell className="font-mono text-xs font-semibold">{o.officeCode}</TableCell>
                      <TableCell className="text-xs">{o.name ?? <span className="text-muted-foreground">Belum berlaku pada {formatDate(data.asOf)}</span>}</TableCell>
                      <TableCell className="text-xs">{o.classificationCode ?? "—"}</TableCell>
                      <TableCell>
                        {o.isActive === null ? (
                          <Badge variant="secondary" className="text-xs">—</Badge>
                        ) : o.isActive ? (
                          <Badge variant="success" className="text-xs">Aktif</Badge>
                        ) : (
                          <Badge variant="secondary" className="text-xs">Diarsipkan</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <Link href={`/organization/offices/${o.id}${asOf ? `?asOf=${asOf}` : ""}`}>
                          <Button variant="outline" size="sm" className="text-xs">Struktur</Button>
                        </Link>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>

            {totalPages > 1 && (
              <div className="flex items-center justify-between p-4 border-t text-xs">
                <span className="text-muted-foreground">
                  Halaman {data.page} dari {totalPages} · {data.totalCount} kantor
                </span>
                <div className="flex gap-2">
                  {data.page > 1 && (
                    <Link href={link(data.page - 1)}>
                      <Button variant="outline" size="sm" className="text-xs">Sebelumnya</Button>
                    </Link>
                  )}
                  {data.page < totalPages && (
                    <Link href={link(data.page + 1)}>
                      <Button variant="outline" size="sm" className="text-xs">Berikutnya</Button>
                    </Link>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
