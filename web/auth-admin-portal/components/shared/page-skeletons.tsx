import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader } from "@/components/ui/card";

export function TablePageSkeleton({
  title = "Memuat Data...",
  rowCount = 5,
}: {
  title?: string;
  rowCount?: number;
}) {
  return (
    <div className="space-y-6" role="status" aria-label={title}>
      <span className="sr-only">{title}</span>
      {/* Breadcrumb & Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="space-y-2">
          <Skeleton className="h-4 w-40" />
          <Skeleton className="h-8 w-60" />
        </div>
        <Skeleton className="h-10 w-36" />
      </div>

      {/* Filter / Search Bar */}
      <Card className="border border-neutral-200/80 dark:border-slate-800 shadow-sm">
        <CardContent className="pt-6 pb-6">
          <div className="flex flex-col sm:flex-row gap-4 justify-between">
            <Skeleton className="h-10 w-full sm:w-72" />
            <div className="flex gap-2">
              <Skeleton className="h-10 w-32" />
              <Skeleton className="h-10 w-24" />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Table Skeleton */}
      <Card className="border border-neutral-200/80 dark:border-slate-800 shadow-sm">
        <CardContent className="p-0">
          <div className="p-4 border-b border-neutral-200 dark:border-slate-800 bg-neutral-50 dark:bg-slate-900/50">
            <div className="grid grid-cols-4 sm:grid-cols-6 gap-4">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-4 w-20 hidden sm:block" />
              <Skeleton className="h-4 w-20 hidden sm:block" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-16 ml-auto" />
            </div>
          </div>
          <div className="divide-y divide-neutral-100 dark:divide-slate-800">
            {Array.from({ length: rowCount }).map((_, i) => (
              <div key={i} className="p-4 grid grid-cols-4 sm:grid-cols-6 gap-4 items-center">
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-4 w-40" />
                <Skeleton className="h-5 w-16 rounded-full hidden sm:block" />
                <Skeleton className="h-4 w-28 hidden sm:block" />
                <Skeleton className="h-5 w-14 rounded-full" />
                <Skeleton className="h-8 w-8 rounded ml-auto" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export function DetailPageSkeleton() {
  return (
    <div className="space-y-6">
      {/* Breadcrumb & Header */}
      <div className="space-y-2">
        <Skeleton className="h-4 w-48" />
        <div className="flex items-center gap-3">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-6 w-20 rounded-full" />
        </div>
      </div>

      {/* Tabs Bar Skeleton */}
      <div className="flex gap-2 border-b border-neutral-200 dark:border-slate-800 pb-2">
        <Skeleton className="h-10 w-28 rounded-md" />
        <Skeleton className="h-10 w-36 rounded-md" />
        <Skeleton className="h-10 w-32 rounded-md" />
        <Skeleton className="h-10 w-24 rounded-md" />
      </div>

      {/* Main Content Card Skeleton */}
      <Card className="border border-neutral-200/80 dark:border-slate-800 shadow-sm">
        <CardHeader className="space-y-2">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-96" />
        </CardHeader>
        <CardContent className="space-y-6 pt-2">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-full" />
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-full" />
            </div>
          </div>
          <div className="space-y-2">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-24 w-full" />
          </div>
          <div className="flex justify-end gap-3 pt-4">
            <Skeleton className="h-10 w-28" />
            <Skeleton className="h-10 w-32" />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export function DashboardPageSkeleton() {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-2">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-8 w-56" />
      </div>

      {/* 4 Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i} className="border border-neutral-200/80 dark:border-slate-800 shadow-sm">
            <CardContent className="p-6 space-y-3">
              <div className="flex justify-between items-center">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-9 w-9 rounded-lg" />
              </div>
              <Skeleton className="h-8 w-20" />
              <Skeleton className="h-3 w-32" />
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Chart & Activities */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2 border border-neutral-200/80 dark:border-slate-800 shadow-sm">
          <CardHeader className="space-y-2">
            <Skeleton className="h-6 w-44" />
            <Skeleton className="h-4 w-72" />
          </CardHeader>
          <CardContent>
            <Skeleton className="h-[300px] w-full rounded-lg" />
          </CardContent>
        </Card>

        <Card className="border border-neutral-200/80 dark:border-slate-800 shadow-sm">
          <CardHeader className="space-y-2">
            <Skeleton className="h-6 w-36" />
            <Skeleton className="h-4 w-48" />
          </CardHeader>
          <CardContent className="space-y-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="flex items-center gap-3">
                <Skeleton className="h-8 w-8 rounded-full" />
                <div className="space-y-1 flex-1">
                  <Skeleton className="h-4 w-full" />
                  <Skeleton className="h-3 w-24" />
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
