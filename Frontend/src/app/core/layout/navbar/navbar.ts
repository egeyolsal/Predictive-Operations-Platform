import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, interval } from 'rxjs';
import { ToolbarModule } from 'primeng/toolbar';
import { AvatarModule } from 'primeng/avatar';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { BadgeModule } from 'primeng/badge';
import { PopoverModule } from 'primeng/popover';
import { CommonModule } from '@angular/common';
import { Auth } from '../../auth/auth';
import { API_BASE_URL } from '../../config/api-config';
import { NotificationService, NotificationDto } from '../../services/notification.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, ToolbarModule, AvatarModule, MenuModule, InputTextModule, BadgeModule, PopoverModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar implements OnInit {
  protected readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly notificationService = inject(NotificationService);

  menuItems: MenuItem[] | undefined;
  pageTitle = signal('');
  isDashboard = signal(true);
  
  notifications = signal<NotificationDto[]>([]);
  unreadCount = signal(0);
  private readNotificationIds = new Set<string>();

  ngOnInit() {
    this.updatePageTitle(this.router.url);
    this.loadReadNotifications();
    this.loadNotifications();

    const sub = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.updatePageTitle(event.urlAfterRedirects);
    });

    const pollSub = interval(15000).subscribe(() => this.loadNotifications());

    this.destroyRef.onDestroy(() => {
      sub.unsubscribe();
      pollSub.unsubscribe();
    });
    this.menuItems = [
      {
        label: 'Profile',
        icon: 'pi pi-user',
        command: () => this.router.navigate(['/profile'])
      },
      {
        label: 'Settings',
        icon: 'pi pi-cog',
        command: () => this.router.navigate(['/settings'])
      },
      {
        separator: true
      },
      {
        label: 'Logout',
        icon: 'pi pi-sign-out',
        command: () => this.logout()
      }
    ];
  }

  private updatePageTitle(url: string) {
    if (url.includes('/dashboard')) {
      this.isDashboard.set(true);
      this.pageTitle.set('');
    } else {
      this.isDashboard.set(false);
      if (url.includes('/tasks')) this.pageTitle.set('Tasks Management');
      else if (url.includes('/inventory')) this.pageTitle.set('Inventory');
      else if (url.includes('/invoices')) this.pageTitle.set('Invoices');
      else if (url.includes('/customers')) this.pageTitle.set('Customers');
      else if (url.includes('/suppliers')) this.pageTitle.set('Suppliers');
      else this.pageTitle.set('Workspace');
    }
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }

  getAvatarUrl(): string {
    const url = this.auth.profilePictureUrl();
    if (!url) return '';
    if (url.startsWith('http')) return url;
    
    const serverUrl = API_BASE_URL.endsWith('/api') 
      ? API_BASE_URL.substring(0, API_BASE_URL.length - 4) 
      : API_BASE_URL;
      
    return `${serverUrl}${url}`;
  }

  private getStorageKey(): string {
    const username = this.auth.username() || 'anonymous';
    return `read_notifications_${username}`;
  }

  private loadReadNotifications() {
    const saved = localStorage.getItem(this.getStorageKey());
    if (saved) {
      try {
        const ids = JSON.parse(saved);
        if (Array.isArray(ids)) {
          this.readNotificationIds = new Set(ids);
        }
      } catch (e) {
        console.error('Error parsing read notifications', e);
      }
    }
  }

  loadNotifications() {
    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        // Backend now sends proper UTC ISO strings (with Z suffix), no need to patch
        const formattedData = data.map(n => ({
          ...n,
          isRead: this.readNotificationIds.has(n.id)
        }));
        formattedData.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
        this.notifications.set(formattedData);
        // Unread = any notification whose ID is not in the persisted read set
        const unreadCount = formattedData.filter(n => !n.isRead).length;
        this.unreadCount.set(unreadCount);
      },
      error: (err) => console.error('Error loading notifications', err)
    });
  }

  onNotificationClick(notification: NotificationDto) {
    if (!this.readNotificationIds.has(notification.id)) {
      this.readNotificationIds.add(notification.id);
      localStorage.setItem(this.getStorageKey(), JSON.stringify(Array.from(this.readNotificationIds)));
      
      this.notifications.update(current => 
        current.map(n => n.id === notification.id ? { ...n, isRead: true } : n)
      );
      this.unreadCount.update(c => Math.max(0, c - 1));
    }

    this.router.navigate([notification.link]);
  }

  getRelativeTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHour = Math.floor(diffMin / 60);
    const diffDay = Math.floor(diffHour / 24);

    if (diffSec < 60) return 'just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    if (diffHour < 24) return `${diffHour}h ago`;
    if (diffDay === 1) return 'yesterday';
    return `${diffDay}d ago`;
  }
}