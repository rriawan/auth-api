"use client";

import { Button } from "@/components/ui/button";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  Loader2,
  XCircle,
} from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useRef, useState, useTransition } from "react";
import { handleConfirmActivationAction } from "./actions/confirm-activation";

export default function ConfirmActivationComponent() {
  const searchParams = useSearchParams();
  const token = searchParams.get("token") || "";

  const [isPending, startTransition] = useTransition();
  const hasTriggeredRef = useRef(false);
  const [success, setSuccess] = useState<boolean | null>(null);
  const [message, setMessage] = useState<string>("");

  useEffect(() => {
    if (!token || hasTriggeredRef.current) return;
    hasTriggeredRef.current = true;

    startTransition(async () => {
      const res = await handleConfirmActivationAction(token);
      setSuccess(res.success);
      setMessage(
        res.success
          ? res.message || "Akun Anda berhasil diverifikasi dan diaktifkan."
          : res.error || "Aktivasi akun gagal."
      );
    });
  }, [token]);

  // Jika token tidak disertakan pada tautan
  if (!token) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto w-16 h-16 rounded-full bg-red-100 dark:bg-red-950/50 flex items-center justify-center text-red-600 dark:text-red-400">
          <AlertCircle className="w-8 h-8" />
        </div>

        <div>
          <h4 className="text-xl font-semibold mb-2">
            Token Aktivasi Tidak Ditemukan
          </h4>
          <p className="text-sm text-neutral-600 dark:text-neutral-300 leading-relaxed max-w-md mx-auto">
            Tautan konfirmasi aktivasi akun yang Anda buka tidak memiliki parameter token yang valid. Silakan ajukan tautan aktivasi baru.
          </p>
        </div>

        <div className="pt-2 flex flex-col gap-3">
          <Link href="/auth/activate-account">
            <Button className="w-full rounded-lg h-[48px] text-sm font-medium">
              Minta Tautan Aktivasi Baru
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

  // Sedang memverifikasi token ke backend
  if (isPending || success === null) {
    return (
      <div className="space-y-6 text-center py-8">
        <div className="mx-auto w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center text-primary">
          <Loader2 className="w-8 h-8 animate-spin" />
        </div>

        <div>
          <h4 className="text-xl font-semibold mb-2">Memverifikasi Aktivasi...</h4>
          <p className="text-sm text-neutral-600 dark:text-neutral-300">
            Mohon tunggu sebentar, kami sedang memvalidasi token aktivasi akun Anda ke server autentikasi.
          </p>
        </div>
      </div>
    );
  }

  // Aktivasi Sukses
  if (success) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-950/50 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 className="w-8 h-8" />
        </div>

        <div>
          <h4 className="text-xl font-semibold mb-2">Akun Berhasil Diaktifkan!</h4>
          <p className="text-sm text-neutral-600 dark:text-neutral-300 leading-relaxed max-w-md mx-auto">
            {message}
          </p>
        </div>

        <div className="pt-2">
          <Link href="/auth/login">
            <Button className="w-full rounded-lg h-[48px] text-sm font-medium">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Masuk ke Akun Anda
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  // Aktivasi Gagal
  return (
    <div className="space-y-6 text-center">
      <div className="mx-auto w-16 h-16 rounded-full bg-red-100 dark:bg-red-950/50 flex items-center justify-center text-red-600 dark:text-red-400">
        <XCircle className="w-8 h-8" />
      </div>

      <div>
        <h4 className="text-xl font-semibold mb-2">Aktivasi Gagal</h4>
        <p className="text-sm text-neutral-600 dark:text-neutral-300 leading-relaxed max-w-md mx-auto">
          {message}
        </p>
      </div>

      <div className="pt-2 flex flex-col gap-3">
        <Link href="/auth/activate-account">
          <Button className="w-full rounded-lg h-[48px] text-sm font-medium">
            Minta Tautan Aktivasi Baru
          </Button>
        </Link>
        <Link href="/auth/login">
          <Button
            variant="outline"
            className="w-full rounded-lg h-[48px] text-sm font-medium"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Kembali ke Halaman Masuk
          </Button>
        </Link>
      </div>
    </div>
  );
}
