export interface InventoryItem {
  id: number;
  name: string;
  category: string;
  currentStock: number;
  criticalThreshold: number;
  isBelowCriticalThreshold: boolean;
}