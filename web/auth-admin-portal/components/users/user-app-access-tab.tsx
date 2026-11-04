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
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { AdminUserDto, ClientAppDto, RoleDto } from "@/lib/api/types";
import {
  AlertCircle,
  AppWindow,
  CheckCircle2,
  ExternalLink,
  KeyRound,
  Layers,
  Loader2,
  Lock,
  Save,
  Search,
  Shield,
  Sparkles,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import toast from "react-hot-toast";
import {
  getUserAppAccessDetailAction,
  setUserAppRolesAction,
  UserAppAccessDetail,
} from "./actions/users";

interface UserAppAccessTabProps {
  user: AdminUserDto;
  apps: ClientAppDto[];
  initialSelectedAppId: string;
  initialAccessDetail: UserAppAccessDetail | null;
  appsFetchError?: string | null;
}

export function UserAppAccessTab({
  user,
  apps,
  initialSelectedAppId,
  initialAccessDetail,
  appsFetchError,
}: UserAppAccessTabProps) {
  const router = useRouter();

  const [appSearch, setAppSearch] = useState("");
  const [selectedAppId, setSelectedAppId] =
    useState<string>(initialSelectedAppId);

  // State data role pada aplikasi terpilih
  const [availableRoles, setAvailableRoles] = useState<RoleDto[]>(
    initialAccessDetail?.availableRoles || []
  );
  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>(
    initialAccessDetail?.userAccess.assignedRoles.map((r) => r.id) || []
  );
  const [isLoadingAccess, setIsLoadingAccess] = useState(false);
  const [accessError, setAccessError] = useState<string | null>(null);
  const [isSavingRoles, setIsSavingRoles] = useState(false);

  const selectedApp = useMemo(
    () => apps.find((a) => a.id === selectedAppId) || null,
    [apps, selectedAppId]
  );

  const filteredApps = useMemo(() => {
    const q = appSearch.toLowerCase().trim();
    return apps.filter(
      (a) =>
        !q ||
        a.name.toLowerCase().includes(q) ||
        a.clientId.toLowerCase().includes(q)
    );
  }, [apps, appSearch]);

  // Peta jumlah role yang dimiliki user per clientAppId (dari user.appAssignments)
  const assignedRolesMap = useMemo(() => {
    const map: Record<string, string[]> = {};
    for (const assign of user.appAssignments) {
      map[assign.clientAppId] = assign.roles;
    }
    return map;
  }, [user.appAssignments]);

  const handleSelectApp = async (appId: string) => {
    setSelectedAppId(appId);
    setIsLoadingAccess(true);
    setAccessError(null);

    try {
      const res = await getUserAppAccessDetailAction(user.id, appId);
      if (!res.success || !res.data) {
        setAccessError(
          res.error || "Gagal memuat daftar role untuk aplikasi ini."
        );
        setAvailableRoles([]);
        setSelectedRoleIds([]);
        return;
      }

      setAvailableRoles(res.data.availableRoles);
      setSelectedRoleIds(res.data.userAccess.assignedRoles.map((r) => r.id));
    } catch (err) {
      setAccessError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat memuat akses aplikasi."
      );
    } finally {
      setIsLoadingAccess(false);
    }
  };

  const toggleRole = (roleId: string) => {
    setSelectedRoleIds((prev) =>
      prev.includes(roleId)
        ? prev.filter((id) => id !== roleId)
        : [...prev, roleId]
    );
  };

  // Hitung daftar kode permission efektif secara dinamis dari role yang dicentang
  const effectivePermissionsPreview = useMemo(() => {
    const codes = new Set<string>();
    for (const role of availableRoles) {
      if (selectedRoleIds.includes(role.id)) {
        for (const perm of role.permissions) {
          codes.add(perm.code);
        }
      }
    }
    return Array.from(codes).sort();
  }, [availableRoles, selectedRoleIds]);

  const handleSaveRoles = async () => {
    if (!selectedApp) return;
    setAccessError(null);
    setIsSavingRoles(true);

    try {
      const res = await setUserAppRolesAction(user.id, selectedApp.id, {
        roleIds: selectedRoleIds,
      });

      if (!res.success) {
        setAccessError(
          res.error || "Gagal menyimpan penugasan role pengguna."
        );
        setIsSavingRoles(false);
        return;
      }

      toast.success(
        `Hak akses '${user.username}' pada aplikasi '${selectedApp.name}' berhasil diperbarui (${selectedRoleIds.length} role).`
      );
      router.refresh();
    } catch (err) {
      setAccessError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menyimpan penugasan role."
      );
    } finally {
      setIsSavingRoles(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Ringkasan Hak Akses Aplikasi Saat Ini */}
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        <CardHeader className="pb-3 border-b border-neutral-200 dark:border-slate-800">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <Layers className="w-4 h-4" />
              </div>
              <div>
                <CardTitle className="text-base font-semibold">
                  Ringkasan Akses Aplikasi Aktif ({user.appAssignments.length}{" "}
                  Aplikasi)
                </CardTitle>
                <CardDescription className="text-xs">
                  Daftar aplikasi tempat pengguna{" "}
                  <strong>@{user.username}</strong> saat ini memiliki minimal
                  satu Role.
                </CardDescription>
              </div>
            </div>
          </div>
        </CardHeader>

        <CardContent className="p-4">
          {user.appAssignments.length === 0 ? (
            <div className="p-6 rounded-lg border border-dashed text-center text-xs text-muted-foreground">
              Pengguna ini belum ditugaskan ke Role pada aplikasi mana pun.
              Pilih aplikasi pada panel di bawah untuk memberikan hak akses.
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {user.appAssignments.map((assign) => {
                const isSelected = assign.clientAppId === selectedAppId;
                return (
                  <div
                    key={assign.clientAppId}
                    onClick={() => void handleSelectApp(assign.clientAppId)}
                    className={`p-3.5 rounded-lg border transition-colors cursor-pointer ${
                      isSelected
                        ? "border-primary bg-primary/5 dark:bg-primary/10"
                        : "border-neutral-200 dark:border-slate-800 hover:bg-neutral-50 dark:hover:bg-slate-900/60"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <div className="font-semibold text-sm text-foreground">
                          {assign.appName}
                        </div>
                        <code className="text-[11px] font-mono text-muted-foreground">
                          {assign.clientId}
                        </code>
                      </div>
                      <Badge variant="secondary" className="text-[10px]">
                        {assign.roles.length} Role
                      </Badge>
                    </div>
                    <div className="flex flex-wrap gap-1 mt-2.5">
                      {assign.roles.map((roleName) => (
                        <Badge
                          key={roleName}
                          variant="outline"
                          className="text-[11px] bg-white dark:bg-slate-950"
                        >
                          {roleName}
                        </Badge>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Panel Utama: Pilih Aplikasi -> Centang Role */}
      {appsFetchError ? (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription className="text-xs">
            {appsFetchError}
          </AlertDescription>
        </Alert>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Kolom Kiri (4/12): Pemilih Aplikasi (ClientApp) */}
          <Card className="lg:col-span-4 border border-neutral-200 dark:border-slate-800 shadow-sm h-fit">
            <CardHeader className="p-4 border-b border-neutral-200 dark:border-slate-800">
              <CardTitle className="text-sm font-semibold flex items-center gap-2">
                <AppWindow className="w-4 h-4 text-primary" />
                1. Pilih Aplikasi Klien ({apps.length})
              </CardTitle>
              <div className="relative mt-2">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground" />
                <Input
                  placeholder="Cari aplikasi..."
                  value={appSearch}
                  onChange={(e) => setAppSearch(e.target.value)}
                  className="pl-8 h-8 text-xs"
                />
              </div>
            </CardHeader>

            <CardContent className="p-2 max-h-96 overflow-y-auto divide-y divide-neutral-100 dark:divide-slate-800/60">
              {filteredApps.length === 0 ? (
                <div className="p-4 text-center text-xs text-muted-foreground">
                  Aplikasi tidak ditemukan.
                </div>
              ) : (
                filteredApps.map((app) => {
                  const isSelected = app.id === selectedAppId;
                  const assignedRoleNames = assignedRolesMap[app.id] || [];

                  return (
                    <button
                      key={app.id}
                      type="button"
                      onClick={() => void handleSelectApp(app.id)}
                      className={`w-full text-left p-3 rounded-lg transition-colors flex items-center justify-between gap-2 ${
                        isSelected
                          ? "bg-primary/10 text-primary font-medium"
                          : "hover:bg-neutral-50 dark:hover:bg-slate-900/60 text-foreground"
                      }`}
                    >
                      <div className="min-w-0">
                        <div className="flex items-center gap-1.5">
                          <span className="text-xs font-semibold truncate">
                            {app.name}
                          </span>
                          {!app.isActive && (
                            <Badge
                              variant="secondary"
                              className="text-[9px] px-1 py-0"
                            >
                              Nonaktif
                            </Badge>
                          )}
                        </div>
                        <div className="text-[11px] font-mono text-muted-foreground truncate">
                          {app.clientId}
                        </div>
                      </div>

                      {assignedRoleNames.length > 0 ? (
                        <Badge
                          variant="success"
                          className="text-[10px] px-2 py-0.5 shrink-0"
                        >
                          {assignedRoleNames.length} Role
                        </Badge>
                      ) : (
                        <span className="text-[11px] text-muted-foreground shrink-0">
                          0 Role
                        </span>
                      )}
                    </button>
                  );
                })
              )}
            </CardContent>
          </Card>

          {/* Kolom Kanan (8/12): Checklist Role & Pratinjau Izin Efektif */}
          <Card className="lg:col-span-8 border border-neutral-200 dark:border-slate-800 shadow-sm">
            {!selectedApp ? (
              <CardContent className="p-12 text-center text-xs text-muted-foreground">
                Silakan pilih aplikasi di sebelah kiri untuk mengatur penugasan
                role.
              </CardContent>
            ) : (
              <>
                <CardHeader className="border-b border-neutral-200 dark:border-slate-800">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                    <div>
                      <div className="flex flex-wrap items-center gap-2">
                        <CardTitle className="text-base font-semibold">
                          2. Penugasan Role pada {selectedApp.name}
                        </CardTitle>
                        <code className="text-xs font-mono bg-neutral-100 dark:bg-slate-800 px-2 py-0.5 rounded">
                          {selectedApp.clientId}
                        </code>
                      </div>
                      <CardDescription className="text-xs mt-0.5">
                        Centang satu atau lebih Role yang ingin ditugaskan ke{" "}
                        <strong>@{user.username}</strong> pada aplikasi ini.
                      </CardDescription>
                    </div>

                    <Link href={`/apps/${selectedApp.id}`}>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8 gap-1.5 text-xs"
                      >
                        <span>Kelola Role Aplikasi</span>
                        <ExternalLink className="w-3.5 h-3.5" />
                      </Button>
                    </Link>
                  </div>
                </CardHeader>

                <CardContent className="p-6 space-y-6">
                  {accessError && (
                    <Alert variant="destructive">
                      <AlertCircle className="h-4 w-4" />
                      <AlertDescription className="text-xs">
                        {accessError}
                      </AlertDescription>
                    </Alert>
                  )}

                  {isLoadingAccess ? (
                    <div className="py-12 flex flex-col items-center justify-center gap-2 text-muted-foreground">
                      <Loader2 className="w-6 h-6 animate-spin text-primary" />
                      <p className="text-xs">
                        Memuat daftar Role untuk {selectedApp.name}...
                      </p>
                    </div>
                  ) : availableRoles.length === 0 ? (
                    <div className="p-8 rounded-lg border border-dashed text-center space-y-2">
                      <Shield className="w-8 h-8 text-muted-foreground/50 mx-auto" />
                      <p className="text-sm font-medium text-foreground">
                        Aplikasi Ini Belum Memiliki Role
                      </p>
                      <p className="text-xs text-muted-foreground max-w-md mx-auto">
                        Tambahkan Role terlebih dahulu pada halaman detail
                        aplikasi <strong>{selectedApp.name}</strong> sebelum
                        menugaskan pengguna.
                      </p>
                    </div>
                  ) : (
                    <>
                      {/* Toolbar Pilih Semua / Cabut Semua Role */}
                      <div className="flex items-center justify-between">
                        <span className="text-xs font-semibold text-foreground">
                          Daftar Role Tersedia ({selectedRoleIds.length} dari{" "}
                          {availableRoles.length} dipilih)
                        </span>
                        <Can permission="users.write">
                          <div className="flex items-center gap-2">
                            <button
                              type="button"
                              onClick={() =>
                                setSelectedRoleIds(
                                  availableRoles.map((r) => r.id)
                                )
                              }
                              disabled={
                                isSavingRoles ||
                                selectedRoleIds.length === availableRoles.length
                              }
                              className="text-xs font-medium text-primary hover:underline disabled:opacity-40"
                            >
                              Pilih Semua Role
                            </button>
                            <span className="text-muted-foreground text-xs">
                              •
                            </span>
                            <button
                              type="button"
                              onClick={() => setSelectedRoleIds([])}
                              disabled={
                                isSavingRoles || selectedRoleIds.length === 0
                              }
                              className="text-xs text-muted-foreground hover:text-foreground hover:underline disabled:opacity-40"
                            >
                              Cabut Semua Role
                            </button>
                          </div>
                        </Can>
                      </div>

                      {/* Daftar Checkbox Role */}
                      <div className="space-y-2.5">
                        {availableRoles.map((role) => {
                          const isChecked = selectedRoleIds.includes(role.id);
                          const checkId = `user-app-role-${role.id}`;

                          return (
                            <label
                              key={role.id}
                              htmlFor={checkId}
                              className={`flex items-start gap-3 p-3.5 rounded-lg border transition-colors cursor-pointer ${
                                isChecked
                                  ? "border-primary/60 bg-primary/5 dark:bg-primary/10"
                                  : "border-neutral-200 dark:border-slate-800 hover:bg-neutral-50 dark:hover:bg-slate-900/50"
                              }`}
                            >
                              <Checkbox
                                id={checkId}
                                checked={isChecked}
                                onCheckedChange={() => toggleRole(role.id)}
                                disabled={isSavingRoles}
                                className="mt-0.5"
                              />
                              <div className="flex-1 min-w-0 space-y-1.5">
                                <div className="flex flex-wrap items-center gap-2">
                                  <span className="font-semibold text-sm text-foreground">
                                    {role.name}
                                  </span>
                                  {role.isSystemRole && (
                                    <Badge
                                      variant="outline"
                                      className="text-[10px] px-1.5 py-0 border-blue-300 dark:border-blue-700 bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 gap-1"
                                    >
                                      <Shield className="w-2.5 h-2.5" />
                                      System Role
                                    </Badge>
                                  )}
                                  <Badge
                                    variant="secondary"
                                    className="text-[10px]"
                                  >
                                    {role.permissions.length} Izin
                                  </Badge>
                                </div>

                                {role.description && (
                                  <p className="text-xs text-muted-foreground">
                                    {role.description}
                                  </p>
                                )}

                                {role.permissions.length > 0 && (
                                  <div className="flex flex-wrap gap-1 pt-0.5">
                                    {role.permissions.map((perm) => (
                                      <code
                                        key={perm.id}
                                        className="text-[11px] font-mono bg-white dark:bg-slate-900 text-neutral-700 dark:text-neutral-300 px-1.5 py-0.5 rounded border border-neutral-200/80 dark:border-slate-700"
                                      >
                                        {perm.code}
                                      </code>
                                    ))}
                                  </div>
                                )}
                              </div>
                            </label>
                          );
                        })}
                      </div>

                      {/* Pratinjau Izin Efektif (Effective Permissions) */}
                      <div className="rounded-lg border border-neutral-200 dark:border-slate-800 bg-neutral-50/60 dark:bg-slate-900/40 p-4 space-y-2.5">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2 text-xs font-semibold text-foreground">
                            <Sparkles className="w-4 h-4 text-primary" />
                            <span>
                              Izin Efektif pada {selectedApp.name} (
                              {effectivePermissionsPreview.length} Permission)
                            </span>
                          </div>
                          {selectedRoleIds.length > 0 ? (
                            <Badge variant="success" className="text-[10px] gap-1">
                              <CheckCircle2 className="w-3 h-3" />
                              Dapat Login ke {selectedApp.clientId}
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="text-[10px] gap-1">
                              <Lock className="w-3 h-3" />
                              Tidak Punya Akses Login
                            </Badge>
                          )}
                        </div>

                        {effectivePermissionsPreview.length === 0 ? (
                          <p className="text-xs text-muted-foreground italic">
                            {selectedRoleIds.length === 0
                              ? "Tidak ada role yang dipilih. Pengguna tidak akan diizinkan login ke aplikasi ini."
                              : "Role yang dipilih belum memiliki kode permission yang dipetakan."}
                          </p>
                        ) : (
                          <div className="flex flex-wrap gap-1.5">
                            {effectivePermissionsPreview.map((code) => (
                              <code
                                key={code}
                                className="inline-flex items-center gap-1 text-[11px] font-mono font-medium bg-white dark:bg-slate-950 text-foreground px-2 py-0.5 rounded border border-neutral-200 dark:border-slate-800"
                              >
                                <KeyRound className="w-2.5 h-2.5 text-primary" />
                                {code}
                              </code>
                            ))}
                          </div>
                        )}
                      </div>

                      {/* Tombol Simpan Penugasan Role */}
                      <Can permission="users.write">
                        <div className="flex justify-end pt-2 border-t border-neutral-200 dark:border-slate-800">
                          <Button
                            type="button"
                            onClick={handleSaveRoles}
                            disabled={isSavingRoles}
                            className="gap-2 text-xs"
                          >
                            {isSavingRoles ? (
                              <Loader2 className="w-4 h-4 animate-spin" />
                            ) : (
                              <Save className="w-4 h-4" />
                            )}
                            Simpan Penugasan Role ({selectedApp.clientId})
                          </Button>
                        </div>
                      </Can>
                    </>
                  )}
                </CardContent>
              </>
            )}
          </Card>
        </div>
      )}
    </div>
  );
}
