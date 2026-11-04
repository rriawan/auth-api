import AuthIllustration from "@/components/auth/auth-illustration";
import ForgotPasswordComponent from "@/components/auth/forgot-password";
import ThemeLogo from "@/components/shared/theme-logo";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Lupa Password | User Management",
  description: "Permintaan reset password akun SSO User Management",
};

export default function ForgotPasswordPage() {
  return (
    <section className="bg-white dark:bg-slate-900 flex flex-wrap min-h-screen">
      {/* Left Illustration */}
      <div className="lg:w-1/2 hidden lg:block">
        <AuthIllustration
          badge="Self-Service Pemulihan Akun"
          title="Pemulihan Akses & Keamanan Akun"
          subtitle="Proses reset password aman berbasis one-time hashed token untuk melindungi identitas Anda lintas aplikasi."
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
              Lupa Password
            </h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-400 text-sm">
              Masukkan username atau email akun Anda. Kami akan memproses tautan reset password.
            </p>
          </div>

          <ForgotPasswordComponent />
        </div>
      </div>
    </section>
  );
}
