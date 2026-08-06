import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InventoryItem, InventoryCreateRequest, InventoryUpdateRequest } from './inventory.models';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root',
})
export class InventoryApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(`${API_BASE_URL}/inventory`);
  }

  create(request: InventoryCreateRequest): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(`${API_BASE_URL}/inventory`, request);
  }

  update(id: number, request: InventoryUpdateRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE_URL}/inventory/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/inventory/${id}`);
  }
}