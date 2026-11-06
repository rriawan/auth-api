import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type {
  EmployeeDetailDto,
  EmployeeHistoryDto,
} from "@/lib/api/employee-types";
import { History, MapPin, ShieldCheck, UserRoundCheck } from "lucide-react";
import {
  ActingTypeLabel,
  formatDate,
  formatDateTime,
  periodLabel,
  RecordStateBadge,
  timingBadge,
} from "./format";

interface EmployeeDetailViewProps {
  detail: EmployeeDetailDto;
  history: EmployeeHistoryDto | null;
  historyError: string | null;
  today: string;
}

function Empty({ children }: { children: React.ReactNode }) {
  return <p className="text-xs text-muted-foreground py-8 text-center">{children}</p>;
}

/** Tampilan hanya-baca: semua teks dirender sebagai teks React (tanpa HTML mentah). */
export function EmployeeDetailView({ detail, history, historyError, today }: EmployeeDetailViewProps) {
  const { placements, actingAssignments, statuses } = detail;

  return (
    <Tabs defaultValue="placements" className="space-y-4">
      <TabsList className="bg-neutral-100 dark:bg-slate-800/80 p-1">
        <TabsTrigger value="placements" className="gap-2 text-xs">
          <MapPin className="w-3.5 h-3.5" /> Penempatan ({placements.length})
        </TabsTrigger>
        <TabsTrigger value="acting" className="gap-2 text-xs">
          <ShieldCheck className="w-3.5 h-3.5" /> Plh./Plt. ({actingAssignments.length})
        </TabsTrigger>
        <TabsTrigger value="status" className="gap-2 text-xs">
          <UserRoundCheck className="w-3.5 h-3.5" /> Status ({statuses.length})
        </TabsTrigger>
        <TabsTrigger value="history" className="gap-2 text-xs">
          <History className="w-3.5 h-3.5" /> Riwayat revisi
        </TabsTrigger>
      </TabsList>

      <TabsContent value="placements">
        <Card>
          <CardContent className="p-0">
            {placements.length === 0 ? (
              <Empty>Pegawai ini belum ditempatkan pada jabatan definitif. Ini berbeda dari posisi yang kosong (vacant).</Empty>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-xs">Posisi</TableHead>
                    <TableHead className="text-xs">Jenis</TableHead>
                    <TableHead className="text-xs">Periode</TableHead>
                    <TableHead className="text-xs">Waktu</TableHead>
                    <TableHead className="text-xs">Referensi</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {placements.map((p) => (
                    <TableRow key={p.id}>
                      <TableCell className="font-mono text-xs font-semibold">{p.positionCode}</TableCell>
                      <TableCell>
                        {p.isPrimary ? (
                          <Badge variant="info" className="text-xs">Utama</Badge>
                        ) : (
                          <Badge variant="secondary" className="text-xs">Rangkap</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-xs">{periodLabel(p.effectiveFrom, p.effectiveToExclusive)}</TableCell>
                      <TableCell>{timingBadge(p.effectiveFrom, p.effectiveToExclusive, today)}</TableCell>
                      <TableCell className="text-xs">{p.decisionReference ?? "—"}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="acting">
        <Card>
          <CardContent className="p-0">
            {actingAssignments.length === 0 ? (
              <Empty>Tidak ada penugasan Plh./Plt.</Empty>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-xs">Jenis</TableHead>
                    <TableHead className="text-xs">Posisi dilaksanakan</TableHead>
                    <TableHead className="text-xs">Periode</TableHead>
                    <TableHead className="text-xs">Waktu</TableHead>
                    <TableHead className="text-xs">Referensi SK</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {actingAssignments.map((a) => (
                    <TableRow key={a.id}>
                      <TableCell className="text-xs"><ActingTypeLabel type={a.assignmentType} /></TableCell>
                      <TableCell className="font-mono text-xs font-semibold">{a.targetPositionCode}</TableCell>
                      <TableCell className="text-xs">{periodLabel(a.effectiveFrom, a.effectiveToExclusive)}</TableCell>
                      <TableCell>{timingBadge(a.effectiveFrom, a.effectiveToExclusive, today)}</TableCell>
                      <TableCell className="text-xs">{a.decisionReference}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="status">
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="text-xs">Status</TableHead>
                  <TableHead className="text-xs">Periode</TableHead>
                  <TableHead className="text-xs">Waktu</TableHead>
                  <TableHead className="text-xs">Alasan</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {statuses.map((s) => (
                  <TableRow key={s.id}>
                    <TableCell className="text-xs font-semibold">{s.statusCode === "Active" ? "Aktif" : "Resign"}</TableCell>
                    <TableCell className="text-xs">{periodLabel(s.effectiveFrom, s.effectiveToExclusive)}</TableCell>
                    <TableCell>{timingBadge(s.effectiveFrom, s.effectiveToExclusive, today)}</TableCell>
                    <TableCell className="text-xs">{s.changeReason}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="history">
        <Card>
          <CardContent className="p-0">
            {historyError ? (
              <Empty>{historyError}</Empty>
            ) : !history ? (
              <Empty>Riwayat tidak tersedia.</Empty>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-xs">Jenis</TableHead>
                    <TableHead className="text-xs">Detail</TableHead>
                    <TableHead className="text-xs">Periode</TableHead>
                    <TableHead className="text-xs">Revisi</TableHead>
                    <TableHead className="text-xs">Alasan</TableHead>
                    <TableHead className="text-xs">Dicatat</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {[
                    ...history.statuses.map((s) => ({ ...s, key: s.id, type: "Status", detail: s.statusCode === "Active" ? "Aktif" : "Resign" })),
                    ...history.placements.map((p) => ({ ...p, key: p.id, type: "Penempatan", detail: `${p.positionCode}${p.isPrimary ? " (utama)" : " (rangkap)"}` })),
                    ...history.actingAssignments.map((a) => ({ ...a, key: a.id, type: a.assignmentType.toUpperCase(), detail: a.targetPositionCode })),
                  ]
                    .sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
                    .map((r) => (
                      <TableRow key={r.key}>
                        <TableCell className="text-xs font-semibold">{r.type}</TableCell>
                        <TableCell className="text-xs">
                          {r.detail} <RecordStateBadge state={r.recordState} superseded={r.supersededAtUtc !== null} />
                        </TableCell>
                        <TableCell className="text-xs">{periodLabel(r.effectiveFrom, r.effectiveToExclusive)}</TableCell>
                        <TableCell className="text-xs">#{r.revisionNumber}</TableCell>
                        <TableCell className="text-xs">{r.changeReason}</TableCell>
                        <TableCell className="text-xs">{formatDateTime(r.createdAtUtc)}</TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
        <p className="text-[11px] text-muted-foreground mt-2">
          Berlaku per {formatDate(today)} (zona Asia/Jakarta). Catatan: koreksi tanggal lampau belum didukung pada rilis ini.
        </p>
      </TabsContent>
    </Tabs>
  );
}
