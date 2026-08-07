import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Auth } from '../../core/auth/auth';
import { API_BASE_URL } from '../../core/config/api-config';

export interface ProfileDto {
  username: string;
  email: string;
  phoneNumber?: string;
  role: string;
}

export interface UpdateProfileDto {
  email: string;
  phoneNumber?: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);
  private readonly baseUrl = `${API_BASE_URL}/profile`;

  getProfile(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>(this.baseUrl);
  }

  updateProfile(dto: UpdateProfileDto): Observable<any> {
    return this.http.put(this.baseUrl, dto);
  }

  changePassword(dto: ChangePasswordDto): Observable<{ message: string, token: string }> {
    return this.http.post<{ message: string, token: string }>(`${this.baseUrl}/change-password`, dto).pipe(
      tap(response => {
        if (response.token) {
          // Update the stored token with the new one
          localStorage.setItem('auth_token', response.token);
        }
      })
    );
  }
}
