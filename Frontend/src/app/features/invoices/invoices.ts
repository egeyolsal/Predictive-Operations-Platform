import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService } from 'primeng/api';

import { InvoiceService } from './invoice.service';
import { InvoiceResponseDto, InvoiceType } from './invoice.models';
import { InventoryApi } from '../inventory/inventory-api';
import { InventoryItem } from '../inventory/inventory.models';
import { CustomerService } from '../../core/services/customer.service';
import { Customer } from '../../core/models/customer.model';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, TableModule, ButtonModule, 
    TabsModule, InputTextModule, SelectModule, InputNumberModule, DatePickerModule,
    DatePipe, CurrencyPipe
  ],
  templateUrl: './invoices.html',
  styleUrls: ['./invoices.scss']
})
export class InvoicesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly invoiceService = inject(InvoiceService);
  private readonly inventoryApi = inject(InventoryApi);
  private readonly customerService = inject(CustomerService);
  private readonly messageService = inject(MessageService);

  readonly invoices = signal<InvoiceResponseDto[]>([]);
  readonly inventoryItems = signal<InventoryItem[]>([]);
  readonly customers = signal<Customer[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);

  readonly invoiceTypes = [
    { label: 'Inbound (Purchase)', value: InvoiceType.Inbound },
    { label: 'Outbound (Sale)', value: InvoiceType.Outbound },
    { label: 'Internal Consumption', value: InvoiceType.InternalConsumption }
  ];

  readonly form = this.fb.nonNullable.group({
    invoiceNumber: ['', Validators.required],
    invoiceDate: [new Date(), Validators.required],
    type: [InvoiceType.Outbound, Validators.required],
    customerId: [null as number | null],
    lineItems: this.fb.array([])
  });

  get lineItems(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  ngOnInit(): void {
    this.loadData();
    this.addLineItem(); // Start with one empty line item
  }

  loadData(): void {
    this.isLoading.set(true);
    this.invoiceService.getAll().subscribe({
      next: (data) => {
        this.invoices.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.inventoryApi.getAll().subscribe(data => this.inventoryItems.set(data));
    this.customerService.getCustomers().subscribe(data => this.customers.set(data));
  }

  createLineItem(): FormGroup {
    return this.fb.group({
      inventoryItemId: [null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
    });
  }

  addLineItem(): void {
    this.lineItems.push(this.createLineItem());
  }

  removeLineItem(index: number): void {
    if (this.lineItems.length > 1) {
      this.lineItems.removeAt(index);
    }
  }

  onTypeChange(): void {
    const type = this.form.get('type')?.value;
    const customerCtrl = this.form.get('customerId');
    
    if (type === InvoiceType.InternalConsumption) {
      customerCtrl?.clearValidators();
      customerCtrl?.setValue(null);
    } else {
      customerCtrl?.setValidators([Validators.required]);
    }
    customerCtrl?.updateValueAndValidity();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const rawVal = this.form.getRawValue();

    const payload = {
      invoiceNumber: rawVal.invoiceNumber,
      invoiceDate: (rawVal.invoiceDate as Date).toISOString(),
      type: rawVal.type,
      customerId: rawVal.customerId,
      lineItems: rawVal.lineItems.map((li: any) => ({
        inventoryItemId: li.inventoryItemId,
        quantity: li.quantity,
        unitPrice: li.unitPrice
      }))
    };

    this.invoiceService.create(payload).subscribe({
      next: (newInvoice) => {
        this.isSubmitting.set(false);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Invoice created successfully' });
        this.invoices.update(invs => [...invs, newInvoice]);
        this.form.reset({
          invoiceNumber: '',
          invoiceDate: new Date(),
          type: InvoiceType.Outbound,
          customerId: null
        });
        this.lineItems.clear();
        this.addLineItem();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        console.error(err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error || 'Failed to create invoice' });
      }
    });
  }
}
