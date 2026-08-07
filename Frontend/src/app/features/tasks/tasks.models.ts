export enum TaskItemStatus {
  ToDo = 'ToDo',
  InProgress = 'InProgress',
  Done = 'Done'
}

export enum TaskPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High'
}

export interface TaskItem {
  id: number;
  title: string;
  description?: string;
  status: TaskItemStatus;
  priority: TaskPriority;
  assignedUserId: number;
  assignedUserName?: string;
  categoryId: number;
  categoryName?: string;
  createdAt: string;
  completedAt?: string;
  expectedDurationHours: number;
  isAnomalous: boolean;
}

export interface TaskCreateDto {
  title: string;
  description?: string;
  status: TaskItemStatus;
  priority: TaskPriority;
  assignedUserId: number;
  categoryId: number;
  expectedDurationHours: number;
}

export interface TaskUpdateDto {
  title: string;
  description?: string;
  status: TaskItemStatus;
  priority: TaskPriority;
  assignedUserId: number;
  categoryId: number;
  expectedDurationHours: number;
}

export interface User {
  id: number;
  username: string;
}

export interface Category {
  id: number;
  name: string;
}

export interface TaskMaterialConsumptionDto {
  taskId: number;
  barcode: string;
  quantity: number;
}

export interface TaskMaterialResponseDto {
  id: number;
  inventoryItemId: number;
  inventoryItemName: string;
  quantityUsed: number;
  transactionDate: string;
}
