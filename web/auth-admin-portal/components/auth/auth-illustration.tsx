import React from "react";
import { ShieldCheck, KeyRound, Users, Lock, CheckCircle2 } from "lucide-react";

export default function AuthIllustration({
  title = "Pusat Manajemen Pengguna & Keamanan Akses",
  subtitle = "Layanan terpusat Autentikasi (AuthN) dan Otorisasi (AuthZ) berbasis Single User Pool untuk mengamankan seluruh aplikasi enterprise.",
  badge = "Centralized Authentication & SSO",
}: {
  title?: string;
  subtitle?: string;
  badge?: string;
}) {
  return (
    <div className="relative w-full h-full min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950 text-white flex flex-col justify-between p-10 xl:p-14 overflow-hidden select-none">
      {/* Decorative ambient background glows */}
      <div className="absolute -top-24 -left-24 w-96 h-96 bg-blue-600/20 rounded-full blur-3xl pointer-events-none" />
      <div className="absolute top-1/2 -right-24 w-96 h-96 bg-indigo-600/20 rounded-full blur-3xl pointer-events-none" />
      <div className="absolute -bottom-20 left-1/3 w-80 h-80 bg-cyan-500/15 rounded-full blur-3xl pointer-events-none" />

      {/* Decorative Grid Pattern */}
      <div
        className="absolute inset-0 opacity-[0.03] pointer-events-none"
        style={{
          backgroundImage: `radial-gradient(circle at 1px 1px, white 1px, transparent 0)`,
          backgroundSize: "28px 28px",
        }}
      />

      {/* Top Header Tag */}
      <div className="relative z-10">
        <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-blue-500/10 border border-blue-400/20 text-blue-300 text-xs font-semibold tracking-wide">
          <ShieldCheck className="w-4 h-4 text-blue-400" />
          <span>{badge}</span>
        </div>
      </div>

      {/* Center Illustration Graphic */}
      <div className="relative z-10 my-auto py-8 flex items-center justify-center">
        <div className="relative w-full max-w-[420px] aspect-square flex items-center justify-center">
          {/* Outer Rotating Pulse Ring */}
          <div className="absolute inset-0 rounded-full border border-blue-500/20 animate-[spin_60s_linear_infinite]" />
          <div className="absolute inset-8 rounded-full border border-dashed border-indigo-500/25" />
          <div className="absolute inset-16 rounded-full border border-blue-400/15" />

          {/* Core Central Shield Emblem */}
          <div className="relative z-20 w-32 h-32 rounded-3xl bg-gradient-to-tr from-blue-600 via-indigo-600 to-cyan-500 p-0.5 shadow-2xl shadow-blue-500/40">
            <div className="w-full h-full rounded-[22px] bg-slate-950/80 backdrop-blur-md flex flex-col items-center justify-center text-white">
              <ShieldCheck className="w-14 h-14 text-blue-400 drop-shadow-[0_0_12px_rgba(96,165,250,0.5)]" />
              <span className="text-[10px] font-mono tracking-wider text-blue-200 uppercase mt-1">
                Verified
              </span>
            </div>
          </div>

          {/* Floating Card 1: User Identity (Top Right) */}
          <div className="absolute -top-2 right-0 sm:right-2 z-30 bg-slate-900/85 backdrop-blur-md border border-slate-700/60 rounded-2xl p-3.5 shadow-xl shadow-black/40 flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white">
              <Users className="w-4 h-4" />
            </div>
            <div className="space-y-0.5">
              <div className="flex items-center gap-1.5">
                <span className="text-xs font-bold text-white">superadmin</span>
                <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
              </div>
              <p className="text-[10px] text-slate-400 font-mono">Role: SuperAdmin</p>
            </div>
          </div>

          {/* Floating Card 2: Connected Applications (Bottom Left) */}
          <div className="absolute -bottom-2 left-0 sm:left-2 z-30 bg-slate-900/85 backdrop-blur-md border border-slate-700/60 rounded-2xl p-3.5 shadow-xl shadow-black/40 flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white">
              <KeyRound className="w-4 h-4" />
            </div>
            <div className="space-y-0.5">
              <div className="flex items-center gap-1">
                <span className="text-xs font-bold text-white">ClientApps SSO</span>
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
              </div>
              <p className="text-[10px] text-slate-400">auth-admin, erp, portal</p>
            </div>
          </div>

          {/* Floating Card 3: Security & Session (Bottom Right) */}
          <div className="absolute bottom-12 -right-4 sm:right-0 z-30 bg-slate-900/85 backdrop-blur-md border border-slate-700/60 rounded-2xl p-3 shadow-xl shadow-black/40 hidden sm:flex items-center gap-2.5">
            <div className="w-7 h-7 rounded-lg bg-emerald-500/20 text-emerald-400 flex items-center justify-center">
              <Lock className="w-3.5 h-3.5" />
            </div>
            <div>
              <p className="text-[11px] font-semibold text-white">RS256 JWT</p>
              <p className="text-[9px] text-slate-400">Signed & Encrypted</p>
            </div>
          </div>
        </div>
      </div>

      {/* Bottom Information & Highlights */}
      <div className="relative z-10 space-y-4">
        <div>
          <h2 className="text-2xl xl:text-3xl font-extrabold text-white tracking-tight leading-snug">
            {title}
          </h2>
          <p className="text-sm text-slate-400 mt-2 max-w-lg leading-relaxed">
            {subtitle}
          </p>
        </div>

        {/* Feature Pills */}
        <div className="flex flex-wrap gap-2 pt-2">
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-slate-800/80 border border-slate-700/50 text-xs font-medium text-slate-300">
            <CheckCircle2 className="w-3.5 h-3.5 text-blue-400" />
            Single User Pool SSO
          </span>
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-slate-800/80 border border-slate-700/50 text-xs font-medium text-slate-300">
            <CheckCircle2 className="w-3.5 h-3.5 text-indigo-400" />
            Role &amp; Permissions (RBAC)
          </span>
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-lg bg-slate-800/80 border border-slate-700/50 text-xs font-medium text-slate-300">
            <CheckCircle2 className="w-3.5 h-3.5 text-cyan-400" />
            Audit Trail 24/7
          </span>
        </div>
      </div>
    </div>
  );
}

