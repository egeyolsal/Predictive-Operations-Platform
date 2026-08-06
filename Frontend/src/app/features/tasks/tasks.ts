import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DatePipe } from '@angular/common';
import { TasksApi } from './tasks-api';
import { TaskItem, TaskItemStatus } from './tasks.models';
import { TasksForm } from './tasks-form/tasks-form';
import { Auth } from '../../core/auth/auth';

@Component({
  selector: 'app-tasks',
  imports: [TableModule, TagModule, InputTextModule, ButtonModule, ToastModule, TasksForm, DatePipe],
  providers: [MessageService],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss',
})
export class Tasks implements OnInit {
  private readonly tasksApi = inject(TasksApi);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(Auth);

  readonly isAdmin = computed(() => this.auth.role() === 'Admin');

  readonly items = signal<TaskItem[]>([]);
  readonly isLoading = signal(true);
  readonly searchTerm = signal('');

  readonly dialogVisible = signal(false);
  readonly editingItem = signal<TaskItem | null>(null);

  readonly TaskItemStatus = TaskItemStatus; // Expose enum to template

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
  }

  loadItems(): void {
    this.isLoading.set(true);
    this.tasksApi.getAll().subscribe({
      next: (data) => {
        this.items.set(data);
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
}
