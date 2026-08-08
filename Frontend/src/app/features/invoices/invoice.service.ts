import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InvoiceResponseDto, InvoiceCreateDto } from './invoice.models';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/Invoice`;

  getAll(): Observable<InvoiceResponseDto[]> {
    return this.http.get<InvoiceResponseDto[]>(this.baseUrl);
  }

  getById(id: number): Observable<InvoiceResponseDto> {
    return this.http.get<InvoiceResponseDto>(`${this.baseUrl}/${id}`);
  }

  create(dto: InvoiceCreateDto): Observable<InvoiceResponseDto> {
    return this.http.post<InvoiceResponseDto>(this.baseUrl, dto);
  }

  cancelInvoice(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/cancel`, {});
  }

  downloadPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/pdf`, { responseType: 'blob' });
  }
}
