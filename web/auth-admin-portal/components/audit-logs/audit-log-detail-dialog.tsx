"use client";

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
import { AuditLogDto } from "@/lib/api/types";
import {
  Check,
  CheckCircle2,
  Copy,
  ExternalLink,
  FileCode2,
  Globe,
  Monitor,
  XCircle,
} from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import toast from "react-hot-toast";
import {
  getAuditEventLabel,
  prettyPrintDetailJson,
} from "./audit-log-constants";

interface AuditLogDetailDialogProps {
  log: AuditLogDto | null;
  onClose: () => void;
}

export function AuditLogDetailDialog({
  log,
  onClose,
}: AuditLogDetailDialogProps) {
  const [copiedKey, setCopiedKey] = useState<string | null>(null);

  if (!log) return null;

  const formattedJson = prettyPrintDetailJson(log.detailJson);

  const handleCopy = (value: string, label: string) => {
    navigator.clipboard.writeText(value);
    setCopiedKey(label);
    toast.success(`${label} berhasil disalin!`);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  const formatFullDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "long",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <Dialog
      open={!!log}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) onClose();
      }}
    >
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <div className="flex flex-wrap items-center justify-between gap-2 pr-6">
            <div>
              <DialogTitle className="text-lg font-semibold flex items-center gap-2">
                <span>Detail Log Audit Keamanan</span>
              </DialogTitle>
              <DialogDescription className="text-xs mt-0.5">
                Rekaman jejak audit sistem pada {formatFullDate(log.timestampUtc)}
              </DialogDescription>
            </div>

            {log.isSuccess ? (
              <Badge variant="success" className="gap-1 text-xs">
                <CheckCircle2 className="w-3.5 h-3.5" />
                Berhasil
              </Badge>
            ) : (
              <Badge variant="danger" className="gap-1 text-xs">
                <XCircle className="w-3.5 h-3.5" />
                Gagal
              </Badge>
            )}
          </div>
        </DialogHeader>

        <div className="space-y-4 py-1 text-xs">
          {/* Grid Informasi Utama */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 rounded-lg border border-neutral-200 dark:border-slate-800 bg-neutral-50/60 dark:bg-slate-900/50 p-3.5">
            <div className="space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium">
                Jenis Event (EventType)
              </span>
              <div className="flex flex-wrap items-center gap-1.5">
                <code className="font-mono font-semibold text-foreground bg-white dark:bg-slate-950 px-2 py-0.5 rounded border border-neutral-200 dark:border-slate-800">
                  {log.eventType}
                </code>
                <span className="text-muted-foreground">
                  ({getAuditEventLabel(log.eventType)})
                </span>
              </div>
            </div>

            <div className="space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium">
                ID Log Audit
              </span>
              <div className="flex items-center gap-1.5">
                <code className="font-mono text-[11px] text-foreground truncate">
                  {log.id}
                </code>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-5 w-5 shrink-0"
                  onClick={() => handleCopy(log.id, "ID Log")}
                  title="Salin ID Log"
                >
                  {copiedKey === "ID Log" ? (
                    <Check className="w-3 h-3 text-emerald-500" />
                  ) : (
                    <Copy className="w-3 h-3" />
                  )}
                </Button>
              </div>
            </div>

            <div className="space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium">
                Pengguna Terkait (Actor / Target)
              </span>
              {log.userId ? (
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-foreground">
                    @{log.username || "unknown"}
                  </span>
                  <Link
                    href={`/users/${log.userId}`}
                    onClick={onClose}
                    className="inline-flex items-center gap-1 text-[11px] text-primary hover:underline"
                  >
                    <span>Lihat User</span>
                    <ExternalLink className="w-3 h-3" />
                  </Link>
                </div>
              ) : (
                <span className="text-muted-foreground italic">
                  Sistem / Anonim
                </span>
              )}
            </div>

            <div className="space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium">
                Aplikasi Klien (ClientApp)
              </span>
              {log.clientId ? (
                <div className="flex items-center gap-2">
                  <code className="font-mono font-semibold bg-white dark:bg-slate-950 px-1.5 py-0.5 rounded border border-neutral-200 dark:border-slate-800">
                    {log.clientId}
                  </code>
                  {log.clientAppId && (
                    <Link
                      href={`/apps/${log.clientAppId}`}
                      onClick={onClose}
                      className="inline-flex items-center gap-1 text-[11px] text-primary hover:underline"
                    >
                      <span>Lihat Aplikasi</span>
                      <ExternalLink className="w-3 h-3" />
                    </Link>
                  )}
                </div>
              ) : (
                <span className="text-muted-foreground italic">
                  Lintas Aplikasi (Global)
                </span>
              )}
            </div>

            <div className="space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium flex items-center gap-1">
                <Globe className="w-3 h-3" />
                Alamat IP Klien
              </span>
              <div className="font-mono text-foreground">
                {log.ipAddress || "-"}
              </div>
            </div>

            <div className="space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium flex items-center gap-1">
                <Monitor className="w-3 h-3" />
                Waktu Server (UTC ISO)
              </span>
              <div className="font-mono text-foreground">
                {log.timestampUtc}
              </div>
            </div>
          </div>

          {/* User Agent */}
          <div className="space-y-1">
            <span className="text-[11px] text-muted-foreground font-medium">
              User-Agent Perangkat / Browser
            </span>
            <div className="p-2.5 rounded-lg border border-neutral-200 dark:border-slate-800 bg-neutral-50/40 dark:bg-slate-900/40 font-mono text-[11px] text-muted-foreground break-all">
              {log.userAgent || "Tidak tercatat"}
            </div>
          </div>

          {/* Payload Detail JSON (Pretty Printed, Tanpa dangerouslySetInnerHTML) */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <FileCode2 className="w-4 h-4 text-primary" />
                Payload Detail (detailJson)
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-7 gap-1.5 text-[11px]"
                onClick={() => handleCopy(formattedJson, "Payload JSON")}
              >
                {copiedKey === "Payload JSON" ? (
                  <Check className="w-3 h-3 text-emerald-500" />
                ) : (
                  <Copy className="w-3 h-3" />
                )}
                Salin JSON
              </Button>
            </div>

            <pre className="p-3.5 rounded-lg bg-slate-950 text-slate-100 font-mono text-xs overflow-x-auto max-h-64 border border-slate-800 leading-relaxed">
              <code>{formattedJson}</code>
            </pre>
          </div>
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            className="text-xs"
          >
            Tutup
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

