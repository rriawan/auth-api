import { z } from "zod";

export const passwordRule = z
  .string({ required_error: "Password wajib diisi" })
  .min(8, { message: "Password minimal 8 karakter" })
  .max(256, { message: "Password maksimal 256 karakter" });

export const emailOrUsernameRule = z
  .string({ required_error: "Username atau email wajib diisi" })
  .min(1, "Username atau email tidak boleh kosong")
  .max(256, "Maksimal 256 karakter");

export const loginSchema = z.object({
  usernameOrEmail: emailOrUsernameRule,
  password: passwordRule,
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const forgotPasswordSchema = z.object({
  emailOrUsername: emailOrUsernameRule,
});

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    token: z.string().min(1, "Token reset password diperlukan"),
    newPassword: passwordRule,
    confirmPassword: z.string({
      required_error: "Konfirmasi password wajib diisi",
    }),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Konfirmasi password tidak cocok",
    path: ["confirmPassword"],
  });

export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z
      .string({ required_error: "Password saat ini wajib diisi" })
      .min(1, "Password saat ini wajib diisi"),
    newPassword: passwordRule,
    confirmNewPassword: z.string({
      required_error: "Konfirmasi password baru wajib diisi",
    }),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: "Konfirmasi password baru tidak cocok",
    path: ["confirmNewPassword"],
  })
  .refine((data) => data.currentPassword !== data.newPassword, {
    message: "Password baru tidak boleh sama dengan password saat ini",
    path: ["newPassword"],
  });

export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

export const requestActivationSchema = z.object({
  emailOrUsername: emailOrUsernameRule,
});

export type RequestActivationFormData = z.infer<typeof requestActivationSchema>;

