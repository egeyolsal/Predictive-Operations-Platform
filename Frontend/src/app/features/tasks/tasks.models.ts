export enum TaskItemStatus {
  ToDo = 0,
  InProgress = 1,
  Done = 2
}

export interface TaskItem {
  id: number;
  title: string;
  description?: string;
  status: TaskItemStatus;
  assignedUserId: number;
  categoryId: number;
  createdAt: string;
  completedAt?: string;
  expectedDurationHours: number;
  isAnomalous: boolean;
}

export interface TaskCreateDto {
  title: string;
  description?: string;
  status: TaskItemStatus;
  assignedUserId: number;
  categoryId: number;
  expectedDurationHours: number;
}

export interface TaskUpdateDto {
  title: string;
  description?: string;
  status: TaskItemStatus;
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
