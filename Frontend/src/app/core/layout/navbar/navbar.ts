import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
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

    this.destroyRef.onDestroy(() => sub.unsubscribe());
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

  private loadReadNotifications() {
    const saved = localStorage.getItem('read_notifications');
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
        // Mark ones as read based on localStorage
        const unreadList = data.filter(n => !this.readNotificationIds.has(n.id));
        this.notifications.set(data.map(n => ({
          ...n,
          isRead: this.readNotificationIds.has(n.id)
        })));
        this.unreadCount.set(unreadList.length);
      },
      error: (err) => console.error('Error loading notifications', err)
    });
  }

  onNotificationClick(notification: NotificationDto) {
    if (!this.readNotificationIds.has(notification.id)) {
      this.readNotificationIds.add(notification.id);
      localStorage.setItem('read_notifications', JSON.stringify(Array.from(this.readNotificationIds)));
      
      this.notifications.update(current => 
        current.map(n => n.id === notification.id ? { ...n, isRead: true } : n)
      );
      this.unreadCount.update(c => Math.max(0, c - 1));
    }

    this.router.navigate([notification.link]);
  }
}