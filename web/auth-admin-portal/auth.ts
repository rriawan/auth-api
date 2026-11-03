import NextAuth from "next-auth";
import Credentials from "next-auth/providers/credentials";

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    Credentials({
      credentials: {
        usernameOrEmail: {},
        password: {},
      },
      authorize: async () => {
        // Fase F1: Placeholder auth configuration
        // Fase F3 akan menghubungkan langsung ke POST /api/v1/auth/login milik auth-api
        return null;
      },
    }),
  ],
  pages: {
    signIn: "/auth/login",
  },
});
