import { z } from "zod";

export const permissionCodeRule = z
  .string({ required_error: "Kode permission wajib diisi" })
  .min(3, { message: "Kode permission minimal 3 karakter" })
  .max(150, { message: "Kode permission maksimal 150 karakter" })
  .regex(/^[a-z0-9-]+(\.[a-z0-9-]+)+$/, {
    message: "Format kode permission harus 'modul.aksi' dengan huruf kecil (contoh: invoice.view, user.create)",
  });

export const createPermissionSchema = z.object({
  code: permissionCodeRule,
  description: z
    .string()
    .max(500, { message: "Deskripsi maksimal 500 karakter" })
    .optional()
    .nullable(),
});

export type CreatePermissionFormData = z.infer<typeof createPermissionSchema>;

export const updatePermissionSchema = z.object({
  code: permissionCodeRule,
  description: z
    .string()
    .max(500, { message: "Deskripsi maksimal 500 karakter" })
    .optional()
    .nullable(),
});

export type UpdatePermissionFormData = z.infer<typeof updatePermissionSchema>;

