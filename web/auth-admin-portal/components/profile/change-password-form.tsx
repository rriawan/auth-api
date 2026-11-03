"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { changePasswordSchema } from "@/lib/validation/auth.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AlertCircle,
  CheckCircle2,
  Eye,
  EyeOff,
  KeyRound,
  Loader2,
  Lock,
  ShieldCheck,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { useForm } from "react-hook-form";
import toast from "react-hot-toast";
import { z } from "zod";
import { handleChangePasswordAction } from "./actions/change-password";

export default function ChangePasswordForm() {
  const router = useRouter();
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const form = useForm<z.infer<typeof changePasswordSchema>>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: "",
      newPassword: "",
      confirmNewPassword: "",
    },
  });

  const onSubmit = (values: z.infer<typeof changePasswordSchema>) => {
    setErrorMessage(null);

    startTransition(async () => {
      try {
        const formData = new FormData();
        formData.append("currentPassword", values.currentPassword);
        formData.append("newPassword", values.newPassword);
        formData.append("confirmNewPassword", values.confirmNewPassword);

        const res = await handleChangePasswordAction(formData);

        if (!res.success) {
          setErrorMessage(res.error || "Gagal mengubah password.");
          toast.error(res.error || "Gagal mengubah password.");
        } else {
          setSuccessMessage(
            res.message ||
              "Password berhasil diperbarui. Mengarahkan Anda ke halaman login..."
          );
          toast.success("Password berhasil diubah!");
          setTimeout(() => {
            router.push("/auth/login?passwordChanged=true");
          }, 2000);
        }
      } catch {
        setErrorMessage(
          "Terjadi kesalahan saat menghubungi server. Silakan coba kembali."
        );
        toast.error("Terjadi kesalahan.");
      }
    });
  };

  return (
    <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
      <CardHeader>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
            <KeyRound className="w-5 h-5" />
          </div>
          <div>
            <CardTitle className="text-lg font-semibold">Ganti Password</CardTitle>
            <CardDescription className="text-sm">
              Perbarui kata sandi akun Anda secara berkala untuk menjaga keamanan akun portal.
            </CardDescription>
          </div>
        </div>
      </CardHeader>

      <CardContent>
        {successMessage ? (
          <div className="p-6 rounded-xl bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-900 text-center space-y-4">
            <div className="mx-auto w-12 h-12 rounded-full bg-emerald-100 dark:bg-emerald-900/50 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
              <CheckCircle2 className="w-6 h-6" />
            </div>
            <div>
              <h5 className="font-semibold text-emerald-900 dark:text-emerald-200">
                Password Berhasil Diperbarui
              </h5>
              <p className="text-sm text-emerald-700 dark:text-emerald-300 mt-1 max-w-md mx-auto">
                {successMessage}
              </p>
            </div>
          </div>
        ) : (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5 max-w-xl">
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

              {/* Password Saat Ini */}
              <FormField
                control={form.control}
                name="currentPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-sm font-medium">Password Saat Ini</FormLabel>
                    <FormControl>
                      <div className="relative">
                        <Lock className="absolute start-4 top-1/2 transform -translate-y-1/2 w-4.5 h-4.5 text-neutral-500" />
                        <Input
                          {...field}
                          type={showCurrentPassword ? "text" : "password"}
                          placeholder="Masukkan password saat ini"
                          className="ps-11 pe-11 h-12 rounded-xl"
                          disabled={isPending}
                        />
                        <Button
                          type="button"
                          onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                          className="absolute right-3 top-1/2 transform -translate-y-1/2 !p-0 bg-transparent hover:bg-transparent text-muted-foreground h-[unset]"
                        >
                          {showCurrentPassword ? (
                            <EyeOff className="w-4.5 h-4.5" />
                          ) : (
                            <Eye className="w-4.5 h-4.5" />
                          )}
                        </Button>
                      </div>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Password Baru */}
              <FormField
                control={form.control}
                name="newPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-sm font-medium">Password Baru</FormLabel>
                    <FormControl>
                      <div className="relative">
                        <Lock className="absolute start-4 top-1/2 transform -translate-y-1/2 w-4.5 h-4.5 text-neutral-500" />
                        <Input
                          {...field}
                          type={showNewPassword ? "text" : "password"}
                          placeholder="Minimal 8 karakter"
                          className="ps-11 pe-11 h-12 rounded-xl"
                          disabled={isPending}
                        />
                        <Button
                          type="button"
                          onClick={() => setShowNewPassword(!showNewPassword)}
                          className="absolute right-3 top-1/2 transform -translate-y-1/2 !p-0 bg-transparent hover:bg-transparent text-muted-foreground h-[unset]"
                        >
                          {showNewPassword ? (
                            <EyeOff className="w-4.5 h-4.5" />
                          ) : (
                            <Eye className="w-4.5 h-4.5" />
                          )}
                        </Button>
                      </div>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Konfirmasi Password Baru */}
              <FormField
                control={form.control}
                name="confirmNewPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="text-sm font-medium">Ulangi Password Baru</FormLabel>
                    <FormControl>
                      <div className="relative">
                        <Lock className="absolute start-4 top-1/2 transform -translate-y-1/2 w-4.5 h-4.5 text-neutral-500" />
                        <Input
                          {...field}
                          type={showConfirmPassword ? "text" : "password"}
                          placeholder="Ulangi password baru yang sama"
                          className="ps-11 pe-11 h-12 rounded-xl"
                          disabled={isPending}
                        />
                        <Button
                          type="button"
                          onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                          className="absolute right-3 top-1/2 transform -translate-y-1/2 !p-0 bg-transparent hover:bg-transparent text-muted-foreground h-[unset]"
                        >
                          {showConfirmPassword ? (
                            <EyeOff className="w-4.5 h-4.5" />
                          ) : (
                            <Eye className="w-4.5 h-4.5" />
                          )}
                        </Button>
                      </div>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="p-3.5 rounded-xl bg-neutral-100 dark:bg-slate-900/60 border border-neutral-200 dark:border-slate-800 text-xs text-neutral-600 dark:text-neutral-300 flex items-start gap-2.5">
                <ShieldCheck className="w-4 h-4 text-primary shrink-0 mt-0.5" />
                <span>
                  <strong>Catatan Keamanan:</strong> Demi keamanan akun Anda, setelah password berhasil diubah, backend akan mencabut seluruh sesi aktif dan Anda akan diminta masuk kembali.
                </span>
              </div>

              <Button
                type="submit"
                className="rounded-lg h-12 px-6 text-sm font-medium"
                disabled={isPending}
              >
                {isPending ? (
                  <>
                    <Loader2 className="animate-spin h-4 w-4 mr-2" />
                    Menyimpan Perubahan...
                  </>
                ) : (
                  "Ubah Password"
                )}
              </Button>
            </form>
          </Form>
        )}
      </CardContent>
    </Card>
  );
}

