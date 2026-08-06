export interface InventoryItem {
  id: number;
  name: string;
  category: string;
  currentStock: number;
  criticalThreshold: number;
  isBelowCriticalThreshold: boolean;
}

export interface InventoryCreateRequest {
  name: string;
  category: string;
  currentStock: number;
  criticalThreshold: number;
}

export interface InventoryUpdateRequest {
  name: string;
  category: string;
  currentStock: number;
  criticalThreshold: number;
}