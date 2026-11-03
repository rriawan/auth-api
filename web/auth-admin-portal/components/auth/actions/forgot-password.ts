'use server';

import { forgotPasswordSchema } from '@/lib/zod';

export async function handleForgotPasswordAction(formData: FormData) {
  const emailOrUsername = formData.get('emailOrUsername');
  const parsed = forgotPasswordSchema.safeParse({ emailOrUsername });

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
