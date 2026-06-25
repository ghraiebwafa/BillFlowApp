export type ClientResponse = {
  id: string;
  companyName: string;
  contactName: string;
  email: string;
  phoneNumber?: string | null;
  address?: string | null;
  country?: string | null;
  taxNumber?: string | null;
  notes?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export function clientInitial(companyName: string): string {
  const trimmed = companyName.trim();
  return trimmed.length > 0 ? trimmed[0]!.toUpperCase() : "?";
}
