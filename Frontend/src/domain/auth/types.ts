export const UserRole = {
  SuperAdmin: 1,
  Admin: 2,
  Visitor: 3,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export type UserProfile = {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  role: UserRole;
  isEmailConfirmed: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
};

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
};

export type AuthResponse = AuthTokens & {
  user: UserProfile;
  message?: string | null;
};

export type MessageResponse = {
  message: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
};
