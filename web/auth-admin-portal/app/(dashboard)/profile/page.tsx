import { auth } from "@/auth";
import ChangePasswordForm from "@/components/profile/change-password-form";
import ProfileOverview from "@/components/profile/profile-overview";
import { ProfileOrganization } from "@/components/profile/profile-organization";
import { selfOrganizationApi } from "@/lib/api/self-organization";
import type { SelfOrganizationProfileDto } from "@/lib/api/employee-types";
import { getErrorMessage } from "@/lib/api/errors";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { authApi } from "@/lib/api/auth";
import { UserProfileSummaryDto } from "@/lib/api/types";
import { Building2, KeyRound, User } from "lucide-react";
import type { Metadata } from "next";
import { redirect } from "next/navigation";

export const metadata: Metadata = {
  title: "Profil & Password | Auth Admin Portal",
  description: "Kelola profil akun administrator dan ubah kata sandi",
};

export default async function ProfilePage() {
  const session = await auth();
  if (!session?.user) {
    redirect("/auth/login");
  }

  // Ambil data profil terkini dari backend API
  let userProfile: UserProfileSummaryDto;
  try {
    userProfile = await authApi.getMe();
  } catch {
    // Fallback ke data session jika pemanggilan backend gagal
    userProfile = {
      id: session.user.id,
      username: session.user.username,
      email: session.user.email,
      fullName: session.user.fullName,
      clientId: "auth-admin",
      isAccountVerified: true,
      roles: session.user.roles || [],
      permissions: session.user.permissions || [],
    };
  }

  // Ringkasan organisasi sendiri bersifat tambahan: kegagalan memuatnya tidak boleh merusak halaman profil.
  let organization: SelfOrganizationProfileDto | null = null;
  let organizationError: string | null = null;
  try {
    organization = await selfOrganizationApi.get();
  } catch (err) {
    organizationError = getErrorMessage(err);
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink href="/">Dashboard</BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Akun Saya</BreadcrumbPage>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>Profil &amp; Keamanan</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      {/* Header Halaman */}
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-neutral-900 dark:text-white">
          Profil &amp; Keamanan Akun
        </h1>
        <p className="text-sm text-neutral-500 dark:text-neutral-400">
          Informasi identitas akun administrator terdaftar, perizinan akses, dan pengaturan kata sandi.
        </p>
      </div>

      {/* Tabs Navigasi */}
      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList className="bg-neutral-100 dark:bg-slate-800 p-1 rounded-xl">
          <TabsTrigger
            value="overview"
            className="flex items-center gap-2 rounded-lg py-2 px-4 text-xs font-medium cursor-pointer"
          >
            <User className="w-4 h-4" />
            Informasi Profil
          </TabsTrigger>
          <TabsTrigger
            value="password"
            className="flex items-center gap-2 rounded-lg py-2 px-4 text-xs font-medium cursor-pointer"
          >
            <KeyRound className="w-4 h-4" />
            Ganti Password
          </TabsTrigger>
          <TabsTrigger
            value="organization"
            className="flex items-center gap-2 rounded-lg py-2 px-4 text-xs font-medium cursor-pointer"
          >
            <Building2 className="w-4 h-4" />
            Organisasi
          </TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <ProfileOverview user={userProfile} />
        </TabsContent>

        <TabsContent value="password" className="space-y-6">
          <ChangePasswordForm />
        </TabsContent>

        <TabsContent value="organization" className="space-y-6">
          <ProfileOrganization profile={organization} error={organizationError} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

