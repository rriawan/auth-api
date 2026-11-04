import type { ProblemDetails } from "./types";

export class ApiException extends Error {
  public readonly status: number;
  public readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    const message =
      problem.detail ||
      problem.title ||
      `Terjadi kesalahan pada server (status ${status}).`;
    super(message);
    this.name = "ApiException";
    this.status = status;
    this.problem = problem;
  }

  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }

  get isRateLimited(): boolean {
    return this.status === 429;
  }

  get isServerError(): boolean {
    return this.status >= 500;
  }
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiException) {
    if (error.isRateLimited) {
      return "Terlalu banyak permintaan. Silakan tunggu beberapa saat.";
    }
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Terjadi kesalahan yang tidak diharapkan. Silakan coba kembali.";
}

