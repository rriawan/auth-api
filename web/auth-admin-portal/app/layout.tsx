import { LoadingProvider } from "@/contexts/LoadingContext";
import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Auth Admin Portal — Layanan Terpusat Autentikasi & Otorisasi",
  description: "Portal Administrasi dan Layanan Terpusat SSO, JWT RS256, Client Apps, dan RBAC",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="id" suppressHydrationWarning>
      <body className="font-sans antialiased">
        <LoadingProvider>
          {children}
        </LoadingProvider>
      </body>
    </html>
  );
}
