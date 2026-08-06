import { Component, inject, input, output, effect, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { InventoryApi } from '../inventory-api';
import { InventoryItem } from '../inventory.models';

@Component({
  selector: 'app-inventory-form',
  imports: [ReactiveFormsModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, ButtonModule],
  templateUrl: './inventory-form.html',
  styleUrl: './inventory-form.scss',
})
export class InventoryForm {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryApi = inject(InventoryApi);

  readonly visible = input.required<boolean>();
  readonly editingItem = input<InventoryItem | null>(null);

  readonly visibleChange = output<boolean>();
  readonly saved = output<void>();

  readonly isSubmitting = signal(false);

  readonly categoryOptions = ['Bakım', 'Üretim', 'Lojistik', 'Kalite Kontrol', 'Diğer'];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    category: ['', [Validators.required, Validators.maxLength(50)]],
    currentStock: [0, [Validators.required, Validators.min(0)]],
    criticalThreshold: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    effect(() => {
      const item = this.editingItem();
      if (item) {
        this.form.patchValue({
          name: item.name,
          category: item.category,
          currentStock: item.currentStock,
          criticalThreshold: item.criticalThreshold,
        });
      } else {
        this.form.reset({ name: '', category: '', currentStock: 0, criticalThreshold: 0 });
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

    const request$: Observable<InventoryItem | void> = editing
      ? this.inventoryApi.update(editing.id, value)
      : this.inventoryApi.create(value);

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