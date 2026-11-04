import { Loader2 } from "lucide-react";

export default function Loading() {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-white/80 dark:bg-slate-950/80 backdrop-blur-sm">
      <div className="flex flex-col items-center gap-3">
        <Loader2 className="w-10 h-10 animate-spin text-primary" />
        <span className="text-sm font-medium text-neutral-600 dark:text-neutral-400">
          Memuat data...
        </span>
      </div>
    </div>
  );
}
