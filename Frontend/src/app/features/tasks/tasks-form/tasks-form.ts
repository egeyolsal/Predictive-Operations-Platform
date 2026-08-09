import { Component, inject, input, output, effect, signal, OnInit, untracked, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { ChangeDetectorRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TasksApi } from '../tasks-api';
import { Auth } from '../../../core/auth/auth';
import { TaskItem, TaskItemStatus, TaskPriority, User, Category, TaskMaterialResponseDto } from '../tasks.models';

@Component({
  selector: 'app-tasks-form',
  imports: [ReactiveFormsModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, ButtonModule, TextareaModule, TabsModule, TableModule, DatePipe],
  templateUrl: './tasks-form.html',
  styleUrl: './tasks-form.scss',
})
export class TasksForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly tasksApi = inject(TasksApi);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(Auth);

  readonly isAdmin = computed(() => this.auth.role() === 'Admin');

  readonly visible = input.required<boolean>();
  readonly editingItem = input<TaskItem | null>(null);

  readonly visibleChange = output<boolean>();
  readonly saved = output<void>();

  readonly isSubmitting = signal(false);

  readonly users = signal<User[]>([]);
  readonly categories = signal<Category[]>([]);

  readonly statusOptions = [
    { label: 'To Do', value: TaskItemStatus.ToDo },
    { label: 'In Progress', value: TaskItemStatus.InProgress },
    { label: 'Done', value: TaskItemStatus.Done },
  ];

  readonly priorityOptions = [
    { label: 'Low', value: TaskPriority.Low },
    { label: 'Medium', value: TaskPriority.Medium },
    { label: 'High', value: TaskPriority.High },
  ];

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    status: [TaskItemStatus.ToDo, [Validators.required]],
    priority: [TaskPriority.Medium, [Validators.required]],
    assignedUserId: [null as number | null, [Validators.required]],
    categoryId: [null as number | null, [Validators.required]],
    expectedDurationHours: [1, [Validators.required, Validators.min(0.1)]],
  });

  // Materials State
  readonly taskMaterials = signal<TaskMaterialResponseDto[]>([]);
  readonly isMaterialsLoading = signal(false);

  readonly consumeForm = this.fb.nonNullable.group({
    barcode: ['', [Validators.required]],
    quantity: [1, [Validators.required, Validators.min(1)]],
  });

  constructor() {
    effect(() => {
      const isVisible = this.visible();
      untracked(() => {
        if (isVisible) {
          if (!this.isAdmin()) {
            this.form.disable();
          } else {
            this.form.enable();
          }
          const item = this.editingItem();
          if (item) {
            this.form.patchValue({
              title: item.title,
              description: item.description || '',
              status: item.status,
              priority: item.priority ?? TaskPriority.Medium,
              assignedUserId: item.assignedUserId,
              categoryId: item.categoryId,
              expectedDurationHours: item.expectedDurationHours,
            });
            this.fetchMaterials(item.id);
          } else {
            this.form.reset({
              title: '',
              description: '',
              status: TaskItemStatus.ToDo,
              priority: TaskPriority.Medium,
              assignedUserId: null,
              categoryId: null,
              expectedDurationHours: 1,
            });
            this.taskMaterials.set([]);
          }
        }
      });
    });
  }

  fetchMaterials(taskId: number): void {
    this.isMaterialsLoading.set(true);
    this.tasksApi.getTaskMaterials(taskId).subscribe({
      next: (data) => {
        const formattedData = data.map(m => ({
          ...m,
          transactionDate: (m.transactionDate && !m.transactionDate.endsWith('Z')) ? m.transactionDate + 'Z' : m.transactionDate
        }));
        this.taskMaterials.set(formattedData);
        this.isMaterialsLoading.set(false);
        this.cdr.detectChanges();
      },
      error: () => {
        this.isMaterialsLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  onConsumeMaterial(): void {
    if (this.consumeForm.invalid) {
      this.consumeForm.markAllAsTouched();
      return;
    }

    const taskId = this.editingItem()?.id;
    if (!taskId) return;

    const val = this.consumeForm.getRawValue();
    this.tasksApi.consumeMaterial({
      taskId,
      barcode: val.barcode,
      quantity: val.quantity
    }).subscribe({
      next: (res) => {
        this.fetchMaterials(taskId);
        this.consumeForm.reset({ barcode: '', quantity: 1 });
        this.messageService.add({ severity: 'success', summary: 'Consumed', detail: res.message });
      },
      error: (err) => {
        console.error(err);
        const errMsg = typeof err.error === 'string' ? err.error : (err.error?.message || err.error?.title || err.message || 'Failed to consume material');
        this.messageService.add({ severity: 'error', summary: 'Hata', detail: errMsg });
      }
    });
  }

  ngOnInit(): void {
    this.tasksApi.getUsers().subscribe(data => this.users.set(data));
    this.tasksApi.getCategories().subscribe(data => this.categories.set(data));
  }

  onHide(): void {
    this.visibleChange.emit(false);
  }

  isCriticalStockTask(): boolean {
    const item = this.editingItem();
    // Uses isSystemGenerated flag instead of brittle string matching
    return !!item && item.isSystemGenerated === true;
  }

  getSupplierEmail(): string {
    const item = this.editingItem();
    if (!item || !item.description) return 'supplier@example.com';
    const emailMatch = item.description.match(/([a-zA-Z0-9._-]+@[a-zA-Z0-9._-]+\.[a-zA-Z0-9_-]+)/);
    return emailMatch ? emailMatch[1] : 'supplier@example.com';
  }

  getSupplierMailtoLink(): string {
    const email = this.getSupplierEmail();
    const item = this.editingItem();
    const subject = encodeURIComponent('Urgent Stock Reorder Request - Critical Stock Alert');
    const body = encodeURIComponent(`Hello,\n\nOur inventory management system has automatically detected a critical stock level for the following item and we would like to place an urgent order.\n\nTask Reference: ${item?.title}\n\nPlease confirm availability and provide the earliest possible delivery date.\n\nBest regards.`);
    return `mailto:${email}?subject=${subject}&body=${body}`;
  }

  private loadUsersAndCategories(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const rawValue = this.form.getRawValue();
    const value = {
      ...rawValue,
      assignedUserId: rawValue.assignedUserId!,
      categoryId: rawValue.categoryId!,
    };

    const editing = this.editingItem();

    const request$: Observable<TaskItem | void> = editing
      ? this.tasksApi.update(editing.id, value)
      : this.tasksApi.create(value);

    request$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.visibleChange.emit(false);
        this.saved.emit();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        console.error(err);
        const errMsg = typeof err.error === 'string' ? err.error : (err.error?.message || err.error?.title || err.message || 'Failed to save task');
        this.messageService.add({ severity: 'error', summary: 'Error', detail: errMsg });
      },
    });
  }
}
