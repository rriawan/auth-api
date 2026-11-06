import { Badge } from "@/components/ui/badge";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { UserAppAccessDetail } from "@/components/users/actions/users";
import { UserAppAccessTab } from "@/components/users/user-app-access-tab";
import { UserInfoTab } from "@/components/users/user-info-tab";
import { todayJakarta } from "@/components/employees/format";
import { UserOrganizationTab } from "@/components/users/user-organization-tab";
import { auth } from "@/auth";
import { employeesApi } from "@/lib/api/employees";
import { hasPermission } from "@/lib/auth/permission-rules";
import type { UserOrganizationDto } from "@/lib/api/employee-types";
import { appsApi } from "@/lib/api/apps";
import { getErrorMessage } from "@/lib/api/errors";
import { rolesApi } from "@/lib/api/roles";
import { AdminUserDto, ClientAppDto } from "@/lib/api/types";
import { userAppRolesApi } from "@/lib/api/user-app-roles";
import { usersApi } from "@/lib/api/users";
import {
  AlertTriangle,
  ArrowLeft,
  Building2,
  CheckCircle2,
  Layers,
  Lock,
  Mail,
  ShieldAlert,
  UserCheck,
  UserCog,
  XCircle,
} from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

interface UserDetailPageProps {
  params: Promise<{ userId: string }>;
}

function checkUserLocked(lockoutEndUtc: string | null): boolean {
  if (!lockoutEndUtc) return false;
  try {
    return new Date(lockoutEndUtc) > new Date();
  } catch {
    return false;
  }
}

export async function generateMetadata({
  params,
}: UserDetailPageProps): Promise<Metadata> {
  const { userId } = await params;
  try {
    const user = await usersApi.getById(userId);
    return {
      title: `${user.fullName} (@${user.username}) | Auth Admin Portal`,
      description: `Kelola profil, keamanan, dan penugasan akses aplikasi untuk pengguna ${user.username}`,
    };
  } catch {
    return {
      title: "Detail Pengguna | Auth Admin Portal",
    };
  }
}

