import { formatDate, periodLabel } from "@/components/employees/format";
import { StructureTree } from "@/components/organization/structure-tree";
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { OfficeDetailDto, OfficeStructureDto } from "@/lib/api/employee-types";
import { getErrorMessage } from "@/lib/api/errors";
import { organizationApi } from "@/lib/api/organization";
import { AlertCircle, ArrowLeft, History, Network } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Detail Kantor | Auth Admin Portal",
};

interface OfficeDetailPageProps {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ asOf?: string }>;
}

export default async function OfficeDetailPage({ params, searchParams }: OfficeDetailPageProps) {
  const { id } = await params;
  const query = await searchParams;
  const asOf = /^\d{4}-\d{2}-\d{2}$/.test(query.asOf ?? "") ? query.asOf! : "";

  let office: OfficeDetailDto | null = null;
  let structure: OfficeStructureDto | null = null;
  let fetchError: string | null = null;
  try {
    [office, structure] = await Promise.all([
      organizationApi.office(id, true),
      organizationApi.structure(id, asOf || null),
    ]);
  } catch (err) {
    fetchError = getErrorMessage(err);
  }

  if (fetchError || !office || !structure) {
    return (
      <div className="space-y-4">
        <Link href="/organization/offices">
          <Button variant="outline" className="gap-2 text-xs">
            <ArrowLeft className="w-4 h-4" /> Kembali ke daftar kantor
          </Button>
        </Link>
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">{fetchError ?? "Kantor tidak ditemukan."}</AlertDescription>
        </Alert>
      </div>
    );
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
            <BreadcrumbLink href="/organization/offices">Kantor</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{office.officeCode}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex flex-wrap items-center gap-2">
        <Link href="/organization/offices">
          <Button variant="outline" size="icon" className="h-9 w-9" title="Kembali ke daftar kantor">
            <ArrowLeft className="w-4 h-4" />
          </Button>
        </Link>
        <h1 className="text-2xl font-bold tracking-tight text-foreground">{structure.officeName ?? `Kantor ${office.officeCode}`}</h1>
        <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 px-2 py-0.5 rounded">{office.officeCode}</code>
      </div>

      <Tabs defaultValue="structure" className="space-y-4">
        <TabsList className="bg-neutral-100 dark:bg-slate-800/80 p-1">
          <TabsTrigger value="structure" className="gap-2 text-xs">
            <Network className="w-3.5 h-3.5" /> Struktur
          </TabsTrigger>
          <TabsTrigger value="versions" className="gap-2 text-xs">
            <History className="w-3.5 h-3.5" /> Versi efektif ({office.versions.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="structure" className="space-y-3">
          <form method="get" className="flex items-center gap-2">
            <Input name="asOf" type="date" defaultValue={asOf || structure.asOf} aria-label="Berlaku pada tanggal" className="text-xs max-w-[11rem]" />
            <Button type="submit" variant="outline" className="text-xs">Tampilkan</Button>
            <span className="text-xs text-muted-foreground">Struktur per {formatDate(structure.asOf)}</span>
          </form>
          <Card>
            <CardContent className="p-5">
              <StructureTree units={structure.units} />
            </CardContent>
          </Card>
          <p className="text-[11px] text-muted-foreground">
            Pengelolaan unit, posisi, dan versi efektif dilakukan melalui API admin; antarmuka penyuntingan akan ditambahkan pada tahap berikutnya.
          </p>
        </TabsContent>

        <TabsContent value="versions">
          <Card>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-xs">Nama</TableHead>
                    <TableHead className="text-xs">Klasifikasi</TableHead>
                    <TableHead className="text-xs">Periode</TableHead>
                    <TableHead className="text-xs">Status</TableHead>
                    <TableHead className="text-xs">Revisi</TableHead>
                    <TableHead className="text-xs">Alasan</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {office.versions.map((v) => (
                    <TableRow key={v.id}>
                      <TableCell className="text-xs">{v.name}</TableCell>
                      <TableCell className="text-xs">{v.classificationCode}</TableCell>
                      <TableCell className="text-xs">{periodLabel(v.effectiveFrom, v.effectiveToExclusive)}</TableCell>
                      <TableCell>
                        {v.supersededAtUtc ? (
                          <Badge variant="secondary" className="text-xs">Digantikan</Badge>
                        ) : v.isActive ? (
                          <Badge variant="success" className="text-xs">Aktif</Badge>
                        ) : (
                          <Badge variant="warning" className="text-xs">Diarsipkan</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-xs">#{v.revisionNumber}</TableCell>
                      <TableCell className="text-xs">{v.changeReason}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
