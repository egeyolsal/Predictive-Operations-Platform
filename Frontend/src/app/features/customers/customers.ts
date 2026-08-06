import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CustomerService } from '../../core/services/customer.service';
import { Customer } from '../../core/models/customer.model';

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
  customers: Customer[] = [];
  displayDialog: boolean = false;
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
        this.customers = data;
      },
      error: (err) => {
        console.error('Error fetching customers', err);
      }
    });
  }

  showDialogToAdd(): void {
    this.customerForm.reset();
    this.displayDialog = true;
  }

  saveCustomer(): void {
    if (this.customerForm.invalid) return;

    const val = this.customerForm.value;
    this.customerService.createCustomer(val).subscribe({
      next: (res) => {
        this.customers.push(res);
        this.displayDialog = false;
        this.customerForm.reset();
      },
      error: (err) => {
        console.error('Error creating customer', err);
      }
    });
  }
}
