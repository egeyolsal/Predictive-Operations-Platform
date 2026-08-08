import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { interval } from 'rxjs';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DatePipe } from '@angular/common';
import { TasksApi } from './tasks-api';
import { TaskItem, TaskItemStatus, TaskPriority } from './tasks.models';
import { TasksForm } from './tasks-form/tasks-form';
import { Auth } from '../../core/auth/auth';

@Component({
  selector: 'app-tasks',
  imports: [CommonModule, TableModule, TagModule, InputTextModule, ButtonModule, ToastModule, TasksForm, DatePipe],
  providers: [MessageService],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss',
})
export class Tasks implements OnInit {
  private readonly tasksApi = inject(TasksApi);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(Auth);
  private readonly destroyRef = inject(DestroyRef);

  readonly isAdmin = computed(() => this.auth.role() === 'Admin');
  readonly isAnalyst = computed(() => this.auth.role() === 'Analyst');

  readonly items = signal<TaskItem[]>([]);
  readonly isLoading = signal(true);
  readonly searchTerm = signal('');

  readonly dialogVisible = signal(false);
  readonly editingItem = signal<TaskItem | null>(null);

  readonly TaskItemStatus = TaskItemStatus; // Expose enum to template
  readonly TaskPriority = TaskPriority; // Expose enum to template

  readonly filteredItems = computed(() => {
    const term = this.searchTerm().trim();
    if (!term) {
      return this.items();
    }

    const lowerTerm = term.replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();

    return this.items().filter((item) => {
      const title = (item.title || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
      return title.startsWith(lowerTerm);
    });
  });

  ngOnInit(): void {
    this.loadItems();
    const pollSub = interval(15000).subscribe(() => this.loadItems(true));
    this.destroyRef.onDestroy(() => pollSub.unsubscribe());
  }

  loadItems(isPolling = false): void {
    if (!isPolling) {
      this.isLoading.set(true);
    }
    this.tasksApi.getAll().subscribe({
      next: (data) => {
        const formattedData = data.map(item => ({
          ...item,
          createdAt: (item.createdAt && !item.createdAt.endsWith('Z')) ? item.createdAt + 'Z' : item.createdAt,
          completedAt: (item.completedAt && !item.completedAt.endsWith('Z')) ? item.completedAt + 'Z' : item.completedAt
        }));
        this.items.set(formattedData);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }

  openAddDialog(): void {
    this.editingItem.set(null);
    this.dialogVisible.set(true);
  }

  openEditDialog(item: TaskItem): void {
    this.editingItem.set(item);
    this.dialogVisible.set(true);
  }

  onDialogVisibleChange(visible: boolean): void {
    this.dialogVisible.set(visible);
  }

  onSaved(): void {
    const wasEditing = this.editingItem() !== null;
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: wasEditing ? 'Task updated successfully.' : 'Task added successfully.',
    });
    this.loadItems();
  }

  onDelete(item: TaskItem): void {
    if (!confirm(`Delete task "${item.title}"? This cannot be undone.`)) {
      return;
    }

    this.tasksApi.delete(item.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Task deleted successfully.',
        });
        this.loadItems();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to delete task.',
        });
      },
    });
  }

  onCompleteTask(item: TaskItem): void {
    if (!confirm(`Mark task "${item.title}" as completed?`)) {
      return;
    }

    this.tasksApi.updateStatus(item.id, TaskItemStatus.Done).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Task marked as completed.',
        });
        this.loadItems();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update task status.',
        });
      },
    });
  }

  getStatusSeverity(status: TaskItemStatus): 'info' | 'warn' | 'success' | 'secondary' {
    switch (status) {
      case TaskItemStatus.ToDo:
        return 'secondary';
      case TaskItemStatus.InProgress:
        return 'info';
      case TaskItemStatus.Done:
        return 'success';
      default:
        return 'secondary';
    }
  }

  getStatusLabel(status: TaskItemStatus): string {
    switch (status) {
      case TaskItemStatus.ToDo:
        return 'To Do';
      case TaskItemStatus.InProgress:
        return 'In Progress';
      case TaskItemStatus.Done:
        return 'Done';
      default:
        return 'Unknown';
    }
  }

  getPrioritySeverity(priority: TaskPriority): 'info' | 'warn' | 'danger' {
    switch (priority) {
      case TaskPriority.Low: return 'info';
      case TaskPriority.Medium: return 'warn';
      case TaskPriority.High: return 'danger';
      default: return 'info';
    }
  }

  getPriorityLabel(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low: return 'Low';
      case TaskPriority.Medium: return 'Medium';
      case TaskPriority.High: return 'High';
      default: return 'Unknown';
    }
  }

  isCriticalStockTask(task: TaskItem): boolean {
    return !!task.title && task.title.includes('Kritik Stok Uyarısı');
  }

  isAnomalousTask(task: TaskItem): boolean {
    return task.isAnomalous === true;
  }

  showAnomalyWarning(task: TaskItem): boolean {
    return (this.isCriticalStockTask(task) || this.isAnomalousTask(task)) && (this.isAdmin() || this.isAnalyst());
  }

  getActualDurationHours(task: TaskItem): string | null {
    if (task.status !== TaskItemStatus.Done || !task.completedAt || !task.createdAt) {
      return null;
    }
    const created = new Date(task.createdAt);
    const completed = new Date(task.completedAt);
    const diffMs = completed.getTime() - created.getTime();
    if (diffMs < 0) return '0.0';
    const diffHours = diffMs / (1000 * 60 * 60);
    return diffHours.toFixed(1);
  }

  extractSupplierEmail(description: string | null | undefined): string | null {
    if (!description) return null;
    const emailMatch = description.match(/([a-zA-Z0-9._-]+@[a-zA-Z0-9._-]+\.[a-zA-Z0-9_-]+)/);
    return emailMatch ? emailMatch[1] : 'supplier@example.com';
  }
}
