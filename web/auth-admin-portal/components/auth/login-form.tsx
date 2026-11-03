"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { useLoading } from "@/contexts/LoadingContext";
import { loginSchema } from "@/lib/validation/auth.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import { AlertCircle, Eye, EyeOff, Loader2, Lock, User } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState, useTransition } from "react";
import { useForm } from "react-hook-form";
import toast from "react-hot-toast";
import { z } from "zod";
import { handleLoginAction } from "./actions/login";

const LoginForm = () => {
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/";
  const urlError = searchParams.get("error");

  const [showPassword, setShowPassword] = useState(false);
  const [isPending, startTransition] = useTransition();
  const { loading, setLoading } = useLoading();
  const [serverError, setServerError] = useState<string | null>(
    urlError === "SessionExpired"
      ? "Sesi Anda telah berakhir. Silakan masuk kembali."
      : null
  );

  const form = useForm<z.infer<typeof loginSchema>>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      usernameOrEmail: "superadmin",
      password: "",
    },
  });

  const onSubmit = (values: z.infer<typeof loginSchema>) => {
    setServerError(null);
    setLoading(true);

    startTransition(async () => {
      try {
        const formData = new FormData();
        formData.append("usernameOrEmail", values.usernameOrEmail);
        formData.append("password", values.password);
        formData.append("callbackUrl", callbackUrl);

        const res = await handleLoginAction(formData);

        if (res?.error) {
          setServerError(res.error);
          toast.error(res.error);
        } else {
          toast.success("Login berhasil!");
        }
      } catch {
        // Jika Server Action melakukan internal redirect, browser akan menangani navigasi
      } finally {
        setLoading(false);
      }
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
        {/* Banner error dari server / query parameter */}
        {serverError && (
          <Alert variant="destructive" className="rounded-xl border-red-500/50 bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription className="text-sm font-medium">
              {serverError}
            </AlertDescription>
          </Alert>
        )}

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
                    disabled={loading || isPending}
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
                    type={showPassword ? "text" : "password"}
                    placeholder="Password"
                    name="password"
                    className="ps-13 pe-12 h-14 rounded-xl bg-neutral-100 dark:bg-slate-800 border border-neutral-300 dark:border-slate-700 focus:border-primary dark:focus:border-primary focus-visible:border-primary !shadow-none !ring-0"
                    disabled={loading || isPending}
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
            <label htmlFor="remember" className="text-sm cursor-pointer select-none">
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
          className="w-full rounded-lg h-[52px] text-sm mt-2 font-medium"
          disabled={loading || isPending}
        >
          {isPending || loading ? (
            <>
              <Loader2 className="animate-spin h-4.5 w-4.5 mr-2" />
              Memproses Autentikasi...
            </>
          ) : (
            "Masuk"
          )}
        </Button>
      </form>
    </Form>
  );
};

export default LoginForm;
