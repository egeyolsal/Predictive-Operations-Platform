import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Supplier, SupplierCreateDto } from '../models/supplier.model';
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

  // Future endpoints (Update, Delete) can go here
}
