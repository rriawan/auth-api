import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { History, Layers, ShieldCheck, Users } from "lucide-react";
import Link from "next/link";

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-neutral-900 dark:text-white">
          Dashboard Administrasi
        </h1>
        <p className="text-sm text-neutral-500 dark:text-neutral-400">
          Selamat datang di Auth Admin Portal — Layanan Terpusat Autentikasi &amp; Otorisasi.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Link href="/apps">
          <Card className="hover:border-primary transition-colors cursor-pointer">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">Aplikasi Terdaftar</CardTitle>
              <Layers className="w-5 h-5 text-primary" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">Client Apps</div>
              <p className="text-xs text-muted-foreground mt-1">
                Kelola aplikasi &amp; rotasi API Key
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href="/users">
          <Card className="hover:border-primary transition-colors cursor-pointer">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">Pengguna SSO</CardTitle>
              <Users className="w-5 h-5 text-primary" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">User Pool</div>
              <p className="text-xs text-muted-foreground mt-1">
                Kelola akun &amp; hak akses aplikasi
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href="/audit-logs">
          <Card className="hover:border-primary transition-colors cursor-pointer">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">Audit Logs</CardTitle>
              <History className="w-5 h-5 text-primary" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">Keamanan</div>
              <p className="text-xs text-muted-foreground mt-1">
                Riwayat login, lockout &amp; mutasi akses
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href="/profile">
          <Card className="hover:border-primary transition-colors cursor-pointer">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">Akun Admin</CardTitle>
              <ShieldCheck className="w-5 h-5 text-primary" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">SuperAdmin</div>
              <p className="text-xs text-muted-foreground mt-1">
                Profil &amp; manajemen password
              </p>
            </CardContent>
          </Card>
        </Link>
      </div>
    </div>
  );
}

