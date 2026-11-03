'use server';

import { forgotPasswordSchema } from '@/lib/zod';

export async function handleForgotPasswordAction(formData: FormData) {
  const usernameOrEmail = formData.get('usernameOrEmail');
  const parsed = forgotPasswordSchema.safeParse({ usernameOrEmail });

  if (!parsed.success) {
    return {
      success: false,
      error: parsed.error.flatten().fieldErrors,
    };
  }

  return {
    success: true,
  };
}
