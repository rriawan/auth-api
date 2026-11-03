export * from "./validation";
import { emailOrUsernameRule, passwordRule } from "./validation/auth.schema";

// Backward-compatibility aliases
export const passwordField = passwordRule;
export const emailOrUsernameField = emailOrUsernameRule;
