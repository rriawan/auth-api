"use client";

import { Can } from "@/components/auth/can";
import { CommandDialog } from "@/components/employees/command-dialog";
import type {
  ClassificationDto,
  OfficeVersionDto,
  PositionSummaryDto,
  UnitSummaryDto,
} from "@/lib/api/employee-types";
import { Building2, GitBranchPlus, Network, Pencil, Plus } from "lucide-react";
import {
  addOfficeVersionAction,
  addPositionVersionAction,
  addUnitVersionAction,
  createOfficeAction,
  createPositionAction,
  createUnitAction,
} from "./actions/organization";

const UNIT_TYPES = [
  { value: "Directorate", label: "Direktorat" },
  { value: "Division", label: "Divisi" },
  { value: "Department", label: "Bagian" },
  { value: "Section", label: "Seksi" },
  { value: "WorkGroup", label: "Kelompok Kerja" },
  { value: "Other", label: "Unit lain" },
];

const TRACKS = [
  { value: "Line", label: "Lini (struktural)" },
  { value: "Functional", label: "Fungsional" },
];

const EFFECTIVE_HELP =
  "Berlaku mulai tanggal ini dan tidak boleh sebelum hari ini. Versi sebelumnya tetap tersimpan sebagai riwayat.";

const NOTICE =
  "Perubahan master memeriksa seluruh struktur kantor di semua periode (siklus, kapasitas, kantor/unit aktif) dan penempatan pegawai terdampak; bila ada yang tidak sah, seluruh perubahan dibatalkan.";

const classificationOptions = (items: ClassificationDto[]) =>
  items.map((c) => ({ value: c.code, label: `${c.displayName} (${c.code})` }));

// ---- kantor -----------------------------------------------------------------------------------------------

