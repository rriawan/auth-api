import { isTokenExpiringSoon } from "@/lib/auth/jwt-utils";
import { refreshAccessTokenSingleFlight } from "@/lib/auth/token-refresh";
import { decode, encode, JWT } from "next-auth/jwt";
import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";

const publicRoutes = [
  "/auth/login",
  "/auth/forgot-password",
  "/auth/reset-password",
  "/auth/create-password",
  "/auth/activate-account",
];

export async function proxy(req: NextRequest) {
  const { pathname } = req.nextUrl;

  // Lewati static assets, route internal Next.js, dan endpoint NextAuth API
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/favicon.ico") ||
    pathname.startsWith("/api/auth") ||
    pathname.startsWith("/images") ||
    pathname.startsWith("/assets") ||
    pathname.startsWith("/manifest.json")
  ) {
    return NextResponse.next();
  }

  const isPublic = publicRoutes.some((route) => pathname.startsWith(route));

  // Ambil session cookie NextAuth (mendukung http dev dan https production)
  const sessionCookie =
    req.cookies.get("__Secure-authjs.session-token") ||
    req.cookies.get("authjs.session-token");

  // Jika tidak memiliki session cookie
  if (!sessionCookie?.value) {
    if (!isPublic) {
      const loginUrl = new URL("/auth/login", req.url);
      loginUrl.searchParams.set("callbackUrl", pathname + req.nextUrl.search);
      return NextResponse.redirect(loginUrl);
    }
    return NextResponse.next();
  }

  // Decode token sesi terenkripsi
  let token: JWT | null = null;
  try {
    token = await decode({
      token: sessionCookie.value,
      secret: process.env.AUTH_SECRET!,
      salt: sessionCookie.name,
    });
  } catch {
    if (!isPublic) {
      const loginUrl = new URL("/auth/login", req.url);
      loginUrl.searchParams.set("error", "SessionExpired");
      const res = NextResponse.redirect(loginUrl);
      res.cookies.delete(sessionCookie.name);
      return res;
    }
    return NextResponse.next();
  }

  if (!token) {
    if (!isPublic) {
      const loginUrl = new URL("/auth/login", req.url);
      loginUrl.searchParams.set("callbackUrl", pathname + req.nextUrl.search);
      const res = NextResponse.redirect(loginUrl);
      res.cookies.delete(sessionCookie.name);
      return res;
    }
    return NextResponse.next();
  }

  // Token valid: periksa apakah sudah mendekati kedaluwarsa (<= 60 detik)
  const expiresAt = (token.accessTokenExpiresAt as number) || 0;
  const isExpiring = isTokenExpiringSoon(expiresAt, 60);

  if (isExpiring && token.refreshToken) {
    try {
      // Rotasi token secara single-flight ke backend
      const refreshed = await refreshAccessTokenSingleFlight(
        token.refreshToken as string,
        req.headers
      );

      token.accessToken = refreshed.accessToken;
      token.refreshToken = refreshed.refreshToken;
      token.accessTokenExpiresAt =
        Date.now() + refreshed.expiresInSeconds * 1000;

      // Encode kembali cookie sesi dengan token yang baru
      const newCookieValue = await encode({
        token,
        secret: process.env.AUTH_SECRET!,
        salt: sessionCookie.name,
      });

      const res =
        isPublic && pathname === "/auth/login"
          ? NextResponse.redirect(new URL("/", req.url))
          : NextResponse.next();

      // Tulis ulang header Set-Cookie ke response agar tersimpan ke browser
      res.cookies.set(sessionCookie.name, newCookieValue, {
        httpOnly: true,
        sameSite: "lax",
        path: "/",
        secure: sessionCookie.name.startsWith("__Secure-"),
        maxAge: 14 * 24 * 60 * 60,
      });

      return res;
    } catch {
      // Refresh token ditolak oleh backend (misal kedaluwarsa atau tercabut)
      const loginUrl = new URL("/auth/login", req.url);
      loginUrl.searchParams.set("error", "SessionExpired");
      const res = NextResponse.redirect(loginUrl);
      res.cookies.delete(sessionCookie.name);
      return res;
    }
  }

  // Jika sudah terautentikasi dan mencoba mengakses halaman login
  if (isPublic && pathname === "/auth/login") {
    return NextResponse.redirect(new URL("/", req.url));
  }

  return NextResponse.next();
}
