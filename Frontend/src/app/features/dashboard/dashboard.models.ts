export interface TaskActivityDto {
  date: string;
  count: number;
}

export interface TopInventoryUsedDto {
  name: string;
  quantity: number;
}

export interface StaffPerformanceDto {
  staffName: string;
  completedTasks: number;
}

export interface DashboardDto {
  totalActiveInventory?: number;
  lowStockCount?: number;
  pendingTasks: number;
  inProgressTasks: number;
  completedTasks: number;
  pendingTasksTrend: number;
  inProgressTasksTrend: number;
  completedTasksTrend: number;
  taskActivity: TaskActivityDto[];
  topInventoryUsed: TopInventoryUsedDto[];
  staffPerformance: StaffPerformanceDto[];
}
