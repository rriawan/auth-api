"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { LifecycleRunResultDto } from "@/lib/api/employee-types";
import { AlertCircle, CheckCircle2, Loader2, Play, Search } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import toast from "react-hot-toast";
import { processResignationsAction } from "./actions/employees";
import { formatDate } from "./format";

/**
 * Operasi lifecycle akun. Selalu mulai dengan pemeriksaan (dry-run, tidak menulis apa pun); eksekusi nyata baru
 * tersedia setelah pemeriksaan menemukan akun dan dikonfirmasi secara eksplisit.
 */
export function LifecyclePanel() {
  const [result, setResult] = useState<LifecycleRunResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<"check" | "run" | null>(null);
  const [confirmed, setConfirmed] = useState(false);

  const execute = async (dryRun: boolean) => {
    setBusy(dryRun ? "check" : "run");
    setError(null);
    const res = await processResignationsAction(dryRun, dryRun ? undefined : result?.processed.map((p) => p.userId));
    setBusy(null);
    if (!res.success || !res.data) {
      setError(res.error ?? "Operasi gagal.");
      return;
    }
    setResult(res.data);
    setConfirmed(false);
    if (!dryRun) {
      toast.success(`${res.data.processed.length} akun dinonaktifkan, ${res.data.revokedSessions} sesi dicabut.`);
    }
  };

  const canRun = result?.dryRun === true && result.processed.length > 0;

  return (
    <div className="space-y-4">
      <Alert>
        <AlertCircle className="h-4 w-4" />
        <AlertDescription className="text-xs space-y-1">
          <p>
            Perintah ini menonaktifkan akun yang tertaut ke pegawai dengan resign yang sudah berlaku dan mencabut seluruh refresh token-nya.
            Role dan akses aplikasi tidak diubah, dan mengaktifkan kembali pegawai tidak mengaktifkan kembali akun.
          </p>
          <p>
            Penolakan login/refresh bergantung pada pengaturan <code>EnforceEmploymentStatus</code> di backend; perintah ini tidak bergantung padanya.
            Token akses yang sudah terbit tetap berlaku bagi aplikasi yang memverifikasi secara lokal sampai kedaluwarsa.
          </p>
        </AlertDescription>
      </Alert>

      {error && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">{error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-wrap gap-2">
        <Button onClick={() => execute(true)} disabled={busy !== null} variant="outline" className="gap-2 text-xs">
          {busy === "check" ? <Loader2 className="w-4 h-4 animate-spin" /> : <Search className="w-4 h-4" />}
          Periksa akun terdampak (dry-run)
        </Button>
      </div>

      {result && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm flex flex-wrap items-center gap-2">
              {result.dryRun ? "Hasil pemeriksaan" : "Hasil eksekusi"}
              <Badge variant={result.dryRun ? "info" : "success"} className="text-xs">{result.dryRun ? "Dry-run, tidak ada perubahan" : "Perubahan diterapkan"}</Badge>
              <span className="text-xs font-normal text-muted-foreground">per {formatDate(result.asOf)}</span>
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 p-0">
            {result.processed.length === 0 ? (
              <p className="text-xs text-muted-foreground p-6 flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4 text-green-600" /> Tidak ada akun aktif yang terkait resign berlaku.
              </p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-xs">NIP</TableHead>
                    <TableHead className="text-xs">Pegawai</TableHead>
                    <TableHead className="text-xs">Akun</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {result.processed.map((p) => (
                    <TableRow key={p.userId}>
                      <TableCell className="font-mono text-xs font-semibold">{p.employeeNumber}</TableCell>
                      <TableCell className="text-xs">
                        <Link href={`/employees/${p.employeeId}`} className="underline underline-offset-4">Detail pegawai</Link>
                      </TableCell>
                      <TableCell className="text-xs">
                        <Link href={`/users/${p.userId}`} className="underline underline-offset-4">Detail akun</Link>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
            <div className="px-6 pb-4 text-xs text-muted-foreground space-y-1">
              {!result.dryRun && <p>Sesi refresh token dicabut: {result.revokedSessions}.</p>}
              {result.remaining > 0 && (
                <p className="text-amber-600">
                  Masih ada {result.remaining} akun yang belum diproses karena batas satu kali jalan; jalankan lagi setelah ini.
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {canRun && (
        <Card className="border-red-200 dark:border-red-900/50">
          <CardContent className="p-5 space-y-3">
            <label className="flex items-start gap-2 text-xs cursor-pointer">
              <input type="checkbox" checked={confirmed} onChange={(e) => setConfirmed(e.target.checked)} disabled={busy !== null} className="mt-0.5" />
              <span>
                Saya memahami bahwa {result.processed.length} akun di atas akan dinonaktifkan dan seluruh sesinya dicabut. Mengaktifkan kembali akun
                harus dilakukan manual pada halaman pengguna.
              </span>
            </label>
            <Button onClick={() => execute(false)} disabled={!confirmed || busy !== null} variant="destructive" className="gap-2 text-xs">
              {busy === "run" ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />}
              Nonaktifkan akun &amp; cabut sesi
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
