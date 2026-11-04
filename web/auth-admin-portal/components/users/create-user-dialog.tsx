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
import {
  AlertCircle,
  Eye,
  EyeOff,
  Loader2,
  UserPlus,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import toast from "react-hot-toast";
import { createUserAction } from "./actions/users";

export function CreateUserDialog() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Form states
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isActive, setIsActive] = useState(true);
  const [isAccountVerified, setIsAccountVerified] = useState(true);

  const resetForm = () => {
    setUsername("");
    setEmail("");
    setFullName("");
    setPassword("");
    setConfirmPassword("");
    setShowPassword(false);
    setIsActive(true);
    setIsAccountVerified(true);
    setError(null);
    setFieldErrors({});
  };

  const handleUsernameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value.replace(/\s+/g, "");
    setUsername(val);
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});

    if (password !== confirmPassword) {
      setFieldErrors({
        confirmPassword: "Konfirmasi password tidak cocok dengan password.",
      });
      return;
    }

    setIsLoading(true);

    try {
      const res = await createUserAction({
        username,
        email,
        fullName,
        password,
        isActive,
        isAccountVerified,
      });

      if (!res.success) {
        setError(res.error || "Gagal membuat akun pengguna baru.");
        if (res.fieldErrors) {
          setFieldErrors(res.fieldErrors);
        }
        setIsLoading(false);
        return;
      }

      if (res.data) {
        toast.success(
          `Pengguna '${res.data.username}' berhasil didaftarkan!`
        );
        setOpen(false);
        resetForm();
        router.refresh();
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
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        setOpen(nextOpen);
        if (!nextOpen) resetForm();
      }}
    >
      <DialogTrigger asChild>
        <Button className="gap-2 shadow-xs">
          <UserPlus className="w-4 h-4" />
          Tambah User Baru
        </Button>
      </DialogTrigger>

      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold">
            Tambah Akun Pengguna SSO
          </DialogTitle>
          <DialogDescription className="text-xs">
            Buat akun identitas global baru pada Single User Pool yang dapat
            ditugaskan ke berbagai aplikasi klien.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription className="text-xs">{error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            <div className="space-y-1.5">
              <Label htmlFor="create-username" className="text-xs font-semibold">
                Username <span className="text-red-500">*</span>
              </Label>
              <Input
                id="create-username"
                placeholder="contoh: budi.santoso"
                value={username}
                onChange={handleUsernameChange}
                disabled={isLoading}
                required
                className={`text-xs font-mono ${
                  fieldErrors.username ? "border-red-500" : ""
                }`}
              />
              {fieldErrors.username ? (
                <p className="text-xs text-red-500">{fieldErrors.username}</p>
              ) : (
                <p className="text-[11px] text-muted-foreground">
                  Huruf, angka, titik, strip, underscore (3–100 karakter).
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="create-email" className="text-xs font-semibold">
                Alamat Email <span className="text-red-500">*</span>
              </Label>
              <Input
                id="create-email"
                type="email"
                placeholder="budi@perusahaan.co.id"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isLoading}
                required
                className={`text-xs ${
                  fieldErrors.email ? "border-red-500" : ""
                }`}
              />
              {fieldErrors.email && (
                <p className="text-xs text-red-500">{fieldErrors.email}</p>
              )}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-fullName" className="text-xs font-semibold">
              Nama Lengkap <span className="text-red-500">*</span>
            </Label>
            <Input
              id="create-fullName"
              placeholder="contoh: Budi Santoso"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              disabled={isLoading}
              required
              className={`text-xs ${
                fieldErrors.fullName ? "border-red-500" : ""
              }`}
            />
            {fieldErrors.fullName && (
              <p className="text-xs text-red-500">{fieldErrors.fullName}</p>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            <div className="space-y-1.5">
              <Label htmlFor="create-password" className="text-xs font-semibold">
                Password Awal <span className="text-red-500">*</span>
              </Label>
              <div className="relative">
                <Input
                  id="create-password"
                  type={showPassword ? "text" : "password"}
                  placeholder="Minimal 8 karakter"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={isLoading}
                  required
                  className={`pr-9 text-xs ${
                    fieldErrors.password ? "border-red-500" : ""
                  }`}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  tabIndex={-1}
                  aria-label={
                    showPassword ? "Sembunyikan password" : "Tampilkan password"
                  }
                >
                  {showPassword ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </button>
              </div>
              {fieldErrors.password ? (
                <p className="text-xs text-red-500">{fieldErrors.password}</p>
              ) : (
                <p className="text-[11px] text-muted-foreground">
                  Minimal 8, maksimal 256 karakter.
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="create-confirmPassword"
                className="text-xs font-semibold"
              >
                Konfirmasi Password <span className="text-red-500">*</span>
              </Label>
              <Input
                id="create-confirmPassword"
                type={showPassword ? "text" : "password"}
                placeholder="Ulangi password awal"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                disabled={isLoading}
                required
                className={`text-xs ${
                  fieldErrors.confirmPassword ? "border-red-500" : ""
                }`}
              />
              {fieldErrors.confirmPassword && (
                <p className="text-xs text-red-500">
                  {fieldErrors.confirmPassword}
                </p>
              )}
            </div>
          </div>

          <div className="space-y-2.5 pt-1">
            <div className="flex items-center justify-between rounded-lg border p-3 bg-neutral-50/50 dark:bg-slate-900/50">
              <div className="space-y-0.5 pr-3">
                <Label
                  htmlFor="create-isActive"
                  className="text-xs font-medium cursor-pointer"
                >
                  Status Akun Aktif
                </Label>
                <p className="text-[11px] text-muted-foreground">
                  Izinkan pengguna ini login ke aplikasi yang ditugaskan.
                </p>
              </div>
              <Switch
                id="create-isActive"
                checked={isActive}
                onCheckedChange={setIsActive}
                disabled={isLoading}
              />
            </div>

            <div className="flex items-center justify-between rounded-lg border p-3 bg-neutral-50/50 dark:bg-slate-900/50">
              <div className="space-y-0.5 pr-3">
                <Label
                  htmlFor="create-isVerified"
                  className="text-xs font-medium cursor-pointer"
                >
                  Akun Terverifikasi
                </Label>
                <p className="text-[11px] text-muted-foreground">
                  Jika nonaktif, pengguna harus melakukan aktivasi akun terlebih
                  dahulu sebelum dapat login.
                </p>
              </div>
              <Switch
                id="create-isVerified"
                checked={isAccountVerified}
                onCheckedChange={setIsAccountVerified}
                disabled={isLoading}
              />
            </div>
          </div>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
              disabled={isLoading}
              className="text-xs"
            >
              Batal
            </Button>
            <Button type="submit" disabled={isLoading} className="gap-2 text-xs">
              {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
              Simpan Pengguna
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

