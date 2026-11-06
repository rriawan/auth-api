import "server-only";

import { apiFetch } from "./client";
import type { SelfOrganizationProfileDto } from "./employee-types";

export const selfOrganizationApi = {
  /** Profil organisasi akun yang sedang login (referensi saja, bukan permission). */
  get: (): Promise<SelfOrganizationProfileDto> =>
    apiFetch<SelfOrganizationProfileDto>("/api/v1/auth/me/organization"),
};
