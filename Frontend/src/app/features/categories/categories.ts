import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { CategoriesApi } from './categories-api';
import { CategoryItem } from './categories.models';
import { Auth } from '../../core/auth/auth';
import { CategoryForm } from './category-form/category-form';

@Component({
  selector: 'app-categories',
  imports: [CommonModule, TableModule, ButtonModule, ToastModule, InputTextModule, CategoryForm],
  providers: [MessageService],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class CategoriesComponent implements OnInit {
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly messageService = inject(MessageService);
  private readonly auth = inject(Auth);

  readonly isAdmin = computed(() => this.auth.role() === 'Admin');

  readonly items = signal<CategoryItem[]>([]);
  readonly isLoading = signal(true);
  readonly searchTerm = signal('');

  readonly dialogVisible = signal(false);
  readonly editingItem = signal<CategoryItem | null>(null);

  readonly filteredItems = computed(() => {
    const term = this.searchTerm().trim();
    if (!term) return this.items();
    
    const lowerTerm = term.replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();

    return this.items().filter(item => {
      const name = (item.name || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
      const desc = (item.description || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();

      const nameWords = name.split(' ');
      const descWords = desc.split(' ');

      const matchName = nameWords.some(word => word.startsWith(lowerTerm));
      const matchDesc = descWords.some(word => word.startsWith(lowerTerm));

      return matchName || matchDesc || name.startsWith(lowerTerm) || desc.startsWith(lowerTerm);
    });
  });

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(): void {
    this.isLoading.set(true);
    this.categoriesApi.getAll().subscribe({
      next: (data) => {
        this.items.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories' });
      }
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

  openEditDialog(item: CategoryItem): void {
    this.editingItem.set(item);
    this.dialogVisible.set(true);
  }

  onDialogVisibleChange(visible: boolean): void {
    this.dialogVisible.set(visible);
  }

  onSaved(): void {
    this.loadItems();
  }

  onDelete(item: CategoryItem): void {
    if (!confirm(`Delete category "${item.name}"? This cannot be undone.`)) {
      return;
    }

    this.categoriesApi.delete(item.id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Category deleted' });
        this.loadItems();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete category (Ensure it is not being used)' });
      }
    });
  }
}
