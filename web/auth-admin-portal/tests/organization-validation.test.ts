import test from "node:test";
import assert from "node:assert";

import {
  createOfficeSchema,
  createPositionSchema,
  createUnitSchema,
  officeVersionSchema,
  positionVersionSchema,
  unitVersionSchema,
} from "../lib/validation/organization.schema.ts";

const GUID = "11111111-1111-4111-8111-111111111111";
const base = { effectiveFrom: "2026-12-01", reason: "Reorganisasi" };

test("createOfficeSchema: kode kantor tepat tiga digit", () => {
  const ok = { ...base, officeCode: "220", name: "Cabang Jakarta", classificationCode: "CAB-I" };
  assert.equal(createOfficeSchema.safeParse(ok).success, true);
  for (const code of ["22", "2200", "AB1", "", " 22 "]) {
    assert.equal(createOfficeSchema.safeParse({ ...ok, officeCode: code }).success, false, `kode '${code}' seharusnya ditolak`);
  }
  assert.equal(createOfficeSchema.parse({ ...ok, officeCode: " 220 " }).officeCode, "220");
});

test("officeVersionSchema: nama, klasifikasi, status aktif, alasan wajib", () => {
  const ok = { ...base, name: "Cabang", classificationCode: "CAB-II", isActive: true };
  assert.equal(officeVersionSchema.safeParse(ok).success, true);
  assert.equal(officeVersionSchema.safeParse({ ...ok, name: "" }).success, false);
  assert.equal(officeVersionSchema.safeParse({ ...ok, isActive: undefined }).success, false);
  assert.equal(officeVersionSchema.safeParse({ ...ok, reason: "  " }).success, false);
});

test("createUnitSchema: kode unit, jenis, induk opsional", () => {
  const ok = { ...base, officeId: GUID, unitCode: "KEU-SDM", unitType: "Section", name: "Keuangan", parentOrgUnitId: "" };
  assert.equal(createUnitSchema.safeParse(ok).success, true);
  assert.equal(createUnitSchema.safeParse({ ...ok, parentOrgUnitId: GUID }).success, true);
  assert.equal(createUnitSchema.safeParse({ ...ok, parentOrgUnitId: "bukan-guid" }).success, false);
  assert.equal(createUnitSchema.safeParse({ ...ok, unitType: "Cabang" }).success, false);
  for (const code of ["-A", "A B", "", "A".repeat(51)]) {
    assert.equal(createUnitSchema.safeParse({ ...ok, unitCode: code }).success, false, `kode '${code}' seharusnya ditolak`);
  }
});

test("unitVersionSchema: status aktif dan jenis unit wajib", () => {
  const ok = { ...base, unitType: "Division", name: "Divisi", isActive: false, parentOrgUnitId: "" };
  assert.equal(unitVersionSchema.safeParse(ok).success, true);
  assert.equal(unitVersionSchema.safeParse({ ...ok, unitType: undefined }).success, false);
});

test("createPositionSchema: kapasitas kosong atau 1–100000; jalur Line/Functional", () => {
  const ok = {
    ...base,
    orgUnitId: GUID,
    positionCode: "KASI-KEU",
    name: "Kepala Seksi",
    track: "Line",
    isUnitHead: true,
    maxIncumbents: "1",
    reportsToPositionId: "",
    levelId: "",
  };
  assert.equal(createPositionSchema.safeParse(ok).success, true);
  assert.equal(createPositionSchema.safeParse({ ...ok, isUnitHead: false, maxIncumbents: "" }).success, true); // tidak dibatasi
  for (const capacity of ["0", "-1", "1.5", "abc", "100001", "1234567"]) {
    assert.equal(createPositionSchema.safeParse({ ...ok, maxIncumbents: capacity }).success, false, `kapasitas '${capacity}' seharusnya ditolak`);
  }
  assert.equal(createPositionSchema.safeParse({ ...ok, track: "Direct" }).success, false);
  assert.equal(createPositionSchema.safeParse({ ...ok, orgUnitId: "" }).success, false);
  assert.equal(createPositionSchema.safeParse({ ...ok, isUnitHead: undefined }).success, false);
});

test("positionVersionSchema: pelaporan opsional harus GUID bila diisi", () => {
  const ok = {
    ...base,
    name: "Pelaksana",
    track: "Line",
    isUnitHead: false,
    maxIncumbents: "",
    reportsToPositionId: GUID,
    levelId: "",
    isActive: true,
  };
  assert.equal(positionVersionSchema.safeParse(ok).success, true);
  assert.equal(positionVersionSchema.safeParse({ ...ok, reportsToPositionId: "x" }).success, false);
  assert.equal(positionVersionSchema.safeParse({ ...ok, isActive: undefined }).success, false);
});

test("tanggal harus tanggal kalender yang valid, bukan sekadar berformat", () => {
  const ok = { officeCode: "220", name: "Cabang", classificationCode: "CAB-I", reason: "x" };
  assert.equal(createOfficeSchema.safeParse({ ...ok, effectiveFrom: "2026-12-01" }).success, true);
  for (const date of ["2026-02-31", "2026-13-45", "2027-02-29", "0000-00-00"]) {
    assert.equal(createOfficeSchema.safeParse({ ...ok, effectiveFrom: date }).success, false, `tanggal '${date}' seharusnya ditolak`);
  }
  assert.equal(createOfficeSchema.safeParse({ ...ok, effectiveFrom: "2028-02-29" }).success, true); // tahun kabisat
});

test("pimpinan unit wajib berkapasitas tepat 1 pada pesan kolom kapasitas", () => {
  const position = {
    ...base,
    orgUnitId: GUID,
    positionCode: "KASI",
    name: "Kepala Seksi",
    track: "Line",
    isUnitHead: true,
    reportsToPositionId: "",
    levelId: "",
  };
  for (const capacity of ["", "2", "10"]) {
    const result = createPositionSchema.safeParse({ ...position, maxIncumbents: capacity });
    assert.equal(result.success, false, `kapasitas '${capacity}' untuk pimpinan seharusnya ditolak`);
    assert.equal(result.error!.issues[0].path[0], "maxIncumbents");
  }
  assert.equal(createPositionSchema.safeParse({ ...position, maxIncumbents: "1" }).success, true);
  // bukan pimpinan: kapasitas kosong tetap sah (tidak dibatasi)
  assert.equal(createPositionSchema.safeParse({ ...position, isUnitHead: false, maxIncumbents: "" }).success, true);
  assert.equal(positionVersionSchema.safeParse({ ...position, isActive: true, maxIncumbents: "" }).success, false);
});
