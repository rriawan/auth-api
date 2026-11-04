"use client";

import { useEffect } from "react";
import Link from "next/link";
import { AlertTriangle, RefreshCw, LogIn } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function AuthError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Auth Route Error:", error);
  }, [error]);

  return (
    <div className="w-full max-w-md mx-auto text-center space-y-6 py-8">
      <div className="w-14 h-14 mx-auto rounded-full bg-red-50 dark:bg-red-950/50 flex items-center justify-center text-red-600 dark:text-red-400">
        <AlertTriangle className="w-7 h-7" />
      </div>

      <div className="space-y-2">
        <h2 className="text-xl font-bold tracking-tight text-neutral-900 dark:text-neutral-100">
          Gagal Memproses Autentikasi
        </h2>
        <p className="text-sm text-neutral-600 dark:text-neutral-400">
          {error.message ||
            "Terjadi kesalahan saat memproses permintaan autentikasi Anda."}
        </p>
      </div>

      {error.digest && (
        <p className="text-xs font-mono text-neutral-400 dark:text-neutral-500">
          Kode Pelacakan: {error.digest}
        </p>
      )}

      <div className="pt-2 flex flex-col sm:flex-row gap-2 justify-center">
        <Button
          onClick={() => reset()}
          variant="default"
          className="gap-2"
        >
          <RefreshCw className="w-4 h-4" />
          Coba Lagi
        </Button>
        <Button variant="outline" asChild className="gap-2">
          <Link href="/auth/login">
            <LogIn className="w-4 h-4" />
            Kembali ke Login
          </Link>
        </Button>
      </div>
    </div>
  );
}
