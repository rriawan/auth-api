import AuthIllustration from "@/components/auth/auth-illustration";
import ResetPasswordComponent from "@/components/auth/reset-password";
import ThemeLogo from "@/components/shared/theme-logo";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Reset Password | User Management",
  description: "Atur ulang password akun SSO Anda",
};

export default function ResetPasswordPage() {
  return (
    <section className="bg-white dark:bg-slate-900 flex flex-wrap min-h-screen">
      {/* Left Illustration */}
      <div className="lg:w-1/2 hidden lg:block">
        <AuthIllustration
          badge="Proteksi Kredensial Pengguna"
          title="Keamanan Password & Enkripsi"
          subtitle="Buat password baru dengan kombinasi yang kuat untuk menjaga integritas akses akun Anda."
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
              Atur Ulang Password
            </h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-400 text-sm">
              Buat password baru yang aman dan belum pernah digunakan sebelumnya.
            </p>
          </div>

          <Suspense
            fallback={
              <div className="h-64 flex items-center justify-center text-sm text-neutral-500">
                Memuat formulir...
              </div>
            }
          >
            <ResetPasswordComponent />
          </Suspense>
        </div>
      </div>
    </section>
  );
}
