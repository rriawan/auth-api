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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Textarea } from "@/components/ui/textarea";
import { ClientAppDto, PermissionDto, RoleDto } from "@/lib/api/types";
import {
  AlertCircle,
  AlertTriangle,
  CheckSquare,
  Info,
  KeyRound,
  Loader2,
  Lock,
  Pencil,
  Plus,
  Search,
  Shield,
  Trash2,
  Users,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import toast from "react-hot-toast";
import {
  createRoleAction,
  deleteRoleAction,
  setRolePermissionsAction,
  updateRoleAction,
} from "./actions/roles";

interface AppRolesTabProps {
  app: ClientAppDto;
  initialRoles: RoleDto[];
  availablePermissions: PermissionDto[];
  fetchError?: string | null;
}

export function AppRolesTab({
  app,
  initialRoles,
  availablePermissions,
  fetchError,
}: AppRolesTabProps) {
  const router = useRouter();

  // Filter & Pencarian Tabel Role
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState<"all" | "system" | "custom">(
    "all"
  );

  // Dialog Tambah / Ubah Role
  const [formDialogOpen, setFormDialogOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [initialPermIds, setInitialPermIds] = useState<string[]>([]);
  const [createPermSearch, setCreatePermSearch] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Dialog Pemetaan Permission (Checkbox List)
  const [mappingRole, setMappingRole] = useState<RoleDto | null>(null);
  const [selectedPermIds, setSelectedPermIds] = useState<string[]>([]);
  const [mapSearch, setMapSearch] = useState("");
  const [isSavingMap, setIsSavingMap] = useState(false);
  const [mapError, setMapError] = useState<string | null>(null);

  // Dialog Konfirmasi Hapus Role
  const [deletingRole, setDeletingRole] = useState<RoleDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const filteredRoles = useMemo(() => {
    return initialRoles.filter((role) => {
      const q = search.toLowerCase().trim();
      const matchQuery =
        !q ||
        role.name.toLowerCase().includes(q) ||
        (role.description && role.description.toLowerCase().includes(q)) ||
        role.permissions.some((p) => p.code.toLowerCase().includes(q));

      const matchType =
        typeFilter === "all" ||
        (typeFilter === "system" && role.isSystemRole) ||
        (typeFilter === "custom" && !role.isSystemRole);

      return matchQuery && matchType;
    });
  }, [initialRoles, search, typeFilter]);

  // Pengelompokan permission per modul untuk dialog pemetaan
  const filteredMappingPermissions = useMemo(() => {
    const q = mapSearch.toLowerCase().trim();
    return availablePermissions.filter(
      (p) =>
        !q ||
        p.code.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q))
    );
  }, [availablePermissions, mapSearch]);

  const groupedMappingPermissions = useMemo(() => {
    const groups: Record<string, PermissionDto[]> = {};
    for (const perm of filteredMappingPermissions) {
      const mod = perm.code.split(".")[0] || "lainnya";
      if (!groups[mod]) {
        groups[mod] = [];
      }
      groups[mod].push(perm);
    }
    return Object.entries(groups).sort(([a], [b]) => a.localeCompare(b));
  }, [filteredMappingPermissions]);

  const filteredCreatePermissions = useMemo(() => {
    const q = createPermSearch.toLowerCase().trim();
    return availablePermissions.filter(
      (p) =>
        !q ||
        p.code.toLowerCase().includes(q) ||
        (p.description && p.description.toLowerCase().includes(q))
    );
  }, [availablePermissions, createPermSearch]);

  const openCreateDialog = () => {
    setEditingRole(null);
    setName("");
    setDescription("");
    setInitialPermIds([]);
    setCreatePermSearch("");
    setFormError(null);
    setFieldErrors({});
    setFormDialogOpen(true);
  };

  const openEditDialog = (role: RoleDto) => {
    setEditingRole(role);
    setName(role.name);
    setDescription(role.description || "");
    setFormError(null);
    setFieldErrors({});
    setFormDialogOpen(true);
  };

  const toggleInitialPerm = (permId: string) => {
    setInitialPermIds((prev) =>
      prev.includes(permId)
        ? prev.filter((id) => id !== permId)
        : [...prev, permId]
    );
  };

  const handleSubmitRoleForm = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setIsSubmitting(true);

    try {
      if (editingRole) {
        const res = await updateRoleAction(
          app.id,
          editingRole.id,
          editingRole.isSystemRole,
          editingRole.name,
          {
            name: editingRole.isSystemRole ? editingRole.name : name,
            description: description || null,
          }
        );

        if (!res.success) {
          setFormError(res.error || "Gagal memperbarui role.");
          if (res.fieldErrors) setFieldErrors(res.fieldErrors);
          setIsSubmitting(false);
          return;
        }

        toast.success(`Role '${editingRole.name}' berhasil diperbarui.`);
      } else {
        const res = await createRoleAction(app.id, {
          name,
          description: description || null,
          permissionIds: initialPermIds,
        });

        if (!res.success) {
          setFormError(res.error || "Gagal membuat role baru.");
          if (res.fieldErrors) setFieldErrors(res.fieldErrors);
          setIsSubmitting(false);
          return;
        }

        toast.success(`Role '${name}' berhasil dibuat.`);
      }

      setFormDialogOpen(false);
      router.refresh();
    } catch (err) {
      setFormError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menyimpan role."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const openMappingDialog = (role: RoleDto) => {
    setMappingRole(role);
    setSelectedPermIds(role.permissions.map((p) => p.id));
    setMapSearch("");
    setMapError(null);
  };

  const toggleMappedPerm = (permId: string) => {
    setSelectedPermIds((prev) =>
      prev.includes(permId)
        ? prev.filter((id) => id !== permId)
        : [...prev, permId]
    );
  };

  const toggleModulePerms = (modulePerms: PermissionDto[]) => {
    const ids = modulePerms.map((p) => p.id);
    const allSelected = ids.every((id) => selectedPermIds.includes(id));

    if (allSelected) {
      setSelectedPermIds((prev) => prev.filter((id) => !ids.includes(id)));
    } else {
      setSelectedPermIds((prev) => Array.from(new Set([...prev, ...ids])));
    }
  };

  const handleSavePermissionsMap = async () => {
    if (!mappingRole) return;
    setMapError(null);
    setIsSavingMap(true);

    try {
      const res = await setRolePermissionsAction(app.id, mappingRole.id, {
        permissionIds: selectedPermIds,
      });

      if (!res.success) {
        setMapError(res.error || "Gagal memperbarui pemetaan permission.");
        setIsSavingMap(false);
        return;
      }

      toast.success(
        `Pemetaan permission untuk role '${mappingRole.name}' berhasil disimpan (${selectedPermIds.length} izin).`
      );
      setMappingRole(null);
      router.refresh();
    } catch (err) {
      setMapError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menyimpan pemetaan permission."
      );
    } finally {
      setIsSavingMap(false);
    }
  };

  const openDeleteDialog = (role: RoleDto) => {
    if (role.isSystemRole) return;
    setDeletingRole(role);
    setDeleteError(null);
  };

  const handleConfirmDeleteRole = async () => {
    if (!deletingRole) return;
    setDeleteError(null);
    setIsDeleting(true);

    try {
      const res = await deleteRoleAction(
        app.id,
        deletingRole.id,
        deletingRole.isSystemRole
      );

      if (!res.success) {
        setDeleteError(res.error || "Gagal menghapus role.");
        setIsDeleting(false);
        return;
      }

      toast.success(`Role '${deletingRole.name}' berhasil dihapus.`);
      setDeletingRole(null);
      router.refresh();
    } catch (err) {
      setDeleteError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menghapus role."
      );
    } finally {
      setIsDeleting(false);
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        year: "numeric",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  const systemRolesCount = initialRoles.filter((r) => r.isSystemRole).length;
  const customRolesCount = initialRoles.filter((r) => !r.isSystemRole).length;

  return (
    <>
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        <CardHeader className="border-b border-neutral-200 dark:border-slate-800">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <Users className="w-5 h-5" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <CardTitle className="text-lg font-semibold">
                    Daftar Peran Aplikasi (Roles)
                  </CardTitle>
                  <Badge variant="outline" className="text-xs">
                    {initialRoles.length} Role
                  </Badge>
                </div>
                <CardDescription className="text-xs mt-0.5">
                  Kelola grup peran pengguna dan pemetaan hak akses (permissions)
                  untuk aplikasi <strong>{app.name}</strong>.
                </CardDescription>
              </div>
            </div>

            <Can permission="roles.write">
              <Button
                type="button"
                onClick={openCreateDialog}
                className="gap-2 text-xs h-9 self-start sm:self-auto"
              >
                <Plus className="w-4 h-4" />
                Tambah Role
              </Button>
            </Can>
          </div>
        </CardHeader>

        {fetchError ? (
          <CardContent className="p-6">
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription className="text-xs">
                {fetchError}
              </AlertDescription>
            </Alert>
          </CardContent>
        ) : (
          <>
            {/* Bar Pencarian & Filter Tipe Role */}
            <div className="p-4 border-b border-neutral-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-neutral-50/50 dark:bg-slate-900/40">
              <div className="relative flex-1 max-w-sm">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <Input
                  placeholder="Cari nama role, deskripsi, atau izin..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-9 h-9 text-xs bg-white dark:bg-slate-950"
                />
              </div>

              <div className="flex items-center gap-1.5 self-start sm:self-auto">
                <Button
                  type="button"
                  variant={typeFilter === "all" ? "default" : "outline"}
                  size="sm"
                  onClick={() => setTypeFilter("all")}
                  className="text-xs h-8"
                >
                  Semua ({initialRoles.length})
                </Button>
                <Button
                  type="button"
                  variant={typeFilter === "system" ? "default" : "outline"}
                  size="sm"
                  onClick={() => setTypeFilter("system")}
                  className="text-xs h-8"
                >
                  System Role ({systemRolesCount})
                </Button>
                <Button
                  type="button"
                  variant={typeFilter === "custom" ? "default" : "outline"}
                  size="sm"
                  onClick={() => setTypeFilter("custom")}
                  className="text-xs h-8"
                >
                  Custom ({customRolesCount})
                </Button>
              </div>
            </div>

            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent bg-neutral-50 dark:bg-slate-900/60 text-xs">
                    <TableHead className="w-[28%]">Nama Role</TableHead>
                    <TableHead>Izin yang Dipetakan (Permissions)</TableHead>
                    <TableHead className="w-[14%]">Dibuat Pada</TableHead>
                    <TableHead className="text-right w-[22%]">Aksi</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredRoles.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} className="h-44 text-center">
                        <div className="flex flex-col items-center justify-center text-muted-foreground space-y-2">
                          <Shield className="w-8 h-8 opacity-40" />
                          <p className="text-sm font-medium">
                            {initialRoles.length === 0
                              ? "Belum ada role yang terdaftar pada aplikasi ini."
                              : "Tidak ada role yang sesuai dengan filter."}
                          </p>
                          {initialRoles.length === 0 ? (
                            <p className="text-xs max-w-md">
                              Buat role baru (misal{" "}
                              <code className="font-mono">Admin</code> atau{" "}
                              <code className="font-mono">FinanceManager</code>)
                              lalu petakan kode permission yang dimiliki role
                              tersebut.
                            </p>
                          ) : (
                            <p className="text-xs">
                              Coba gunakan kata kunci pencarian lain atau ubah
                              filter tipe role.
                            </p>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredRoles.map((role) => {
                      const previewPerms = role.permissions.slice(0, 4);
                      const remainingCount =
                        role.permissions.length - previewPerms.length;

                      return (
                        <TableRow
                          key={role.id}
                          className="hover:bg-neutral-50/70 dark:hover:bg-slate-800/40"
                        >
                          <TableCell className="align-top py-3.5">
                            <div className="space-y-1">
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
                              </div>
                              {role.description ? (
                                <p className="text-xs text-muted-foreground line-clamp-2">
                                  {role.description}
                                </p>
                              ) : (
                                <span className="text-xs text-muted-foreground/60 italic">
                                  Tanpa deskripsi
                                </span>
                              )}
                            </div>
                          </TableCell>

                          <TableCell className="align-middle py-3">
                            <div className="space-y-1.5">
                              <div className="flex flex-wrap items-center gap-1.5">
                                <Badge
                                  variant="secondary"
                                  className="text-[11px] font-medium"
                                >
                                  {role.permissions.length} Izin
                                </Badge>
                                {previewPerms.map((p) => (
                                  <code
                                    key={p.id}
                                    className="text-[11px] font-mono bg-neutral-100 dark:bg-slate-800 text-neutral-700 dark:text-neutral-300 px-1.5 py-0.5 rounded border border-neutral-200/70 dark:border-slate-700"
                                  >
                                    {p.code}
                                  </code>
                                ))}
                                {remainingCount > 0 && (
                                  <span className="text-[11px] text-muted-foreground font-medium px-1">
                                    +{remainingCount} lainnya
                                  </span>
                                )}
                              </div>
                              {role.permissions.length === 0 && (
                                <p className="text-[11px] text-muted-foreground italic">
                                  Belum ada permission yang dipetakan ke role
                                  ini.
                                </p>
                              )}
                            </div>
                          </TableCell>

                          <TableCell className="align-middle text-xs text-muted-foreground whitespace-nowrap">
                            {formatDate(role.createdAtUtc)}
                          </TableCell>

                          <TableCell className="align-middle text-right">
                            <Can
                              permission="roles.write"
                              fallback={
                                <span className="text-[11px] text-muted-foreground italic">
                                  Baca saja
                                </span>
                              }
                            >
                              <div className="flex items-center justify-end gap-1.5">
                                <Button
                                  type="button"
                                  variant="outline"
                                  size="sm"
                                  className="h-8 gap-1.5 text-xs"
                                  onClick={() => openMappingDialog(role)}
                                  title={`Atur pemetaan permission untuk role ${role.name}`}
                                >
                                  <CheckSquare className="w-3.5 h-3.5 text-primary" />
                                  <span>Atur Izin</span>
                                </Button>

                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="icon"
                                  className="h-8 w-8 text-muted-foreground hover:text-foreground"
                                  onClick={() => openEditDialog(role)}
                                  title={
                                    role.isSystemRole
                                      ? `Ubah deskripsi System Role ${role.name} (nama dikunci)`
                                      : `Ubah role ${role.name}`
                                  }
                                >
                                  <Pencil className="w-3.5 h-3.5" />
                                </Button>

                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="icon"
                                  disabled={role.isSystemRole}
                                  className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/40 disabled:opacity-40 disabled:pointer-events-auto disabled:cursor-not-allowed"
                                  onClick={() => openDeleteDialog(role)}
                                  title={
                                    role.isSystemRole
                                      ? "System Role bawaan tidak dapat dihapus"
                                      : `Hapus role ${role.name}`
                                  }
                                >
                                  <Trash2 className="w-3.5 h-3.5" />
                                </Button>
                              </div>
                            </Can>
                          </TableCell>
                        </TableRow>
                      );
                    })
                  )}
                </TableBody>
              </Table>

              <div className="p-3 border-t border-neutral-200 dark:border-slate-800 text-xs text-muted-foreground flex justify-between items-center bg-neutral-50/30 dark:bg-slate-900/20">
                <span>
                  Menampilkan <strong>{filteredRoles.length}</strong> dari{" "}
                  <strong>{initialRoles.length}</strong> role terdaftar
                </span>
              </div>
            </CardContent>
          </>
        )}
      </Card>

      {/* Dialog Tambah / Ubah Role */}
      <Dialog
        open={formDialogOpen}
        onOpenChange={(nextOpen) => {
          setFormDialogOpen(nextOpen);
          if (!nextOpen) {
            setFormError(null);
            setFieldErrors({});
          }
        }}
      >
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="text-lg font-semibold">
              {editingRole ? `Ubah Role: ${editingRole.name}` : "Buat Role Baru"}
            </DialogTitle>
            <DialogDescription className="text-xs">
              {editingRole
                ? `Perbarui informasi peran pengguna pada aplikasi ${app.name}.`
                : `Tambahkan peran baru beserta daftar izin awal untuk aplikasi ${app.name}.`}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmitRoleForm} className="space-y-4">
            {formError && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">
                  {formError}
                </AlertDescription>
              </Alert>
            )}

            {editingRole?.isSystemRole && (
              <div className="p-3 rounded-lg bg-blue-50/70 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-900/60 flex items-start gap-2.5 text-xs text-blue-800 dark:text-blue-300">
                <Info className="w-4 h-4 shrink-0 mt-0.5" />
                <div>
                  <strong>System Role Bawaan:</strong> Nama role{" "}
                  <code className="font-mono font-semibold">
                    {editingRole.name}
                  </code>{" "}
                  dikunci oleh sistem dan tidak dapat diubah maupun dihapus. Anda
                  tetap dapat memperbarui deskripsi role ini.
                </div>
              </div>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="role-name" className="text-xs font-semibold">
                Nama Role <span className="text-red-500">*</span>
              </Label>
              <Input
                id="role-name"
                placeholder="contoh: FinanceManager, Auditor, atau Operator"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={isSubmitting || !!editingRole?.isSystemRole}
                required
                className={`text-xs ${
                  fieldErrors.name ? "border-red-500" : ""
                }`}
              />
              {fieldErrors.name ? (
                <p className="text-xs text-red-500">{fieldErrors.name}</p>
              ) : (
                <p className="text-[11px] text-muted-foreground">
                  {editingRole?.isSystemRole
                    ? "Nama System Role dinonaktifkan dari perubahan."
                    : "Nama peran unik pada aplikasi ini (2–100 karakter)."}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="role-description"
                className="text-xs font-semibold"
              >
                Deskripsi
              </Label>
              <Textarea
                id="role-description"
                placeholder="Penjelasan tanggung jawab atau cakupan akses role ini..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={isSubmitting}
                rows={2}
                className={`text-xs ${
                  fieldErrors.description ? "border-red-500" : ""
                }`}
              />
              {fieldErrors.description && (
                <p className="text-xs text-red-500">
                  {fieldErrors.description}
                </p>
              )}
            </div>

            {/* Pilihan Permission Awal saat membuat Role baru */}
            {!editingRole && (
              <div className="space-y-2 pt-1">
                <div className="flex items-center justify-between">
                  <Label className="text-xs font-semibold">
                    Pemetaan Permission Awal ({initialPermIds.length}/
                    {availablePermissions.length} dipilih)
                  </Label>
                  {availablePermissions.length > 0 && (
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() =>
                          setInitialPermIds(
                            availablePermissions.map((p) => p.id)
                          )
                        }
                        className="text-[11px] text-primary hover:underline font-medium"
                      >
                        Pilih Semua
                      </button>
                      <span className="text-muted-foreground text-[11px]">
                        •
                      </span>
                      <button
                        type="button"
                        onClick={() => setInitialPermIds([])}
                        className="text-[11px] text-muted-foreground hover:text-foreground hover:underline"
                      >
                        Kosongkan
                      </button>
                    </div>
                  )}
                </div>

                {availablePermissions.length === 0 ? (
                  <div className="p-3 rounded-lg border border-dashed text-center text-xs text-muted-foreground">
                    Belum ada permission pada aplikasi ini. Anda dapat membuat
                    role terlebih dahulu lalu menambahkan permission di tab{" "}
                    <strong>Permissions</strong>.
                  </div>
                ) : (
                  <div className="rounded-lg border border-neutral-200 dark:border-slate-800 overflow-hidden">
                    <div className="p-2 border-b border-neutral-200 dark:border-slate-800 bg-neutral-50/60 dark:bg-slate-900/50">
                      <div className="relative">
                        <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground" />
                        <Input
                          placeholder="Filter kode permission..."
                          value={createPermSearch}
                          onChange={(e) => setCreatePermSearch(e.target.value)}
                          className="pl-8 h-8 text-xs bg-white dark:bg-slate-950"
                        />
                      </div>
                    </div>

                    <div className="max-h-48 overflow-y-auto divide-y divide-neutral-100 dark:divide-slate-800/60 p-1">
                      {filteredCreatePermissions.length === 0 ? (
                        <p className="p-3 text-center text-xs text-muted-foreground">
                          Tidak ada permission yang cocok.
                        </p>
                      ) : (
                        filteredCreatePermissions.map((perm) => {
                          const isChecked = initialPermIds.includes(perm.id);
                          const checkId = `create-role-perm-${perm.id}`;
                          return (
                            <label
                              key={perm.id}
                              htmlFor={checkId}
                              className="flex items-start gap-2.5 p-2 rounded hover:bg-neutral-50 dark:hover:bg-slate-800/50 cursor-pointer"
                            >
                              <Checkbox
                                id={checkId}
                                checked={isChecked}
                                onCheckedChange={() =>
                                  toggleInitialPerm(perm.id)
                                }
                                className="mt-0.5"
                              />
                              <div className="space-y-0.5 text-xs">
                                <div className="font-mono font-medium text-foreground">
                                  {perm.code}
                                </div>
                                {perm.description && (
                                  <div className="text-[11px] text-muted-foreground">
                                    {perm.description}
                                  </div>
                                )}
                              </div>
                            </label>
                          );
                        })
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setFormDialogOpen(false)}
                disabled={isSubmitting}
                className="text-xs"
              >
                Batal
              </Button>
              <Button
                type="submit"
                disabled={isSubmitting}
                className="gap-2 text-xs"
              >
                {isSubmitting && <Loader2 className="w-4 h-4 animate-spin" />}
                {editingRole ? "Simpan Perubahan" : "Buat Role"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Dialog Pemetaan Permission ke Role (Checkbox List) */}
      <Dialog
        open={!!mappingRole}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !isSavingMap) {
            setMappingRole(null);
            setMapError(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <KeyRound className="w-4 h-4" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <DialogTitle className="text-lg font-semibold">
                    Pemetaan Izin Role: {mappingRole?.name}
                  </DialogTitle>
                  {mappingRole?.isSystemRole && (
                    <Badge
                      variant="outline"
                      className="text-[10px] px-1.5 py-0 border-blue-300 dark:border-blue-700 bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300"
                    >
                      System Role
                    </Badge>
                  )}
                </div>
                <DialogDescription className="text-xs">
                  Centang kode permission yang ingin diberikan kepada pengguna
                  dengan role <strong>{mappingRole?.name}</strong> pada aplikasi{" "}
                  <strong>{app.name}</strong>.
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="space-y-3 py-1">
            {mapError && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">
                  {mapError}
                </AlertDescription>
              </Alert>
            )}

            {availablePermissions.length === 0 ? (
              <div className="p-8 rounded-lg border border-dashed text-center space-y-2">
                <Lock className="w-8 h-8 text-muted-foreground/50 mx-auto" />
                <p className="text-sm font-medium text-foreground">
                  Belum Ada Permission Terdaftar
                </p>
                <p className="text-xs text-muted-foreground max-w-md mx-auto">
                  Aplikasi <strong>{app.name}</strong> belum memiliki daftar
                  permission. Silakan tambahkan permission terlebih dahulu pada
                  tab <strong>Permissions</strong>.
                </p>
              </div>
            ) : (
              <>
                {/* Toolbar Pencarian & Pilih Semua */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 bg-neutral-50 dark:bg-slate-900/60 p-2.5 rounded-lg border border-neutral-200 dark:border-slate-800">
                  <div className="relative flex-1">
                    <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground" />
                    <Input
                      placeholder="Cari kode atau deskripsi permission..."
                      value={mapSearch}
                      onChange={(e) => setMapSearch(e.target.value)}
                      className="pl-8 h-8 text-xs bg-white dark:bg-slate-950"
                    />
                  </div>

                  <div className="flex items-center justify-between sm:justify-end gap-2 shrink-0">
                    <Badge variant="secondary" className="text-xs">
                      {selectedPermIds.length} / {availablePermissions.length}{" "}
                      dipilih
                    </Badge>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="h-8 text-xs"
                      onClick={() =>
                        setSelectedPermIds(
                          availablePermissions.map((p) => p.id)
                        )
                      }
                      disabled={
                        isSavingMap ||
                        selectedPermIds.length === availablePermissions.length
                      }
                    >
                      Pilih Semua
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="h-8 text-xs"
                      onClick={() => setSelectedPermIds([])}
                      disabled={isSavingMap || selectedPermIds.length === 0}
                    >
                      Kosongkan
                    </Button>
                  </div>
                </div>

                {/* Daftar Checkbox Permission Dikelompokkan per Modul */}
                <div className="max-h-80 overflow-y-auto space-y-3 pr-1">
                  {groupedMappingPermissions.length === 0 ? (
                    <div className="p-6 text-center text-xs text-muted-foreground border rounded-lg">
                      Tidak ada permission yang cocok dengan kata kunci{" "}
                      <strong>&ldquo;{mapSearch}&rdquo;</strong>.
                    </div>
                  ) : (
                    groupedMappingPermissions.map(([mod, perms]) => {
                      const selectedInMod = perms.filter((p) =>
                        selectedPermIds.includes(p.id)
                      ).length;
                      const allInModSelected =
                        perms.length > 0 && selectedInMod === perms.length;

                      return (
                        <div
                          key={mod}
                          className="rounded-lg border border-neutral-200 dark:border-slate-800 overflow-hidden"
                        >
                          <div className="px-3 py-2 bg-neutral-100/70 dark:bg-slate-800/70 border-b border-neutral-200 dark:border-slate-800 flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              <Badge
                                variant="outline"
                                className="font-mono text-[11px] bg-white dark:bg-slate-900"
                              >
                                {mod}.*
                              </Badge>
                              <span className="text-xs text-muted-foreground">
                                ({selectedInMod}/{perms.length} dipilih)
                              </span>
                            </div>
                            <button
                              type="button"
                              onClick={() => toggleModulePerms(perms)}
                              disabled={isSavingMap}
                              className="text-[11px] font-medium text-primary hover:underline"
                            >
                              {allInModSelected
                                ? "Batal Pilih Modul"
                                : "Pilih Modul Ini"}
                            </button>
                          </div>

                          <div className="divide-y divide-neutral-100 dark:divide-slate-800/60 bg-white dark:bg-slate-950">
                            {perms.map((perm) => {
                              const isChecked = selectedPermIds.includes(
                                perm.id
                              );
                              const checkboxId = `map-perm-${perm.id}`;
                              return (
                                <label
                                  key={perm.id}
                                  htmlFor={checkboxId}
                                  className="flex items-start gap-3 px-3 py-2.5 hover:bg-neutral-50 dark:hover:bg-slate-900/60 cursor-pointer transition-colors"
                                >
                                  <Checkbox
                                    id={checkboxId}
                                    checked={isChecked}
                                    onCheckedChange={() =>
                                      toggleMappedPerm(perm.id)
                                    }
                                    disabled={isSavingMap}
                                    className="mt-0.5"
                                  />
                                  <div className="flex-1 min-w-0">
                                    <div className="flex items-center gap-2">
                                      <code className="text-xs font-mono font-semibold text-foreground">
                                        {perm.code}
                                      </code>
                                    </div>
                                    {perm.description && (
                                      <p className="text-xs text-muted-foreground mt-0.5">
                                        {perm.description}
                                      </p>
                                    )}
                                  </div>
                                </label>
                              );
                            })}
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>
              </>
            )}
          </div>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setMappingRole(null)}
              disabled={isSavingMap}
              className="text-xs"
            >
              Batal
            </Button>
            <Button
              type="button"
              onClick={handleSavePermissionsMap}
              disabled={isSavingMap || availablePermissions.length === 0}
              className="gap-2 text-xs"
            >
              {isSavingMap && <Loader2 className="w-4 h-4 animate-spin" />}
              Simpan Pemetaan ({selectedPermIds.length} Izin)
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog Konfirmasi Hapus Role */}
      <Dialog
        open={!!deletingRole}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !isDeleting) {
            setDeletingRole(null);
            setDeleteError(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-red-100 dark:bg-red-950/60 text-red-600 dark:text-red-400 flex items-center justify-center shrink-0">
                <AlertTriangle className="w-5 h-5" />
              </div>
              <div>
                <DialogTitle className="text-lg font-semibold text-red-700 dark:text-red-400">
                  Hapus Role?
                </DialogTitle>
                <DialogDescription className="text-xs">
                  Tindakan ini bersifat permanen dan tidak dapat dibatalkan.
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="space-y-3 py-2">
            {deleteError && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">
                  {deleteError}
                </AlertDescription>
              </Alert>
            )}

            <p className="text-xs text-muted-foreground leading-relaxed">
              Apakah Anda yakin ingin menghapus role{" "}
              <strong className="text-foreground">{deletingRole?.name}</strong>{" "}
              dari aplikasi <strong>{app.name}</strong>?
            </p>

            <div className="p-3 rounded-lg bg-amber-50/70 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 text-xs text-amber-800 dark:text-amber-300">
              Seluruh pengguna yang saat ini ditugaskan ke role tersebut pada
              aplikasi ini akan kehilangan hak akses dari role ini.
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              autoFocus
              onClick={() => setDeletingRole(null)}
              disabled={isDeleting}
              className="text-xs"
            >
              Batal
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleConfirmDeleteRole}
              disabled={isDeleting}
              className="gap-2 text-xs"
            >
              {isDeleting && <Loader2 className="w-4 h-4 animate-spin" />}
              Ya, Hapus Role
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
