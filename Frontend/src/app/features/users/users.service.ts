import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserAdminListDto, UpdateUserRoleDto } from './users.models';
import { API_BASE_URL } from '../../core/config/api-config';

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/User`;

  getAdminUserList(): Observable<UserAdminListDto[]> {
    return this.http.get<UserAdminListDto[]>(`${this.baseUrl}/admin-list`);
  }

  updateUserRole(id: number, dto: UpdateUserRoleDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${id}/role`, dto);
  }
}
