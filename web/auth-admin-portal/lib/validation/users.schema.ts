import { z } from "zod";
import { passwordRule } from "./auth.schema.ts";

export const createUserSchema = z.object({
  username: z
    .string({ required_error: "Username wajib diisi" })
    .min(3, { message: "Username minimal 3 karakter" })
    .max(100, { message: "Username maksimal 100 karakter" })
    .regex(/^[a-zA-Z0-9._-]+$/, {
      message: "Username hanya boleh menggunakan huruf, angka, titik, strip, dan underscore",
    }),
  email: z
    .string({ required_error: "Email wajib diisi" })
    .email({ message: "Format email tidak valid" })
    .max(256, { message: "Email maksimal 256 karakter" }),
  fullName: z
    .string({ required_error: "Nama lengkap wajib diisi" })
    .min(2, { message: "Nama lengkap minimal 2 karakter" })
    .max(200, { message: "Nama lengkap maksimal 200 karakter" }),
  password: passwordRule,
  isActive: z.boolean().default(true),
  isAccountVerified: z.boolean().default(true),
});

export type CreateUserFormData = z.infer<typeof createUserSchema>;

export const updateUserSchema = z.object({
  email: z
    .string({ required_error: "Email wajib diisi" })
    .email({ message: "Format email tidak valid" })
    .max(256, { message: "Email maksimal 256 karakter" }),
  fullName: z
    .string({ required_error: "Nama lengkap wajib diisi" })
    .min(2, { message: "Nama lengkap minimal 2 karakter" })
    .max(200, { message: "Nama lengkap maksimal 200 karakter" }),
  isActive: z.boolean(),
  isAccountVerified: z.boolean(),
});

export type UpdateUserFormData = z.infer<typeof updateUserSchema>;

