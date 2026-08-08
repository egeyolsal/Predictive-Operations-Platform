import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';

export interface StockPredictionDto {
  inventoryItemId: number;
  inventoryItemName: string;
  currentStock: number;
  criticalThreshold: number;
  dailyConsumptionRate: number;
  daysUntilZero: number | null;
  daysUntilCritical: number | null;
  isCritical: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private http = inject(HttpClient);
  private baseUrl = `${API_BASE_URL}/Analytics`;

  getStockPredictions(): Observable<StockPredictionDto[]> {
    return this.http.get<StockPredictionDto[]>(`${this.baseUrl}/stock-predictions`);
  }
}
