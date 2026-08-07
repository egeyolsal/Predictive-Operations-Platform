import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CategoryItem, CategoryCreateRequest, CategoryUpdateRequest } from './categories.models';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root'
})
export class CategoriesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/category`;

  getAll(): Observable<CategoryItem[]> {
    return this.http.get<CategoryItem[]>(this.baseUrl);
  }

  create(dto: CategoryCreateRequest): Observable<CategoryItem> {
    return this.http.post<CategoryItem>(this.baseUrl, dto);
  }

  update(id: number, dto: CategoryUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
