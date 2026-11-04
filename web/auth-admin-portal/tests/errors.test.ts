import test from "node:test";
import assert from "node:assert";

import { ApiException, getErrorMessage } from "../lib/api/errors.ts";
import type { ProblemDetails } from "../lib/api/types.ts";

test("ApiException: creates instance with problem details and status", () => {
  const problem: ProblemDetails = {
    type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    title: "Bad Request",
    status: 400,
    detail: "Username atau password salah.",
  };

  const ex = new ApiException(400, problem);

  assert.strictEqual(ex.name, "ApiException");
  assert.strictEqual(ex.status, 400);
  assert.strictEqual(ex.message, "Username atau password salah.");
  assert.deepStrictEqual(ex.problem, problem);
});

test("ApiException: message falls back to title if detail is missing", () => {
  const problem: ProblemDetails = {
    title: "Akun Terkunci",
    status: 403,
  };

  const ex = new ApiException(403, problem);
  assert.strictEqual(ex.message, "Akun Terkunci");
});

test("ApiException: message falls back to default server error if detail and title are missing", () => {
  const problem: ProblemDetails = {
    status: 500,
  };

  const ex = new ApiException(500, problem);
  assert.strictEqual(ex.message, "Terjadi kesalahan pada server (status 500).");
});

test("ApiException: helper booleans correctly match HTTP status codes", () => {
  const ex401 = new ApiException(401, { status: 401, title: "Unauthorized" });
  assert.strictEqual(ex401.isUnauthorized, true);
  assert.strictEqual(ex401.isForbidden, false);
  assert.strictEqual(ex401.isNotFound, false);
  assert.strictEqual(ex401.isConflict, false);
  assert.strictEqual(ex401.isRateLimited, false);
  assert.strictEqual(ex401.isServerError, false);

  const ex403 = new ApiException(403, { status: 403, title: "Forbidden" });
  assert.strictEqual(ex403.isUnauthorized, false);
  assert.strictEqual(ex403.isForbidden, true);

  const ex404 = new ApiException(404, { status: 404, title: "Not Found" });
  assert.strictEqual(ex404.isNotFound, true);

  const ex409 = new ApiException(409, { status: 409, title: "Conflict" });
  assert.strictEqual(ex409.isConflict, true);

  const ex429 = new ApiException(429, { status: 429, title: "Too Many Requests" });
  assert.strictEqual(ex429.isRateLimited, true);

  const ex500 = new ApiException(500, { status: 500, title: "Internal Server Error" });
  assert.strictEqual(ex500.isServerError, true);

  const ex503 = new ApiException(503, { status: 503, title: "Service Unavailable" });
  assert.strictEqual(ex503.isServerError, true);
});

test("ApiException: preserves RFC 9457 validation errors dictionary", () => {
  const problem: ProblemDetails = {
    status: 400,
    title: "One or more validation errors occurred.",
    errors: {
      Email: ["Email wajib diisi.", "Format email tidak valid."],
      Password: ["Password minimal 8 karakter."],
    },
  };

  const ex = new ApiException(400, problem);
  assert.ok(ex.problem.errors);
  assert.strictEqual(ex.problem.errors?.["Email"]?.length, 2);
  assert.strictEqual(ex.problem.errors?.["Password"]?.[0], "Password minimal 8 karakter.");
});

test("getErrorMessage: handles ApiException with status 429 gracefully", () => {
  const ex = new ApiException(429, {
    status: 429,
    title: "Too Many Requests",
    detail: "Batas percobaan terlampaui.",
  });

  const message = getErrorMessage(ex);
  assert.strictEqual(
    message,
    "Terlalu banyak permintaan. Silakan tunggu beberapa saat."
  );
});

test("getErrorMessage: handles general ApiException", () => {
  const ex = new ApiException(409, {
    status: 409,
    title: "Conflict",
    detail: "Client ID sudah terdaftar.",
  });

  assert.strictEqual(getErrorMessage(ex), "Client ID sudah terdaftar.");
});

test("getErrorMessage: handles standard Error instance", () => {
  const standardError = new Error("Koneksi jaringan terputus.");
  assert.strictEqual(getErrorMessage(standardError), "Koneksi jaringan terputus.");
});

test("getErrorMessage: handles non-Error values with fallback message", () => {
  assert.strictEqual(
    getErrorMessage("raw string"),
    "Terjadi kesalahan yang tidak diharapkan. Silakan coba kembali."
  );
  assert.strictEqual(
    getErrorMessage(null),
    "Terjadi kesalahan yang tidak diharapkan. Silakan coba kembali."
  );
  assert.strictEqual(
    getErrorMessage(undefined),
    "Terjadi kesalahan yang tidak diharapkan. Silakan coba kembali."
  );
});
