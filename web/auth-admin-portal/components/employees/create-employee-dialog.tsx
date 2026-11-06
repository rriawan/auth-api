"use client";

import { UserPlus } from "lucide-react";
import { createEmployeeAction } from "./actions/employees";
import { CommandDialog } from "./command-dialog";

/** Mendaftarkan pegawai baru. Tidak membuat akun dan tidak mengubah role siapa pun. */
export function CreateEmployeeDialog({ today }: { today: string }) {
  return (
    <CommandDialog
      triggerLabel="Tambah Pegawai"
      triggerIcon={<UserPlus className="w-4 h-4" />}
      triggerVariant="default"
      title="Tambah Pegawai"
      description="Daftarkan pegawai pada master pegawai. Penempatan jabatan dan penautan akun dilakukan terpisah."
      notice="Membuat pegawai tidak membuat akun dan tidak memberi akses aplikasi apa pun."
      fields={[
        { kind: "text", name: "employeeNumber", label: "NIP / nomor pegawai", required: true, help: "Disimpan sebagai teks; maksimal 50 karakter." },
        { kind: "text", name: "employeeFullName", label: "Nama resmi pegawai", required: true },
        { kind: "date", name: "effectiveFrom", label: "Tanggal mulai aktif", required: true, help: "Tidak boleh sebelum hari ini." },
        { kind: "textarea", name: "reason", label: "Alasan", required: true },
      ]}
      initial={{ employeeNumber: "", employeeFullName: "", effectiveFrom: today, reason: "" }}
      successMessage="Pegawai berhasil didaftarkan."
      onSubmit={(v) => createEmployeeAction(v)}
    />
  );
}
