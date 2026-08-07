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
import { MessageService, ConfirmationService } from 'primeng/api';

import { InvoiceService } from './invoice.service';
import { InvoiceResponseDto, InvoiceType } from './invoice.models';
import { InventoryApi } from '../inventory/inventory-api';
import { InventoryItem } from '../inventory/inventory.models';
import { CustomerService } from '../../core/services/customer.service';
import { Customer } from '../../core/models/customer.model';
import { SupplierService, SupplierItemResponseDto } from '../../core/services/supplier.service';
import { Supplier } from '../../core/models/supplier.model';
import { Auth } from '../../core/auth/auth';

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
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  readonly auth = inject(Auth);

  readonly invoices = signal<InvoiceResponseDto[]>([]);
  readonly inventoryItems = signal<InventoryItem[]>([]);
  readonly customers = signal<Customer[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly supplierItems = signal<SupplierItemResponseDto[]>([]);
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
    supplierId: [null as number | null],
    lineItems: this.fb.array([])
  });

  get lineItems(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  ngOnInit(): void {
    this.loadData();
    this.addLineItem(); // Start with one empty line item

    // Listen to Type changes
    this.form.get('type')?.valueChanges.subscribe(() => this.onTypeChange());
    // Listen to Supplier changes
    this.form.get('supplierId')?.valueChanges.subscribe(supplierId => {
      if (supplierId) {
        this.supplierService.getSupplierItems(supplierId).subscribe(data => {
          this.supplierItems.set(data);
          // Auto-fill price for existing line items if they match
          this.lineItems.controls.forEach(ctrl => {
            const itemId = ctrl.get('inventoryItemId')?.value;
            if (itemId) this.updateLineItemPrice(ctrl as FormGroup, itemId);
          });
        });
      } else {
        this.supplierItems.set([]);
      }
    });
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
    this.supplierService.getSuppliers().subscribe(data => this.suppliers.set(data));
  }

  createLineItem(): FormGroup {
    const group = this.fb.group({
      inventoryItemId: [null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [{value: 0, disabled: this.form.get('type')?.value === InvoiceType.Inbound}, [Validators.required, Validators.min(0.01)]]
    });

    group.get('inventoryItemId')?.valueChanges.subscribe(itemId => {
      if (this.form.get('type')?.value === InvoiceType.Inbound) {
        this.updateLineItemPrice(group, itemId);
      }
    });

    return group;
  }

  updateLineItemPrice(group: FormGroup, itemId: number | null): void {
    if (!itemId) return;
    const sItems = this.supplierItems();
    const found = sItems.find(i => i.inventoryItemId === itemId);
    if (found) {
      group.get('unitPrice')?.setValue(found.price);
    }
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
    const supplierCtrl = this.form.get('supplierId');
    
    if (type === InvoiceType.InternalConsumption) {
      customerCtrl?.clearValidators();
      customerCtrl?.setValue(null);
      supplierCtrl?.clearValidators();
      supplierCtrl?.setValue(null);
    } else if (type === InvoiceType.Inbound) {
      customerCtrl?.clearValidators();
      customerCtrl?.setValue(null);
      supplierCtrl?.setValidators([Validators.required]);
    } else {
      // Outbound
      supplierCtrl?.clearValidators();
      supplierCtrl?.setValue(null);
      customerCtrl?.setValidators([Validators.required]);
    }
    
    customerCtrl?.updateValueAndValidity();
    supplierCtrl?.updateValueAndValidity();

    // Toggle unitPrice disable state
    this.lineItems.controls.forEach(ctrl => {
      const unitPriceCtrl = ctrl.get('unitPrice');
      if (type === InvoiceType.Inbound) {
        unitPriceCtrl?.disable();
      } else {
        unitPriceCtrl?.enable();
      }
    });
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
      invoiceDate: new Date((rawVal.invoiceDate as Date).getTime() - ((rawVal.invoiceDate as Date).getTimezoneOffset() * 60000)).toISOString(),
      type: rawVal.type,
      customerId: rawVal.customerId,
      supplierId: rawVal.supplierId,
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
          customerId: null,
          supplierId: null
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

  confirmCancel(invoice: InvoiceResponseDto): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to cancel invoice #${invoice.invoiceNumber}? This will revert all stock movements.`,
      header: 'Cancel Invoice',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.invoiceService.cancelInvoice(invoice.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Cancelled', detail: 'Invoice has been cancelled successfully.' });
            this.loadData();
          },
          error: (err) => {
            console.error(err);
            this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to cancel invoice.' });
          }
        });
      }
    });
  }
}
