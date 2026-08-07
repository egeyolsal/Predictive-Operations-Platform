import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardDto } from './dashboard.models';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root'
})
export class DashboardApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/Dashboard`;

  getDashboard(): Observable<DashboardDto> {
    return this.http.get<DashboardDto>(this.baseUrl);
  }
}
