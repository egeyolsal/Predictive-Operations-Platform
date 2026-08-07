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

export interface ItemSupplierAssignDto {
  inventoryItemId: number;
  price: number;
  leadTimeDays: number;
}

export interface ItemSupplierResponseDto {
  supplierId: number;
  supplierName: string;
  price: number;
  leadTimeDays: number;
}
