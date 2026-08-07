import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, ForgotPasswordRequest, ResetPasswordRequest } from './auth.models';
import { API_BASE_URL } from '../config/api-config';

const TOKEN_KEY = 'auth_token';
const USERNAME_KEY = 'auth_username';
const ROLE_KEY = 'auth_role';
const PIC_KEY = 'auth_pic';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly http = inject(HttpClient);

  readonly isAuthenticated = signal<boolean>(!!localStorage.getItem(TOKEN_KEY));
  readonly username = signal<string | null>(localStorage.getItem(USERNAME_KEY));
  readonly role = signal<string | null>(localStorage.getItem(ROLE_KEY));
  readonly profilePictureUrl = signal<string | null>(localStorage.getItem(PIC_KEY));

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE_URL}/auth/login`, credentials)
      .pipe(tap(response => this.setSession(response)));
  }

  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE_URL}/auth/register`, credentials)
      .pipe(tap(response => this.setSession(response)));
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${API_BASE_URL}/auth/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${API_BASE_URL}/auth/reset-password`, request);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USERNAME_KEY);
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(PIC_KEY);
    this.isAuthenticated.set(false);
    this.username.set(null);
    this.role.set(null);
    this.profilePictureUrl.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USERNAME_KEY, response.username);
    localStorage.setItem(ROLE_KEY, response.role);
    if (response.profilePictureUrl) {
      localStorage.setItem(PIC_KEY, response.profilePictureUrl);
      this.profilePictureUrl.set(response.profilePictureUrl);
    } else {
      localStorage.removeItem(PIC_KEY);
      this.profilePictureUrl.set(null);
    }
    this.isAuthenticated.set(true);
    this.username.set(response.username);
    this.role.set(response.role);
  }
  
  updateProfilePicture(url: string | null): void {
    if (url) {
      localStorage.setItem(PIC_KEY, url);
      this.profilePictureUrl.set(url);
    } else {
      localStorage.removeItem(PIC_KEY);
      this.profilePictureUrl.set(null);
    }
  }
}