import { z } from "zod";

const isRealDate = (value: string) => {
  const parsed = new Date(`${value}T00:00:00Z`);
  return !Number.isNaN(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value;
};

const dateRule = (label: string) =>
  z
    .string({ required_error: `${label} wajib diisi` })
    .regex(/^\d{4}-\d{2}-\d{2}$/, {
      message: `${label} harus berformat tanggal (YYYY-MM-DD)`,
    })
    .refine(isRealDate, { message: `${label} bukan tanggal yang valid` });

const reasonRule = z
  .string({ required_error: "Alasan wajib diisi" })
  .trim()
  .min(1, { message: "Alasan wajib diisi" })
  .max(500, { message: "Alasan maksimal 500 karakter" });

const decisionRule = z
  .string()
  .trim()
  .max(200, { message: "Referensi keputusan maksimal 200 karakter" })
  .optional();

const nameRule = z
  .string({ required_error: "Nama wajib diisi" })
  .trim()
  .min(1, { message: "Nama wajib diisi" })
  .max(200, { message: "Nama maksimal 200 karakter" });

const optionalGuid = z
  .string()
  .refine((v) => v === "" || /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v), {
    message: "Pilihan tidak valid",
  })
  .optional();

const guidRule = (label: string) =>
  z
    .string({ required_error: `${label} wajib dipilih` })
    .uuid({ message: `${label} wajib dipilih` });

/** Kode unit/posisi: 1–50 karakter huruf, angka, titik, garis bawah, atau strip (selaras backend). */
export const orgCodeRule = z
  .string({ required_error: "Kode wajib diisi" })
  .trim()
  .regex(/^[A-Za-z0-9][A-Za-z0-9_.-]{0,49}$/, {
    message: "Kode 1–50 karakter: huruf, angka, titik, garis bawah, atau strip",
  });

/** Kode kantor: tepat tiga digit. */
export const officeCodeRule = z
  .string({ required_error: "Kode kantor wajib diisi" })
  .trim()
  .regex(/^\d{3}$/, { message: "Kode kantor harus tepat tiga digit" });

export const classificationRule = z
  .string({ required_error: "Klasifikasi wajib dipilih" })
  .trim()
  .min(1, { message: "Klasifikasi wajib dipilih" })
  .max(20);

export const unitTypes = [
  "Directorate",
  "Division",
  "Department",
  "Section",
  "WorkGroup",
  "Other",
] as const;

const unitTypeRule = z.enum(unitTypes, { required_error: "Jenis unit wajib dipilih" });
const trackRule = z.enum(["Line", "Functional"], { required_error: "Jalur posisi wajib dipilih" });

/** Kapasitas kosong = tidak dibatasi; selain itu bilangan bulat 1–100000. */
const capacityRule = z
  .string()
  .trim()
  .regex(/^(\d{1,6})?$/, { message: "Kapasitas harus bilangan bulat positif atau kosong" })
  .refine((v) => v === "" || (Number(v) >= 1 && Number(v) <= 100000), {
    message: "Kapasitas harus 1–100000, atau kosong untuk tidak dibatasi",
  })
  .optional();

export const createOfficeSchema = z.object({
  officeCode: officeCodeRule,
  name: nameRule,
  classificationCode: classificationRule,
  effectiveFrom: dateRule("Tanggal mulai"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type CreateOfficeFormData = z.infer<typeof createOfficeSchema>;

export const officeVersionSchema = z.object({
  name: nameRule,
  classificationCode: classificationRule,
  isActive: z.boolean(),
  effectiveFrom: dateRule("Tanggal efektif"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type OfficeVersionFormData = z.infer<typeof officeVersionSchema>;

export const createUnitSchema = z.object({
  officeId: guidRule("Kantor"),
  unitCode: orgCodeRule,
  parentOrgUnitId: optionalGuid,
  unitType: unitTypeRule,
  name: nameRule,
  effectiveFrom: dateRule("Tanggal mulai"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type CreateUnitFormData = z.infer<typeof createUnitSchema>;

export const unitVersionSchema = z.object({
  parentOrgUnitId: optionalGuid,
  unitType: unitTypeRule,
  name: nameRule,
  isActive: z.boolean(),
  effectiveFrom: dateRule("Tanggal efektif"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type UnitVersionFormData = z.infer<typeof unitVersionSchema>;

export const createPositionSchema = z.object({
  orgUnitId: guidRule("Unit"),
  positionCode: orgCodeRule,
  name: nameRule,
  track: trackRule,
  isUnitHead: z.boolean(),
  maxIncumbents: capacityRule,
  levelId: optionalGuid,
  reportsToPositionId: optionalGuid,
  effectiveFrom: dateRule("Tanggal mulai"),
  reason: reasonRule,
  decisionReference: decisionRule,
})
.superRefine((value, ctx) => {
  if (value.isUnitHead && (value.maxIncumbents ?? "").trim() !== "1") {
    ctx.addIssue({ code: "custom", path: ["maxIncumbents"], message: "Pimpinan unit wajib berkapasitas tepat 1" });
  }
});
export type CreatePositionFormData = z.infer<typeof createPositionSchema>;

export const positionVersionSchema = z.object({
  name: nameRule,
  track: trackRule,
  isUnitHead: z.boolean(),
  maxIncumbents: capacityRule,
  levelId: optionalGuid,
  reportsToPositionId: optionalGuid,
  isActive: z.boolean(),
  effectiveFrom: dateRule("Tanggal efektif"),
  reason: reasonRule,
  decisionReference: decisionRule,
})
.superRefine((value, ctx) => {
  if (value.isUnitHead && (value.maxIncumbents ?? "").trim() !== "1") {
    ctx.addIssue({ code: "custom", path: ["maxIncumbents"], message: "Pimpinan unit wajib berkapasitas tepat 1" });
  }
});
export type PositionVersionFormData = z.infer<typeof positionVersionSchema>;
