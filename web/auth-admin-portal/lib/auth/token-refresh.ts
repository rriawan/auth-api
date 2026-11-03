export interface RefreshResult {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  expiresAtUtc: string;
}

const inFlightRefreshes = new Map<string, Promise<RefreshResult>>();

/**
 * Melakukan rotasi refresh token ke backend dengan pola Single-Flight.
 * Menjamin hanya 1 permintaan POST /api/v1/auth/refresh yang berjalan untuk token yang sama,
 * guna mencegah pemicuan 'Refresh Token Reuse Detection' di backend.
 */
export async function refreshAccessTokenSingleFlight(
  refreshToken: string,
  incomingHeaders?: Headers
): Promise<RefreshResult> {
  const existingPromise = inFlightRefreshes.get(refreshToken);
  if (existingPromise) {
    return existingPromise;
  }

  const promise = (async () => {
    const baseUrl = process.env.AUTH_API_BASE_URL || "http://localhost:5100";
    const clientId = process.env.AUTH_API_CLIENT_ID || "auth-admin";
    const url = `${baseUrl.replace(/\/+$/, "")}/api/v1/auth/refresh`;

    const requestHeaders: Record<string, string> = {
      "Content-Type": "application/json",
    };

    if (incomingHeaders) {
      const forwardedFor =
        incomingHeaders.get("x-forwarded-for") ||
        incomingHeaders.get("x-real-ip");
      if (forwardedFor) {
        requestHeaders["X-Forwarded-For"] = forwardedFor;
      }
      const userAgent = incomingHeaders.get("user-agent");
      if (userAgent) {
        requestHeaders["User-Agent"] = userAgent;
      }
    }

    const response = await fetch(url, {
      method: "POST",
      headers: requestHeaders,
      body: JSON.stringify({
        refreshToken,
        clientId,
      }),
      cache: "no-store",
    });

    if (!response.ok) {
      let detail = "Gagal memperbarui token autentikasi.";
      try {
        const errorJson = await response.json();
        if (errorJson.detail) {
          detail = errorJson.detail;
        }
      } catch {
        // fallback
      }
      const error = new Error(detail) as Error & { status: number };
      error.status = response.status;
      throw error;
    }

    const data = await response.json();
    return {
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      expiresInSeconds: data.expiresInSeconds,
      expiresAtUtc: data.expiresAtUtc,
    };
  })();

  inFlightRefreshes.set(refreshToken, promise);

  try {
    return await promise;
  } finally {
    inFlightRefreshes.delete(refreshToken);
  }
}

