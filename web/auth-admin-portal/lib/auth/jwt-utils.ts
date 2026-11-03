export interface DecodedAccessTokenClaims {
  sub: string;
  username: string;
  email: string;
  roles: string[];
  permissions: string[];
  exp: number;
  iat?: number;
  jti?: string;
  aud?: string;
}

/**
 * Mendekode payload JWT claims dari access token backend tanpa verifikasi kriptografi.
 * Berjalan aman di seluruh runtime (Edge, Node.js, Browser).
 */
export function decodeAccessTokenClaims(
  token: string
): DecodedAccessTokenClaims | null {
  if (!token || typeof token !== "string") {
    return null;
  }

  try {
    const parts = token.split(".");
    if (parts.length < 2) {
      return null;
    }

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(
      base64.length + ((4 - (base64.length % 4)) % 4),
      "="
    );

    let jsonString: string;
    if (typeof atob === "function") {
      jsonString = atob(padded);
    } else if (typeof Buffer !== "undefined") {
      jsonString = Buffer.from(padded, "base64").toString("utf8");
    } else {
      return null;
    }

    const payload = JSON.parse(jsonString);

    const roles: string[] = Array.isArray(payload.roles)
      ? payload.roles
      : typeof payload.roles === "string"
      ? [payload.roles]
      : [];

    const permissions: string[] = Array.isArray(payload.permissions)
      ? payload.permissions
      : typeof payload.permissions === "string"
      ? [payload.permissions]
      : [];

    return {
      sub: String(payload.sub || ""),
      username: String(payload.unique_name || payload.username || ""),
      email: String(payload.email || ""),
      roles,
      permissions,
      exp: Number(payload.exp || 0),
      iat: payload.iat ? Number(payload.iat) : undefined,
      jti: payload.jti ? String(payload.jti) : undefined,
      aud: payload.aud ? String(payload.aud) : undefined,
    };
  } catch {
    return null;
  }
}

/**
 * Mengecek apakah token sudah kedaluwarsa atau mendekati kedaluwarsa.
 * @param expiresAtMs Timestamp kedaluwarsa dalam milidetik
 * @param bufferSeconds Waktu tenggang sebelum kedaluwarsa resmi (default: 60 detik)
 */
export function isTokenExpiringSoon(
  expiresAtMs: number,
  bufferSeconds: number = 60
): boolean {
  if (!expiresAtMs || expiresAtMs <= 0) {
    return true;
  }
  return Date.now() >= expiresAtMs - bufferSeconds * 1000;
}

