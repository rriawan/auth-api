import ResetPasswordComponent from "@/components/auth/reset-password";
import ThemeLogo from "@/components/shared/theme-logo";
import AuthImage from "@/public/assets/images/auth/auth-img.png";
import type { Metadata } from "next";
import Image from "next/image";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Reset Password | Auth Admin Portal",
  description: "Atur ulang password akun SSO Anda",
};

export default function ResetPasswordPage() {
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

            <h4 className="font-semibold mb-3 text-2xl">Atur Ulang Password</h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-300 text-sm">
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

