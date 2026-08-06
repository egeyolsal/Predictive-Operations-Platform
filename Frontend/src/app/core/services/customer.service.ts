import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer, CustomerCreateDto } from '../models/customer.model';
import { API_BASE_URL } from '../config/api-config';

@Injectable({
  providedIn: 'root'
})
export class CustomerService {
  private apiUrl = `${API_BASE_URL}/Customer`;

  constructor(private http: HttpClient) {}

  getCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(this.apiUrl);
  }

  getCustomerById(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.apiUrl}/${id}`);
  }

  createCustomer(customer: CustomerCreateDto): Observable<Customer> {
    return this.http.post<Customer>(this.apiUrl, customer);
  }

  updateCustomer(id: number, customer: CustomerCreateDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, customer);
  }

  deleteCustomer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
