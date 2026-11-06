"use server";

import { revalidatePath } from "next/cache";
import { z } from "zod";
import type { ActionResult } from "@/components/apps/actions/apps";
import { employeesApi } from "@/lib/api/employees";
import { ApiException, getErrorMessage } from "@/lib/api/errors";
import type {
  EmployeeMutationResultDto,
  EmployeeSummaryDto,
} from "@/lib/api/employee-types";
import { organizationApi } from "@/lib/api/organization";
import { requirePermission } from "@/lib/auth/permissions";
import {
  assignActingSchema,
  assignDefinitiveSchema,
  changeStatusSchema,
  createEmployeeSchema,
  endActingSchema,
  endAssignmentSchema,
  linkEmployeeSchema,
  transferSchema,
  unlinkEmployeeSchema,
  updateEmployeeSchema,
} from "@/lib/validation/employees.schema";

export interface PickerOption {
  value: string;
  label: string;
}

type Mutation = ActionResult<EmployeeMutationResultDto>;

function friendly(error: unknown): string {
  if (error instanceof ApiException) {
    if (error.status === 412) {
      return "Data pegawai telah diubah oleh pihak lain. Muat ulang halaman lalu ulangi perubahan.";
    }
    if (error.status === 428) {
      return "Versi data pegawai tidak terbaca. Muat ulang halaman lalu ulangi perubahan.";
    }
  }
  return getErrorMessage(error);
}

function invalid(error: z.ZodError): Mutation {
  const fieldErrors: Record<string, string> = {};
  for (const issue of error.issues) {
    const field = issue.path[0]?.toString();
    if (field && !fieldErrors[field]) fieldErrors[field] = issue.message;
  }
  return {
    success: false,
    error: "Validasi formulir gagal. Silakan periksa kolom isian.",
    fieldErrors,
  };
}

const blank = (value: string | undefined) => (value && value.trim() ? value.trim() : null);

/** Menjalankan satu mutasi pegawai: hak akses, validasi, panggilan API, revalidasi halaman. */
async function run<S extends z.ZodTypeAny>(
  permissions: string[],
  schema: S,
  input: unknown,
  call: (data: z.infer<S>) => Promise<EmployeeMutationResult>,
  paths: string[]
): Promise<Mutation> {
  try {
    for (const permission of permissions) await requirePermission(permission);
    const parsed = schema.safeParse(input);
    if (!parsed.success) return invalid(parsed.error);
    const data = await call(parsed.data);
    for (const path of paths) revalidatePath(path);
    return { success: true, data };
  } catch (error) {
    return { success: false, error: friendly(error) };
  }
}

type EmployeeMutationResult = EmployeeMutationResultDto;

export async function createEmployeeAction(input: unknown): Promise<Mutation> {
  return run(["employees.write"], createEmployeeSchema, input, (d) => employeesApi.create(d), ["/employees"]);
}

export async function updateEmployeeAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.write"], updateEmployeeSchema, input, (d) => employeesApi.updateIdentity(id, d, version), ["/employees", `/employees/${id}`]);
}

export async function changeStatusAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.lifecycle.write"], changeStatusSchema, input, (d) =>
    employeesApi.changeStatus(id, { ...d, decisionReference: blank(d.decisionReference) }, version), ["/employees", `/employees/${id}`]);
}

export async function assignDefinitiveAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.assignments.write"], assignDefinitiveSchema, input, (d) =>
    employeesApi.assign(id, {
      ...d,
      effectiveToExclusive: blank(d.effectiveToExclusive),
      decisionReference: blank(d.decisionReference),
    }, version), [`/employees/${id}`]);
}

export async function transferAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.assignments.write"], transferSchema, input, (d) =>
    employeesApi.transfer(id, { ...d, decisionReference: blank(d.decisionReference) }, version), [`/employees/${id}`]);
}

export async function endAssignmentAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.assignments.write"], endAssignmentSchema, input, (d) =>
    employeesApi.endAssignment(id, d.assignmentId, {
      action: "END",
      effectiveToExclusive: d.effectiveToExclusive,
      reason: d.reason,
    }, version), [`/employees/${id}`]);
}

export async function assignActingAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.assignments.write"], assignActingSchema, input, (d) =>
    employeesApi.assignActing(id, { ...d, effectiveToExclusive: blank(d.effectiveToExclusive) }, version), [`/employees/${id}`]);
}

export async function endActingAction(id: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.assignments.write"], endActingSchema, input, (d) =>
    employeesApi.endActing(id, d.actingId, {
      action: "END",
      effectiveToExclusive: d.effectiveToExclusive,
      reason: d.reason,
    }, version), [`/employees/${id}`]);
}

/** Menautkan akun ke pegawai. Tidak mengubah role atau akses aplikasi akun. */
export async function linkEmployeeAction(userId: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.link.write", "users.write"], linkEmployeeSchema, input, (d) =>
    employeesApi.link(userId, d, version), ["/employees", `/users/${userId}`]);
}

export async function unlinkEmployeeAction(userId: string, version: string, input: unknown): Promise<Mutation> {
  return run(["employees.link.write", "users.write"], unlinkEmployeeSchema, input, (d) =>
    employeesApi.unlink(userId, d, version), ["/employees", `/users/${userId}`]);
}

// ---- pemilih (picker) -----------------------------------------------------------------------------------

export async function searchEmployeesAction(search: string): Promise<ActionResult<EmployeeSummaryDto[]>> {
  try {
    await requirePermission("employees.read");
    const term = search.trim();
    if (term.length < 2) return { success: true, data: [] };
    const page = await employeesApi.list({ search: term, page: 1, pageSize: 10 });
    return { success: true, data: page.items };
  } catch (error) {
    return { success: false, error: getErrorMessage(error) };
  }
}

export async function listOfficeOptionsAction(): Promise<ActionResult<PickerOption[]>> {
  try {
    await requirePermission("organization.read");
    const page = await organizationApi.offices({ page: 1, pageSize: 100 });
    const options = page.items.map((o) => ({ value: o.id, label: `${o.officeCode} — ${o.name ?? "(tanpa nama pada tanggal ini)"}` }));
    if (page.totalCount > page.items.length) {
      options.push({ value: "", label: `… hanya ${page.items.length} dari ${page.totalCount} kantor yang ditampilkan` });
    }
    return { success: true, data: options };
  } catch (error) {
    return { success: false, error: getErrorMessage(error) };
  }
}

export async function listPositionOptionsAction(officeId: string): Promise<ActionResult<PickerOption[]>> {
  try {
    await requirePermission("organization.read");
    const [units, positions] = await Promise.all([
      organizationApi.units(officeId),
      organizationApi.positions(officeId),
    ]);
    const unitName = new Map(units.map((u) => [u.id, u.name ?? u.unitCode]));
    return {
      success: true,
      data: positions
        .filter((p) => p.isActive)
        .map((p) => ({
          value: p.id,
          label: `${unitName.get(p.orgUnitId) ?? "Unit"} / ${p.positionCode} — ${p.name ?? ""}`.trim(),
        })),
    };
  } catch (error) {
    return { success: false, error: getErrorMessage(error) };
  }
}
