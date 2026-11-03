import { ClientRoot } from "@/app/client-root";
import { auth } from "@/auth";
import { SessionProvider } from "next-auth/react";
import { cookies } from "next/headers";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get("sidebar_state")?.value === "true";
  const session = await auth();

  return (
    <SessionProvider session={session}>
      <ClientRoot defaultOpen={defaultOpen}>{children}</ClientRoot>
    </SessionProvider>
  );
}
