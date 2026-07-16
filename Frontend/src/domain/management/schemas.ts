import { z } from "zod";
import { userProfileSchema } from "../auth/schemas";
import type { UserRole } from "../auth/types";

export const userManagementResponseSchema = userProfileSchema.extend({
  updatedAt: z.string().nullable().optional(),
});

export type UserManagementResponse = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  role: UserRole;
  isEmailConfirmed: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
  lastLoginAt?: string | null;
};

export const userManagementListSchema = z.array(userManagementResponseSchema);
