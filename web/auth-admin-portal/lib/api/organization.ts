import "server-only";

import { apiFetch } from "./client";
import type {
  ClassificationDto,
  OrganizationMutationResultDto,
  OfficeDetailDto,
  OfficePageDto,
  OfficeStructureDto,
  PositionSummaryDto,
  UnitSummaryDto,
} from "./employee-types";

const base = "/api/v1/admin/organization";
const enc = encodeURIComponent;

export interface OfficesQuery {
  search?: string | null;
  classification?: string | null;
  asOf?: string | null;
  page?: number;
  pageSize?: number;
}

function write(method: "POST", path: string, body: unknown, version?: string) {
  return apiFetch<OrganizationMutationResultDto>(`${base}${path}`, {
    method,
    body,
    headers: version ? { "If-Match": version } : undefined,
  });
}

export const organizationApi = {
  createOffice: (body: unknown) => write("POST", "/offices", body),
  addOfficeVersion: (id: string, body: unknown, version: string) =>
    write("POST", `/offices/${enc(id)}/versions`, body, version),
  createUnit: (body: unknown) => write("POST", "/units", body),
  addUnitVersion: (id: string, body: unknown, version: string) =>
    write("POST", `/units/${enc(id)}/versions`, body, version),
  createPosition: (body: unknown) => write("POST", "/positions", body),
  addPositionVersion: (id: string, body: unknown, version: string) =>
    write("POST", `/positions/${enc(id)}/versions`, body, version),

  classifications: (): Promise<ClassificationDto[]> =>
    apiFetch<ClassificationDto[]>(`${base}/classifications`),

  offices: (query: OfficesQuery = {}): Promise<OfficePageDto> => {
    const params = new URLSearchParams();
    if (query.search) params.set("search", query.search);
    if (query.classification) params.set("classification", query.classification);
    if (query.asOf) params.set("asOf", query.asOf);
    if (query.page) params.set("page", query.page.toString());
    if (query.pageSize) params.set("pageSize", query.pageSize.toString());
    const qs = params.toString();
    return apiFetch<OfficePageDto>(`${base}/offices${qs ? `?${qs}` : ""}`);
  },

  office: (id: string, includeHistory = false): Promise<OfficeDetailDto> =>
    apiFetch<OfficeDetailDto>(
      `${base}/offices/${enc(id)}?includeHistory=${includeHistory}`
    ),

  structure: (id: string, asOf?: string | null): Promise<OfficeStructureDto> =>
    apiFetch<OfficeStructureDto>(
      `${base}/offices/${enc(id)}/structure${asOf ? `?asOf=${enc(asOf)}` : ""}`
    ),

  units: (officeId: string, asOf?: string | null): Promise<UnitSummaryDto[]> =>
    apiFetch<UnitSummaryDto[]>(
      `${base}/units?officeId=${enc(officeId)}${asOf ? `&asOf=${enc(asOf)}` : ""}`
    ),

  positions: (officeId: string, asOf?: string | null): Promise<PositionSummaryDto[]> =>
    apiFetch<PositionSummaryDto[]>(
      `${base}/positions?officeId=${enc(officeId)}${asOf ? `&asOf=${enc(asOf)}` : ""}`
    ),
};
