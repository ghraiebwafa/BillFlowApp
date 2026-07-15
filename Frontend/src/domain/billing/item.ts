export type ItemResponse = {
  id: string;
  name: string;
  description?: string | null;
  unitPrice: number;
  currency: string;
  vatRate: number;
  category?: string | null;
  unit?: string | null;
  isActive: boolean;
  isArchived: boolean;
  createdAt: string;
  updatedAt?: string | null;
};
