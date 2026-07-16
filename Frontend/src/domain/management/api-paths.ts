const management = "/api/v1.0/management";

export const managementApi = {
  admins: `${management}/admins`,
  admin: (id: string) => `${management}/admins/${id}`,
  visitors: `${management}/visitors`,
  visitor: (id: string) => `${management}/visitors/${id}`,
} as const;
