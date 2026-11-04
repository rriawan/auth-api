"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { ClientAppWithRawKeyResponse } from "@/lib/api/types";
import { AlertCircle, Loader2, Plus } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { createAppAction } from "./actions/apps";
import { ApiKeyRevealModal } from "./api-key-reveal-modal";

interface CreateAppDialogProps {
  onAppCreated?: (data: ClientAppWithRawKeyResponse) => void;
}

export function CreateAppDialog({ onAppCreated }: CreateAppDialogProps) {
  const [open, setOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Form states
  const [clientId, setClientId] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [allowedOrigins, setAllowedOrigins] = useState("");
  const [isActive, setIsActive] = useState(true);

  // State untuk modal sekali-tampil API Key
  const [revealedKeyData, setRevealedKeyData] =
    useState<ClientAppWithRawKeyResponse | null>(null);
  const [showRevealModal, setShowRevealModal] = useState(false);

  const resetForm = () => {
    setClientId("");
    setName("");
    setDescription("");
    setAllowedOrigins("");
    setIsActive(true);
    setError(null);
    setFieldErrors({});
  };

  const handleClientIdChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Otomatis ubah menjadi huruf kecil dan ganti spasi dengan tanda hubung
    const val = e.target.value.toLowerCase().replace(/\s+/g, "-");
    setClientId(val);
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});
    setIsLoading(true);

    try {
      const res = await createAppAction({
        clientId,
        name,
        description: description || undefined,
        allowedOrigins: allowedOrigins || undefined,
        isActive,
      });

      if (!res.success) {
        setError(res.error || "Gagal mendaftarkan aplikasi.");
        if (res.fieldErrors) {
          setFieldErrors(res.fieldErrors);
        }
        setIsLoading(false);
        return;
      }

      if (res.data) {
        toast.success(`Aplikasi '${res.data.app.name}' berhasil didaftarkan!`);
        setRevealedKeyData(res.data);
        setShowRevealModal(true);
        if (onAppCreated) {
          onAppCreated(res.data);
        }
        setOpen(false);
        resetForm();
      }
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan yang tidak diharapkan."
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={(nextOpen) => {
          setOpen(nextOpen);
          if (!nextOpen) resetForm();
        }}
      >
        <DialogTrigger asChild>
          <Button className="gap-2 shadow-xs">
            <Plus className="w-4 h-4" />
            Daftarkan Aplikasi Baru
          </Button>
        </DialogTrigger>

        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="text-xl font-semibold">
              Daftarkan Aplikasi Klien Baru
            </DialogTitle>
            <DialogDescription className="text-sm">
              Tambahkan sistem atau aplikasi baru yang akan mengonsumsi API auth terpusat.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="clientId" className="text-xs font-semibold">
                Client ID <span className="text-red-500">*</span>
              </Label>
              <Input
                id="clientId"
                placeholder="contoh: erp-core atau portal-hr"
                value={clientId}
                onChange={handleClientIdChange}
                disabled={isLoading}
                required
                className={fieldErrors.clientId ? "border-red-500" : ""}
              />
              {fieldErrors.clientId ? (
                <p className="text-xs text-red-500">{fieldErrors.clientId}</p>
              ) : (
                <p className="text-[11px] text-muted-foreground">
                  Pengenal unik. Hanya huruf kecil, angka, dan tanda hubung (-).
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="name" className="text-xs font-semibold">
                Nama Aplikasi <span className="text-red-500">*</span>
              </Label>
              <Input
                id="name"
                placeholder="contoh: Enterprise Resource Planning"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={isLoading}
                required
                className={fieldErrors.name ? "border-red-500" : ""}
              />
              {fieldErrors.name && (
                <p className="text-xs text-red-500">{fieldErrors.name}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="description" className="text-xs font-semibold">
                Deskripsi
              </Label>
              <Textarea
                id="description"
                placeholder="Deskripsi singkat fungsi dan tujuan aplikasi..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={isLoading}
                rows={2}
                className={fieldErrors.description ? "border-red-500" : ""}
              />
              {fieldErrors.description && (
                <p className="text-xs text-red-500">{fieldErrors.description}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="allowedOrigins" className="text-xs font-semibold">
                Allowed Origins (CORS)
              </Label>
              <Input
                id="allowedOrigins"
                placeholder="https://app.domain.com, https://dev.domain.com"
                value={allowedOrigins}
                onChange={(e) => setAllowedOrigins(e.target.value)}
                disabled={isLoading}
                className={fieldErrors.allowedOrigins ? "border-red-500" : ""}
              />
              <p className="text-[11px] text-muted-foreground">
                Daftar origin web yang diizinkan memanggil API dari browser. Pisahkan dengan koma.
              </p>
            </div>

            <div className="flex items-center justify-between rounded-lg border p-3 bg-neutral-50/50 dark:bg-slate-900/50">
              <div className="space-y-0.5">
                <Label htmlFor="isActive" className="text-xs font-medium cursor-pointer">
                  Status Aplikasi Aktif
                </Label>
                <p className="text-[11px] text-muted-foreground">
                  Aplikasi dapat langsung melakukan autentikasi setelah dibuat.
                </p>
              </div>
              <Switch
                id="isActive"
                checked={isActive}
                onCheckedChange={setIsActive}
                disabled={isLoading}
              />
            </div>

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setOpen(false)}
                disabled={isLoading}
              >
                Batal
              </Button>
              <Button type="submit" disabled={isLoading} className="gap-2">
                {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
                Daftarkan Aplikasi
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Modal Sekali-Tampil untuk API Key */}
      <ApiKeyRevealModal
        isOpen={showRevealModal}
        onClose={() => {
          setShowRevealModal(false);
          setRevealedKeyData(null);
        }}
        data={revealedKeyData}
        mode="created"
      />
    </>
  );
}

