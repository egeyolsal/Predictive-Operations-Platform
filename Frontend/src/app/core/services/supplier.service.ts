import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Supplier, SupplierCreateDto, ItemSupplierAssignDto } from '../models/supplier.model';

export interface SupplierItemResponseDto {
  inventoryItemId: number;
  inventoryItemName: string;
  category: string;
  currentStock: number;
  price: number;
  leadTimeDays: number;
}
import { API_BASE_URL } from '../config/api-config';

@Injectable({
  providedIn: 'root'
})
export class SupplierService {
  private apiUrl = `${API_BASE_URL}/Supplier`;

  constructor(private http: HttpClient) {}

  getSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(this.apiUrl);
  }

  createSupplier(supplier: SupplierCreateDto): Observable<Supplier> {
    return this.http.post<Supplier>(this.apiUrl, supplier);
  }

  updateSupplier(id: number, supplier: SupplierCreateDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, supplier);
  }

  deleteSupplier(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  assignItem(supplierId: number, dto: ItemSupplierAssignDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${supplierId}/assign-item`, dto);
  }

  getSupplierItems(supplierId: number): Observable<SupplierItemResponseDto[]> {
    return this.http.get<SupplierItemResponseDto[]>(`${this.apiUrl}/${supplierId}/items`);
  }
}
