import AuthIllustration from "@/components/auth/auth-illustration";
import ConfirmActivationComponent from "@/components/auth/confirm-activation";
import ThemeLogo from "@/components/shared/theme-logo";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Konfirmasi Aktivasi Akun | User Management",
  description: "Verifikasi dan aktivasi akun SSO Anda",
};

export default function ConfirmActivationPage() {
  return (
    <section className="bg-white dark:bg-slate-900 flex flex-wrap min-h-screen">
      {/* Left Illustration */}
      <div className="lg:w-1/2 hidden lg:block">
        <AuthIllustration />
      </div>

      {/* Right Content */}
      <div className="lg:w-1/2 w-full py-8 px-6 flex flex-col justify-center">
        <div className="lg:max-w-[464px] w-full mx-auto">
          {/* Logo and heading */}
          <div className="text-center sm:text-left">
            <div className="mb-2.5 inline-block max-w-[290px]">
              <ThemeLogo />
            </div>

            <h4 className="font-semibold mb-3 text-2xl">
              Konfirmasi Aktivasi Akun
            </h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-300 text-sm">
              Memproses verifikasi kredensial akun pengguna terdaftar.
            </p>
          </div>

          <Suspense
            fallback={
              <div className="h-64 flex items-center justify-center text-sm text-neutral-500">
                Memvalidasi aktivasi akun...
              </div>
            }
          >
            <ConfirmActivationComponent />
          </Suspense>
        </div>
      </div>
    </section>
  );
}
