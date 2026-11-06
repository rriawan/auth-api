import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDate } from "@/components/employees/format";
import type { PositionSummaryDto, UnitSummaryDto } from "@/lib/api/employee-types";
import {
  CreatePositionDialog,
  CreateUnitDialog,
  PositionVersionDialog,
  UnitVersionDialog,
} from "./org-dialogs";

function ActiveBadge({ active }: { active: boolean | null }) {
  if (active === null) return <Badge variant="secondary" className="text-xs">Belum berlaku</Badge>;
  return active ? (
    <Badge variant="success" className="text-xs">Aktif</Badge>
  ) : (
    <Badge variant="secondary" className="text-xs">Nonaktif</Badge>
  );
}

interface MasterTablesProps {
  officeId: string;
  units: UnitSummaryDto[];
  positions: PositionSummaryDto[];
  today: string;
}

const UNIT_TYPE_LABEL: Record<string, string> = {
  Directorate: "Direktorat",
  Division: "Divisi",
  Department: "Bagian",
  Section: "Seksi",
  WorkGroup: "Kelompok Kerja",
  Other: "Unit lain",
};

/** Daftar unit pada keadaan hari ini, dengan tombol tambah dan versi efektif baru. */
export function UnitsTable({ officeId, units, positions: _positions, today }: MasterTablesProps) {
  void _positions;
  const unitName = new Map(units.map((u) => [u.id, `${u.unitCode} — ${u.name ?? ""}`]));
  return (
    <div className="space-y-3">
      <p className="text-[11px] text-muted-foreground">
        Keadaan per hari ini ({formatDate(today)}); tidak mengikuti tanggal pada tab Struktur. Perubahan berlaku mulai tanggal efektif yang dipilih.
      </p>
      <div className="flex justify-end">
        <CreateUnitDialog officeId={officeId} units={units} today={today} />
      </div>
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="text-xs">Kode</TableHead>
                <TableHead className="text-xs">Nama</TableHead>
                <TableHead className="text-xs">Jenis</TableHead>
                <TableHead className="text-xs">Unit induk</TableHead>
                <TableHead className="text-xs">Status</TableHead>
                <TableHead className="text-xs text-right">Aksi</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {units.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-8 text-xs text-muted-foreground">
                    Belum ada unit pada kantor ini.
                  </TableCell>
                </TableRow>
              ) : (
                units.map((u) => (
                  <TableRow key={u.id}>
                    <TableCell className="font-mono text-xs font-semibold">{u.unitCode}</TableCell>
                    <TableCell className="text-xs">{u.name ?? "—"}</TableCell>
                    <TableCell className="text-xs">{u.unitType ? UNIT_TYPE_LABEL[u.unitType] ?? u.unitType : "—"}</TableCell>
                    <TableCell className="text-xs">{u.parentOrgUnitId ? unitName.get(u.parentOrgUnitId) ?? "—" : "—"}</TableCell>
                    <TableCell><ActiveBadge active={u.isActive} /></TableCell>
                    <TableCell className="text-right">
                      <UnitVersionDialog officeId={officeId} unit={u} units={units} today={today} />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}

/** Daftar posisi pada keadaan hari ini, dengan tombol tambah dan versi efektif baru. */
export function PositionsTable({ officeId, units, positions, today }: MasterTablesProps) {
  const unitName = new Map(units.map((u) => [u.id, `${u.unitCode} — ${u.name ?? ""}`]));
  return (
    <div className="space-y-3">
      <p className="text-[11px] text-muted-foreground">
        Keadaan per hari ini ({formatDate(today)}); tidak mengikuti tanggal pada tab Struktur. Perubahan berlaku mulai tanggal efektif yang dipilih.
      </p>
      <div className="flex justify-end">
        <CreatePositionDialog officeId={officeId} units={units} positions={positions} today={today} />
      </div>
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="text-xs">Kode</TableHead>
                <TableHead className="text-xs">Nama</TableHead>
                <TableHead className="text-xs">Unit</TableHead>
                <TableHead className="text-xs">Kapasitas</TableHead>
                <TableHead className="text-xs">Status</TableHead>
                <TableHead className="text-xs text-right">Aksi</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {positions.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-8 text-xs text-muted-foreground">
                    Belum ada posisi pada kantor ini.
                  </TableCell>
                </TableRow>
              ) : (
                positions.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell className="font-mono text-xs font-semibold">{p.positionCode}</TableCell>
                    <TableCell className="text-xs">
                      {p.name ?? "—"}
                      {p.isUnitHead && <Badge variant="info" className="text-xs ml-2">Pimpinan unit</Badge>}
                      {p.track === "Functional" && <Badge variant="secondary" className="text-xs ml-2">Fungsional</Badge>}
                    </TableCell>
                    <TableCell className="text-xs">{unitName.get(p.orgUnitId) ?? "—"}</TableCell>
                    <TableCell className="text-xs">{p.maxIncumbents ?? "Tidak dibatasi"}</TableCell>
                    <TableCell><ActiveBadge active={p.isActive} /></TableCell>
                    <TableCell className="text-right">
                      <PositionVersionDialog officeId={officeId} position={p} positions={positions} today={today} />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
