import AuthIllustration from "@/components/auth/auth-illustration";
import LoginForm from "@/components/auth/login-form";
import ThemeLogo from "@/components/shared/theme-logo";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Masuk | User Management",
  description: "Masuk ke User Management Portal Layanan Terpusat Autentikasi & Otorisasi",
};

export default function LoginPage() {
  return (
    <section className="bg-white dark:bg-slate-900 flex flex-wrap min-h-screen">
      {/* Left Illustration */}
      <div className="lg:w-1/2 hidden lg:block">
        <AuthIllustration
          badge="Centralized Authentication & SSO"
          title="Pusat Manajemen Pengguna & Keamanan Akses"
          subtitle="Layanan terpusat Autentikasi (AuthN) dan Otorisasi (AuthZ) berbasis Single User Pool untuk mengamankan seluruh aplikasi enterprise."
        />
      </div>

      {/* Right Form */}
      <div className="lg:w-1/2 w-full py-8 px-6 flex flex-col justify-center">
        <div className="lg:max-w-[464px] w-full mx-auto">
          {/* Logo and heading */}
          <div>
            <div className="mb-4 inline-block">
              <ThemeLogo />
            </div>

            <h4 className="font-semibold mb-2 text-2xl text-neutral-900 dark:text-neutral-100">
              Masuk ke Akun Anda
            </h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-400 text-sm">
              Selamat datang kembali. Masukkan kredensial admin Anda untuk melanjutkan.
            </p>
          </div>

          {/* Login Form wrapped in Suspense for searchParams */}
          <Suspense
            fallback={
              <div className="h-64 flex items-center justify-center text-sm text-neutral-500">
                Memuat formulir...
              </div>
            }
          >
            <LoginForm />
          </Suspense>
        </div>
      </div>
    </section>
  );
}
