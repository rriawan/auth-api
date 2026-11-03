'use server';

import { resetPasswordSchema } from "@/lib/zod";
import { redirect } from "next/navigation";

export async function handleResetPassword(formData: FormData) {
  const values = {
    password: formData.get("password"),
    confirmPassword: formData.get("confirmPassword"),
  };

  const result = resetPasswordSchema.safeParse(values);

  if (!result.success) {
    throw new Error("Validation failed.");
  }

  redirect("/auth/login");
}
