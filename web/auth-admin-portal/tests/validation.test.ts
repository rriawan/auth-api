import test from "node:test";
import assert from "node:assert";

import {
  passwordRule,
  emailOrUsernameRule,
  loginSchema,
  forgotPasswordSchema,
  resetPasswordSchema,
  changePasswordSchema,
} from "../lib/validation/auth.schema.ts";

import {
  clientIdRule,
  createClientAppSchema,
  updateClientAppSchema,
} from "../lib/validation/apps.schema.ts";

import {
  permissionCodeRule,
  createPermissionSchema,
  updatePermissionSchema,
} from "../lib/validation/permissions.schema.ts";

import {
  createRoleSchema,
  updateRoleSchema,
  setRolePermissionsSchema,
  setUserAppRolesSchema,
} from "../lib/validation/roles.schema.ts";

import {
  createUserSchema,
  updateUserSchema,
} from "../lib/validation/users.schema.ts";

test("auth.schema: passwordRule validation", () => {
  // Valid passwords (8 - 256 chars)
  assert.strictEqual(passwordRule.safeParse("Password123!").success, true);
  assert.strictEqual(passwordRule.safeParse("12345678").success, true);
  assert.strictEqual(passwordRule.safeParse("a".repeat(256)).success, true);

  // Invalid: too short (< 8 chars)
  const tooShort = passwordRule.safeParse("short12");
  assert.strictEqual(tooShort.success, false);
  if (!tooShort.success) {
    assert.strictEqual(tooShort.error.issues[0].message, "Password minimal 8 karakter");
  }

  // Invalid: too long (> 256 chars)
  const tooLong = passwordRule.safeParse("a".repeat(257));
  assert.strictEqual(tooLong.success, false);
  if (!tooLong.success) {
    assert.strictEqual(tooLong.error.issues[0].message, "Password maksimal 256 karakter");
  }
});

test("auth.schema: emailOrUsernameRule validation", () => {
  assert.strictEqual(emailOrUsernameRule.safeParse("admin").success, true);
  assert.strictEqual(emailOrUsernameRule.safeParse("admin@example.com").success, true);

  // Empty string
  const empty = emailOrUsernameRule.safeParse("");
  assert.strictEqual(empty.success, false);

  // Too long
  const tooLong = emailOrUsernameRule.safeParse("a".repeat(257));
  assert.strictEqual(tooLong.success, false);
});

test("auth.schema: loginSchema validation", () => {
  const valid = loginSchema.safeParse({
    usernameOrEmail: "superadmin",
    password: "Password123!",
  });
  assert.strictEqual(valid.success, true);

  const missingPassword = loginSchema.safeParse({
    usernameOrEmail: "superadmin",
  });
  assert.strictEqual(missingPassword.success, false);
});

test("auth.schema: forgotPasswordSchema validation", () => {
  const valid = forgotPasswordSchema.safeParse({
    emailOrUsername: "user@example.com",
  });
  assert.strictEqual(valid.success, true);

  const empty = forgotPasswordSchema.safeParse({
    emailOrUsername: "",
  });
  assert.strictEqual(empty.success, false);
});

test("auth.schema: resetPasswordSchema validation", () => {
  // Matching passwords with token
  const valid = resetPasswordSchema.safeParse({
    token: "valid-reset-token-123",
    newPassword: "NewSecretPassword123!",
    confirmPassword: "NewSecretPassword123!",
  });
  assert.strictEqual(valid.success, true);

  // Mismatching passwords
  const mismatch = resetPasswordSchema.safeParse({
    token: "valid-reset-token-123",
    newPassword: "NewSecretPassword123!",
    confirmPassword: "DifferentPassword123!",
  });
  assert.strictEqual(mismatch.success, false);
  if (!mismatch.success) {
    assert.strictEqual(mismatch.error.issues[0].message, "Konfirmasi password tidak cocok");
  }

  // Missing token
  const noToken = resetPasswordSchema.safeParse({
    token: "",
    newPassword: "NewSecretPassword123!",
    confirmPassword: "NewSecretPassword123!",
  });
  assert.strictEqual(noToken.success, false);
});

