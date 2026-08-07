import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InventoryItem, InventoryCreateRequest, InventoryUpdateRequest } from './inventory.models';
import { ItemSupplierResponseDto } from '../../core/models/supplier.model';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root',
})
export class InventoryApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/inventory`;

  getAll(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(this.apiUrl);
  }

  create(request: InventoryCreateRequest): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(this.apiUrl, request);
  }

  update(id: number, request: InventoryUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getItemSuppliers(id: number): Observable<ItemSupplierResponseDto[]> {
    return this.http.get<ItemSupplierResponseDto[]>(`${this.apiUrl}/${id}/suppliers`);
  }
}