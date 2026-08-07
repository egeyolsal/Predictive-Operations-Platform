import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { InventoryApi } from './inventory-api';
import { InventoryItem } from './inventory.models';
import { InventoryForm } from './inventory-form/inventory-form';
import { Auth } from '../../core/auth/auth';

@Component({
  selector: 'app-inventory',
  imports: [TableModule, TagModule, InputTextModule, ButtonModule, ToastModule, InventoryForm],
  providers: [MessageService],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
})
export class Inventory implements OnInit {
  private readonly inventoryApi = inject(InventoryApi);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(Auth);

  readonly isAdmin = computed(() => this.auth.role() === 'Admin');

  readonly items = signal<InventoryItem[]>([]);
  readonly isLoading = signal(true);
  readonly searchTerm = signal('');

  readonly dialogVisible = signal(false);
  readonly editingItem = signal<InventoryItem | null>(null);

  readonly filteredItems = computed(() => {
    const term = this.searchTerm().trim();
    if (!term) {
      return this.items();
    }
    
    const lowerTerm = term.replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
    
    return this.items().filter((item) => {
      const name = (item.name || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
      const cat = (item.categoryName || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
      return name.startsWith(lowerTerm) || cat.startsWith(lowerTerm) || (item.barcode && item.barcode.toLowerCase().includes(lowerTerm));
    });
  });

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(): void {
    this.isLoading.set(true);
    this.inventoryApi.getAll().subscribe({
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

  openEditDialog(item: InventoryItem): void {
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
      detail: wasEditing ? 'Item updated successfully.' : 'Item added successfully.',
    });
    this.loadItems();
  }

  onDelete(item: InventoryItem): void {
    if (!confirm(`Delete "${item.name}"? This cannot be undone.`)) {
      return;
    }

    this.inventoryApi.delete(item.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Item deleted successfully.',
        });
        this.loadItems();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to delete item.',
        });
      },
    });
  }
}