import { Component, inject, computed, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Auth } from '../../auth/auth';
import { API_BASE_URL } from '../../config/api-config';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {
  private readonly auth = inject(Auth);
  readonly isAdmin = computed(() => this.auth.role() === 'Admin');
  readonly canViewCategories = computed(() => this.auth.role() === 'Admin' || this.auth.role() === 'Analyst');
  readonly profilePictureUrl = computed(() => this.auth.profilePictureUrl());
  readonly username = computed(() => this.auth.username());

  readonly isCollapsed = signal(false);

  toggleSidebar(): void {
    this.isCollapsed.update(v => !v);
  }

  getAvatarUrl(): string {
    const url = this.profilePictureUrl();
    if (!url) return '';
    if (url.startsWith('http')) return url;
    
    const serverUrl = API_BASE_URL.endsWith('/api') 
      ? API_BASE_URL.substring(0, API_BASE_URL.length - 4) 
      : API_BASE_URL;
      
    return `${serverUrl}${url}`;
  }
}