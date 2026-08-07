import { Component, OnInit, signal, inject, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessageService, ConfirmationService } from 'primeng/api';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CustomerService } from '../../core/services/customer.service';
import { Customer } from '../../core/models/customer.model';
import { Auth } from '../../core/auth/auth';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TableModule,
    DialogModule,
    ButtonModule,
    InputTextModule
  ],
  templateUrl: './customers.html',
  styleUrls: ['./customers.scss']
})
export class CustomersComponent implements OnInit {
  private readonly auth = inject(Auth);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly isAdmin = computed(() => this.auth.role() === 'Admin');

  customers = signal<Customer[]>([]);
  displayDialog = signal<boolean>(false);
  editingCustomer = signal<Customer | null>(null);
  customerForm!: FormGroup;

  constructor(
    private customerService: CustomerService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadCustomers();
  }

  initForm(): void {
    this.customerForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', Validators.email],
      phone: [''],
      address: ['']
    });
  }

  loadCustomers(): void {
    this.customerService.getCustomers().subscribe({
      next: (data) => {
        this.customers.set(data);
      },
      error: (err) => {
        console.error('Error fetching customers', err);
      }
    });
  }

  showDialogToAdd(): void {
    this.editingCustomer.set(null);
    this.customerForm.reset();
    this.displayDialog.set(true);
  }

  editCustomer(customer: Customer): void {
    this.editingCustomer.set(customer);
    this.customerForm.patchValue(customer);
    this.displayDialog.set(true);
  }

  deleteCustomer(customer: Customer): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete ${customer.name}?`,
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.customerService.deleteCustomer(customer.id).subscribe({
          next: () => {
            this.customers.update(c => c.filter(x => x.id !== customer.id));
            this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer deleted successfully' });
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error('Error deleting customer', err);
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete customer' });
            this.cdr.detectChanges();
          }
        });
      }
    });
  }

  saveCustomer(): void {
    if (this.customerForm.invalid) return;

    const val = this.customerForm.value;
    const editing = this.editingCustomer();

    if (editing) {
      this.customerService.updateCustomer(editing.id, val).subscribe({
        next: () => {
          this.customers.update(c => c.map(x => x.id === editing.id ? { ...x, ...val } : x));
          this.displayDialog.set(false);
          this.customerForm.reset();
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer updated successfully' });
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error updating customer', err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update customer' });
          this.cdr.detectChanges();
        }
      });
    } else {
      this.customerService.createCustomer(val).subscribe({
        next: (res) => {
          this.customers.update(c => [...c, res]);
          this.displayDialog.set(false);
          this.customerForm.reset();
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Customer created successfully' });
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error creating customer', err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to create customer' });
          this.cdr.detectChanges();
        }
      });
    }
  }
}
