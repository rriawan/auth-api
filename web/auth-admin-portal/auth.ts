import { authApi } from "@/lib/api/auth";
import { ApiException } from "@/lib/api/errors";
import { decodeAccessTokenClaims } from "@/lib/auth/jwt-utils";
import { loginSchema } from "@/lib/validation/auth.schema";
import NextAuth, { CredentialsSignin } from "next-auth";
import Credentials from "next-auth/providers/credentials";

class CustomAuthError extends CredentialsSignin {
  code: string;
  constructor(message: string, code: string) {
    super(message);
    this.code = code;
    this.message = message;
  }
}

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    Credentials({
      credentials: {
        usernameOrEmail: {},
        password: {},
      },
      authorize: async (credentials) => {
        const parsed = loginSchema.safeParse(credentials);
        if (!parsed.success) {
          throw new CredentialsSignin("Format data login tidak valid.");
        }

        try {
          const clientId = process.env.AUTH_API_CLIENT_ID || "auth-admin";
          const res = await authApi.login({
            usernameOrEmail: parsed.data.usernameOrEmail,
            password: parsed.data.password,
            clientId,
          });

          return {
            id: res.user.id,
            username: res.user.username,
            email: res.user.email,
            fullName: res.user.fullName,
            accessToken: res.accessToken,
            accessTokenExpiresAt: Date.now() + res.expiresInSeconds * 1000,
            refreshToken: res.refreshToken,
          };
        } catch (error) {
          if (error instanceof ApiException) {
            const detail =
              error.problem?.detail ||
              error.message ||
              "Username/email atau password salah.";
            const errorCode = error.isUnauthorized
              ? "invalid_credentials"
              : error.isForbidden
              ? "forbidden"
              : error.isRateLimited
              ? "rate_limited"
              : "server_error";
            throw new CustomAuthError(detail, errorCode);
          }
          throw new CredentialsSignin("Gagal terhubung ke layanan autentikasi.");
        }
      },
    }),
  ],
  pages: {
    signIn: "/auth/login",
    error: "/auth/login",
  },
  session: {
    strategy: "jwt",
    maxAge: 14 * 24 * 60 * 60, // 14 hari, selaras dengan RefreshTokenExpirationDays backend
  },
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        token.accessToken = user.accessToken;
        token.accessTokenExpiresAt = user.accessTokenExpiresAt;
        token.refreshToken = user.refreshToken;
        token.user = {
          id: user.id,
          username: user.username,
          email: user.email,
          fullName: user.fullName,
        };
      }
      return token;
    },
    async session({ session, token }) {
      if (token.user) {
        session.user = {
          ...session.user,
          id: token.user.id,
          username: token.user.username,
          email: token.user.email,
          fullName: token.user.fullName,
        };
      }

      if (token.accessToken) {
        const claims = decodeAccessTokenClaims(token.accessToken);
        if (claims) {
          session.user.roles = claims.roles;
          session.user.permissions = claims.permissions;
        } else {
          session.user.roles = [];
          session.user.permissions = [];
        }
      } else {
        session.user.roles = [];
        session.user.permissions = [];
      }

      if (token.error) {
        session.error = token.error;
      }

      return session;
    },
  },
});
