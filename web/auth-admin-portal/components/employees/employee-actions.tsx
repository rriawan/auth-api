"use client";

import { Can } from "@/components/auth/can";
import type { EmployeeDetailDto } from "@/lib/api/employee-types";
import { ArrowRightLeft, CalendarX2, MapPin, Pencil, PowerOff, ShieldCheck } from "lucide-react";
import {
  assignActingAction,
  assignDefinitiveAction,
  changeStatusAction,
  endActingAction,
  endAssignmentAction,
  transferAction,
  updateEmployeeAction,
} from "./actions/employees";
import { CommandDialog } from "./command-dialog";
import { isEnded, periodLabel } from "./format";

const NO_ROLE_CHANGE =
  "Perubahan ini hanya mengubah data kepegawaian. Role dan permission akun tidak berubah otomatis; atur akses di halaman pengguna bila diperlukan.";

interface EmployeeActionsProps {
  detail: EmployeeDetailDto;
  today: string;
}

/** Tombol aksi pada halaman detail pegawai. Semua perubahan membawa ETag pegawai (If-Match) dari data yang sedang ditampilkan. */
export function EmployeeActions({ detail, today }: EmployeeActionsProps) {
  const { employee, placements, actingAssignments } = detail;
  const version = employee.version;
  const id = employee.id;

  // Penempatan/acting yang sudah berakhir tidak dapat menjadi sumber mutasi, diakhiri, atau dasar acting.
  const openPlacements = placements.filter((p) => !isEnded(p.effectiveToExclusive, today));
  const openActing = actingAssignments.filter((a) => !isEnded(a.effectiveToExclusive, today));
  const placementOptions = openPlacements.map((p) => ({
    value: p.id,
    label: `${p.positionCode}${p.isPrimary ? " (utama)" : ""} — ${periodLabel(p.effectiveFrom, p.effectiveToExclusive)}`,
  }));
  const actingOptions = openActing.map((a) => ({
    value: a.id,
    label: `${a.assignmentType.toUpperCase()} ${a.targetPositionCode} — ${periodLabel(a.effectiveFrom, a.effectiveToExclusive)}`,
  }));
  const hasPlacement = placementOptions.length > 0;
  const hasPrimary = openPlacements.some((p) => p.isPrimary);

  return (
    <div className="flex flex-wrap gap-2">
      <Can permission="employees.write">
        <CommandDialog
          triggerLabel="Ubah identitas"
          triggerIcon={<Pencil className="w-4 h-4" />}
          title="Ubah identitas pegawai"
          description="Nomor pegawai harus unik. Nama resmi tidak otomatis mengubah nama akun."
          fields={[
            { kind: "text", name: "employeeNumber", label: "NIP / nomor pegawai", required: true },
            { kind: "text", name: "employeeFullName", label: "Nama resmi pegawai", required: true },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ employeeNumber: employee.employeeNumber, employeeFullName: employee.employeeFullName, reason: "" }}
          successMessage="Identitas pegawai diperbarui."
          onSubmit={(v) => updateEmployeeAction(id, version, v)}
        />
      </Can>

      <Can permission="employees.lifecycle.write">
        <CommandDialog
          triggerLabel="Ubah status"
          triggerIcon={<PowerOff className="w-4 h-4" />}
          title="Ubah status kepegawaian"
          description="Tanggal efektif adalah hari pertama status berlaku. Untuk resign, itu adalah hari pertama akun tidak boleh digunakan."
          notice="Resign menutup penempatan dan acting mulai tanggal efektif. Akun baru dinonaktifkan oleh proses lifecycle operator; mengaktifkan kembali pegawai tidak mengaktifkan kembali akun."
          fields={[
            {
              kind: "select",
              name: "statusCode",
              label: "Status baru",
              required: true,
              options: [
                { value: "Active", label: "Aktif" },
                { value: "Resigned", label: "Resign" },
              ],
            },
            { kind: "date", name: "effectiveFrom", label: "Tanggal efektif", required: true, help: "Tidak boleh sebelum hari ini." },
            { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ statusCode: "", effectiveFrom: today, decisionReference: "", reason: "" }}
          submitLabel="Terapkan status"
          successMessage="Status pegawai diperbarui."
          onSubmit={(v) => changeStatusAction(id, version, v)}
        />
      </Can>

      <Can permission="employees.assignments.write">
        <CommandDialog
          triggerLabel="Tambah penempatan"
          triggerIcon={<MapPin className="w-4 h-4" />}
          title="Tambah penempatan definitif"
          description="Satu penempatan utama per periode; penempatan tambahan (rangkap) harus tercakup oleh jabatan utama."
          notice={NO_ROLE_CHANGE}
          fields={[
            { kind: "position", name: "positionId", label: "Posisi" },
            { kind: "checkbox", name: "isPrimary", label: "Jabatan utama" },
            { kind: "date", name: "effectiveFrom", label: "Tanggal mulai", required: true },
            { kind: "date", name: "effectiveToExclusive", label: "Tanggal berakhir (opsional)", help: "Eksklusif: hari pertama penempatan sudah tidak berlaku." },
            { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ positionId: "", isPrimary: !hasPrimary, effectiveFrom: today, effectiveToExclusive: "", decisionReference: "", reason: "" }}
          successMessage="Penempatan ditambahkan."
          onSubmit={(v) => assignDefinitiveAction(id, version, v)}
        />

        <CommandDialog
          triggerLabel="Mutasi / promosi"
          triggerIcon={<ArrowRightLeft className="w-4 h-4" />}
          title="Mutasi atau promosi"
          description="Menutup penempatan asal dan membuka penempatan di posisi tujuan pada tanggal efektif, secara atomik."
          notice={NO_ROLE_CHANGE}
          disabled={!hasPlacement}
          fields={[
            { kind: "select", name: "sourceAssignmentId", label: "Penempatan asal", options: placementOptions, required: true },
            { kind: "position", name: "targetPositionId", label: "Posisi tujuan" },
            { kind: "date", name: "effectiveFrom", label: "Tanggal efektif", required: true },
            { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ sourceAssignmentId: "", targetPositionId: "", effectiveFrom: today, decisionReference: "", reason: "" }}
          submitLabel="Terapkan mutasi"
          successMessage="Mutasi diterapkan."
          onSubmit={(v) => transferAction(id, version, v)}
        />

        <CommandDialog
          triggerLabel="Akhiri penempatan"
          triggerIcon={<CalendarX2 className="w-4 h-4" />}
          triggerVariant="outline"
          title="Akhiri penempatan"
          description="Tanggal berakhir bersifat eksklusif. Penempatan yang masih menjadi dasar acting tidak dapat diakhiri sebelum acting-nya diakhiri."
          disabled={!hasPlacement}
          fields={[
            { kind: "select", name: "assignmentId", label: "Penempatan", options: placementOptions, required: true },
            { kind: "date", name: "effectiveToExclusive", label: "Tanggal berakhir (eksklusif)", required: true },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ assignmentId: "", effectiveToExclusive: today, reason: "" }}
          submitLabel="Akhiri"
          successMessage="Penempatan diakhiri."
          onSubmit={(v) => endAssignmentAction(id, version, v)}
        />

        <CommandDialog
          triggerLabel="Tambah Plh./Plt."
          triggerIcon={<ShieldCheck className="w-4 h-4" />}
          title="Tambah penugasan Plh./Plt."
          description="Penugasan sementara pada posisi lain; wajib punya penempatan definitif asal, periode, dan referensi SK."
          notice="Plh./Plt. tidak memberi atau mencabut role dan permission secara otomatis. Role manual tidak mengikuti berakhirnya acting."
          disabled={!hasPlacement}
          fields={[
            { kind: "select", name: "sourceAssignmentId", label: "Penempatan definitif asal", options: placementOptions, required: true },
            { kind: "position", name: "targetPositionId", label: "Posisi yang dilaksanakan" },
            {
              kind: "select",
              name: "assignmentType",
              label: "Jenis",
              required: true,
              options: [
                { value: "Plh", label: "Plh. (Pelaksana Harian)" },
                { value: "Plt", label: "Plt. (Pelaksana Tugas)" },
              ],
            },
            { kind: "date", name: "effectiveFrom", label: "Tanggal mulai", required: true },
            { kind: "date", name: "effectiveToExclusive", label: "Tanggal berakhir (opsional)" },
            { kind: "text", name: "decisionReference", label: "Referensi SK", required: true },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ sourceAssignmentId: "", targetPositionId: "", assignmentType: "", effectiveFrom: today, effectiveToExclusive: "", decisionReference: "", reason: "" }}
          successMessage="Penugasan Plh./Plt. ditambahkan."
          onSubmit={(v) => assignActingAction(id, version, v)}
        />

        <CommandDialog
          triggerLabel="Akhiri Plh./Plt."
          triggerIcon={<CalendarX2 className="w-4 h-4" />}
          title="Akhiri penugasan Plh./Plt."
          disabled={actingOptions.length === 0}
          fields={[
            { kind: "select", name: "actingId", label: "Penugasan", options: actingOptions, required: true },
            { kind: "date", name: "effectiveToExclusive", label: "Tanggal berakhir (eksklusif)", required: true },
            { kind: "textarea", name: "reason", label: "Alasan", required: true },
          ]}
          initial={{ actingId: "", effectiveToExclusive: today, reason: "" }}
          submitLabel="Akhiri"
          successMessage="Penugasan diakhiri."
          onSubmit={(v) => endActingAction(id, version, v)}
        />
      </Can>
    </div>
  );
}
