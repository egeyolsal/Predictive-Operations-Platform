import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SupplierService } from '../../core/services/supplier.service';
import { Supplier } from '../../core/models/supplier.model';

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
  suppliers: Supplier[] = [];
  displayDialog: boolean = false;
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
        this.suppliers = data;
      },
      error: (err) => {
        console.error('Error fetching suppliers', err);
      }
    });
  }

  showDialogToAdd(): void {
    this.supplierForm.reset();
    this.displayDialog = true;
  }

  saveSupplier(): void {
    if (this.supplierForm.invalid) return;

    const val = this.supplierForm.value;
    this.supplierService.createSupplier(val).subscribe({
      next: (res) => {
        this.suppliers.push(res);
        this.displayDialog = false;
        this.supplierForm.reset();
      },
      error: (err) => {
        console.error('Error creating supplier', err);
      }
    });
  }
}
