import { Component, inject, input, output, effect, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { TasksApi } from '../tasks-api';
import { TaskItem, TaskItemStatus, User, Category } from '../tasks.models';

@Component({
  selector: 'app-tasks-form',
  imports: [ReactiveFormsModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, ButtonModule, TextareaModule],
  templateUrl: './tasks-form.html',
  styleUrl: './tasks-form.scss',
})
export class TasksForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly tasksApi = inject(TasksApi);

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

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    status: [TaskItemStatus.ToDo, [Validators.required]],
    assignedUserId: [null as number | null, [Validators.required]],
    categoryId: [null as number | null, [Validators.required]],
    expectedDurationHours: [1, [Validators.required, Validators.min(0.1)]],
  });

  constructor() {
    effect(() => {
      const item = this.editingItem();
      if (item) {
        this.form.patchValue({
          title: item.title,
          description: item.description || '',
          status: item.status,
          assignedUserId: item.assignedUserId,
          categoryId: item.categoryId,
          expectedDurationHours: item.expectedDurationHours,
        });
      } else {
        this.form.reset({
          title: '',
          description: '',
          status: TaskItemStatus.ToDo,
          assignedUserId: null,
          categoryId: null,
          expectedDurationHours: 1,
        });
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
      error: () => {
        this.isSubmitting.set(false);
      },
    });
  }
}
