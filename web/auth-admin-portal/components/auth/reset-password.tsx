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
import { resetPasswordSchema } from "@/lib/validation/auth.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  Eye,
  EyeOff,
  Loader2,
  Lock,
} from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, useTransition } from "react";
import { useForm } from "react-hook-form";
import toast from "react-hot-toast";
import { z } from "zod";
import { handleResetPassword } from "./actions/handleResetPassword";

export default function ResetPasswordComponent() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token") || "";
  const router = useRouter();

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const form = useForm<z.infer<typeof resetPasswordSchema>>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      token,
      newPassword: "",
      confirmPassword: "",
    },
    values: {
      token,
      newPassword: "",
      confirmPassword: "",
    },
  });

  const onSubmit = (values: z.infer<typeof resetPasswordSchema>) => {
    setErrorMessage(null);

    startTransition(async () => {
      try {
        const formData = new FormData();
        formData.append("token", values.token);
        formData.append("newPassword", values.newPassword);
        formData.append("confirmPassword", values.confirmPassword);

        const res = await handleResetPassword(formData);

        if (!res.success) {
          setErrorMessage(res.error || "Gagal mengatur ulang password.");
          toast.error(res.error || "Gagal mengatur ulang password.");
        } else {
          setSuccessMessage(
            res.message || "Password berhasil diubah. Silakan masuk kembali."
          );
          toast.success("Password berhasil diubah!");
          setTimeout(() => {
            router.push("/auth/login");
          }, 2500);
        }
      } catch {
        setErrorMessage(
          "Terjadi kesalahan saat memproses reset password. Silakan coba kembali."
        );
        toast.error("Terjadi kesalahan.");
      }
    });
  };

  // Jika token tidak disertakan pada tautan
  if (!token) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto w-16 h-16 rounded-full bg-red-100 dark:bg-red-950/50 flex items-center justify-center text-red-600 dark:text-red-400">
          <AlertCircle className="w-8 h-8" />
        </div>

        <div>
          <h4 className="text-xl font-semibold mb-2">
            Token Reset Tidak Ditemukan
          </h4>
          <p className="text-sm text-neutral-600 dark:text-neutral-300 leading-relaxed max-w-md mx-auto">
            Tautan reset password yang Anda buka tidak memiliki parameter token
            yang valid. Silakan ajukan permohonan reset password baru.
          </p>
        </div>

        <div className="pt-2 flex flex-col gap-3">
          <Link href="/auth/forgot-password">
            <Button className="w-full rounded-lg h-[48px] text-sm font-medium">
              Minta Tautan Reset Baru
            </Button>
          </Link>
          <Link href="/auth/login">
            <Button
              variant="outline"
              className="w-full rounded-lg h-[48px] text-sm font-medium"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Kembali ke Login
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  // Jika reset password berhasil
  if (successMessage) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-950/50 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 className="w-8 h-8" />
        </div>

        <div>
          <h4 className="text-xl font-semibold mb-2">Password Berhasil Diubah</h4>
          <p className="text-sm text-neutral-600 dark:text-neutral-300 leading-relaxed max-w-md mx-auto">
            {successMessage} Mengarahkan Anda ke halaman masuk...
          </p>
        </div>

        <div className="pt-2">
          <Link href="/auth/login">
            <Button className="w-full rounded-lg h-[48px] text-sm font-medium">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Masuk Sekarang
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

        {/* Input Token tersembunyi */}
        <input type="hidden" {...form.register("token")} />

        {/* Password Baru */}
        <FormField
          control={form.control}
          name="newPassword"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <div className="relative">
                  <Lock className="absolute start-5 top-1/2 transform -translate-y-1/2 w-5 h-5 text-neutral-700 dark:text-neutral-200" />
                  <Input
                    {...field}
                    type={showPassword ? "text" : "password"}
                    placeholder="Password Baru (min. 8 karakter)"
                    name="newPassword"
                    className="ps-13 pe-12 h-14 rounded-xl bg-neutral-100 dark:bg-slate-800 border border-neutral-300 dark:border-slate-700 focus:border-primary dark:focus:border-primary focus-visible:border-primary !shadow-none !ring-0"
                    disabled={isPending}
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

        {/* Konfirmasi Password */}
        <FormField
          control={form.control}
          name="confirmPassword"
          render={({ field }) => (
            <FormItem>
              <FormControl>
                <div className="relative">
                  <Lock className="absolute start-5 top-1/2 transform -translate-y-1/2 w-5 h-5 text-neutral-700 dark:text-neutral-200" />
                  <Input
                    {...field}
                    type={showConfirmPassword ? "text" : "password"}
                    placeholder="Ulangi Password Baru"
                    name="confirmPassword"
                    className="ps-13 pe-12 h-14 rounded-xl bg-neutral-100 dark:bg-slate-800 border border-neutral-300 dark:border-slate-700 focus:border-primary dark:focus:border-primary focus-visible:border-primary !shadow-none !ring-0"
                    disabled={isPending}
                  />
                  <Button
                    type="button"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    className="absolute right-4 top-1/2 transform -translate-y-1/2 !p-0 bg-transparent hover:bg-transparent text-muted-foreground h-[unset]"
                  >
                    {showConfirmPassword ? (
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

        <Button
          type="submit"
          className="w-full rounded-lg h-[52px] text-sm mt-2 font-medium"
          disabled={isPending}
        >
          {isPending ? (
            <>
              <Loader2 className="animate-spin h-4.5 w-4.5 mr-2" />
              Menyimpan Password Baru...
            </>
          ) : (
            "Simpan Password Baru"
          )}
        </Button>

        <div className="text-center mt-4">
          <Link
            href="/auth/login"
            className="text-sm text-primary font-medium hover:underline flex items-center justify-center gap-1.5"
          >
            <ArrowLeft className="w-4 h-4" />
            Kembali ke Halaman Masuk
          </Link>
        </div>
      </form>
    </Form>
  );
}

