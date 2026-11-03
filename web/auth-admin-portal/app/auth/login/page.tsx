import LoginForm from "@/components/auth/login-form";
import ThemeLogo from "@/components/shared/theme-logo";
import AuthImage from "@/public/assets/images/auth/auth-img.png";
import type { Metadata } from "next";
import Image from "next/image";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Masuk | Auth Admin Portal",
  description: "Masuk ke Auth Admin Portal Layanan Terpusat Autentikasi & Otorisasi",
};

export default function LoginPage() {
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

            <h4 className="font-semibold mb-3 text-2xl">Masuk ke Akun Anda</h4>
            <p className="mb-8 text-neutral-500 dark:text-neutral-300 text-sm">
              Selamat datang kembali. Masukkan kredensial admin Anda untuk melanjutkan.
            </p>
          </div>

          {/* Login Form wrapped in Suspense for searchParams */}
          <Suspense fallback={<div className="h-64 flex items-center justify-center text-sm text-neutral-500">Memuat formulir...</div>}>
            <LoginForm />
          </Suspense>
        </div>
      </div>
    </section>
  );
}
