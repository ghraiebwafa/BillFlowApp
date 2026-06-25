import { UserRole, type UserProfile } from "../../domain/auth/types";

export function normalizeRole(role: UserProfile["role"] | string | number): UserRole {
  if (typeof role === "number") return role as UserRole;
  if (role === "SuperAdmin") return UserRole.SuperAdmin;
  if (role === "Admin") return UserRole.Admin;
  return UserRole.Visitor;
}

export function isAdminRole(role: UserRole): boolean {
  return role === UserRole.Admin || role === UserRole.SuperAdmin;
}

export function isVisitorRole(role: UserRole): boolean {
  return role === UserRole.Visitor;
}

export function homePathForRole(role: UserRole): string {
  return isAdminRole(role) ? "/admin/users" : "/dashboard";
}
