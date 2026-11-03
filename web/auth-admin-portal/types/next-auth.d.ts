import { DefaultSession, DefaultUser } from "next-auth";
import { DefaultJWT } from "next-auth/jwt";

declare module "next-auth" {
  interface Session {
    user: {
      id: string;
      username: string;
      email: string;
      fullName: string;
      roles: string[];
      permissions: string[];
    } & DefaultSession["user"];
    error?: string;
  }

  interface User extends DefaultUser {
    id: string;
    username: string;
    email: string;
    fullName: string;
    accessToken: string;
    accessTokenExpiresAt: number;
    refreshToken: string;
  }
}

declare module "next-auth/jwt" {
  interface JWT extends DefaultJWT {
    accessToken?: string;
    accessTokenExpiresAt?: number;
    refreshToken?: string;
    user?: {
      id: string;
      username: string;
      email: string;
      fullName: string;
    };
    error?: string;
  }
}

