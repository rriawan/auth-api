import ActivateAccountFormComponent from "@/components/auth/activate-account-form";
import ThemeLogo from "@/components/shared/theme-logo";
import AuthImage from "@/public/assets/images/auth/forgot-pass-img.png";
import type { Metadata } from "next";
import Image from "next/image";

export const metadata: Metadata = {
  title: "Aktivasi Akun | Auth Admin Portal",
  description: "Kirim ulang tautan aktivasi akun SSO Anda",
};

export default function ActivateAccountPage() {
  return (
    <section className="bg-white dark:bg-slate-900 flex flex-wrap min-h-screen">
      {/* Left Image */}
      <div className="lg:w-1/2 hidden lg:block">
        <div className="flex items-center justify-center h-screen flex-col">
          <Image
            src={AuthImage}
            alt="Auth Illustration"
            className="object-cover w-full h-full"
            priority
          />
        </div>
      </div>

      {/* Right Form */}
      <div className="lg:w-1/2 w-full py-8 px-6 flex flex-col justify-center">
        <div className="lg:max-w-[464px] w-full mx-auto">
          {/* Logo and heading */}
          <div>
            <div className="mb-2.5 inline-block max-w-[290px]">
              <ThemeLogo />
            </div>

            <h4 className="font-semibold mb-3 text-2xl">Aktivasi Akun</h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-300 text-sm">
              Akun Anda belum aktif? Masukkan username atau email Anda untuk mendapatkan tautan aktivasi akun baru.
            </p>
          </div>

          <ActivateAccountFormComponent />
        </div>
      </div>
    </section>
  );
}

