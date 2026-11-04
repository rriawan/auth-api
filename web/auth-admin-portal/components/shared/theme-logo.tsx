"use client";

import Link from "next/link";
import { ShieldCheck } from "lucide-react";

function ThemeLogo() {
  return (
    <Link href="/" className="flex items-center gap-2.5 group select-none">
      <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-blue-500/25 group-hover:scale-105 transition-transform duration-200 shrink-0">
        <ShieldCheck className="w-5 h-5 stroke-[2.2]" />
      </div>
      <span className="font-bold text-xl tracking-tight text-neutral-900 dark:text-neutral-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
        User Management
      </span>
    </Link>
  );
}

export default ThemeLogo;
