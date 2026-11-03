import { z } from "zod";

export const createRoleSchema = z.object({
  name: z
    .string({ required_error: "Nama role wajib diisi" })
    .min(2, { message: "Nama role minimal 2 karakter" })
    .max(100, { message: "Nama role maksimal 100 karakter" }),
  description: z
    .string()
    .max(500, { message: "Deskripsi maksimal 500 karakter" })
    .optional()
    .nullable(),
  permissionIds: z.array(z.string().uuid()).optional().default([]),
});

export type CreateRoleFormData = z.infer<typeof createRoleSchema>;

export const updateRoleSchema = z.object({
  name: z
    .string({ required_error: "Nama role wajib diisi" })
    .min(2, { message: "Nama role minimal 2 karakter" })
    .max(100, { message: "Nama role maksimal 100 karakter" }),
  description: z
    .string()
    .max(500, { message: "Deskripsi maksimal 500 karakter" })
    .optional()
    .nullable(),
});

export type UpdateRoleFormData = z.infer<typeof updateRoleSchema>;

export const setRolePermissionsSchema = z.object({
  permissionIds: z.array(z.string().uuid()),
});

export type SetRolePermissionsFormData = z.infer<typeof setRolePermissionsSchema>;

export const setUserAppRolesSchema = z.object({
  roleIds: z.array(z.string().uuid()),
});

export type SetUserAppRolesFormData = z.infer<typeof setUserAppRolesSchema>;

