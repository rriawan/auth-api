import { z } from "zod";

// Password Schema Base: min 8, max 256
export const passwordField = z
  .string({ required_error: "Password wajib diisi" })
  .min(8, { message: "Password minimal 8 karakter" })
  .max(256, { message: "Password maksimal 256 karakter" });

// Email/Username schema base
export const emailOrUsernameField = z
  .string({ required_error: "Username atau email wajib diisi" })
  .min(1, "Username atau email tidak boleh kosong");

// Login Schema
export const loginSchema = z.object({
  usernameOrEmail: emailOrUsernameField,
  password: passwordField,
});

// Forgot Password Schema
export const forgotPasswordSchema = z.object({
  usernameOrEmail: emailOrUsernameField,
});

// Reset Password Schema
export const resetPasswordSchema = z
  .object({
    password: passwordField,
    confirmPassword: z.string({
      required_error: "Konfirmasi password wajib diisi",
    }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Konfirmasi password tidak cocok",
    path: ["confirmPassword"],
  });

export const createPasswordSchema = resetPasswordSchema;
