import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InventoryItem } from './inventory.models';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root',
})
export class InventoryApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(`${API_BASE_URL}/inventory`);
  }
}