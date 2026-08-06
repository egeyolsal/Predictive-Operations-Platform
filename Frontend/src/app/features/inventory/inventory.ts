import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { InventoryApi } from './inventory-api';
import { InventoryItem } from './inventory.models';

@Component({
  selector: 'app-inventory',
  imports: [TableModule, TagModule, InputTextModule],
  templateUrl: './inventory.html',
  styleUrl: './inventory.scss',
})
export class Inventory implements OnInit {
  private readonly inventoryApi = inject(InventoryApi);

  readonly items = signal<InventoryItem[]>([]);
  readonly isLoading = signal(true);
  readonly searchTerm = signal('');

  readonly filteredItems = computed(() => {
    const term = this.searchTerm().toLocaleLowerCase('tr-TR').trim();
    if (!term) {
      return this.items();
    }
    return this.items().filter(
      (item) =>
        (item.name?.toLocaleLowerCase('tr-TR') || '').startsWith(term)
    );
  });

  ngOnInit(): void {
    this.inventoryApi.getAll().subscribe({
      next: (data) => {
        this.items.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }
}