export default async function UserDetailPage({ params }: UserDetailPageProps) {
  const { userId } = await params;

  let user: AdminUserDto | null = null;
  let fetchError: string | null = null;

  try {
    user = await usersApi.getById(userId);
  } catch (err) {
    fetchError = getErrorMessage(err);
  }

  if (fetchError || !user) {
    return (
      <div className="space-y-6">
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbLink href="/users">Pengguna</BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>Detail</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>

        <Card className="border-red-200 dark:border-red-900/50 bg-red-50/20 dark:bg-red-950/20">
          <CardContent className="p-8 text-center space-y-4">
            <ShieldAlert className="w-12 h-12 text-red-500 mx-auto" />
            <h2 className="text-lg font-semibold text-red-800 dark:text-red-300">
              Pengguna Tidak Ditemukan
            </h2>
            <p className="text-xs text-muted-foreground max-w-md mx-auto">
              {fetchError ||
                "Akun pengguna yang Anda cari tidak ditemukan atau ID tidak valid."}
            </p>
            <Link href="/users">
              <Button variant="outline" className="gap-2 text-xs">
                <ArrowLeft className="w-4 h-4" />
                Kembali ke Daftar Pengguna
              </Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Ambil daftar aplikasi terdaftar untuk panel Akses Aplikasi
  let apps: ClientAppDto[] = [];
  let appsFetchError: string | null = null;

  try {
    apps = await appsApi.getAll();
  } catch (err) {
    appsFetchError = getErrorMessage(err);
  }

  const initialSelectedAppId =
    user.appAssignments[0]?.clientAppId || apps[0]?.id || "";

  let initialAccessDetail: UserAppAccessDetail | null = null;
  if (initialSelectedAppId) {
    try {
      const [availableRoles, userAccess] = await Promise.all([
        rolesApi.getAll(initialSelectedAppId),
        userAppRolesApi.getRoles(user.id, initialSelectedAppId),
      ]);
      initialAccessDetail = { availableRoles, userAccess };
    } catch {
      initialAccessDetail = null;
    }
  }

  // Tab Organisasi bersifat opsional: kegagalan memuatnya tidak boleh merusak halaman pengguna.
  const session = await auth();
  const canReadEmployees = hasPermission(session?.user?.permissions, session?.user?.roles, "employees.read");
  let organization: UserOrganizationDto | null = null;
  let organizationError: string | null = null;
  if (canReadEmployees) {
    try {
      organization = await employeesApi.getByUser(user.id);
    } catch (err) {
      organizationError = getErrorMessage(err);
    }
  }

  const isLocked = checkUserLocked(user.lockoutEndUtc);

  const formatDate = (dateStr: string) => {
    try {
      const d = new Date(dateStr);
      return new Intl.DateTimeFormat("id-ID", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }).format(d);
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="space-y-6">
      {/* Breadcrumb Navigasi */}
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink href="/users">Pengguna</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{user.fullName}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header Detail Pengguna */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link href="/users">
            <Button
              variant="outline"
              size="icon"
              className="h-9 w-9 shrink-0 text-muted-foreground hover:text-foreground"
              title="Kembali ke Daftar Pengguna"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </Link>
          <div className="w-12 h-12 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0 font-bold text-lg">
            {user.fullName.charAt(0).toUpperCase()}
          </div>
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                {user.fullName}
              </h1>
              <code className="text-xs font-mono font-medium bg-neutral-100 dark:bg-slate-800 text-neutral-800 dark:text-neutral-200 px-2 py-0.5 rounded">
                @{user.username}
              </code>

              {/* Badge Status: Aktif (Hijau) / Nonaktif (Abu-abu) */}
              {user.isActive ? (
                <Badge variant="success" className="gap-1 text-xs">
                  <CheckCircle2 className="w-3 h-3" />
                  Aktif
                </Badge>
              ) : (
                <Badge
                  variant="secondary"
                  className="gap-1 text-xs text-muted-foreground"
                >
                  <XCircle className="w-3 h-3" />
                  Nonaktif
                </Badge>
              )}

              {/* Badge Status: Terkunci (Merah) */}
              {isLocked && (
                <Badge variant="danger" className="gap-1 text-xs">
                  <Lock className="w-3 h-3" />
                  Terkunci
                </Badge>
              )}

              {/* Badge Status: Belum Verifikasi (Kuning) / Terverifikasi */}
              {user.isAccountVerified ? (
                <Badge variant="info" className="gap-1 text-xs">
                  <UserCheck className="w-3 h-3" />
                  Terverifikasi
                </Badge>
              ) : (
                <Badge variant="warning" className="gap-1 text-xs">
                  <AlertTriangle className="w-3 h-3" />
                  Belum Verifikasi
                </Badge>
              )}
            </div>

            <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground mt-1">
              <span className="inline-flex items-center gap-1">
                <Mail className="w-3.5 h-3.5" />
                {user.email}
              </span>
              <span>•</span>
              <span>Terdaftar pada {formatDate(user.createdAtUtc)}</span>
              {user.updatedAtUtc && (
                <>
                  <span>•</span>
                  <span>Diperbarui {formatDate(user.updatedAtUtc)}</span>
                </>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Tab Navigasi Detail Pengguna */}
      <Tabs defaultValue="info" className="space-y-4">
        <TabsList className="bg-neutral-100 dark:bg-slate-800/80 p-1">
          <TabsTrigger value="info" className="gap-2 text-xs">
            <UserCog className="w-3.5 h-3.5" />
            Profil &amp; Keamanan Akun
          </TabsTrigger>
          <TabsTrigger value="app-access" className="gap-2 text-xs">
            <Layers className="w-3.5 h-3.5" />
            Akses Aplikasi ({user.appAssignments.length})
          </TabsTrigger>
          {canReadEmployees && (
          <TabsTrigger value="organization" className="gap-2 text-xs">
            <Building2 className="w-3.5 h-3.5" />
            Organisasi
          </TabsTrigger>
          )}
        </TabsList>

        <TabsContent value="info">
          <UserInfoTab user={user} isLocked={isLocked} />
        </TabsContent>

        <TabsContent value="app-access">
          <UserAppAccessTab
            user={user}
            apps={apps}
            initialSelectedAppId={initialSelectedAppId}
            initialAccessDetail={initialAccessDetail}
            appsFetchError={appsFetchError}
          />
        </TabsContent>

        {canReadEmployees && (
          <TabsContent value="organization">
            <UserOrganizationTab userId={user.id} organization={organization} fetchError={organizationError} today={todayJakarta()} />
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}
