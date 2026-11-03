import { z } from "zod";

export const clientIdRule = z
  .string({ required_error: "Client ID wajib diisi" })
  .min(2, { message: "Client ID minimal 2 karakter" })
  .max(100, { message: "Client ID maksimal 100 karakter" })
  .regex(/^[a-z0-9-]+$/, {
    message: "Client ID hanya boleh menggunakan huruf kecil, angka, dan tanda hubung (-)",
  });

export const createClientAppSchema = z.object({
  clientId: clientIdRule,
  name: z
    .string({ required_error: "Nama aplikasi wajib diisi" })
    .min(2, { message: "Nama aplikasi minimal 2 karakter" })
    .max(200, { message: "Nama aplikasi maksimal 200 karakter" }),
  description: z
    .string()
    .max(500, { message: "Deskripsi maksimal 500 karakter" })
    .optional()
    .nullable(),
  allowedOrigins: z
    .string()
    .max(2000, { message: "Allowed Origins maksimal 2000 karakter" })
    .optional()
    .nullable(),
  isActive: z.boolean().default(true),
});

export type CreateClientAppFormData = z.infer<typeof createClientAppSchema>;

export const updateClientAppSchema = z.object({
  name: z
    .string({ required_error: "Nama aplikasi wajib diisi" })
    .min(2, { message: "Nama aplikasi minimal 2 karakter" })
    .max(200, { message: "Nama aplikasi maksimal 200 karakter" }),
  description: z
    .string()
    .max(500, { message: "Deskripsi maksimal 500 karakter" })
    .optional()
    .nullable(),
  allowedOrigins: z
    .string()
    .max(2000, { message: "Allowed Origins maksimal 2000 karakter" })
    .optional()
    .nullable(),
  isActive: z.boolean(),
});

export type UpdateClientAppFormData = z.infer<typeof updateClientAppSchema>;

