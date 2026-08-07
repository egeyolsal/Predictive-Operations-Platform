export interface InventoryItem {
  id: number;
  name: string;
  categoryId: number;
  categoryName: string;
  barcode?: string;
  currentStock: number;
  criticalThreshold: number;
  isBelowCriticalThreshold: boolean;
}

export interface InventoryCreateRequest {
  name: string;
  categoryId: number;
  barcode?: string;
  currentStock: number;
  criticalThreshold: number;
}

export interface InventoryUpdateRequest {
  name: string;
  categoryId: number;
  barcode?: string;
  currentStock: number;
  criticalThreshold: number;
}