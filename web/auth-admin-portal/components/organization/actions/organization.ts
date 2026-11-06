"use server";

import { revalidatePath } from "next/cache";
import { unstable_rethrow } from "next/navigation";
import { z } from "zod";
import type { ActionResult } from "@/components/apps/actions/apps";
import type {
  OrganizationMutationResultDto,
  TemplateApplyPreviewDto,
  TemplateApplyResultDto,
  TemplateMutationResultDto,
} from "@/lib/api/employee-types";
import { ApiException, getErrorMessage } from "@/lib/api/errors";
import { organizationApi } from "@/lib/api/organization";
import { requirePermission } from "@/lib/auth/permissions";
import {
  applyTemplateSchema,
  createOfficeSchema,
  createPositionSchema,
  createUnitSchema,
  previewTemplateSchema,
  publishTemplateSchema,
  officeVersionSchema,
  positionVersionSchema,
  unitVersionSchema,
} from "@/lib/validation/organization.schema";

type Mutation = ActionResult<OrganizationMutationResultDto>;

function friendly(error: unknown): string {
  if (error instanceof ApiException) {
    if (error.status === 412) {
      return "Data telah diubah oleh pihak lain. Muat ulang halaman lalu ulangi perubahan.";
    }
    if (error.status === 428) {
      return "Versi data tidak terbaca. Muat ulang halaman lalu ulangi perubahan.";
    }
  }
  if (error instanceof ApiException) {
    if (/later version exists/i.test(error.message)) {
      return "Sudah ada versi terjadwal di masa depan untuk data ini. Versi baru hanya dapat ditambahkan setelah versi terjadwal terakhir; periksa riwayat versi dan tanggal efektifnya.";
    }
    if (/No current version covers/i.test(error.message)) {
      return "Belum ada versi yang berlaku pada tanggal efektif tersebut. Pilih tanggal efektif pada atau setelah versi pertama.";
    }
  }
  return getErrorMessage(error);
}

const uuid = z.string().uuid();

function invalid<T = OrganizationMutationResultDto>(error: z.ZodError): ActionResult<T> {
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
const capacity = (value: string | undefined) => (value && value.trim() ? Number(value) : null);

/** Satu perubahan master: hak akses, validasi, panggilan API, revalidasi halaman. */
async function run<S extends z.ZodTypeAny>(
  ids: string[],
  schema: S,
  input: unknown,
  call: (data: z.infer<S>) => Promise<OrganizationMutationResultDto>,
  paths: string[]
): Promise<Mutation> {
  try {
    await requirePermission("organization.write");
    if (!ids.every((id) => uuid.safeParse(id).success)) {
      return { success: false, error: "Identitas data tidak valid. Muat ulang halaman." };
    }
    const parsed = schema.safeParse(input);
    if (!parsed.success) return invalid(parsed.error);
    const data = await call(parsed.data);
    for (const path of paths) revalidatePath(path);
    return { success: true, data };
  } catch (error) {
    unstable_rethrow(error);
    return { success: false, error: friendly(error) };
  }
}

export async function createOfficeAction(input: unknown): Promise<Mutation> {
  return run([], createOfficeSchema, input, (d) =>
    organizationApi.createOffice({ ...d, decisionReference: blank(d.decisionReference) }), ["/organization/offices"]);
}

export async function addOfficeVersionAction(officeId: string, version: string, input: unknown): Promise<Mutation> {
  return run([officeId], officeVersionSchema, input, (d) =>
    organizationApi.addOfficeVersion(officeId, { ...d, decisionReference: blank(d.decisionReference) }, version),
    ["/organization/offices", `/organization/offices/${officeId}`]);
}

export async function createUnitAction(officeId: string, input: unknown): Promise<Mutation> {
  return run([officeId], createUnitSchema, { ...(input as object), officeId }, (d) =>
    organizationApi.createUnit({ ...d, parentOrgUnitId: blank(d.parentOrgUnitId), decisionReference: blank(d.decisionReference) }),
    [`/organization/offices/${officeId}`]);
}

export async function addUnitVersionAction(officeId: string, unitId: string, version: string, input: unknown): Promise<Mutation> {
  return run([officeId, unitId], unitVersionSchema, input, (d) =>
    organizationApi.addUnitVersion(unitId, { ...d, parentOrgUnitId: blank(d.parentOrgUnitId), decisionReference: blank(d.decisionReference) }, version),
    [`/organization/offices/${officeId}`]);
}

export async function createPositionAction(officeId: string, input: unknown): Promise<Mutation> {
  return run([officeId], createPositionSchema, input, (d) =>
    organizationApi.createPosition({
      ...d,
      maxIncumbents: capacity(d.maxIncumbents),
      levelId: blank(d.levelId),
      reportsToPositionId: blank(d.reportsToPositionId),
      decisionReference: blank(d.decisionReference),
    }), [`/organization/offices/${officeId}`]);
}

export async function addPositionVersionAction(officeId: string, positionId: string, version: string, input: unknown): Promise<Mutation> {
  return run([officeId, positionId], positionVersionSchema, input, (d) =>
    organizationApi.addPositionVersion(positionId, {
      ...d,
      maxIncumbents: capacity(d.maxIncumbents),
      levelId: blank(d.levelId),
      reportsToPositionId: blank(d.reportsToPositionId),
      decisionReference: blank(d.decisionReference),
    }, version), [`/organization/offices/${officeId}`]);
}

// ---- template struktur -------------------------------------------------------------------------------------

type Guarded<T> = ActionResult<T>;

async function guarded<T>(ids: string[], work: () => Promise<Guarded<T>>): Promise<Guarded<T>> {
  try {
    await requirePermission("organization.write");
    if (!ids.every((id) => uuid.safeParse(id).success)) {
      return { success: false, error: "Identitas data tidak valid. Muat ulang halaman." };
    }
    return await work();
  } catch (error) {
    unstable_rethrow(error);
    return { success: false, error: friendly(error) };
  }
}

export async function publishTemplateAction(templateId: string, version: string, input: unknown): Promise<Guarded<TemplateMutationResultDto>> {
  return guarded([templateId], async () => {
    const parsed = publishTemplateSchema.safeParse(input);
    if (!parsed.success) return invalid<TemplateMutationResultDto>(parsed.error);
    const data = await organizationApi.publishTemplate(
      templateId, { reason: parsed.data.reason, decisionReference: blank(parsed.data.decisionReference) }, version);
    revalidatePath("/organization/offices", "layout");
    return { success: true, data };
  });
}

export async function previewTemplateAction(officeId: string, input: unknown): Promise<Guarded<TemplateApplyPreviewDto>> {
  return guarded([officeId], async () => {
    const parsed = previewTemplateSchema.safeParse(input);
    if (!parsed.success) return invalid<TemplateApplyPreviewDto>(parsed.error);
    return { success: true, data: await organizationApi.previewTemplate(officeId, parsed.data.templateId, parsed.data.effectiveFrom) };
  });
}

export async function applyTemplateAction(officeId: string, input: unknown): Promise<Guarded<TemplateApplyResultDto>> {
  return guarded([officeId], async () => {
    const parsed = applyTemplateSchema.safeParse(input);
    if (!parsed.success) return invalid<TemplateApplyResultDto>(parsed.error);
    const data = await organizationApi.applyTemplate(officeId, { ...parsed.data, decisionReference: blank(parsed.data.decisionReference) });
    revalidatePath(`/organization/offices/${officeId}`);
    return { success: true, data };
  });
}
