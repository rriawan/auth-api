'use client';

import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { useLoading } from '@/contexts/LoadingContext';
import { loginSchema } from '@/lib/zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Loader2, Lock, User } from 'lucide-react';
import { signIn } from 'next-auth/react';
import Link from 'next/link';
import { useState, useTransition } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { handleLoginAction } from './actions/login';

const LoginForm = () => {
  const [showPassword, setShowPassword] = useState(false);
  const [isPending, startTransition] = useTransition();
  const { loading, setLoading } = useLoading();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const form = useForm<z.infer<typeof loginSchema>>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      usernameOrEmail: 'superadmin',
      password: '',
    },
  });

  const onSubmit = (values: z.infer<typeof loginSchema>) => {
    setLoading(true);
    setIsSubmitting(true);

    startTransition(async () => {
      try {
        const formData = new FormData();
        formData.append('usernameOrEmail', values.usernameOrEmail);
        formData.append('password', values.password);

        const res = await handleLoginAction(formData);

        if (res?.error) {
          toast.error(res.error);
        } else {
          await signIn('credentials', {
            redirect: true,
            usernameOrEmail: values.usernameOrEmail,
            password: values.password,
            callbackUrl: '/',
          });
          toast.success('Login berhasil!');
        }
      } catch {
        toast.error('Terjadi kesalahan. Silakan coba kembali.');
      } finally {
        setLoading(false);
      }
    });

    setTimeout(() => {
      setIsSubmitting(false);
    }, 2000);
  };

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-5"
      >
        {/* Username or Email Field */}
        <FormField
          control={form.control}
          name="usernameOrEmail"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <div className="relative">
                  <User className="absolute start-5 top-1/2 transform -translate-y-1/2 w-5 h-5 text-neutral-700 dark:text-neutral-200" />
                  <Input
                    {...field}
                    type="text"
                    placeholder="Username atau Email"
                    name="usernameOrEmail"
                    className="ps-13 pe-12 h-14 rounded-xl bg-neutral-100 dark:bg-slate-800 border border-neutral-300 dark:border-slate-700 focus:border-primary dark:focus:border-primary focus-visible:border-primary !shadow-none !ring-0"
                    disabled={loading}
                  />
                </div>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Password Field */}
        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <div className="relative">
                  <Lock className="absolute start-5 top-1/2 transform -translate-y-1/2 w-5 h-5 text-neutral-700 dark:text-neutral-200" />
                  <Input
                    {...field}
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Password"
                    name="password"
                    className="ps-13 pe-12 h-14 rounded-xl bg-neutral-100 dark:bg-slate-800 border border-neutral-300 dark:border-slate-700 focus:border-primary dark:focus:border-primary focus-visible:border-primary !shadow-none !ring-0"
                    disabled={loading}
                  />
                  <Button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-4 top-1/2 transform -translate-y-1/2 !p-0 bg-transparent hover:bg-transparent text-muted-foreground h-[unset]"
                  >
                    {showPassword ? (
                      <EyeOff className="w-5 h-5" />
                    ) : (
                      <Eye className="w-5 h-5" />
                    )}
                  </Button>
                </div>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Remember Me & Forgot Password */}
        <div className="mt-2 flex justify-between items-center">
          <div className="flex items-center gap-2">
            <Checkbox
              id="remember"
              className="border border-neutral-500 w-4.5 h-4.5"
            />
            <label htmlFor="remember" className="text-sm">
              Ingat saya
            </label>
          </div>
          <Link
            href="/auth/forgot-password"
            className="text-primary font-medium hover:underline text-sm"
          >
            Lupa Password?
          </Link>
        </div>

        {/* Submit Button */}
        <Button
          type="submit"
          className="w-full rounded-lg h-[52px] text-sm mt-2"
          disabled={loading || isPending}
        >
          {isSubmitting || isPending ? (
            <>
              <Loader2 className="animate-spin h-4.5 w-4.5 mr-2" />
              Memproses...
            </>
          ) : (
            'Masuk'
          )}
        </Button>
      </form>
    </Form>
  );
};

export default LoginForm;