export function CreateOfficeDialog({ classifications, today }: { classifications: ClassificationDto[]; today: string }) {
  return (
    <Can permission="organization.write">
      <CommandDialog
        triggerLabel="Tambah kantor"
        triggerIcon={<Building2 className="w-4 h-4" />}
        triggerVariant="default"
        title="Tambah kantor"
        description="Kode kantor tepat tiga digit dan tidak dapat diubah. Nama dan klasifikasi dikelola sebagai versi efektif."
        notice={NOTICE}
        fields={[
          { kind: "text", name: "officeCode", label: "Kode kantor (3 digit)", required: true },
          { kind: "text", name: "name", label: "Nama kantor", required: true },
          { kind: "select", name: "classificationCode", label: "Klasifikasi", options: classificationOptions(classifications), required: true },
          { kind: "date", name: "effectiveFrom", label: "Berlaku mulai", required: true, help: EFFECTIVE_HELP },
          { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
          { kind: "textarea", name: "reason", label: "Alasan", required: true },
        ]}
        initial={{ officeCode: "", name: "", classificationCode: "", effectiveFrom: today, decisionReference: "", reason: "" }}
        successMessage="Kantor ditambahkan."
        onSubmit={(v) => createOfficeAction(v)}
      />
    </Can>
  );
}

export function OfficeVersionDialog({
  officeId,
  version,
  current,
  classifications,
  today,
}: {
  officeId: string;
  version: string;
  current: OfficeVersionDto | null;
  classifications: ClassificationDto[];
  today: string;
}) {
  return (
    <Can permission="organization.write">
      <CommandDialog
        triggerLabel="Versi baru kantor"
        triggerIcon={<Pencil className="w-4 h-4" />}
        title="Versi efektif baru kantor"
        description="Ubah nama, klasifikasi, atau arsipkan kantor mulai tanggal tertentu."
        notice={`${NOTICE} Mengarsipkan kantor ditolak bila masih ada unit aktif.`}
        fields={[
          { kind: "text", name: "name", label: "Nama kantor", required: true },
          { kind: "select", name: "classificationCode", label: "Klasifikasi", options: classificationOptions(classifications), required: true },
          { kind: "checkbox", name: "isActive", label: "Kantor aktif", help: "Hilangkan centang untuk mengarsipkan mulai tanggal efektif." },
          { kind: "date", name: "effectiveFrom", label: "Berlaku mulai", required: true, help: EFFECTIVE_HELP },
          { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
          { kind: "textarea", name: "reason", label: "Alasan", required: true },
        ]}
        initial={{
          name: current?.name ?? "",
          classificationCode: current?.classificationCode ?? "",
          isActive: current?.isActive ?? true,
          effectiveFrom: today,
          decisionReference: "",
          reason: "",
        }}
        successMessage="Versi kantor ditambahkan."
        onSubmit={(v) => addOfficeVersionAction(officeId, version, v)}
      />
    </Can>
  );
}

// ---- unit -------------------------------------------------------------------------------------------------

/** Unit aktif; induk saat ini tetap tampil (ditandai nonaktif) agar nilai yang akan dikirim selalu terlihat. */
function parentOptions(units: UnitSummaryDto[], excludeId?: string, keepId?: string | null) {
  return units
    .filter((u) => u.id !== excludeId && (u.isActive || u.id === keepId))
    .map((u) => ({ value: u.id, label: `${u.unitCode} — ${u.name ?? ""}${u.isActive ? "" : " (nonaktif)"}` }));
}

export function CreateUnitDialog({ officeId, units, today }: { officeId: string; units: UnitSummaryDto[]; today: string }) {
  return (
    <Can permission="organization.write">
      <CommandDialog
        triggerLabel="Tambah unit"
        triggerIcon={<GitBranchPlus className="w-4 h-4" />}
        triggerVariant="default"
        title="Tambah unit organisasi"
        description="Unit induk harus berada di kantor yang sama dan aktif sepanjang periode unit."
        notice={NOTICE}
        fields={[
          { kind: "text", name: "unitCode", label: "Kode unit", required: true, help: "1–50 karakter: huruf, angka, titik, garis bawah, atau strip. Tidak dapat diubah." },
          { kind: "text", name: "name", label: "Nama unit", required: true },
          { kind: "select", name: "unitType", label: "Jenis unit", options: UNIT_TYPES, required: true },
          { kind: "select", name: "parentOrgUnitId", label: "Unit induk (opsional)", options: parentOptions(units) },
          { kind: "date", name: "effectiveFrom", label: "Berlaku mulai", required: true, help: EFFECTIVE_HELP },
          { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
          { kind: "textarea", name: "reason", label: "Alasan", required: true },
        ]}
        initial={{ unitCode: "", name: "", unitType: "", parentOrgUnitId: "", effectiveFrom: today, decisionReference: "", reason: "" }}
        successMessage="Unit ditambahkan."
        onSubmit={(v) => createUnitAction(officeId, v)}
      />
    </Can>
  );
}

export function UnitVersionDialog({
  officeId,
  unit,
  units,
  today,
}: {
  officeId: string;
  unit: UnitSummaryDto;
  units: UnitSummaryDto[];
  today: string;
}) {
  return (
    <Can permission="organization.write">
      <CommandDialog
        triggerLabel="Versi baru"
        triggerIcon={<Pencil className="w-4 h-4" />}
        title={`Versi efektif baru unit ${unit.unitCode}`}
        disabled={unit.isActive === null}
        notice={`${NOTICE} Menonaktifkan unit ditolak bila masih ada posisi aktif atau pegawai ditempatkan di dalamnya.`}
        fields={[
          { kind: "text", name: "name", label: "Nama unit", required: true },
          { kind: "select", name: "unitType", label: "Jenis unit", options: UNIT_TYPES, required: true },
          { kind: "select", name: "parentOrgUnitId", label: "Unit induk (opsional)", options: parentOptions(units, unit.id, unit.parentOrgUnitId) },
          { kind: "checkbox", name: "isActive", label: "Unit aktif" },
          { kind: "date", name: "effectiveFrom", label: "Berlaku mulai", required: true, help: EFFECTIVE_HELP },
          { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
          { kind: "textarea", name: "reason", label: "Alasan", required: true },
        ]}
        initial={{
          name: unit.name ?? "",
          unitType: unit.unitType ?? "",
          parentOrgUnitId: unit.parentOrgUnitId ?? "",
          isActive: unit.isActive ?? true,
          effectiveFrom: today,
          decisionReference: "",
          reason: "",
        }}
        successMessage="Versi unit ditambahkan."
        onSubmit={(v) => addUnitVersionAction(officeId, unit.id, unit.version, v)}
      />
    </Can>
  );
}

// ---- posisi -----------------------------------------------------------------------------------------------

/** Posisi aktif; atasan saat ini tetap tampil (ditandai nonaktif) agar nilai yang akan dikirim selalu terlihat. */
function supervisorOptions(positions: PositionSummaryDto[], excludeId?: string, keepId?: string | null) {
  return positions
    .filter((p) => p.id !== excludeId && (p.isActive || p.id === keepId))
    .map((p) => ({ value: p.id, label: `${p.positionCode} — ${p.name ?? ""}${p.isActive ? "" : " (nonaktif)"}` }));
}

const POSITION_NOTICE =
  `${NOTICE} Pimpinan unit wajib berkapasitas tepat 1 dan hanya satu pimpinan aktif per unit. Kapasitas kosong berarti tidak dibatasi (mis. Pelaksana).`;

export function CreatePositionDialog({
  officeId,
  units,
  positions,
  today,
}: {
  officeId: string;
  units: UnitSummaryDto[];
  positions: PositionSummaryDto[];
  today: string;
}) {
  return (
    <Can permission="organization.write">
      <CommandDialog
        triggerLabel="Tambah posisi"
        triggerIcon={<Plus className="w-4 h-4" />}
        triggerVariant="default"
        title="Tambah posisi"
        description="Satu posisi dapat dipegang banyak pegawai bila kapasitasnya tidak dibatasi; jangan membuat posisi baru per pegawai."
        notice={POSITION_NOTICE}
        disabled={units.length === 0}
        fields={[
          {
            kind: "select",
            name: "orgUnitId",
            label: "Unit",
            required: true,
            options: units.filter((u) => u.isActive).map((u) => ({ value: u.id, label: `${u.unitCode} — ${u.name ?? ""}` })),
          },
          { kind: "text", name: "positionCode", label: "Kode posisi", required: true, help: "1–50 karakter. Tidak dapat diubah." },
          { kind: "text", name: "name", label: "Nama posisi", required: true },
          { kind: "select", name: "track", label: "Jalur", options: TRACKS, required: true },
          { kind: "checkbox", name: "isUnitHead", label: "Pimpinan unit" },
          { kind: "text", name: "maxIncumbents", label: "Kapasitas pemegang (opsional)", help: "Kosong = tidak dibatasi. Pimpinan unit wajib diisi 1." },
          { kind: "select", name: "reportsToPositionId", label: "Melapor ke posisi (opsional)", options: supervisorOptions(positions) },
          { kind: "date", name: "effectiveFrom", label: "Berlaku mulai", required: true, help: EFFECTIVE_HELP },
          { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
          { kind: "textarea", name: "reason", label: "Alasan", required: true },
        ]}
        initial={{
          orgUnitId: "",
          positionCode: "",
          name: "",
          track: "Line",
          isUnitHead: false,
          maxIncumbents: "",
          reportsToPositionId: "",
          levelId: "",
          effectiveFrom: today,
          decisionReference: "",
          reason: "",
        }}
        successMessage="Posisi ditambahkan."
        onSubmit={(v) => createPositionAction(officeId, v)}
      />
    </Can>
  );
}

export function PositionVersionDialog({
  officeId,
  position,
  positions,
  today,
}: {
  officeId: string;
  position: PositionSummaryDto;
  positions: PositionSummaryDto[];
  today: string;
}) {
  return (
    <Can permission="organization.write">
      <CommandDialog
        triggerLabel="Versi baru"
        triggerIcon={<Network className="w-4 h-4" />}
        title={`Versi efektif baru posisi ${position.positionCode}`}
        disabled={position.isActive === null}
        notice={`${POSITION_NOTICE} Menonaktifkan atau mengecilkan kapasitas ditolak bila pegawai yang ditempatkan akan terdampak.`}
        fields={[
          { kind: "text", name: "name", label: "Nama posisi", required: true },
          { kind: "select", name: "track", label: "Jalur", options: TRACKS, required: true },
          { kind: "checkbox", name: "isUnitHead", label: "Pimpinan unit" },
          { kind: "text", name: "maxIncumbents", label: "Kapasitas pemegang (opsional)", help: "Kosong = tidak dibatasi. Pimpinan unit wajib diisi 1." },
          { kind: "select", name: "reportsToPositionId", label: "Melapor ke posisi (opsional)", options: supervisorOptions(positions, position.id, position.reportsToPositionId) },
          { kind: "checkbox", name: "isActive", label: "Posisi aktif" },
          { kind: "date", name: "effectiveFrom", label: "Berlaku mulai", required: true, help: EFFECTIVE_HELP },
          { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
          { kind: "textarea", name: "reason", label: "Alasan", required: true },
        ]}
        initial={{
          name: position.name ?? "",
          track: position.track ?? "Line",
          isUnitHead: position.isUnitHead ?? false,
          maxIncumbents: position.maxIncumbents === null ? "" : String(position.maxIncumbents),
          reportsToPositionId: position.reportsToPositionId ?? "",
          levelId: position.levelId ?? "",
          isActive: position.isActive ?? true,
          effectiveFrom: today,
          decisionReference: "",
          reason: "",
        }}
        successMessage="Versi posisi ditambahkan."
        onSubmit={(v) => addPositionVersionAction(officeId, position.id, position.version, v)}
      />
    </Can>
  );
}
