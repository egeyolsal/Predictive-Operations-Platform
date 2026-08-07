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

  ngOnInit() {
    this.updatePageTitle(this.router.url);
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

  loadNotifications() {
    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        this.notifications.set(data);
        this.unreadCount.set(data.length);
      },
      error: (err) => console.error('Error loading notifications', err)
    });
  }

  onNotificationClick(notification: NotificationDto) {
    this.router.navigate([notification.link]);
    // Optionally mark as read if we implement that feature
  }
}