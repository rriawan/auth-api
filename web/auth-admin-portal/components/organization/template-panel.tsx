"use client";

import { Can } from "@/components/auth/can";
import { CommandDialog } from "@/components/employees/command-dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
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
import type { TemplateApplyPreviewDto, TemplateSummaryDto } from "@/lib/api/employee-types";
import { AlertCircle, FileCheck2, Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import toast from "react-hot-toast";
import { applyTemplateAction, previewTemplateAction, publishTemplateAction } from "./actions/organization";

function errorText(res: { error?: string; fieldErrors?: Record<string, string> }, fallback: string) {
  return res.fieldErrors ? Object.values(res.fieldErrors).join(" ") : (res.error ?? fallback);
}

function ApplyDialog({ officeId, template, today }: { officeId: string; template: TemplateSummaryDto; today: string }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [effectiveFrom, setEffectiveFrom] = useState(today);
  const [reason, setReason] = useState("");
  const [reference, setReference] = useState("");
  const [preview, setPreview] = useState<TemplateApplyPreviewDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // Satu kunci per pembukaan dialog: klik ganda atau ulangan jaringan tidak menggandakan penerapan.
  const [key, setKey] = useState(() => crypto.randomUUID());

  const reset = () => {
    setPreview(null);
    setError(null);
    setReason("");
    setReference("");
    setEffectiveFrom(today);
    setKey(crypto.randomUUID());
  };

  const runPreview = async () => {
    setBusy(true);
    setError(null);
    const res = await previewTemplateAction(officeId, { templateId: template.id, effectiveFrom });
    setBusy(false);
    if (!res.success || !res.data) {
      setPreview(null);
      setError(errorText(res, "Pratinjau gagal."));
      return;
    }
    setPreview(res.data);
  };

  const apply = async () => {
    setBusy(true);
    setError(null);
    const res = await applyTemplateAction(officeId, {
      templateId: template.id,
      effectiveFrom,
      reason,
      decisionReference: reference,
      idempotencyKey: key,
    });
    setBusy(false);
    if (!res.success || !res.data) {
      setError(errorText(res, "Penerapan gagal."));
      return;
    }
    toast.success(`Template diterapkan: ${res.data.unitsCreated} unit, ${res.data.positionsCreated} jabatan.`);
    setOpen(false);
    router.refresh();
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) reset();
      }}
    >
      <DialogTrigger asChild>
        <Button size="sm" className="text-xs">Pratinjau &amp; terapkan</Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Terapkan template {template.classificationCode} v{template.version}</DialogTitle>
          <DialogDescription className="text-xs">
            Membuat seluruh unit dan jabatan template pada kantor ini mulai tanggal efektif. Hanya untuk kantor yang belum memiliki unit atau jabatan.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-3">
          <div className="space-y-1">
            <Label className="text-xs">Berlaku mulai</Label>
            <Input
              type="date"
              value={effectiveFrom}
              min={today}
              onChange={(e) => {
                setEffectiveFrom(e.target.value);
                setPreview(null);
              }}
              className="text-xs max-w-[11rem]"
            />
          </div>
          <Button type="button" variant="outline" className="text-xs" onClick={runPreview} disabled={busy || !effectiveFrom}>
            {busy ? <Loader2 className="w-4 h-4 animate-spin" /> : null} Pratinjau
          </Button>
          {error && (
            <Alert variant="destructive">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription className="text-xs">{error}</AlertDescription>
            </Alert>
          )}
          {preview && (
            <div className="space-y-2">
              {preview.blockers.length > 0 ? (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription className="text-xs">
                    <ul className="list-disc pl-4">
                      {preview.blockers.map((b) => (
                        <li key={b}>{b}</li>
                      ))}
                    </ul>
                  </AlertDescription>
                </Alert>
              ) : (
                <p className="text-xs">
                  Akan dibuat <strong>{preview.unitCount} unit</strong> dan <strong>{preview.positionCount} jabatan</strong> pada {preview.officeCode} — {preview.officeName}.
                </p>
              )}
              <div className="max-h-64 overflow-y-auto rounded border p-2 text-xs space-y-2">
                {preview.units.map((u) => (
                  <div key={u.unitCode}>
                    <div className="font-medium">
                      {u.name} <span className="text-muted-foreground">({u.unitCode}{u.parentUnitCode ? ` ← ${u.parentUnitCode}` : ""})</span>
                    </div>
                    <ul className="pl-4 list-disc text-muted-foreground">
                      {u.positions.map((p) => (
                        <li key={p.positionCode}>
                          {p.name}
                          {p.isUnitHead ? " (pimpinan)" : ""}
                          {p.levelCode ? ` · ${p.levelCode}` : ""} · kapasitas {p.maxIncumbents ?? "banyak"}
                          {p.reportsToCode ? ` · lapor ke ${p.reportsToCode}` : ""}
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
              {preview.canApply && (
                <>
                  <div className="space-y-1">
                    <Label className="text-xs">Referensi keputusan (opsional)</Label>
                    <Input value={reference} onChange={(e) => setReference(e.target.value)} maxLength={200} className="text-xs" />
                  </div>
                  <div className="space-y-1">
                    <Label className="text-xs">Alasan</Label>
                    <Textarea value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} className="text-xs" />
                  </div>
                </>
              )}
            </div>
          )}
        </div>
        <DialogFooter>
          <Button type="button" onClick={apply} disabled={busy || !preview?.canApply || !reason.trim()} className="text-xs">
            {busy ? <Loader2 className="w-4 h-4 animate-spin" /> : null} Terapkan
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function TemplatePanel({
  officeId,
  classificationCode,
  templates,
  today,
}: {
  officeId: string;
  classificationCode: string;
  templates: TemplateSummaryDto[];
  today: string;
}) {
  return (
    <Can permission="organization.write">
      <Card>
        <CardContent className="p-5 space-y-3">
          <div>
            <h2 className="text-sm font-semibold flex items-center gap-2">
              <FileCheck2 className="w-4 h-4" /> Template struktur
            </h2>
            <p className="text-xs text-muted-foreground">
              Template klasifikasi {classificationCode}. Publikasi bersifat final; penerapan hanya pada kantor kosong.
            </p>
          </div>
          {templates.length === 0 && <p className="text-xs text-muted-foreground">Belum ada template untuk klasifikasi ini.</p>}
          {templates.map((t) => (
            <div key={t.id} className="flex flex-wrap items-center gap-3 rounded border p-3 text-xs">
              <span className="font-medium">Versi {t.version}</span>
              <Badge variant={t.state === "Published" ? "success" : "secondary"} className="text-xs">
                {t.state === "Published" ? "Dipublikasikan" : "Draft"}
              </Badge>
              <span className="text-muted-foreground">
                {t.unitCount} unit · {t.positionCount} jabatan
              </span>
              <div className="ml-auto flex gap-2">
                {t.state === "Draft" ? (
                  <CommandDialog
                    triggerLabel="Publikasikan"
                    triggerVariant="outline"
                    title={`Publikasikan template ${t.classificationCode} v${t.version}`}
                    description="Template yang dipublikasikan tidak dapat diubah dan dapat diterapkan ke kantor."
                    fields={[
                      { kind: "text", name: "decisionReference", label: "Referensi keputusan (opsional)" },
                      { kind: "textarea", name: "reason", label: "Alasan", required: true },
                    ]}
                    initial={{ decisionReference: "", reason: "" }}
                    successMessage="Template dipublikasikan."
                    onSubmit={(v) => publishTemplateAction(t.id, t.eTag, v)}
                  />
                ) : (
                  <ApplyDialog officeId={officeId} template={t} today={today} />
                )}
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    </Can>
  );
}
