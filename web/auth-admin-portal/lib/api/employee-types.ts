// Tipe kontrak pegawai & organisasi (enum respons backend berupa string PascalCase).
// File ini hanya berisi tipe sehingga aman diimpor dari client component.

export type EmployeeStatusName = "Active" | "Resigned";
export type RecordStateName = "Draft" | "Confirmed" | "Cancelled";
export type ActingTypeName = "Plh" | "Plt";
export type OrgUnitTypeName =
  | "Directorate"
  | "Division"
  | "Department"
  | "Section"
  | "WorkGroup"
  | "Other";
export type PositionTrackName = "Line" | "Functional";

export interface EmployeeSummaryDto {
  id: string;
  employeeNumber: string;
  employeeFullName: string;
  /** ETag kuat agregat pegawai (sudah berkutip); dikirim apa adanya sebagai If-Match. */
  version: string;
  currentStatus: EmployeeStatusName | null;
  linkedUserId: string | null;
}

export interface EmployeePageDto {
  items: EmployeeSummaryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface StatusRecordDto {
  id: string;
  statusCode: EmployeeStatusName;
  effectiveFrom: string;
  effectiveToExclusive: string | null;
  recordState: RecordStateName;
  revisionNumber: number;
  changeReason: string;
  decisionReference: string | null;
  createdAtUtc: string;
  supersededAtUtc: string | null;
}

export interface PlacementRecordDto {
  id: string;
  assignmentKey: string;
  positionId: string;
  positionCode: string;
  isPrimary: boolean;
  effectiveFrom: string;
  effectiveToExclusive: string | null;
  recordState: RecordStateName;
  revisionNumber: number;
  changeReason: string;
  decisionReference: string | null;
  createdAtUtc: string;
  supersededAtUtc: string | null;
}

export interface ActingRecordDto {
  id: string;
  assignmentKey: string;
  sourceRecordId: string;
  targetPositionId: string;
  targetPositionCode: string;
  assignmentType: ActingTypeName;
  effectiveFrom: string;
  effectiveToExclusive: string | null;
  recordState: RecordStateName;
  revisionNumber: number;
  changeReason: string;
  decisionReference: string;
  createdAtUtc: string;
  supersededAtUtc: string | null;
}

export interface EmployeeDetailDto {
  employee: EmployeeSummaryDto;
  statuses: StatusRecordDto[];
  placements: PlacementRecordDto[];
  actingAssignments: ActingRecordDto[];
}

export interface EmployeeHistoryDto {
  employeeId: string;
  statuses: StatusRecordDto[];
  placements: PlacementRecordDto[];
  actingAssignments: ActingRecordDto[];
}

export interface EmployeeMutationResultDto {
  employee: EmployeeSummaryDto;
  recordId: string | null;
  secondaryRecordId: string | null;
}

export interface UserOrganizationDto {
  userId: string;
  employee: EmployeeDetailDto | null;
}

export interface LifecycleRunResultDto {
  dryRun: boolean;
  asOf: string;
  processed: { userId: string; employeeId: string; employeeNumber: string }[];
  revokedSessions: number;
  remaining: number;
}

export interface ClassificationDto {
  code: string;
  displayName: string;
}

export interface OfficeSummaryDto {
  id: string;
  officeCode: string;
  name: string | null;
  classificationCode: string | null;
  isActive: boolean | null;
  version: string;
}

export interface OfficePageDto {
  items: OfficeSummaryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  asOf: string;
}

export interface OfficeVersionDto {
  id: string;
  classificationCode: string;
  name: string;
  effectiveFrom: string;
  effectiveToExclusive: string | null;
  isActive: boolean;
  revisionNumber: number;
  changeReason: string;
  decisionReference: string | null;
  createdAtUtc: string;
  supersededAtUtc: string | null;
}

export interface OfficeDetailDto {
  id: string;
  officeCode: string;
  version: string;
  versions: OfficeVersionDto[];
}

export interface UnitSummaryDto {
  id: string;
  officeId: string;
  unitCode: string;
  parentOrgUnitId: string | null;
  unitType: OrgUnitTypeName | null;
  name: string | null;
  isActive: boolean | null;
  version: string;
}

export interface PositionSummaryDto {
  id: string;
  orgUnitId: string;
  positionCode: string;
  name: string | null;
  track: PositionTrackName | null;
  isUnitHead: boolean | null;
  maxIncumbents: number | null;
  reportsToPositionId: string | null;
  isActive: boolean | null;
  version: string;
  levelId: string | null;
}

export interface StructurePositionNode {
  id: string;
  positionCode: string;
  name: string;
  track: PositionTrackName;
  isUnitHead: boolean;
  maxIncumbents: number | null;
  reportsToPositionId: string | null;
  definitiveIncumbents: number;
  isVacant: boolean;
}

export interface StructureUnitNode {
  id: string;
  unitCode: string;
  name: string;
  unitType: OrgUnitTypeName;
  positions: StructurePositionNode[];
  children: StructureUnitNode[];
}

export interface OfficeStructureDto {
  officeId: string;
  officeCode: string;
  officeName: string | null;
  asOf: string;
  units: StructureUnitNode[];
}

export interface SelfAssignmentDto {
  id: string;
  type: "DEFINITIVE" | "PLH" | "PLT";
  isPrimary: boolean;
  effectiveFrom: string;
  effectiveToExclusive: string | null;
  sourceAssignmentId: string | null;
  decisionReference: string | null;
  office: { id: string; code: string; name: string; classification: string };
  orgUnit: { id: string; name: string };
  position: {
    id: string;
    name: string;
    track: string;
    levelCode: string | null;
    isUnitHead: boolean;
  };
  formalSupervisor: {
    positionId: string;
    positionName: string;
    isVacant: boolean;
  } | null;
}

export interface SelfOrganizationProfileDto {
  userId: string;
  accountDisplayName: string;
  asOf: string;
  businessTimeZone: string;
  profileVersion: string;
  nextEffectiveChangeAtUtc: string | null;
  employee: {
    id: string;
    employeeNumber: string;
    employeeFullName: string;
    status: string;
  } | null;
  primaryAssignmentId: string | null;
  assignments: SelfAssignmentDto[];
}

export interface OrganizationMutationResultDto {
  id: string;
  /** ETag kuat agregat setelah perubahan. */
  version: string;
  versionRecordId: string;
}

export interface TemplateSummaryDto {
  id: string;
  classificationCode: string;
  version: number;
  state: "Draft" | "Published";
  unitCount: number;
  positionCount: number;
  notes: string | null;
  publishedAtUtc: string | null;
  eTag: string;
}

export interface TemplatePreviewPositionDto {
  positionCode: string;
  name: string;
  isUnitHead: boolean;
  maxIncumbents: number | null;
  levelCode: string | null;
  reportsToCode: string | null;
}

export interface TemplatePreviewUnitDto {
  unitCode: string;
  name: string;
  unitType: string;
  parentUnitCode: string | null;
  positions: TemplatePreviewPositionDto[];
}

export interface TemplateApplyPreviewDto {
  officeId: string;
  officeCode: string;
  officeName: string | null;
  templateId: string;
  templateClassificationCode: string;
  templateVersion: number;
  templateState: string;
  effectiveFrom: string;
  unitCount: number;
  positionCount: number;
  canApply: boolean;
  blockers: string[];
  units: TemplatePreviewUnitDto[];
}

export interface TemplateApplyResultDto {
  applicationId: string;
  officeId: string;
  templateId: string;
  effectiveFrom: string;
  unitsCreated: number;
  positionsCreated: number;
  replayed: boolean;
}

export interface TemplateMutationResultDto {
  id: string;
  version: string;
}
