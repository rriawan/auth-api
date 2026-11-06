import "server-only";

import { apiFetch } from "./client";
import type {
  EmployeeDetailDto,
  EmployeeHistoryDto,
  EmployeeMutationResultDto,
  EmployeePageDto,
  LifecycleRunResultDto,
  UserOrganizationDto,
} from "./employee-types";

export interface EmployeesQuery {
  search?: string | null;
  page?: number;
  pageSize?: number;
}

const base = "/api/v1/admin";
const enc = encodeURIComponent;

/** Semua mutasi pada pegawai wajib membawa ETag agregat pegawai lewat If-Match. */
function mutation(
  method: "POST" | "PUT" | "DELETE",
  path: string,
  body: unknown,
  version?: string
) {
  return apiFetch<EmployeeMutationResultDto>(`${base}${path}`, {
    method,
    body,
    headers: version ? { "If-Match": version } : undefined,
  });
}

export const employeesApi = {
  list: (query: EmployeesQuery = {}): Promise<EmployeePageDto> => {
    const params = new URLSearchParams();
    if (query.search) params.set("search", query.search);
    if (query.page) params.set("page", query.page.toString());
    if (query.pageSize) params.set("pageSize", query.pageSize.toString());
    const qs = params.toString();
    return apiFetch<EmployeePageDto>(`${base}/employees${qs ? `?${qs}` : ""}`);
  },

  get: (id: string): Promise<EmployeeDetailDto> =>
    apiFetch<EmployeeDetailDto>(`${base}/employees/${enc(id)}`),

  history: (id: string): Promise<EmployeeHistoryDto> =>
    apiFetch<EmployeeHistoryDto>(`${base}/employees/${enc(id)}/history`),

  getByUser: (userId: string): Promise<UserOrganizationDto> =>
    apiFetch<UserOrganizationDto>(`${base}/users/${enc(userId)}/organization`),

  create: (body: unknown) => mutation("POST", "/employees", body),

  updateIdentity: (id: string, body: unknown, version: string) =>
    mutation("PUT", `/employees/${enc(id)}`, body, version),

  changeStatus: (id: string, body: unknown, version: string) =>
    mutation("POST", `/employees/${enc(id)}/status-changes`, body, version),

  assign: (id: string, body: unknown, version: string) =>
    mutation("POST", `/employees/${enc(id)}/assignments`, body, version),

  transfer: (id: string, body: unknown, version: string) =>
    mutation("POST", `/employees/${enc(id)}/transfers`, body, version),

  endAssignment: (id: string, assignmentId: string, body: unknown, version: string) =>
    mutation(
      "POST",
      `/employees/${enc(id)}/assignments/${enc(assignmentId)}/revisions`,
      body,
      version
    ),

  assignActing: (id: string, body: unknown, version: string) =>
    mutation("POST", `/employees/${enc(id)}/acting-assignments`, body, version),

  endActing: (id: string, actingId: string, body: unknown, version: string) =>
    mutation(
      "POST",
      `/employees/${enc(id)}/acting-assignments/${enc(actingId)}/revisions`,
      body,
      version
    ),

  link: (userId: string, body: unknown, version: string) =>
    mutation("PUT", `/users/${enc(userId)}/employee-link`, body, version),

  unlink: (userId: string, body: unknown, version: string) =>
    mutation("DELETE", `/users/${enc(userId)}/employee-link`, body, version),

  processResignations: (dryRun: boolean): Promise<LifecycleRunResultDto> =>
    apiFetch<LifecycleRunResultDto>(
      `${base}/employees/lifecycle/process-resignations?dryRun=${dryRun}`,
      { method: "POST" }
    ),
};
