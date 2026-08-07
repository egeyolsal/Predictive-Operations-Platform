import { Component, inject, input, output, effect, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';
import { CategoriesApi } from '../categories-api';
import { CategoryItem } from '../categories.models';
import { Observable } from 'rxjs';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-category-form',
  imports: [ReactiveFormsModule, DialogModule, InputTextModule, TextareaModule, ButtonModule],
  templateUrl: './category-form.html',
  styleUrl: './category-form.scss',
})
export class CategoryForm {
  private readonly fb = inject(FormBuilder);
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly messageService = inject(MessageService);

  readonly visible = input.required<boolean>();
  readonly editingItem = input<CategoryItem | null>(null);

  readonly visibleChange = output<boolean>();
  readonly saved = output<void>();

  readonly isSubmitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
  });

  constructor() {
    effect(() => {
      const item = this.editingItem();
      if (item) {
        this.form.patchValue({
          name: item.name,
          description: item.description || '',
        });
      } else {
        this.form.reset({ name: '', description: '' });
      }
    });
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
    const value = this.form.getRawValue();
    const editing = this.editingItem();

    const request$: Observable<CategoryItem | void> = editing
      ? this.categoriesApi.update(editing.id, value)
      : this.categoriesApi.create(value);

    request$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: editing ? 'Category updated' : 'Category created' });
        this.visibleChange.emit(false);
        this.saved.emit();
      },
      error: () => {
        this.isSubmitting.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Operation failed' });
      },
    });
  }
}
