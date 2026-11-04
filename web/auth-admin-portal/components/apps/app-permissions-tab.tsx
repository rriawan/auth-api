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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Textarea } from "@/components/ui/textarea";
import { ClientAppDto, PermissionDto } from "@/lib/api/types";
import {
  AlertCircle,
  AlertTriangle,
  Check,
  Copy,
  Loader2,
  Lock,
  Pencil,
  Plus,
  Search,
  ShieldCheck,
  Trash2,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import toast from "react-hot-toast";
import {
  createPermissionAction,
  deletePermissionAction,
  updatePermissionAction,
} from "./actions/permissions";

interface AppPermissionsTabProps {
  app: ClientAppDto;
  initialPermissions: PermissionDto[];
  fetchError?: string | null;
}

export function AppPermissionsTab({
  app,
  initialPermissions,
  fetchError,
}: AppPermissionsTabProps) {
  const router = useRouter();

  // Filter & Pencarian
  const [search, setSearch] = useState("");
  const [selectedModule, setSelectedModule] = useState<string>("all");
  const [copiedId, setCopiedId] = useState<string | null>(null);

  // Dialog Tambah / Ubah Permission
  const [formDialogOpen, setFormDialogOpen] = useState(false);
  const [editingPermission, setEditingPermission] =
    useState<PermissionDto | null>(null);
  const [code, setCode] = useState("");
  const [description, setDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Dialog Konfirmasi Hapus Permission
  const [deletingPermission, setDeletingPermission] =
    useState<PermissionDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  // Daftar modul unik dari prefix kode permission (sebelum titik pertama)
  const modules = useMemo(() => {
    const set = new Set<string>();
    for (const perm of initialPermissions) {
      const parts = perm.code.split(".");
      if (parts.length > 1 && parts[0]) {
        set.add(parts[0]);
      }
    }
    return Array.from(set).sort();
  }, [initialPermissions]);

  const filteredPermissions = useMemo(() => {
    return initialPermissions.filter((perm) => {
      const q = search.toLowerCase().trim();
      const matchQuery =
        !q ||
        perm.code.toLowerCase().includes(q) ||
        (perm.description && perm.description.toLowerCase().includes(q));

      const permModule = perm.code.split(".")[0] || "";
      const matchModule =
        selectedModule === "all" || permModule === selectedModule;

      return matchQuery && matchModule;
    });
  }, [initialPermissions, search, selectedModule]);

  const handleCopyCode = (permCode: string, permId: string) => {
    navigator.clipboard.writeText(permCode);
    setCopiedId(permId);
    toast.success(`Kode '${permCode}' disalin!`);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const openCreateDialog = () => {
    setEditingPermission(null);
    setCode("");
    setDescription("");
    setFormError(null);
    setFieldErrors({});
    setFormDialogOpen(true);
  };

  const openEditDialog = (perm: PermissionDto) => {
    setEditingPermission(perm);
    setCode(perm.code);
    setDescription(perm.description || "");
    setFormError(null);
    setFieldErrors({});
    setFormDialogOpen(true);
  };

  const handleCodeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const normalized = e.target.value.toLowerCase().replace(/\s+/g, "");
    setCode(normalized);
  };

  const handleSubmitForm = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setIsSubmitting(true);

    try {
      if (editingPermission) {
        const res = await updatePermissionAction(app.id, editingPermission.id, {
          code,
          description: description || null,
        });

        if (!res.success) {
          setFormError(res.error || "Gagal memperbarui permission.");
          if (res.fieldErrors) setFieldErrors(res.fieldErrors);
          setIsSubmitting(false);
          return;
        }

        toast.success(`Permission '${code}' berhasil diperbarui.`);
      } else {
        const res = await createPermissionAction(app.id, {
          code,
          description: description || null,
        });

        if (!res.success) {
          setFormError(res.error || "Gagal menambahkan permission.");
          if (res.fieldErrors) setFieldErrors(res.fieldErrors);
          setIsSubmitting(false);
          return;
        }

        toast.success(`Permission '${code}' berhasil ditambahkan.`);
      }

      setFormDialogOpen(false);
      router.refresh();
    } catch (err) {
      setFormError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menyimpan permission."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const openDeleteDialog = (perm: PermissionDto) => {
    setDeletingPermission(perm);
    setDeleteError(null);
  };

  const handleConfirmDelete = async () => {
    if (!deletingPermission) return;
    setDeleteError(null);
    setIsDeleting(true);

    try {
      const res = await deletePermissionAction(app.id, deletingPermission.id);
      if (!res.success) {
        setDeleteError(res.error || "Gagal menghapus permission.");
        setIsDeleting(false);
        return;
      }

      toast.success(
        `Permission '${deletingPermission.code}' berhasil dihapus.`
      );
      setDeletingPermission(null);
      router.refresh();
    } catch (err) {
      setDeleteError(
        err instanceof Error
          ? err.message
          : "Terjadi kesalahan saat menghapus permission."
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

  return (
    <>
      <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
        <CardHeader className="border-b border-neutral-200 dark:border-slate-800">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <Lock className="w-5 h-5" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <CardTitle className="text-lg font-semibold">
                    Daftar Izin Aplikasi (Permissions)
                  </CardTitle>
                  <Badge variant="outline" className="text-xs">
                    {initialPermissions.length} Izin
                  </Badge>
                </div>
                <CardDescription className="text-xs mt-0.5">
                  Katalog hak akses granular berpola{" "}
                  <code className="font-mono text-[11px] bg-neutral-100 dark:bg-slate-800 px-1 py-0.5 rounded">
                    modul.aksi
                  </code>{" "}
                  untuk aplikasi <strong>{app.name}</strong>.
                </CardDescription>
              </div>
            </div>

            <Can permission="permissions.write">
              <Button
                type="button"
                onClick={openCreateDialog}
                className="gap-2 text-xs h-9 self-start sm:self-auto"
              >
                <Plus className="w-4 h-4" />
                Tambah Permission
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
            {/* Bar Pencarian & Filter Modul */}
            <div className="p-4 border-b border-neutral-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-neutral-50/50 dark:bg-slate-900/40">
              <div className="relative flex-1 max-w-sm">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <Input
                  placeholder="Cari kode atau deskripsi permission..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-9 h-9 text-xs bg-white dark:bg-slate-950"
                />
              </div>

              {modules.length > 1 && (
                <div className="flex flex-wrap items-center gap-1.5">
                  <Button
                    type="button"
                    variant={selectedModule === "all" ? "default" : "outline"}
                    size="sm"
                    onClick={() => setSelectedModule("all")}
                    className="text-xs h-8"
                  >
                    Semua Modul ({initialPermissions.length})
                  </Button>
                  {modules.map((mod) => {
                    const count = initialPermissions.filter(
                      (p) => p.code.split(".")[0] === mod
                    ).length;
                    return (
                      <Button
                        key={mod}
                        type="button"
                        variant={
                          selectedModule === mod ? "default" : "outline"
                        }
                        size="sm"
                        onClick={() => setSelectedModule(mod)}
                        className="text-xs h-8 font-mono"
                      >
                        {mod}.* ({count})
                      </Button>
                    );
                  })}
                </div>
              )}
            </div>

            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent bg-neutral-50 dark:bg-slate-900/60 text-xs">
                    <TableHead className="w-[32%]">Kode Permission</TableHead>
                    <TableHead className="w-[15%]">Modul</TableHead>
                    <TableHead>Deskripsi</TableHead>
                    <TableHead className="w-[15%]">Dibuat Pada</TableHead>
                    <TableHead className="text-right w-[15%]">Aksi</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredPermissions.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="h-44 text-center">
                        <div className="flex flex-col items-center justify-center text-muted-foreground space-y-2">
                          <ShieldCheck className="w-8 h-8 opacity-40" />
                          <p className="text-sm font-medium">
                            {initialPermissions.length === 0
                              ? "Belum ada permission yang terdaftar pada aplikasi ini."
                              : "Tidak ada permission yang sesuai dengan filter."}
                          </p>
                          {initialPermissions.length === 0 ? (
                            <p className="text-xs max-w-md">
                              Tambahkan kode permission (seperti{" "}
                              <code className="font-mono">invoice.view</code>{" "}
                              atau{" "}
                              <code className="font-mono">report.export</code>)
                              untuk mulai mengatur hak akses granular.
                            </p>
                          ) : (
                            <p className="text-xs">
                              Coba gunakan kata kunci pencarian lain atau reset
                              filter modul.
                            </p>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ) : (
                    filteredPermissions.map((perm) => {
                      const modulePrefix = perm.code.split(".")[0] || "-";
                      return (
                        <TableRow
                          key={perm.id}
                          className="hover:bg-neutral-50/70 dark:hover:bg-slate-800/40"
                        >
                          <TableCell className="align-middle py-3">
                            <div className="flex items-center gap-1.5">
                              <code className="text-xs font-mono font-semibold bg-neutral-100 dark:bg-slate-800 text-neutral-800 dark:text-neutral-200 px-2.5 py-1 rounded border border-neutral-200/80 dark:border-slate-700">
                                {perm.code}
                              </code>
                              <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                className="h-6 w-6 text-muted-foreground hover:text-foreground"
                                onClick={() =>
                                  handleCopyCode(perm.code, perm.id)
                                }
                                title="Salin Kode Permission"
                              >
                                {copiedId === perm.id ? (
                                  <Check className="w-3 h-3 text-emerald-500" />
                                ) : (
                                  <Copy className="w-3 h-3" />
                                )}
                              </Button>
                            </div>
                          </TableCell>

                          <TableCell className="align-middle">
                            <Badge
                              variant="outline"
                              className="text-[11px] font-mono bg-neutral-50 dark:bg-slate-900"
                            >
                              {modulePrefix}
                            </Badge>
                          </TableCell>

                          <TableCell className="align-middle text-xs text-muted-foreground">
                            {perm.description ? (
                              <span className="text-foreground/90">
                                {perm.description}
                              </span>
                            ) : (
                              <span className="italic text-muted-foreground/60">
                                Tanpa deskripsi
                              </span>
                            )}
                          </TableCell>

                          <TableCell className="align-middle text-xs text-muted-foreground whitespace-nowrap">
                            {formatDate(perm.createdAtUtc)}
                          </TableCell>

                          <TableCell className="align-middle text-right">
                            <Can
                              permission="permissions.write"
                              fallback={
                                <span className="text-[11px] text-muted-foreground italic">
                                  Baca saja
                                </span>
                              }
                            >
                              <div className="flex items-center justify-end gap-1">
                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="icon"
                                  className="h-8 w-8 text-muted-foreground hover:text-foreground"
                                  onClick={() => openEditDialog(perm)}
                                  title={`Ubah permission ${perm.code}`}
                                >
                                  <Pencil className="w-3.5 h-3.5" />
                                </Button>
                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="icon"
                                  className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/40"
                                  onClick={() => openDeleteDialog(perm)}
                                  title={`Hapus permission ${perm.code}`}
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
                  Menampilkan <strong>{filteredPermissions.length}</strong> dari{" "}
                  <strong>{initialPermissions.length}</strong> permission
                  terdaftar
                </span>
              </div>
            </CardContent>
          </>
        )}
      </Card>

      {/* Dialog Tambah / Ubah Permission */}
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
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-lg font-semibold">
              {editingPermission ? "Ubah Permission" : "Tambah Permission Baru"}
            </DialogTitle>
            <DialogDescription className="text-xs">
              {editingPermission
                ? `Perbarui kode atau deskripsi izin untuk aplikasi ${app.name}.`
                : `Tambahkan kode izin granular baru untuk aplikasi ${app.name}.`}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmitForm} className="space-y-4">
            {formError && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription className="text-xs">
                  {formError}
                </AlertDescription>
              </Alert>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="perm-code" className="text-xs font-semibold">
                Kode Permission <span className="text-red-500">*</span>
              </Label>
              <Input
                id="perm-code"
                placeholder="contoh: invoice.create atau report.view"
                value={code}
                onChange={handleCodeChange}
                disabled={isSubmitting}
                required
                className={`font-mono text-xs ${
                  fieldErrors.code ? "border-red-500" : ""
                }`}
              />
              {fieldErrors.code ? (
                <p className="text-xs text-red-500">{fieldErrors.code}</p>
              ) : (
                <p className="text-[11px] text-muted-foreground">
                  Gunakan huruf kecil dengan pola{" "}
                  <code className="font-mono">modul.aksi</code> (maksimal 150
                  karakter).
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="perm-description"
                className="text-xs font-semibold"
              >
                Deskripsi
              </Label>
              <Textarea
                id="perm-description"
                placeholder="Penjelasan singkat hak akses yang diberikan oleh permission ini..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={isSubmitting}
                rows={3}
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
                {editingPermission ? "Simpan Perubahan" : "Tambah Permission"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Dialog Konfirmasi Hapus Permission */}
      <Dialog
        open={!!deletingPermission}
        onOpenChange={(nextOpen) => {
          if (!nextOpen && !isDeleting) {
            setDeletingPermission(null);
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
                  Hapus Permission?
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
              Apakah Anda yakin ingin menghapus permission{" "}
              <code className="font-mono font-semibold bg-neutral-100 dark:bg-slate-800 text-foreground px-1.5 py-0.5 rounded">
                {deletingPermission?.code}
              </code>{" "}
              dari aplikasi <strong>{app.name}</strong>?
            </p>

            <div className="p-3 rounded-lg bg-amber-50/70 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/60 text-xs text-amber-800 dark:text-amber-300">
              Seluruh Role pada aplikasi ini yang memiliki permission tersebut
              akan otomatis kehilangan hak akses ini.
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              autoFocus
              onClick={() => setDeletingPermission(null)}
              disabled={isDeleting}
              className="text-xs"
            >
              Batal
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleConfirmDelete}
              disabled={isDeleting}
              className="gap-2 text-xs"
            >
              {isDeleting && <Loader2 className="w-4 h-4 animate-spin" />}
              Ya, Hapus Permission
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
