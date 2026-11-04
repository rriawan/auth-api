"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { ClientAppDto } from "@/lib/api/types";
import { AlertCircle, Loader2, Save, Shield } from "lucide-react";
import { useState } from "react";
import toast from "react-hot-toast";
import { updateAppAction } from "./actions/apps";
import { Can } from "../auth/can";

interface AppInfoTabProps {
  app: ClientAppDto;
}

export function AppInfoTab({ app }: AppInfoTabProps) {
  const isSystemApp = app.clientId.toLowerCase() === "auth-admin";

  const [name, setName] = useState(app.name);
  const [description, setDescription] = useState(app.description || "");
  const [allowedOrigins, setAllowedOrigins] = useState(app.allowedOrigins || "");
  const [isActive, setIsActive] = useState(app.isActive);

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});
    setIsLoading(true);

    try {
      const res = await updateAppAction(app.id, app.clientId, {
        name,
        description: description || undefined,
        allowedOrigins: allowedOrigins || undefined,
        isActive: isSystemApp ? true : isActive,
      });

      if (!res.success) {
        setError(res.error || "Gagal memperbarui aplikasi.");
        if (res.fieldErrors) {
          setFieldErrors(res.fieldErrors);
        }
        return;
      }

      toast.success("Informasi aplikasi berhasil diperbarui!");
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
    <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
      <CardHeader>
        <CardTitle className="text-lg font-semibold">
          Informasi & Konfigurasi Aplikasi
        </CardTitle>
        <CardDescription className="text-xs">
          Perbarui metadata, allowed origins CORS, dan status keaktifan aplikasi klien ini.
        </CardDescription>
      </CardHeader>

      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-5">
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription className="text-xs">{error}</AlertDescription>
            </Alert>
          )}

          {isSystemApp && (
            <Alert className="border-blue-200 dark:border-blue-900/50 bg-blue-50/50 dark:bg-blue-950/20 text-blue-900 dark:text-blue-200">
              <Shield className="h-4 w-4 text-blue-600 dark:text-blue-400" />
              <AlertTitle className="text-xs font-semibold">
                Aplikasi Sistem Utama (Protected)
              </AlertTitle>
              <AlertDescription className="text-xs mt-0.5 text-blue-800 dark:text-blue-300">
                Aplikasi <code>auth-admin</code> merupakan portal autentikasi terpusat. Status keaktifannya dikunci secara permanen dan tidak dapat dinonaktifkan.
              </AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="text-xs font-semibold text-muted-foreground">
                Client ID (Permanen)
              </Label>
              <Input
                value={app.clientId}
                disabled
                className="bg-neutral-100 dark:bg-slate-800 font-mono text-xs cursor-not-allowed text-foreground"
              />
              <p className="text-[11px] text-muted-foreground">
                Client ID adalah pengenal unik sistem dan tidak dapat diubah setelah pembuatan.
              </p>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="appName" className="text-xs font-semibold">
                Nama Aplikasi <span className="text-red-500">*</span>
              </Label>
              <Input
                id="appName"
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
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="appDescription" className="text-xs font-semibold">
              Deskripsi
            </Label>
            <Textarea
              id="appDescription"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              disabled={isLoading}
              rows={3}
              placeholder="Jelaskan peran dan tujuan aplikasi ini..."
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
              value={allowedOrigins}
              onChange={(e) => setAllowedOrigins(e.target.value)}
              disabled={isLoading}
              placeholder="https://app.domain.com, https://portal.domain.com"
              className={fieldErrors.allowedOrigins ? "border-red-500" : ""}
            />
            <p className="text-[11px] text-muted-foreground">
              Daftar origin browser yang diizinkan untuk pemanggilan API lintas-domain. Pisahkan beberapa origin dengan koma (,).
            </p>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-3.5 bg-neutral-50/50 dark:bg-slate-900/50">
            <div className="space-y-0.5">
              <Label
                htmlFor="isActiveToggle"
                className={`text-xs font-medium ${isSystemApp ? "cursor-not-allowed text-muted-foreground" : "cursor-pointer"}`}
              >
                Status Keaktifan Aplikasi
              </Label>
              <p className="text-[11px] text-muted-foreground">
                {isSystemApp
                  ? "Aplikasi sistem auth-admin selalu aktif."
                  : isActive
                    ? "Aplikasi saat ini aktif dan dapat memproses autentikasi pengguna."
                    : "Aplikasi nonaktif. Seluruh sesi aktif dicabut dan pengguna tidak dapat login."}
              </p>
            </div>
            <Switch
              id="isActiveToggle"
              checked={isSystemApp ? true : isActive}
              onCheckedChange={setIsActive}
              disabled={isLoading || isSystemApp}
            />
          </div>
        </CardContent>

        <CardFooter className="border-t border-neutral-200 dark:border-slate-800 pt-4 flex justify-end">
          <Can
            permission="apps.write"
            fallback={
              <p className="text-xs text-muted-foreground italic">
                Anda hanya memiliki akses baca (apps.read). Diperlukan izin &apos;apps.write&apos; untuk menyimpan perubahan.
              </p>
            }
          >
            <Button type="submit" disabled={isLoading} className="gap-2">
              {isLoading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Menyimpan...
                </>
              ) : (
                <>
                  <Save className="w-4 h-4" />
                  Simpan Perubahan
                </>
              )}
            </Button>
          </Can>
        </CardFooter>
      </form>
    </Card>
  );
}
