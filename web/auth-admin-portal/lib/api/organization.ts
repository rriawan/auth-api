import "server-only";

import { apiFetch } from "./client";
import type {
  ClassificationDto,
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

export const organizationApi = {
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
