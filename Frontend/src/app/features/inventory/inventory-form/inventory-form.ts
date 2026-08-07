import { Component, inject, input, output, effect, signal, ChangeDetectorRef } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { InventoryApi } from '../inventory-api';
import { InventoryItem } from '../inventory.models';
import { SupplierService } from '../../../core/services/supplier.service';
import { Supplier, ItemSupplierResponseDto, ItemSupplierAssignDto } from '../../../core/models/supplier.model';

@Component({
  selector: 'app-inventory-form',
  imports: [ReactiveFormsModule, DialogModule, InputTextModule, InputNumberModule, SelectModule, ButtonModule, TabsModule, TableModule, CurrencyPipe],
  templateUrl: './inventory-form.html',
  styleUrl: './inventory-form.scss',
})
export class InventoryForm {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryApi = inject(InventoryApi);
  private readonly supplierService = inject(SupplierService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly messageService = inject(MessageService);

  readonly visible = input.required<boolean>();
  readonly editingItem = input<InventoryItem | null>(null);

  readonly visibleChange = output<boolean>();
  readonly saved = output<void>();

  readonly isSubmitting = signal(false);

  // Suppliers state
  readonly itemSuppliers = signal<ItemSupplierResponseDto[]>([]);
  readonly allSuppliers = signal<Supplier[]>([]);
  readonly isSuppliersLoading = signal(false);

  readonly categoryOptions = ['Bakım', 'Üretim', 'Lojistik', 'Kalite Kontrol', 'Diğer'];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    category: ['', [Validators.required, Validators.maxLength(50)]],
    currentStock: [0, [Validators.required, Validators.min(0)]],
    criticalThreshold: [0, [Validators.required, Validators.min(0)]],
  });

  readonly assignForm = this.fb.nonNullable.group({
    supplierId: [0, [Validators.required, Validators.min(1)]],
    price: [0, [Validators.required, Validators.min(0)]],
    leadTimeDays: [0, [Validators.required, Validators.min(0)]],
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
        this.fetchSuppliers(item.id);
        this.fetchAllSuppliers();
      } else {
        this.form.reset({ name: '', category: '', currentStock: 0, criticalThreshold: 0 });
        this.itemSuppliers.set([]);
      }
    });
  }

  fetchSuppliers(itemId: number): void {
    this.isSuppliersLoading.set(true);
    this.inventoryApi.getItemSuppliers(itemId).subscribe({
      next: (data) => {
        this.itemSuppliers.set(data);
        this.isSuppliersLoading.set(false);
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSuppliersLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  fetchAllSuppliers(): void {
    this.supplierService.getSuppliers().subscribe(data => this.allSuppliers.set(data));
  }

  onAssignSupplier(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }
    
    const itemId = this.editingItem()?.id;
    if (!itemId) return;

    const val = this.assignForm.getRawValue();
    this.supplierService.assignItem(val.supplierId, {
      inventoryItemId: itemId,
      price: val.price,
      leadTimeDays: val.leadTimeDays
    }).subscribe({
      next: () => {
        this.fetchSuppliers(itemId);
        this.assignForm.reset({ supplierId: 0, price: 0, leadTimeDays: 0 });
        this.messageService.add({ severity: 'success', summary: 'Assigned', detail: 'Supplier assigned successfully' });
      },
      error: (err) => {
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to assign supplier' });
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