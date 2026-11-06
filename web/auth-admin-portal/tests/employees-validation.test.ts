import test from "node:test";
import assert from "node:assert";

import {
  assignActingSchema,
  assignDefinitiveSchema,
  changeStatusSchema,
  createEmployeeSchema,
  endAssignmentSchema,
  linkEmployeeSchema,
  transferSchema,
  unlinkEmployeeSchema,
} from "../lib/validation/employees.schema.ts";

const GUID = "11111111-1111-4111-8111-111111111111";

test("createEmployeeSchema: NIP, nama, tanggal, dan alasan wajib", () => {
  const ok = createEmployeeSchema.safeParse({
    employeeNumber: "00123",
    employeeFullName: "Budi Santoso",
    effectiveFrom: "2026-11-06",
    reason: "Pegawai baru",
  });
  assert.equal(ok.success, true);

  const empty = createEmployeeSchema.safeParse({
    employeeNumber: "  ",
    employeeFullName: "B",
    effectiveFrom: "06/11/2026",
    reason: "",
  });
  assert.equal(empty.success, false);
  const fields = new Set(empty.error!.issues.map((i) => i.path[0]));
  assert.ok(fields.has("employeeNumber") && fields.has("employeeFullName") && fields.has("effectiveFrom") && fields.has("reason"));
});

test("NIP disimpan sebagai teks: nol di depan dipertahankan dan dibatasi 50 karakter", () => {
  const base = { employeeFullName: "Budi", effectiveFrom: "2026-11-06", reason: "x" };
  const leading = createEmployeeSchema.parse({ ...base, employeeNumber: " 00123 " });
  assert.equal(leading.employeeNumber, "00123");
  assert.equal(createEmployeeSchema.safeParse({ ...base, employeeNumber: "9".repeat(51) }).success, false);
});

test("changeStatusSchema: hanya Active/Resigned dan alasan wajib", () => {
  const base = { effectiveFrom: "2026-12-01", reason: "Pengunduran diri" };
  assert.equal(changeStatusSchema.safeParse({ ...base, statusCode: "Resigned" }).success, true);
  assert.equal(changeStatusSchema.safeParse({ ...base, statusCode: "Retired" }).success, false);
  assert.equal(changeStatusSchema.safeParse({ ...base, statusCode: "Active", reason: "  " }).success, false);
});

test("assignDefinitiveSchema: posisi harus GUID, tanggal berakhir opsional", () => {
  const base = { isPrimary: true, effectiveFrom: "2026-11-06", reason: "Penempatan awal" };
  assert.equal(assignDefinitiveSchema.safeParse({ ...base, positionId: GUID }).success, true);
  assert.equal(assignDefinitiveSchema.safeParse({ ...base, positionId: GUID, effectiveToExclusive: "" }).success, true);
  assert.equal(assignDefinitiveSchema.safeParse({ ...base, positionId: GUID, effectiveToExclusive: "besok" }).success, false);
  assert.equal(assignDefinitiveSchema.safeParse({ ...base, positionId: "" }).success, false);
});

test("transferSchema dan endAssignmentSchema membutuhkan penempatan/posisi yang dipilih", () => {
  assert.equal(
    transferSchema.safeParse({ sourceAssignmentId: GUID, targetPositionId: GUID, effectiveFrom: "2026-12-01", reason: "Mutasi" }).success,
    true
  );
  assert.equal(
    transferSchema.safeParse({ sourceAssignmentId: "", targetPositionId: GUID, effectiveFrom: "2026-12-01", reason: "Mutasi" }).success,
    false
  );
  assert.equal(endAssignmentSchema.safeParse({ assignmentId: GUID, effectiveToExclusive: "2026-12-31", reason: "Selesai" }).success, true);
  assert.equal(endAssignmentSchema.safeParse({ assignmentId: GUID, effectiveToExclusive: "", reason: "Selesai" }).success, false);
});

test("assignActingSchema: jenis Plh/Plt dan referensi SK wajib", () => {
  const base = {
    sourceAssignmentId: GUID,
    targetPositionId: GUID,
    effectiveFrom: "2026-11-06",
    reason: "Kepala seksi cuti",
  };
  assert.equal(assignActingSchema.safeParse({ ...base, assignmentType: "Plh", decisionReference: "SK-001" }).success, true);
  assert.equal(assignActingSchema.safeParse({ ...base, assignmentType: "Plt", decisionReference: "" }).success, false);
  assert.equal(assignActingSchema.safeParse({ ...base, assignmentType: "Pj", decisionReference: "SK-001" }).success, false);
});

test("linkEmployeeSchema dan unlinkEmployeeSchema: pegawai terpilih dan alasan wajib", () => {
  assert.equal(linkEmployeeSchema.safeParse({ employeeId: GUID, reason: "Penautan akun" }).success, true);
  assert.equal(linkEmployeeSchema.safeParse({ employeeId: "", reason: "Penautan akun" }).success, false);
  assert.equal(unlinkEmployeeSchema.safeParse({ reason: "" }).success, false);
});
