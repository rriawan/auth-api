'use server';

export const handleLoginAction = async (formData: FormData) => {
  const usernameOrEmail = formData.get('usernameOrEmail') as string;
  const password = formData.get('password') as string;

  if (!usernameOrEmail || !password) {
    return { error: 'Username/Email dan password wajib diisi.' };
  }

  return { success: true };
};
