import { z } from "zod";
import { UserRole } from "./types";

export const userProfileSchema = z.object({
  id: z.string().uuid(),
  fullName: z.string(),
  email: z.string().email(),
  phoneNumber: z.string().nullable().optional(),
  role: z.union([
    z.literal(UserRole.SuperAdmin),
    z.literal(UserRole.Admin),
    z.literal(UserRole.Visitor),
    z.number().int().min(1).max(3),
  ]),
  isEmailConfirmed: z.boolean(),
  isActive: z.boolean(),
  createdAt: z.string(),
  lastLoginAt: z.string().nullable().optional(),
});

export const authResponseSchema = z.object({
  accessToken: z.string().min(1),
  refreshToken: z.string().min(1),
  accessTokenExpiresAt: z.string(),
  user: userProfileSchema,
  message: z.string().nullable().optional(),
});

export const messageResponseSchema = z.object({
  message: z.string(),
});
