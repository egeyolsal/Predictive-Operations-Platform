export interface Supplier {
  id: number;
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
}

export interface SupplierCreateDto {
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
}
