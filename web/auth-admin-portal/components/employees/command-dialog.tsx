"use client";

import type { ActionResult } from "@/components/apps/actions/apps";
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
import { Textarea } from "@/components/ui/textarea";
import { AlertCircle, Info, Loader2, Search } from "lucide-react";
import { useRouter } from "next/navigation";
import { ReactNode, useEffect, useState } from "react";
import toast from "react-hot-toast";
import {
  listOfficeOptionsAction,
  listPositionOptionsAction,
  searchEmployeesAction,
  type PickerOption,
} from "./actions/employees";
import type { EmployeeSummaryDto } from "@/lib/api/employee-types";

export type FormValues = Record<string, string | boolean>;

export type FieldDef =
  | {
      kind: "text" | "textarea" | "date";
      name: string;
      label: string;
      required?: boolean;
      help?: string;
      placeholder?: string;
    }
  | {
      kind: "select";
      name: string;
      label: string;
      options: PickerOption[];
      required?: boolean;
      help?: string;
    }
  | { kind: "checkbox"; name: string; label: string; help?: string }
  | { kind: "position"; name: string; label: string; help?: string }
  | { kind: "employee"; name: string; label: string; help?: string };

interface CommandDialogProps {
  triggerLabel: string;
  triggerIcon?: ReactNode;
  triggerVariant?: "default" | "outline" | "destructive" | "secondary";
  title: string;
  description?: string;
  /** Peringatan yang selalu tampil di atas formulir. */
  notice?: string;
  fields: FieldDef[];
  initial?: FormValues;
  submitLabel?: string;
  successMessage: string;
  disabled?: boolean;
  onSubmit: (values: FormValues) => Promise<ActionResult<unknown>>;
}

const selectClass =
  "flex h-9 w-full rounded-md border border-input bg-background px-3 text-xs shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary disabled:opacity-50";

/**
 * Dialog perintah generik untuk operasi admin (ubah identitas, status, penempatan, dsb.).
 * Semua isian divalidasi ulang di server action dan di backend; dialog ini hanya UX.
 */
