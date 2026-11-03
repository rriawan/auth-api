import "server-only";

import { headers } from "next/headers";
import { ApiException } from "./errors";
import { ProblemDetails } from "./types";

const DEFAULT_BASE_URL = "http://localhost:5100";
const DEFAULT_TIMEOUT_MS = 15000;

export interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  token?: string | null;
  timeoutMs?: number;
}

export function getApiBaseUrl(): string {
  const url = process.env.AUTH_API_BASE_URL || DEFAULT_BASE_URL;
  return url.replace(/\/+$/, "");
}

async function resolveForwardedHeaders(): Promise<Record<string, string>> {
  const result: Record<string, string> = {};
  try {
    const reqHeaders = await headers();
    const forwardedFor =
      reqHeaders.get("x-forwarded-for") || reqHeaders.get("x-real-ip");
    if (forwardedFor) {
      result["X-Forwarded-For"] = forwardedFor;
    }
    const userAgent = reqHeaders.get("user-agent");
    if (userAgent) {
      result["User-Agent"] = userAgent;
    }
  } catch {
    // Dipanggil di luar siklus HTTP request Next.js (misal prerender/test)
  }
  return result;
}

export async function apiFetch<T>(
  endpoint: string,
  options: RequestOptions = {}
): Promise<T> {
  const baseUrl = getApiBaseUrl();
  const path = endpoint.startsWith("/") ? endpoint : `/${endpoint}`;
  const url = `${baseUrl}${path}`;

  const forwardedHeaders = await resolveForwardedHeaders();

  const requestHeaders = new Headers();
  requestHeaders.set("Content-Type", "application/json");

  // Tambahkan header proxy forwarded
  for (const [key, value] of Object.entries(forwardedHeaders)) {
    requestHeaders.set(key, value);
  }

  // Tambahkan Bearer token jika disediakan
  if (options.token) {
    requestHeaders.set("Authorization", `Bearer ${options.token}`);
  }

  // Gabungkan custom headers jika ada
  if (options.headers) {
    const custom = new Headers(options.headers);
    custom.forEach((val, key) => requestHeaders.set(key, val));
  }

  const timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  let bodyContent: string | undefined = undefined;
  if (options.body !== undefined && options.body !== null) {
    bodyContent =
      typeof options.body === "string"
        ? options.body
        : JSON.stringify(options.body);
  }

  try {
    const response = await fetch(url, {
      ...options,
      body: bodyContent,
      headers: requestHeaders,
      signal: options.signal || controller.signal,
      cache: "no-store",
    });

    // 204 No Content
    if (response.status === 204) {
      return {} as T;
    }

    const contentType = response.headers.get("content-type") || "";
    const isJson = contentType.includes("application/json");

    if (!response.ok) {
      let problem: ProblemDetails;
      if (isJson) {
        try {
          problem = (await response.json()) as ProblemDetails;
        } catch {
          problem = {
            title: response.statusText,
            status: response.status,
            detail: "Gagal memproses respons error dari server.",
          };
        }
      } else {
        const text = await response.text();
        problem = {
          title: response.statusText,
          status: response.status,
          detail: text || response.statusText,
        };
      }

      throw new ApiException(response.status, problem);
    }

    if (isJson) {
      return (await response.json()) as T;
    }

    return (await response.text()) as unknown as T;
  } catch (error) {
    if (error instanceof ApiException) {
      throw error;
    }

    if (error instanceof Error && error.name === "AbortError") {
      throw new ApiException(408, {
        title: "Request Timeout",
        status: 408,
        detail: `Koneksi ke backend auth-api melampaui batas waktu (${timeoutMs / 1000} detik).`,
      });
    }

    const problem: ProblemDetails = {
      title: "Service Unavailable",
      status: 503,
      detail:
        error instanceof Error
          ? error.message
          : "Tidak dapat terhubung ke server auth-api.",
    };
    throw new ApiException(503, problem);
  } finally {
    clearTimeout(timeoutId);
  }
}

