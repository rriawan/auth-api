"use client";

import { Can } from "@/components/auth/can";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { AdminUserDto } from "@/lib/api/types";
import {
  AlertCircle,
  AlertTriangle,
  Check,
  Copy,
  Loader2,
  Lock,
  LogOut,
  Save,
  ShieldAlert,
  Unlock,
  UserCog,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import toast from "react-hot-toast";
import {
  revokeUserSessionsAction,
  unlockUserAction,
  updateUserAction,
} from "./actions/users";

interface UserInfoTabProps {
  user: AdminUserDto;
  isLocked: boolean;
}

export function UserInfoTab({ user, isLocked }: UserInfoTabProps) {
  const router = useRouter();

  // State Form Edit User
  const [fullName, setFullName] = useState(user.fullName);
  const [email, setEmail] = useState(user.email);
  const [isActive, setIsActive] = useState(user.isActive);
  const [isAccountVerified, setIsAccountVerified] = useState(
    user.isAccountVerified
  );
  const [isSaving, setIsSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Dialog Konfirmasi Nonaktifkan User
  const [confirmDeactivateOpen, setConfirmDeactivateOpen] = useState(false);

  // State Unlock User
  const [isUnlocking, setIsUnlocking] = useState(false);
  const [unlockError, setUnlockError] = useState<string | null>(null);

  // Dialog Konfirmasi Paksa Logout (Revoke Sessions)
  const [revokeDialogOpen, setRevokeDialogOpen] = useState(false);
  const [isRevoking, setIsRevoking] = useState(false);
  const [revokeError, setRevokeError] = useState<string | null>(null);

  // Copy helper
  const [copiedField, setCopiedField] = useState<string | null>(null);

  const handleCopy = (val: string, label: string) => {
    navigator.clipboard.writeText(val);
    setCopiedField(label);
    toast.success(`${label} disalin ke clipboard!`);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const formatDateTime = (dateStr: string | null) => {
    if (!dateStr) return "-";
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  const executeUpdateUser = async () => {
    setFormError(null);
    setFieldErrors({});
    setIsSaving(true);

    try {
      const res = await updateUserAction(user.id, {
        fullName,
        email,
        isActive,
        isAccountVerified,
      });

      if (!res.success) {
        setFormError(res.error || "Gagal menyimpan perubahan data pengguna.");
        if (res.fieldErrors) {
          setFieldErrors(res.fieldErrors);
        }
        setIsSaving(false);
        return;
      }

      toast.success(`Profil pengguna '${user.username}' berhasil diperbarui.`);
      setConfirmDeactivateOpen(false);
      router.refresh();
    } catch (err) {
      setFormError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menyimpan perubahan."
      );
    } finally {
      setIsSaving(false);
    }
  };

  const handleFormSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (user.isActive && !isActive) {
      setConfirmDeactivateOpen(true);
      return;
    }
    await executeUpdateUser();
  };

  const handleUnlockUser = async () => {
    setUnlockError(null);
    setIsUnlocking(true);

    try {
      const res = await unlockUserAction(user.id);
      if (!res.success) {
        setUnlockError(res.error || "Gagal membuka kunci akun pengguna.");
        setIsUnlocking(false);
        return;
      }

      toast.success(
        `Kunci akun '${user.username}' berhasil dibuka dan counter gagal login direset.`
      );
      router.refresh();
    } catch (err) {
      setUnlockError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat membuka kunci akun."
      );
    } finally {
      setIsUnlocking(false);
    }
  };

  const handleConfirmRevokeSessions = async () => {
    setRevokeError(null);
    setIsRevoking(true);

    try {
      const res = await revokeUserSessionsAction(user.id);
      if (!res.success) {
        setRevokeError(res.error || "Gagal mencabut sesi aktif pengguna.");
        setIsRevoking(false);
        return;
      }

      toast.success(
        res.data?.message ||
          `Berhasil mencabut seluruh sesi aktif milik '${user.username}'.`
      );
      setRevokeDialogOpen(false);
      router.refresh();
    } catch (err) {
      setRevokeError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat mencabut sesi pengguna."
      );
    } finally {
      setIsRevoking(false);
    }
  };

  return (
    <>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Kolom Kiri (2/3): Form Edit Profil & Status Pengguna */}
        <Card className="lg:col-span-2 border border-neutral-200 dark:border-slate-800 shadow-sm">
          <CardHeader className="border-b border-neutral-200 dark:border-slate-800">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <UserCog className="w-5 h-5" />
              </div>
              <div>
                <CardTitle className="text-lg font-semibold">
                  Informasi &amp; Status Akun Pengguna
                </CardTitle>
                <CardDescription className="text-xs">
                  Perbarui identitas profil, alamat email, status aktif, dan
                  status verifikasi akun.
                </CardDescription>
              </div>
            </div>
          </CardHeader>

          <CardContent className="p-6">
            <form onSubmit={handleFormSubmit} className="space-y-5">
              {formError && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription className="text-xs">
                    {formError}
                  </AlertDescription>
                </Alert>
              )}

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Username (Read-Only) */}
                <div className="space-y-1.5">
                  <Label className="text-xs font-semibold text-muted-foreground">
                    Username (Permanen)
                  </Label>
                  <div className="flex items-center gap-1.5">
                    <Input
                      value={user.username}
                      disabled
                      className="font-mono text-xs bg-neutral-100 dark:bg-slate-900"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      className="h-9 w-9 shrink-0"
                      onClick={() => handleCopy(user.username, "Username")}
                      title="Salin Username"
                    >
                      {copiedField === "Username" ? (
                        <Check className="w-4 h-4 text-emerald-500" />
                      ) : (
                        <Copy className="w-4 h-4" />
                      )}
                    </Button>
                  </div>
                </div>

                {/* User ID Global (Read-Only) */}
                <div className="space-y-1.5">
                  <Label className="text-xs font-semibold text-muted-foreground">
                    User ID Global (UUID)
                  </Label>
                  <div className="flex items-center gap-1.5">
                    <Input
                      value={user.id}
                      disabled
                      className="font-mono text-xs bg-neutral-100 dark:bg-slate-900"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      className="h-9 w-9 shrink-0"
                      onClick={() => handleCopy(user.id, "User ID")}
                      title="Salin User ID"
                    >
                      {copiedField === "User ID" ? (
                        <Check className="w-4 h-4 text-emerald-500" />
                      ) : (
                        <Copy className="w-4 h-4" />
                      )}
                    </Button>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="edit-fullName" className="text-xs font-semibold">
                    Nama Lengkap <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="edit-fullName"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    disabled={isSaving}
                    required
                    className={`text-xs ${
                      fieldErrors.fullName ? "border-red-500" : ""
                    }`}
                  />
                  {fieldErrors.fullName && (
                    <p className="text-xs text-red-500">
                      {fieldErrors.fullName}
                    </p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="edit-email" className="text-xs font-semibold">
                    Alamat Email <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="edit-email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    disabled={isSaving}
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

              {/* Pengaturan Status Akun */}
              <div className="space-y-3 pt-1">
                <div className="flex items-center justify-between rounded-lg border p-3.5 bg-neutral-50/50 dark:bg-slate-900/50">
                  <div className="space-y-0.5 pr-4">
                    <Label
                      htmlFor="edit-isActive"
                      className="text-xs font-semibold cursor-pointer"
                    >
                      Status Akun Aktif
                    </Label>
                    <p className="text-[11px] text-muted-foreground">
                      Jika dinonaktifkan, pengguna tidak dapat login dan seluruh
                      sesi aktifnya akan segera dicabut oleh sistem.
                    </p>
                  </div>
                  <Switch
                    id="edit-isActive"
                    checked={isActive}
                    onCheckedChange={setIsActive}
                    disabled={isSaving}
                  />
                </div>

                <div className="flex items-center justify-between rounded-lg border p-3.5 bg-neutral-50/50 dark:bg-slate-900/50">
                  <div className="space-y-0.5 pr-4">
                    <Label
                      htmlFor="edit-isVerified"
                      className="text-xs font-semibold cursor-pointer"
                    >
                      Status Verifikasi Akun
                    </Label>
                    <p className="text-[11px] text-muted-foreground">
                      Menentukan apakah akun telah melewati verifikasi/aktivasi
                      identitas.
                    </p>
                  </div>
                  <Switch
                    id="edit-isVerified"
                    checked={isAccountVerified}
                    onCheckedChange={setIsAccountVerified}
                    disabled={isSaving}
                  />
                </div>
              </div>

              <Can permission="users.write">
                <div className="flex justify-end pt-2 border-t border-neutral-200 dark:border-slate-800">
                  <Button
                    type="submit"
                    disabled={isSaving}
                    className="gap-2 text-xs"
                  >
                    {isSaving ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Save className="w-4 h-4" />
                    )}
                    Simpan Perubahan Profil
                  </Button>
                </div>
              </Can>
            </form>
          </CardContent>
        </Card>

        {/* Kolom Kanan (1/3): Proteksi Lockout & Manajemen Sesi */}
        <div className="space-y-6">
          {/* Kartu Proteksi Lockout / Buka Kunci */}
          <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base font-semibold flex items-center gap-2">
                  <Lock className="w-4 h-4 text-primary" />
                  Proteksi Brute-Force
                </CardTitle>
                {isLocked ? (
                  <Badge variant="danger" className="text-[11px]">
                    Terkunci
                  </Badge>
                ) : (
                  <Badge variant="success" className="text-[11px]">
                    Normal
                  </Badge>
                )}
              </div>
              <CardDescription className="text-xs">
                Status penguncian otomatis akibat kegagalan login berulang.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-4">
              {unlockError && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription className="text-xs">
                    {unlockError}
                  </AlertDescription>
                </Alert>
              )}

              <div className="rounded-lg border p-3 bg-neutral-50/60 dark:bg-slate-900/50 space-y-2 text-xs">
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">
                    Gagal Login Berturut-turut:
                  </span>
                  <span className="font-mono font-semibold text-foreground">
                    {user.failedLoginCount} kali
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground">
                    Terkunci Hingga:
                  </span>
                  <span className="font-medium text-foreground">
                    {isLocked
                      ? formatDateTime(user.lockoutEndUtc)
                      : "Tidak terkunci"}
                  </span>
                </div>
              </div>

              <Can permission="users.unlock">
                <Button
                  type="button"
                  variant={isLocked ? "default" : "outline"}
                  className="w-full gap-2 text-xs"
                  onClick={handleUnlockUser}
                  disabled={
                    isUnlocking || (!isLocked && user.failedLoginCount === 0)
                  }
                >
                  {isUnlocking ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <Unlock className="w-4 h-4" />
                  )}
                  Buka Kunci &amp; Reset Counter
                </Button>
              </Can>
            </CardContent>
          </Card>

          {/* Kartu Paksa Logout (Revoke Sessions) */}
          <Card className="border border-red-200/80 dark:border-red-900/50 shadow-sm">
            <CardHeader className="pb-3">
              <CardTitle className="text-base font-semibold text-red-700 dark:text-red-400 flex items-center gap-2">
                <ShieldAlert className="w-4 h-4" />
                Paksa Logout Sesi Aktif
              </CardTitle>
              <CardDescription className="text-xs">
                Cabut seluruh Refresh Token aktif milik pengguna ini di semua
                perangkat dan aplikasi klien.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-3">
              <p className="text-xs text-muted-foreground leading-relaxed">
                Gunakan tindakan darurat ini apabila akun terindikasi
                dikompromikan atau setelah perubahan hak akses sensitif.
              </p>

              <Can permission="users.revoke-sessions">
                <Button
                  type="button"
                  variant="destructive"
                  className="w-full gap-2 text-xs"
                  onClick={() => {
                    setRevokeError(null);
                    setRevokeDialogOpen(true);
                  }}
                >
                  <LogOut className="w-4 h-4" />
                  Paksa Logout Semua Sesi
                </Button>
              </Can>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Dialog Konfirmasi Nonaktifkan Pengguna */}
      <Dialog
        open={confirmDeactivateOpen}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !isSaving) {
            setConfirmDeactivateOpen(false);
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-amber-100 dark:bg-amber-950/60 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0">
                <AlertTriangle className="w-5 h-5" />
              </div>
              <div>
                <DialogTitle className="text-lg font-semibold">
                  Nonaktifkan Akun Pengguna?
                </DialogTitle>
                <DialogDescription className="text-xs">
                  Konfirmasi penonaktifan akses SSO untuk{" "}
                  <strong>@{user.username}</strong>.
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="space-y-3 py-2 text-xs text-muted-foreground leading-relaxed">
            <p>
              Menonaktifkan akun <strong>{user.fullName}</strong> akan langsung
              mencabut seluruh sesi refresh token yang sedang aktif dan menolak
              percobaan login pada seluruh aplikasi klien.
            </p>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              autoFocus
              onClick={() => setConfirmDeactivateOpen(false)}
              disabled={isSaving}
              className="text-xs"
            >
              Batal
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={executeUpdateUser}
              disabled={isSaving}
              className="gap-2 text-xs"
            >
              {isSaving && <Loader2 className="w-4 h-4 animate-spin" />}
              Ya, Nonaktifkan Pengguna
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog Konfirmasi Paksa Logout (Revoke Sessions) */}
      <Dialog
        open={revokeDialogOpen}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !isRevoking) {
            setRevokeDialogOpen(false);
            setRevokeError(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 flex items-center justify-center shrink-0">
                <LogOut className="w-5 h-5" />
              </div>
              <div>
                <DialogTitle className="text-lg font-semibold text-red-700 dark:text-red-400">
                  Paksa Logout Semua Sesi?
                </DialogTitle>
                <DialogDescription className="text-xs">
                  Seluruh sesi aktif pengguna akan dihentikan seketika.
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="space-y-3 py-2">
            {revokeError && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">
                  {revokeError}
                </AlertDescription>
              </Alert>
            )}

            <p className="text-xs text-muted-foreground leading-relaxed">
              Apakah Anda yakin ingin mencabut seluruh Refresh Token aktif milik{" "}
              <strong className="text-foreground">
                {user.fullName} (@{user.username})
              </strong>
              ? Pengguna harus melakukan login ulang pada semua perangkat.
            </p>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              autoFocus
              onClick={() => setRevokeDialogOpen(false)}
              disabled={isRevoking}
              className="text-xs"
            >
              Batal
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleConfirmRevokeSessions}
              disabled={isRevoking}
              className="gap-2 text-xs"
            >
              {isRevoking && <Loader2 className="w-4 h-4 animate-spin" />}
              Ya, Cabut Seluruh Sesi
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
