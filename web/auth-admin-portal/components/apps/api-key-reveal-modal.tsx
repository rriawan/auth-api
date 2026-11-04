"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { ClientAppWithRawKeyResponse } from "@/lib/api/types";
import { AlertTriangle, Check, Copy, KeyRound, ShieldAlert } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";

interface ApiKeyRevealModalProps {
  isOpen: boolean;
  onClose: () => void;
  data: ClientAppWithRawKeyResponse | null;
  mode?: "created" | "rotated";
}

export function ApiKeyRevealModal({
  isOpen,
  onClose,
  data,
  mode = "created",
}: ApiKeyRevealModalProps) {
  const [copied, setCopied] = useState(false);

  if (!data) return null;

  const handleCopy = () => {
    if (!data.rawApiKey) return;
    navigator.clipboard.writeText(data.rawApiKey);
    setCopied(true);
    toast.success("API Key berhasil disalin ke clipboard!");
    setTimeout(() => setCopied(false), 2500);
  };

  const isRotated = mode === "rotated";

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-amber-500/10 text-amber-600 dark:text-amber-400 flex items-center justify-center">
              <KeyRound className="w-5 h-5" />
            </div>
            <div>
              <DialogTitle className="text-xl font-semibold">
                {isRotated
                  ? "API Key Berhasil Dirotasi"
                  : "Aplikasi Terdaftar & API Key Dibuat"}
              </DialogTitle>
              <DialogDescription className="text-sm mt-0.5">
                Kredensial integrasi backend untuk aplikasi{" "}
                <strong className="text-foreground">{data.app.name}</strong>
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="space-y-4 my-2">
          {/* Alert Peringatan Keamanan Kritis */}
          <Alert
            variant="destructive"
            className="border-amber-500/40 bg-amber-50 dark:bg-amber-950/30 text-amber-900 dark:text-amber-200"
          >
            <ShieldAlert className="h-5 w-5 text-amber-600 dark:text-amber-400" />
            <AlertTitle className="font-semibold text-amber-800 dark:text-amber-300">
              Peringatan Keamanan: Kunci Hanya Ditampilkan Sekali
            </AlertTitle>
            <AlertDescription className="text-xs mt-1 text-amber-700 dark:text-amber-300/90 leading-relaxed">
              Kunci API rahasia ini <strong>TIDAK AKAN PERNAH DITAMPILKAN LAGI</strong>.
              Sistem backend hanya menyimpan hash kriptografis SHA-256 dan prefix kunci.
              Harap segera salin dan simpan di secret manager atau environment variable backend aplikasi Anda.
            </AlertDescription>
          </Alert>

          {/* Kotak Tampilan Raw API Key */}
          <div className="rounded-lg border border-neutral-200 dark:border-slate-800 bg-neutral-900 text-neutral-100 p-4 space-y-2">
            <div className="flex items-center justify-between text-xs text-neutral-400">
              <span className="font-medium">Raw Secret API Key:</span>
              <Badge variant="outline" className="text-neutral-300 border-neutral-700 text-[10px]">
                Prefix: {data.app.apiKeyPrefix}
              </Badge>
            </div>
            <div className="p-3 bg-black/60 rounded border border-neutral-800 font-mono text-sm break-all select-all text-emerald-400">
              {data.rawApiKey}
            </div>
            <div className="flex justify-end pt-1">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleCopy}
                className="gap-1.5 text-xs bg-neutral-800 hover:bg-neutral-700 text-white border-neutral-700 hover:text-white"
              >
                {copied ? (
                  <>
                    <Check className="w-3.5 h-3.5 text-emerald-400" />
                    Tersalin!
                  </>
                ) : (
                  <>
                    <Copy className="w-3.5 h-3.5" />
                    Salin API Key
                  </>
                )}
              </Button>
            </div>
          </div>

          {/* Panduan Integrasi Singkat */}
          <div className="rounded-md bg-neutral-50 dark:bg-slate-900 border border-neutral-200 dark:border-slate-800 p-3 space-y-1.5 text-xs">
            <span className="font-semibold text-neutral-700 dark:text-neutral-300 block">
              Header HTTP untuk Aplikasi Konsumen:
            </span>
            <div className="font-mono bg-white dark:bg-slate-950 p-2 rounded border border-neutral-200 dark:border-slate-800 space-y-1 text-neutral-800 dark:text-neutral-200">
              <div>
                <span className="text-purple-600 dark:text-purple-400">X-Client-Id:</span>{" "}
                <span>{data.app.clientId}</span>
              </div>
              <div>
                <span className="text-purple-600 dark:text-purple-400">X-Api-Key:</span>{" "}
                <span className="text-neutral-500 italic">{"<kunci-di-atas>"}</span>
              </div>
            </div>
          </div>
        </div>

        <DialogFooter className="sm:justify-between items-center gap-2">
          <div className="text-xs text-muted-foreground flex items-center gap-1.5">
            <AlertTriangle className="w-3.5 h-3.5 text-amber-500" />
            <span>Pastikan kunci telah tersimpan sebelum menutup dialog ini.</span>
          </div>
          <Button type="button" onClick={onClose} className="w-full sm:w-auto">
            Saya Sudah Menyimpan Kunci Ini
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

