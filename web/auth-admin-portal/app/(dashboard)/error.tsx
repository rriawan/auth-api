"use client";

import { useEffect } from "react";
import Link from "next/link";
import { AlertTriangle, RefreshCw, LayoutDashboard } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

export default function DashboardError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Dashboard Route Error:", error);
  }, [error]);

  return (
    <div className="py-12 flex items-center justify-center">
      <Card className="w-full max-w-lg border-red-200/60 dark:border-red-900/40 shadow-sm bg-white dark:bg-card">
        <CardContent className="pt-8 pb-8 px-6 text-center space-y-4">
          <div className="w-14 h-14 mx-auto rounded-full bg-red-50 dark:bg-red-950/50 flex items-center justify-center text-red-600 dark:text-red-400">
            <AlertTriangle className="w-7 h-7" />
          </div>

          <div className="space-y-2">
            <h2 className="text-xl font-bold tracking-tight text-neutral-900 dark:text-neutral-100">
              Gagal Memuat Data Halaman
            </h2>
            <p className="text-sm text-neutral-600 dark:text-neutral-400">
              {error.message ||
                "Tidak dapat memuat data dari server backend auth-api. Pastikan server backend sedang aktif."}
            </p>
          </div>

          {error.digest && (
            <p className="text-xs font-mono text-neutral-400 dark:text-neutral-500">
              ID Masalah: {error.digest}
            </p>
          )}

          <div className="pt-2 flex flex-col sm:flex-row gap-2 justify-center">
            <Button
              onClick={() => reset()}
              variant="default"
              className="gap-2"
            >
              <RefreshCw className="w-4 h-4" />
              Muat Ulang Halaman
            </Button>
            <Button variant="outline" asChild className="gap-2">
              <Link href="/">
                <LayoutDashboard className="w-4 h-4" />
                Kembali ke Dashboard
              </Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
