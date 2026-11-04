"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ClientAppDto, ClientAppWithRawKeyResponse } from "@/lib/api/types";
import {
  AlertTriangle,
  Check,
  Copy,
  KeyRound,
  Loader2,
  RefreshCw,
  ShieldAlert,
  Terminal,
} from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { rotateApiKeyAction } from "./actions/apps";
import { ApiKeyRevealModal } from "./api-key-reveal-modal";
import { Can } from "../auth/can";

interface AppApiKeyTabProps {
  app: ClientAppDto;
}

export function AppApiKeyTab({ app }: AppApiKeyTabProps) {
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [isRotating, setIsRotating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Modal untuk menampilkan kunci baru setelah rotasi sukses
  const [rotatedData, setRotatedData] =
    useState<ClientAppWithRawKeyResponse | null>(null);
  const [showRevealModal, setShowRevealModal] = useState(false);

  const [copiedPrefix, setCopiedPrefix] = useState(false);

  const handleCopyPrefix = () => {
    navigator.clipboard.writeText(app.apiKeyPrefix);
    setCopiedPrefix(true);
    toast.success("Prefix API Key disalin!");
    setTimeout(() => setCopiedPrefix(false), 2000);
  };

  const handleConfirmRotation = async () => {
    setIsRotating(true);
    setError(null);

    try {
      const res = await rotateApiKeyAction(app.id);
      if (!res.success) {
        setError(res.error || "Gagal merotasi API Key.");
        toast.error(res.error || "Gagal merotasi API Key.");
        setShowConfirmDialog(false);
        return;
      }

      if (res.data) {
        toast.success("API Key berhasil dirotasi!");
        setRotatedData(res.data);
        setShowConfirmDialog(false);
        setShowRevealModal(true);
      }
    } catch (err) {
      const msg =
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat memproses rotasi.";
      setError(msg);
      toast.error(msg);
      setShowConfirmDialog(false);
    } finally {
      setIsRotating(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Kartu Status & Identitas Kunci Saat Ini */}
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
              <KeyRound className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-semibold">
                Kredensial API Key Klien
              </CardTitle>
              <CardDescription className="text-xs">
                Kunci otentikasi antarmesin (Machine-to-Machine) untuk backend aplikasi {app.name}.
              </CardDescription>
            </div>
          </div>
        </CardHeader>

        <CardContent className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <ShieldAlert className="h-4 w-4" />
              <AlertDescription className="text-xs">{error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="p-4 rounded-lg border border-neutral-200 dark:border-slate-800 bg-neutral-50/50 dark:bg-slate-900/50 space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground block">
                Prefix Kunci Aktif
              </span>
              <div className="flex items-center gap-2">
                <code className="text-sm font-mono font-semibold text-foreground bg-white dark:bg-slate-950 px-2 py-1 rounded border border-neutral-200 dark:border-slate-800">
                  {app.apiKeyPrefix}••••••••
                </code>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-muted-foreground hover:text-foreground"
                  onClick={handleCopyPrefix}
                  title="Salin Prefix"
                >
                  {copiedPrefix ? (
                    <Check className="w-3.5 h-3.5 text-emerald-500" />
                  ) : (
                    <Copy className="w-3.5 h-3.5" />
                  )}
                </Button>
              </div>
              <p className="text-[11px] text-muted-foreground">
                Digunakan untuk mengidentifikasi kunci tanpa membocorkan isi rahasia.
              </p>
            </div>

            <div className="p-4 rounded-lg border border-neutral-200 dark:border-slate-800 bg-neutral-50/50 dark:bg-slate-900/50 space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground block">
                Status Kunci
              </span>
              <div className="flex items-center gap-2 pt-0.5">
                <Badge variant={app.isActive ? "success" : "secondary"}>
                  {app.isActive ? "Kunci Aktif & Siap Digunakan" : "Aplikasi Nonaktif"}
                </Badge>
              </div>
              <p className="text-[11px] text-muted-foreground">
                Kunci tersimpan secara aman dalam format hash kriptografis SHA-256.
              </p>
            </div>
          </div>

          {/* Contoh Pemanggilan HTTP */}
          <div className="space-y-2 pt-2">
            <div className="flex items-center gap-2 text-xs font-semibold text-neutral-800 dark:text-neutral-200">
              <Terminal className="w-4 h-4 text-primary" />
              <span>Contoh Integrasi HTTP Header pada Aplikasi Konsumen</span>
            </div>
            <div className="p-4 rounded-lg bg-neutral-900 text-neutral-100 font-mono text-xs space-y-2 border border-neutral-800">
              <div className="text-neutral-400"># Header wajib pada request M2M ke auth-api:</div>
              <div>
                <span className="text-purple-400">X-Client-Id:</span>{" "}
                <span className="text-emerald-400">{app.clientId}</span>
              </div>
              <div>
                <span className="text-purple-400">X-Api-Key:</span>{" "}
                <span className="text-amber-400">{app.apiKeyPrefix}••••••••••••••••••••••••••••••••</span>
              </div>
              <div className="pt-2 text-neutral-400"># Endpoint yang dapat diakses dengan kredensial ini:</div>
              <div className="text-neutral-300">
                POST /api/v1/auth/verify
              </div>
              <div className="text-neutral-300">
                GET  /api/v1/auth/permissions?userId=...&clientId={app.clientId}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Kartu Rotasi Kunci (Destructive) */}
      <Card className="border border-red-200 dark:border-red-900/40 bg-red-50/20 dark:bg-red-950/10 shadow-sm">
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 flex items-center justify-center shrink-0">
              <RefreshCw className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-semibold text-red-900 dark:text-red-300">
                Rotasi API Key
              </CardTitle>
              <CardDescription className="text-xs text-red-800/80 dark:text-red-400/80">
                Ganti kunci API dengan yang baru bila kunci lama diduga bocor atau sebagai bagian dari pemeliharaan rutin.
              </CardDescription>
            </div>
          </div>
        </CardHeader>

        <CardContent className="space-y-3">
          <Alert variant="destructive" className="bg-white dark:bg-slate-900">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle className="text-xs font-semibold">
              Perhatian: Kunci Lama Langsung Dibatalkan
            </AlertTitle>
            <AlertDescription className="text-xs leading-relaxed mt-1">
              Merotasi API Key akan langsung membuat kunci lama hangus seketika. Seluruh panggilan dari backend aplikasi {app.name} yang masih menggunakan kunci lama akan ditolak hingga Anda memperbarui konfigurasi di server aplikasi tersebut.
            </AlertDescription>
          </Alert>
        </CardContent>

        <CardFooter className="border-t border-red-200 dark:border-red-900/40 pt-4 flex justify-between items-center">
          <span className="text-xs text-muted-foreground">
            Tindakan ini memerlukan izin &apos;apps.rotate-key&apos;.
          </span>
          <Can
            permission="apps.rotate-key"
            fallback={
              <Button variant="outline" size="sm" disabled>
                Izin Rotasi Dibatasi
              </Button>
            }
          >
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setShowConfirmDialog(true)}
              className="gap-2"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              Rotasi API Key Sekarang
            </Button>
          </Can>
        </CardFooter>
      </Card>

      {/* Dialog Konfirmasi Rotasi */}
      <Dialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-red-100 dark:bg-red-950/60 text-red-600 flex items-center justify-center">
                <AlertTriangle className="w-5 h-5" />
              </div>
              <div>
                <DialogTitle className="text-lg font-semibold">
                  Konfirmasi Rotasi API Key
                </DialogTitle>
                <DialogDescription className="text-xs mt-0.5">
                  Tindakan ini permanen dan tidak dapat dibatalkan.
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="py-2 space-y-3 text-xs text-muted-foreground leading-relaxed">
            <p>
              Apakah Anda yakin ingin merotasi API Key untuk aplikasi{" "}
              <strong className="text-foreground">{app.name}</strong> (
              <code className="font-mono text-foreground">{app.clientId}</code>)?
            </p>
            <p className="bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-900/60 p-3 rounded text-amber-900 dark:text-amber-200">
              Kunci lama (<code className="font-mono">{app.apiKeyPrefix}••••</code>) akan langsung tidak berlaku. Anda wajib menyalin dan mengonfigurasi kunci baru segera setelah dialog ini ditutup.
            </p>
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowConfirmDialog(false)}
              disabled={isRotating}
            >
              Batal
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleConfirmRotation}
              disabled={isRotating}
              className="gap-2"
            >
              {isRotating && <Loader2 className="w-4 h-4 animate-spin" />}
              Ya, Rotasi Kunci Sekarang
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Modal Sekali-Tampil untuk API Key Baru Hasil Rotasi */}
      <ApiKeyRevealModal
        isOpen={showRevealModal}
        onClose={() => {
          setShowRevealModal(false);
          setRotatedData(null);
        }}
        data={rotatedData}
        mode="rotated"
      />
    </div>
  );
}

