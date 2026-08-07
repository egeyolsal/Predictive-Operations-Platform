import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TaskItem, TaskItemStatus, TaskCreateDto, TaskUpdateDto, User, Category, TaskMaterialConsumptionDto, TaskMaterialResponseDto } from './tasks.models';

import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root'
})
export class TasksApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/Task`;

  getAll(): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(this.baseUrl);
  }

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${API_BASE_URL}/User`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${API_BASE_URL}/Category`);
  }

  getById(id: number): Observable<TaskItem> {
    return this.http.get<TaskItem>(`${this.baseUrl}/${id}`);
  }

  create(dto: TaskCreateDto): Observable<TaskItem> {
    return this.http.post<TaskItem>(this.baseUrl, dto);
  }

  update(id: number, dto: TaskUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  updateStatus(id: number, status: TaskItemStatus): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/status`, { status });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  consumeMaterial(dto: TaskMaterialConsumptionDto): Observable<{ message: string; invoiceNumber: string }> {
    return this.http.post<{ message: string; invoiceNumber: string }>(`${this.baseUrl}/consume-material`, dto);
  }

  getTaskMaterials(id: number): Observable<TaskMaterialResponseDto[]> {
    return this.http.get<TaskMaterialResponseDto[]>(`${this.baseUrl}/${id}/materials`);
  }
}