test("auth.schema: changePasswordSchema validation", () => {
  const valid = changePasswordSchema.safeParse({
    currentPassword: "OldPassword123!",
    newPassword: "BrandNewPassword123!",
    confirmNewPassword: "BrandNewPassword123!",
  });
  assert.strictEqual(valid.success, true);

  // Mismatch
  const mismatch = changePasswordSchema.safeParse({
    currentPassword: "OldPassword123!",
    newPassword: "BrandNewPassword123!",
    confirmNewPassword: "MismatchPassword123!",
  });
  assert.strictEqual(mismatch.success, false);
});

test("apps.schema: clientIdRule validation", () => {
  // Valid clientIds: lowercase, alphanumeric, hyphens
  assert.strictEqual(clientIdRule.safeParse("erp-core").success, true);
  assert.strictEqual(clientIdRule.safeParse("auth-admin").success, true);
  assert.strictEqual(clientIdRule.safeParse("mobile-app-v2").success, true);
  assert.strictEqual(clientIdRule.safeParse("app1").success, true);

  // Invalid: uppercase characters
  assert.strictEqual(clientIdRule.safeParse("ERP-Core").success, false);

  // Invalid: special symbols (underscore, spaces, dots)
  assert.strictEqual(clientIdRule.safeParse("erp_core").success, false);
  assert.strictEqual(clientIdRule.safeParse("erp core").success, false);
  assert.strictEqual(clientIdRule.safeParse("erp.core").success, false);

  // Invalid: too short (< 2)
  assert.strictEqual(clientIdRule.safeParse("a").success, false);

  // Invalid: too long (> 100)
  assert.strictEqual(clientIdRule.safeParse("a".repeat(101)).success, false);
});

test("apps.schema: createClientAppSchema validation", () => {
  const valid = createClientAppSchema.safeParse({
    clientId: "portal-sdm",
    name: "Portal SDM & Payroll",
    description: "Sistem Kepegawaian",
    allowedOrigins: "http://localhost:3000, https://sdm.example.com",
    isActive: true,
  });
  assert.strictEqual(valid.success, true);

  // Defaults isActive to true when omitted
  const omittedActive = createClientAppSchema.safeParse({
    clientId: "portal-sdm",
    name: "Portal SDM & Payroll",
  });
  assert.strictEqual(omittedActive.success, true);
  if (omittedActive.success) {
    assert.strictEqual(omittedActive.data.isActive, true);
  }

  // Rejects invalid clientId
  const invalidClientId = createClientAppSchema.safeParse({
    clientId: "Portal_SDM",
    name: "Portal SDM & Payroll",
  });
  assert.strictEqual(invalidClientId.success, false);
});

test("apps.schema: updateClientAppSchema validation", () => {
  const valid = updateClientAppSchema.safeParse({
    name: "Updated App Name",
    description: "Deskripsi baru",
    allowedOrigins: "http://localhost:3000",
    isActive: true,
  });
  assert.strictEqual(valid.success, true);

  const missingName = updateClientAppSchema.safeParse({
    name: "",
    isActive: false,
  });
  assert.strictEqual(missingName.success, false);
});

test("permissions.schema: permissionCodeRule validation", () => {
  // Valid: modul.aksi format (lowercase, dots, hyphens, alphanumeric)
  assert.strictEqual(permissionCodeRule.safeParse("users.read").success, true);
  assert.strictEqual(permissionCodeRule.safeParse("apps.rotate-key").success, true);
  assert.strictEqual(permissionCodeRule.safeParse("reports.financial.export").success, true);

  // Invalid: no dot (single word)
  assert.strictEqual(permissionCodeRule.safeParse("usersread").success, false);

  // Invalid: uppercase
  assert.strictEqual(permissionCodeRule.safeParse("Users.Read").success, false);

  // Invalid: trailing or leading dots
  assert.strictEqual(permissionCodeRule.safeParse(".users.read").success, false);
  assert.strictEqual(permissionCodeRule.safeParse("users.read.").success, false);
  assert.strictEqual(permissionCodeRule.safeParse("users..read").success, false);
});

