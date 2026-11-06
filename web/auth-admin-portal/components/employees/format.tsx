import { Badge } from "@/components/ui/badge";
import type {
  ActingTypeName,
  EmployeeStatusName,
  RecordStateName,
} from "@/lib/api/employee-types";

/** Tanggal efektif (YYYY-MM-DD) dalam format Indonesia tanpa konversi zona waktu. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  const [y, m, d] = value.split("-").map(Number);
  if (!y || !m || !d) return value;
  return new Intl.DateTimeFormat("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(y, m - 1, d)));
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("id-ID", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Asia/Jakarta",
  }).format(date);
}

/** Tanggal bisnis hari ini (Asia/Jakarta) sebagai YYYY-MM-DD, sama dengan zona bisnis backend. */
export function todayJakarta(): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Jakarta" }).format(new Date());
}

/** Periode setengah terbuka [mulai, akhir): akhir bersifat eksklusif, kosong berarti belum ditentukan. */
export function periodLabel(from: string, toExclusive: string | null): string {
  return `${formatDate(from)} – ${toExclusive ? `sebelum ${formatDate(toExclusive)}` : "belum ditentukan"}`;
}

export function EmployeeStatusBadge({ status }: { status: EmployeeStatusName | null }) {
  if (status === "Active") return <Badge variant="success" className="text-xs">Aktif</Badge>;
  if (status === "Resigned") return <Badge variant="danger" className="text-xs">Resign</Badge>;
  return <Badge variant="secondary" className="text-xs">Belum ada status</Badge>;
}

export function RecordStateBadge({ state, superseded }: { state: RecordStateName; superseded?: boolean }) {
  if (superseded) return <Badge variant="secondary" className="text-xs">Digantikan</Badge>;
  if (state === "Cancelled") return <Badge variant="danger" className="text-xs">Dibatalkan</Badge>;
  if (state === "Draft") return <Badge variant="warning" className="text-xs">Draf</Badge>;
  return <Badge variant="info" className="text-xs">Berlaku</Badge>;
}

export function ActingTypeLabel({ type }: { type: ActingTypeName }) {
  return <>{type === "Plh" ? "Plh. (Pelaksana Harian)" : "Plt. (Pelaksana Tugas)"}</>;
}

/** Rentang sudah berakhir pada tanggal bisnis `today` (akhir bersifat eksklusif). */
export function isEnded(toExclusive: string | null, today: string): boolean {
  return toExclusive !== null && toExclusive <= today;
}

/** Rentang berlaku pada tanggal bisnis `today`. */
export function isInForce(from: string, toExclusive: string | null, today: string): boolean {
  return from <= today && !isEnded(toExclusive, today);
}

/** Penanda waktu relatif terhadap hari ini untuk rentang efektif (jadwal masa depan / berakhir). */
export function timingBadge(from: string, toExclusive: string | null, today: string) {
  if (from > today) return <Badge variant="warning" className="text-xs">Terjadwal</Badge>;
  if (toExclusive && toExclusive <= today) return <Badge variant="secondary" className="text-xs">Berakhir</Badge>;
  return <Badge variant="success" className="text-xs">Berjalan</Badge>;
}
