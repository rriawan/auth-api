"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { useLoading } from "@/contexts/LoadingContext";
import { forgotPasswordSchema } from "@/lib/validation/auth.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  Loader2,
  Mail,
} from "lucide-react";
import Link from "next/link";
import { useState, useTransition } from "react";
import { useForm } from "react-hook-form";
import toast from "react-hot-toast";
import { z } from "zod";
import { handleForgotPasswordAction } from "./actions/forgot-password";

const ForgotPasswordComponent = () => {
  const { loading, setLoading } = useLoading();
  const [isPending, startTransition] = useTransition();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const form = useForm<z.infer<typeof forgotPasswordSchema>>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      emailOrUsername: "",
    },
  });

  const onSubmit = (values: z.infer<typeof forgotPasswordSchema>) => {
    setLoading(true);
    setErrorMessage(null);

    startTransition(async () => {
      try {
        const formData = new FormData();
        formData.append("emailOrUsername", values.emailOrUsername);

        const res = await handleForgotPasswordAction(formData);

        if (!res.success) {
          setErrorMessage(res.error || "Gagal memproses permintaan.");
          toast.error(res.error || "Gagal memproses permintaan.");
        } else {
          setSuccessMessage(
            res.message ||
              "Permintaan reset password telah dikirimkan ke kontak terdaftar Anda."
          );
          toast.success("Permintaan berhasil dikirim.");
        }
      } catch {
        setErrorMessage(
          "Terjadi kesalahan saat memproses permintaan. Silakan coba kembali."
        );
        toast.error("Terjadi kesalahan.");
      } finally {
        setLoading(false);
      }
    });
  };

  if (successMessage) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-950/50 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 className="w-8 h-8" />
        </div>

        <div>
          <h4 className="text-xl font-semibold mb-2">Permintaan Terkirim</h4>
          <p className="text-sm text-neutral-600 dark:text-neutral-300 leading-relaxed max-w-md mx-auto">
            {successMessage}
          </p>
        </div>

        <div className="pt-2">
          <Link href="/auth/login">
            <Button className="w-full rounded-lg h-[48px] text-sm font-medium">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Kembali ke Halaman Masuk
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
        {errorMessage && (
          <Alert
            variant="destructive"
            className="rounded-xl border-red-500/50 bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300"
          >
            <AlertCircle className="h-4 w-4" />
            <AlertDescription className="text-sm font-medium">
              {errorMessage}
            </AlertDescription>
          </Alert>
        )}

        <FormField
          control={form.control}
          name="emailOrUsername"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <div className="relative">
                  <Mail className="absolute start-5 top-1/2 transform -translate-y-1/2 w-5 h-5 text-neutral-700 dark:text-neutral-200" />
                  <Input
                    {...field}
                    type="text"
                    placeholder="Username atau Email Akun"
                    name="emailOrUsername"
                    className="ps-13 pe-12 h-14 rounded-xl bg-neutral-100 dark:bg-slate-800 border border-neutral-300 dark:border-slate-700 focus:border-primary dark:focus:border-primary focus-visible:border-primary !shadow-none !ring-0"
                    disabled={loading || isPending}
                  />
                </div>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button
          type="submit"
          className="w-full rounded-lg h-[52px] text-sm mt-2 font-medium"
          disabled={loading || isPending}
        >
          {loading || isPending ? (
            <>
              <Loader2 className="animate-spin h-4.5 w-4.5 mr-2" />
              Mengirim Permintaan...
            </>
          ) : (
            "Kirim Tautan Reset Password"
          )}
        </Button>

        <div className="flex flex-col gap-2 text-center mt-4">
          <Link
            href="/auth/login"
            className="text-sm text-primary font-medium hover:underline flex items-center justify-center gap-1.5"
          >
            <ArrowLeft className="w-4 h-4" />
            Kembali ke Login
          </Link>

          <Link
            href="/auth/activate-account"
            className="text-xs text-neutral-500 hover:text-neutral-800 dark:hover:text-neutral-200 hover:underline"
          >
            Belum menerima aktivasi akun? Minta aktivasi
          </Link>
        </div>
      </form>
    </Form>
  );
};

export default ForgotPasswordComponent;
