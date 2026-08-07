import { Component, OnInit, signal, inject, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessageService, ConfirmationService } from 'primeng/api';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SupplierService } from '../../core/services/supplier.service';
import { Supplier } from '../../core/models/supplier.model';
import { Auth } from '../../core/auth/auth';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TableModule,
    DialogModule,
    ButtonModule,
    InputTextModule
  ],
  templateUrl: './suppliers.html',
  styleUrls: ['./suppliers.scss']
})
export class SuppliersComponent implements OnInit {
  private readonly auth = inject(Auth);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly isAdmin = computed(() => this.auth.role() === 'Admin');

  suppliers = signal<Supplier[]>([]);
  displayDialog = signal<boolean>(false);
  editingSupplier = signal<Supplier | null>(null);
  supplierForm!: FormGroup;

  constructor(
    private supplierService: SupplierService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadSuppliers();
  }

  initForm(): void {
    this.supplierForm = this.fb.group({
      name: ['', Validators.required],
      contactName: [''],
      phone: [''],
      email: ['', Validators.email]
    });
  }

  loadSuppliers(): void {
    this.supplierService.getSuppliers().subscribe({
      next: (data) => {
        this.suppliers.set(data);
      },
      error: (err) => {
        console.error('Error fetching suppliers', err);
      }
    });
  }

  showDialogToAdd(): void {
    this.editingSupplier.set(null);
    this.supplierForm.reset();
    this.displayDialog.set(true);
  }

  editSupplier(supplier: Supplier): void {
    this.editingSupplier.set(supplier);
    this.supplierForm.patchValue(supplier);
    this.displayDialog.set(true);
  }

  deleteSupplier(supplier: Supplier): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete ${supplier.name}?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.supplierService.deleteSupplier(supplier.id).subscribe({
          next: () => {
            this.suppliers.update(s => s.filter(x => x.id !== supplier.id));
            this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier deleted successfully' });
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Error deleting supplier', err);
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete supplier' });
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  saveSupplier(): void {
    if (this.supplierForm.invalid) return;

    const val = this.supplierForm.value;
    const editing = this.editingSupplier();

    if (editing) {
      this.supplierService.updateSupplier(editing.id, val).subscribe({
        next: () => {
          this.suppliers.update(s => s.map(x => x.id === editing.id ? { ...x, ...val } : x));
          this.displayDialog.set(false);
          this.supplierForm.reset();
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier updated successfully' });
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error updating supplier', err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update supplier' });
          this.cdr.detectChanges();
        }
      });
    } else {
      this.supplierService.createSupplier(val).subscribe({
        next: (res) => {
          this.suppliers.update(s => [...s, res]);
          this.displayDialog.set(false);
          this.supplierForm.reset();
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Supplier created successfully' });
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error creating supplier', err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to create supplier' });
          this.cdr.detectChanges();
        }
      });
    }
  }
}
