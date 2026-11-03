'use client';

import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { useLoading } from '@/contexts/LoadingContext';
import { forgotPasswordSchema } from '@/lib/zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Mail } from 'lucide-react';
import Link from 'next/link';
import { useTransition } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';

const ForgotPasswordComponent = () => {
  const { loading, setLoading } = useLoading();
  const [isPending, startTransition] = useTransition();

  const form = useForm<z.infer<typeof forgotPasswordSchema>>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      usernameOrEmail: '',
    },
  });

  const onSubmit = () => {
    setLoading(true);

    startTransition(async () => {
      try {
        toast.success('Permintaan reset password telah dikirim. Silakan periksa email Anda.');
      } catch {
        toast.error('Terjadi kesalahan. Silakan coba kembali.');
      } finally {
        setTimeout(() => setLoading(false), 1000);
      }
    });
  };

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-5"
      >
        <FormField
          control={form.control}
          name="usernameOrEmail"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <div className="relative">
                  <Mail className="absolute start-5 top-1/2 transform -translate-y-1/2 w-5 h-5 text-neutral-700 dark:text-neutral-200" />
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

        <Button
          type="submit"
          className="w-full rounded-lg h-[52px] text-sm mt-2"
          disabled={loading || isPending}
        >
          {loading || isPending ? (
            <>
              <Loader2 className="animate-spin h-4.5 w-4.5 mr-2" />
              Mengirim...
            </>
          ) : (
            'Kirim Permintaan Reset'
          )}
        </Button>

        <div className="text-center mt-4">
          <Link
            href="/auth/login"
            className="text-sm text-primary font-medium hover:underline"
          >
            Kembali ke Login
          </Link>
        </div>
      </form>
    </Form>
  );
};

export default ForgotPasswordComponent;
