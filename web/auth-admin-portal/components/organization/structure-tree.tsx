import { Badge } from "@/components/ui/badge";
import type { StructureUnitNode } from "@/lib/api/employee-types";
import { Building, UserRound } from "lucide-react";

const UNIT_TYPE_LABEL: Record<string, string> = {
  Directorate: "Direktorat",
  Division: "Divisi",
  Department: "Bagian",
  Section: "Seksi",
  WorkGroup: "Kelompok Kerja",
  Other: "Unit",
};

/**
 * Pohon unit dan posisi pada satu tanggal. Posisi vacant berarti tidak ada pemegang definitif berlaku
 * (bukan cuti/absen); Plh./Plt. tidak menghapus fakta bahwa posisi masih vacant.
 * Semua nilai dirender sebagai teks React (tanpa HTML mentah).
 */
export function StructureTree({ units, depth = 0 }: { units: StructureUnitNode[]; depth?: number }) {
  if (units.length === 0 && depth === 0) {
    return <p className="text-xs text-muted-foreground py-6 text-center">Belum ada unit aktif pada tanggal ini.</p>;
  }

  return (
    <ul className={depth === 0 ? "space-y-3" : "space-y-3 border-l pl-4 ml-2"}>
      {units.map((unit) => (
        <li key={unit.id} className="space-y-2">
          <div className="flex flex-wrap items-center gap-2 text-sm font-semibold">
            <Building className="w-4 h-4 text-muted-foreground" />
            {unit.name}
            <code className="text-[11px] font-mono bg-neutral-100 dark:bg-slate-800 px-1.5 py-0.5 rounded">{unit.unitCode}</code>
            <Badge variant="secondary" className="text-xs">{UNIT_TYPE_LABEL[unit.unitType] ?? unit.unitType}</Badge>
          </div>

          {unit.positions.length > 0 && (
            <ul className="space-y-1 pl-6">
              {unit.positions.map((p) => (
                <li key={p.id} className="flex flex-wrap items-center gap-2 text-xs">
                  <UserRound className="w-3.5 h-3.5 text-muted-foreground" />
                  <span className="font-mono font-semibold">{p.positionCode}</span>
                  <span>{p.name}</span>
                  {p.isUnitHead && <Badge variant="info" className="text-xs">Pimpinan unit</Badge>}
                  {p.track === "Functional" && <Badge variant="secondary" className="text-xs">Fungsional</Badge>}
                  <span className="text-muted-foreground">
                    {p.definitiveIncumbents}
                    {p.maxIncumbents !== null ? ` / ${p.maxIncumbents}` : ""} pemegang
                  </span>
                  {p.isVacant && <Badge variant="warning" className="text-xs">Vacant</Badge>}
                </li>
              ))}
            </ul>
          )}

          {unit.children.length > 0 && <StructureTree units={unit.children} depth={depth + 1} />}
        </li>
      ))}
    </ul>
  );
}
