"use client";

import Link from "next/link";
import { ShieldCheck } from "lucide-react";
import { useSidebarCollapsed } from "@/hooks/useSidebarCollapsed";
import { cn } from "@/lib/utils";

function LogoSidebar() {
  const isCollapsed = useSidebarCollapsed();

  return (
    <Link
      href="/"
      className={cn(
        "sidebar-logo h-[72px] py-3.5 flex items-center border-b border-neutral-100 dark:border-slate-800 transition-all select-none",
        isCollapsed ? "justify-center px-1" : "justify-start px-4 gap-2.5"
      )}
      title="User Management"
    >
      <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-blue-500/25 shrink-0">
        <ShieldCheck className="w-5 h-5 stroke-[2.2]" />
      </div>

      {!isCollapsed && (
        <span className="font-bold text-base tracking-tight text-neutral-900 dark:text-neutral-100 truncate">
          User Management
        </span>
      )}
    </Link>
  );
}

export default LogoSidebar;
