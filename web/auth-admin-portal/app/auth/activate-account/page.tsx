import ActivateAccountFormComponent from "@/components/auth/activate-account-form";
import AuthIllustration from "@/components/auth/auth-illustration";
import ThemeLogo from "@/components/shared/theme-logo";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Aktivasi Akun | User Management",
  description: "Kirim ulang tautan aktivasi akun SSO Anda",
};

export default function ActivateAccountPage() {
  return (
    <section className="bg-white dark:bg-slate-900 flex flex-wrap min-h-screen">
      {/* Left Illustration */}
      <div className="lg:w-1/2 hidden lg:block">
        <AuthIllustration
          badge="Aktivasi Akun Pengguna"
          title="Verifikasi Identitas & Aktivasi"
          subtitle="Verifikasi email Anda untuk mengaktifkan akun global dan mulai mengakses aplikasi-aplikasi yang terdaftar."
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
              Aktivasi Akun
            </h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-400 text-sm">
              Akun Anda belum aktif? Masukkan username atau email Anda untuk mendapatkan tautan aktivasi akun baru.
            </p>
          </div>

          <ActivateAccountFormComponent />
        </div>
      </div>
    </section>
  );
}
