import { z } from "zod";

const dateRule = (label: string) =>
  z
    .string({ required_error: `${label} wajib diisi` })
    .regex(/^\d{4}-\d{2}-\d{2}$/, {
      message: `${label} harus berformat tanggal (YYYY-MM-DD)`,
    });

const optionalDateRule = (label: string) =>
  z
    .string()
    .regex(/^(\d{4}-\d{2}-\d{2})?$/, {
      message: `${label} harus berformat tanggal (YYYY-MM-DD)`,
    })
    .optional();

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

const guidRule = (label: string) =>
  z
    .string({ required_error: `${label} wajib dipilih` })
    .uuid({ message: `${label} wajib dipilih` });

export const employeeNumberRule = z
  .string({ required_error: "Nomor pegawai wajib diisi" })
  .trim()
  .min(1, { message: "Nomor pegawai wajib diisi" })
  .max(50, { message: "Nomor pegawai maksimal 50 karakter" });

export const employeeNameRule = z
  .string({ required_error: "Nama resmi pegawai wajib diisi" })
  .trim()
  .min(2, { message: "Nama resmi minimal 2 karakter" })
  .max(200, { message: "Nama resmi maksimal 200 karakter" });

export const createEmployeeSchema = z.object({
  employeeNumber: employeeNumberRule,
  employeeFullName: employeeNameRule,
  effectiveFrom: dateRule("Tanggal mulai aktif"),
  reason: reasonRule,
});
export type CreateEmployeeFormData = z.infer<typeof createEmployeeSchema>;

export const updateEmployeeSchema = z.object({
  employeeNumber: employeeNumberRule,
  employeeFullName: employeeNameRule,
  reason: reasonRule,
});
export type UpdateEmployeeFormData = z.infer<typeof updateEmployeeSchema>;

export const changeStatusSchema = z.object({
  statusCode: z.enum(["Active", "Resigned"], {
    required_error: "Status wajib dipilih",
  }),
  effectiveFrom: dateRule("Tanggal efektif"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type ChangeStatusFormData = z.infer<typeof changeStatusSchema>;

export const assignDefinitiveSchema = z.object({
  positionId: guidRule("Posisi"),
  isPrimary: z.boolean(),
  effectiveFrom: dateRule("Tanggal mulai"),
  effectiveToExclusive: optionalDateRule("Tanggal berakhir"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type AssignDefinitiveFormData = z.infer<typeof assignDefinitiveSchema>;

export const transferSchema = z.object({
  sourceAssignmentId: guidRule("Penempatan asal"),
  targetPositionId: guidRule("Posisi tujuan"),
  effectiveFrom: dateRule("Tanggal efektif"),
  reason: reasonRule,
  decisionReference: decisionRule,
});
export type TransferFormData = z.infer<typeof transferSchema>;

export const endAssignmentSchema = z.object({
  assignmentId: guidRule("Penempatan"),
  effectiveToExclusive: dateRule("Tanggal berakhir"),
  reason: reasonRule,
});
export type EndAssignmentFormData = z.infer<typeof endAssignmentSchema>;

export const assignActingSchema = z.object({
  sourceAssignmentId: guidRule("Penempatan asal"),
  targetPositionId: guidRule("Posisi tujuan"),
  assignmentType: z.enum(["Plh", "Plt"], {
    required_error: "Jenis penugasan wajib dipilih",
  }),
  effectiveFrom: dateRule("Tanggal mulai"),
  effectiveToExclusive: optionalDateRule("Tanggal berakhir"),
  decisionReference: z
    .string({ required_error: "Referensi SK wajib diisi" })
    .trim()
    .min(1, { message: "Referensi SK wajib diisi" })
    .max(200, { message: "Referensi SK maksimal 200 karakter" }),
  reason: reasonRule,
});
export type AssignActingFormData = z.infer<typeof assignActingSchema>;

export const endActingSchema = z.object({
  actingId: guidRule("Penugasan acting"),
  effectiveToExclusive: dateRule("Tanggal berakhir"),
  reason: reasonRule,
});
export type EndActingFormData = z.infer<typeof endActingSchema>;

export const linkEmployeeSchema = z.object({
  employeeId: guidRule("Pegawai"),
  reason: reasonRule,
});
export type LinkEmployeeFormData = z.infer<typeof linkEmployeeSchema>;

export const unlinkEmployeeSchema = z.object({ reason: reasonRule });
export type UnlinkEmployeeFormData = z.infer<typeof unlinkEmployeeSchema>;