export function CommandDialog(props: CommandDialogProps) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [values, setValues] = useState<FormValues>(props.initial ?? {});

  const reset = () => {
    setValues(props.initial ?? {});
    setError(null);
    setFieldErrors({});
  };

  // Selalu mulai dari `initial` terbaru (versi/tanggal/nilai setelah router.refresh), bukan state lama.
  const changeOpen = (next: boolean) => {
    setOpen(next);
    reset();
  };

  const set = (name: string, value: string | boolean) =>
    setValues((prev) => ({ ...prev, [name]: value }));

  const submit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);
    setFieldErrors({});
    setBusy(true);
    try {
      const res = await props.onSubmit(values);
      if (!res.success) {
        setError(res.error || "Perubahan gagal disimpan.");
        if (res.fieldErrors) setFieldErrors(res.fieldErrors);
        return;
      }
      toast.success(props.successMessage);
      setOpen(false);
      reset();
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Terjadi kesalahan yang tidak diharapkan.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog
      open={open}
      onOpenChange={changeOpen}
    >
      <DialogTrigger asChild>
        <Button variant={props.triggerVariant ?? "outline"} className="gap-2 text-xs" disabled={props.disabled}>
          {props.triggerIcon}
          {props.triggerLabel}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-xl font-semibold">{props.title}</DialogTitle>
          {props.description && (
            <DialogDescription className="text-xs">{props.description}</DialogDescription>
          )}
        </DialogHeader>

        <form onSubmit={submit} className="space-y-4">
          {props.notice && (
            <Alert>
              <Info className="h-4 w-4" />
              <AlertDescription className="text-xs">{props.notice}</AlertDescription>
            </Alert>
          )}
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription className="text-xs">{error}</AlertDescription>
            </Alert>
          )}

          {props.fields.map((field) => {
            const id = `cmd-${field.name}`;
            const err = fieldErrors[field.name];
            return (
              <div key={field.name} className="space-y-1.5">
                {field.kind !== "checkbox" && (
                  <Label htmlFor={id} className="text-xs font-semibold">
                    {field.label}
                    {"required" in field && field.required && <span className="text-red-500"> *</span>}
                  </Label>
                )}

                {(field.kind === "text" || field.kind === "date") && (
                  <Input
                    id={id}
                    type={field.kind === "date" ? "date" : "text"}
                    value={String(values[field.name] ?? "")}
                    placeholder={field.kind === "text" ? field.placeholder : undefined}
                    onChange={(e) => set(field.name, e.target.value)}
                    disabled={busy}
                    className={`text-xs ${err ? "border-red-500" : ""}`}
                  />
                )}

                {field.kind === "textarea" && (
                  <Textarea
                    id={id}
                    rows={3}
                    value={String(values[field.name] ?? "")}
                    placeholder={field.placeholder}
                    onChange={(e) => set(field.name, e.target.value)}
                    disabled={busy}
                    className={`text-xs ${err ? "border-red-500" : ""}`}
                  />
                )}

                {field.kind === "select" && (
                  <select
                    id={id}
                    className={selectClass}
                    value={String(values[field.name] ?? "")}
                    onChange={(e) => set(field.name, e.target.value)}
                    disabled={busy}
                  >
                    <option value="">— Pilih —</option>
                    {field.options.map((o) => (
                      <option key={o.value} value={o.value}>
                        {o.label}
                      </option>
                    ))}
                  </select>
                )}

                {field.kind === "checkbox" && (
                  <label className="flex items-center gap-2 text-xs font-medium cursor-pointer">
                    <input
                      type="checkbox"
                      checked={Boolean(values[field.name])}
                      onChange={(e) => set(field.name, e.target.checked)}
                      disabled={busy}
                    />
                    {field.label}
                  </label>
                )}

                {field.kind === "position" && (
                  <PositionPicker
                    id={id}
                    value={String(values[field.name] ?? "")}
                    onChange={(v) => set(field.name, v)}
                    disabled={busy}
                  />
                )}

                {field.kind === "employee" && (
                  <EmployeePicker
                    value={String(values[field.name] ?? "")}
                    onChange={(employeeId, employeeVersion) => {
                      setValues((prev) => ({ ...prev, [field.name]: employeeId, [`${field.name}Version`]: employeeVersion }));
                    }}
                    disabled={busy}
                  />
                )}

                {err ? (
                  <p className="text-xs text-red-500">{err}</p>
                ) : (
                  field.help && <p className="text-[11px] text-muted-foreground">{field.help}</p>
                )}
              </div>
            );
          })}

          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={() => changeOpen(false)} disabled={busy} className="text-xs">
              Batal
            </Button>
            <Button type="submit" disabled={busy} className="gap-2 text-xs">
              {busy && <Loader2 className="w-4 h-4 animate-spin" />}
              {props.submitLabel ?? "Simpan"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

/** Kantor → posisi aktif. Nilai yang dikirim hanya PositionId (kantor/unit tidak menjadi sumber kebenaran terpisah). */
function PositionPicker({
  id,
  value,
  onChange,
  disabled,
}: {
  id: string;
  value: string;
  onChange: (value: string) => void;
  disabled: boolean;
}) {
  const [offices, setOffices] = useState<PickerOption[]>([]);
  const [positions, setPositions] = useState<PickerOption[]>([]);
  const [office, setOffice] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    listOfficeOptionsAction().then((res) => {
      if (cancelled) return;
      if (res.success) setOffices(res.data ?? []);
      else setMessage(res.error ?? "Gagal memuat daftar kantor.");
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const pickOffice = async (officeId: string) => {
    setOffice(officeId);
    onChange("");
    setPositions([]);
    setMessage(null);
    if (!officeId) return;
    setLoading(true);
    const res = await listPositionOptionsAction(officeId);
    setLoading(false);
    if (res.success) {
      setPositions(res.data ?? []);
      if ((res.data ?? []).length === 0) setMessage("Kantor ini belum memiliki posisi aktif.");
    } else {
      setMessage(res.error ?? "Gagal memuat posisi.");
    }
  };

  return (
    <div className="space-y-2">
      <select className={selectClass} value={office} onChange={(e) => pickOffice(e.target.value)} disabled={disabled} aria-label="Kantor">
        <option value="">— Pilih kantor —</option>
        {offices.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
      <select id={id} className={selectClass} value={value} onChange={(e) => onChange(e.target.value)} disabled={disabled || !office || loading} aria-label="Posisi">
        <option value="">{loading ? "Memuat posisi…" : "— Pilih posisi —"}</option>
        {positions.map((p) => (
          <option key={p.value} value={p.value}>
            {p.label}
          </option>
        ))}
      </select>
      {message && <p className="text-[11px] text-muted-foreground">{message}</p>}
    </div>
  );
}

/** Cari pegawai berdasarkan NIP atau nama (minimal 2 karakter), lalu pilih satu. */
function EmployeePicker({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (value: string, version: string) => void;
  disabled: boolean;
}) {
  const [term, setTerm] = useState("");
  const [results, setResults] = useState<EmployeeSummaryDto[]>([]);
  const [searching, setSearching] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const search = async () => {
    onChange("", ""); // pilihan lama tidak boleh tersisa tanpa terlihat setelah hasil diganti
    setSearching(true);
    setMessage(null);
    const res = await searchEmployeesAction(term);
    setSearching(false);
    if (!res.success) {
      setMessage(res.error ?? "Pencarian gagal.");
      return;
    }
    setResults(res.data ?? []);
    if ((res.data ?? []).length === 0) setMessage("Tidak ada pegawai yang cocok (minimal 2 karakter).");
  };

  return (
    <div className="space-y-2">
      <div className="flex gap-2">
        <Input
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              void search();
            }
          }}
          placeholder="NIP atau nama pegawai"
          disabled={disabled}
          className="text-xs"
        />
        <Button type="button" variant="outline" onClick={() => void search()} disabled={disabled || searching} className="gap-2 text-xs">
          {searching ? <Loader2 className="w-4 h-4 animate-spin" /> : <Search className="w-4 h-4" />}
          Cari
        </Button>
      </div>
      {results.length > 0 && (
        <ul className="rounded-md border divide-y max-h-48 overflow-y-auto">
          {results.map((e) => (
            <li key={e.id}>
              <label className="flex items-start gap-2 p-2 text-xs cursor-pointer hover:bg-muted/50">
                <input type="radio" name="employee-picker" checked={value === e.id} onChange={() => onChange(e.id, e.version)} disabled={disabled || e.linkedUserId !== null} />
                <span>
                  <span className="font-mono font-semibold">{e.employeeNumber}</span> — {e.employeeFullName}
                  {e.linkedUserId && <span className="ml-1 text-muted-foreground">(sudah tertaut ke akun lain)</span>}
                  {e.currentStatus === "Resigned" && <span className="ml-1 text-red-500">(resign)</span>}
                </span>
              </label>
            </li>
          ))}
        </ul>
      )}
      {message && <p className="text-[11px] text-muted-foreground">{message}</p>}
    </div>
  );
}