test("permissions.schema: createPermissionSchema & updatePermissionSchema validation", () => {
  const validCreate = createPermissionSchema.safeParse({
    code: "reports.view",
    description: "Izin melihat laporan",
  });
  assert.strictEqual(validCreate.success, true);

  const invalidCreate = createPermissionSchema.safeParse({
    code: "invalidcode",
  });
  assert.strictEqual(invalidCreate.success, false);

  const validUpdate = updatePermissionSchema.safeParse({
    code: "reports.export",
    description: "Izin mengekspor laporan",
  });
  assert.strictEqual(validUpdate.success, true);
});

test("roles.schema: createRoleSchema & setRolePermissionsSchema validation", () => {
  const validRole = createRoleSchema.safeParse({
    name: "Finance Manager",
    description: "Mengelola laporan keuangan",
    permissionIds: ["11111111-1111-1111-1111-111111111111"],
  });
  assert.strictEqual(validRole.success, true);

  // Role name too short
  const shortName = createRoleSchema.safeParse({
    name: "A",
  });
  assert.strictEqual(shortName.success, false);

  // setRolePermissionsSchema: validates array of UUIDs
  const validUuids = setRolePermissionsSchema.safeParse({
    permissionIds: [
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
    ],
  });
  assert.strictEqual(validUuids.success, true);

  const invalidUuid = setRolePermissionsSchema.safeParse({
    permissionIds: ["not-a-valid-uuid"],
  });
  assert.strictEqual(invalidUuid.success, false);
});

test("roles.schema: updateRoleSchema & setUserAppRolesSchema validation", () => {
  const validUpdate = updateRoleSchema.safeParse({
    name: "Senior Auditor",
    description: "Auditor senior dengan akses luas",
  });
  assert.strictEqual(validUpdate.success, true);

  const invalidUpdate = updateRoleSchema.safeParse({
    name: "",
  });
  assert.strictEqual(invalidUpdate.success, false);

  const validAppRoles = setUserAppRolesSchema.safeParse({
    roleIds: [
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
    ],
  });
  assert.strictEqual(validAppRoles.success, true);

  const invalidAppRoles = setUserAppRolesSchema.safeParse({
    roleIds: ["invalid-uuid"],
  });
  assert.strictEqual(invalidAppRoles.success, false);
});

test("users.schema: createUserSchema validation", () => {
  const valid = createUserSchema.safeParse({
    username: "john.doe-99",
    email: "john.doe@example.com",
    fullName: "John Doe",
    password: "SecurePassword123!",
  });
  assert.strictEqual(valid.success, true);
  if (valid.success) {
    assert.strictEqual(valid.data.isActive, true);
    assert.strictEqual(valid.data.isAccountVerified, true);
  }

  // Invalid username characters (spaces, special signs)
  const invalidUsername = createUserSchema.safeParse({
    username: "john doe",
    email: "john@example.com",
    fullName: "John Doe",
    password: "SecurePassword123!",
  });
  assert.strictEqual(invalidUsername.success, false);

  // Invalid email format
  const invalidEmail = createUserSchema.safeParse({
    username: "johndoe",
    email: "invalid-email-string",
    fullName: "John Doe",
    password: "SecurePassword123!",
  });
  assert.strictEqual(invalidEmail.success, false);

  // Password too short (< 8)
  const weakPassword = createUserSchema.safeParse({
    username: "johndoe",
    email: "john@example.com",
    fullName: "John Doe",
    password: "123",
  });
  assert.strictEqual(weakPassword.success, false);
});

test("users.schema: updateUserSchema validation", () => {
  const valid = updateUserSchema.safeParse({
    email: "updated@example.com",
    fullName: "Updated Full Name",
    isActive: false,
    isAccountVerified: true,
  });
  assert.strictEqual(valid.success, true);

  const invalidEmail = updateUserSchema.safeParse({
    email: "not-an-email",
    fullName: "Updated Name",
    isActive: true,
    isAccountVerified: true,
  });
  assert.strictEqual(invalidEmail.success, false);
});
