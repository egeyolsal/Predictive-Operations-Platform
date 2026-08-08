import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ChartModule } from 'primeng/chart';
import { SkeletonModule } from 'primeng/skeleton';
import { DashboardApi } from './dashboard-api';
import { DashboardDto } from './dashboard.models';
import { Auth } from '../../core/auth/auth';
import { NotificationService, NotificationDto } from '../../core/services/notification.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ChartModule, SkeletonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly dashboardApi = inject(DashboardApi);
  private readonly notificationService = inject(NotificationService);
  readonly auth = inject(Auth);

  readonly data = signal<DashboardDto | null>(null);
  readonly isLoading = signal(true);
  readonly notifications = signal<NotificationDto[]>([]);

  // Chart configuration
  readonly lineChartData = computed(() => {
    const d = this.data();
    if (!d || !d.taskActivity || d.taskActivity.length === 0) return null;

    return {
      labels: d.taskActivity.map(x => x.date),
      datasets: [
        {
          label: 'Tasks Created/Completed',
          data: d.taskActivity.map(x => x.count),
          fill: true,
          borderColor: '#10b981',
          tension: 0.4,
          backgroundColor: 'rgba(16, 185, 129, 0.1)'
        }
      ]
    };
  });

  readonly barChartData = computed(() => {
    const d = this.data();
    if (!d || !d.topInventoryUsed || d.topInventoryUsed.length === 0) return null;

    return {
      labels: d.topInventoryUsed.map(x => x.name),
      datasets: [
        {
          label: 'Quantity Used',
          data: d.topInventoryUsed.map(x => x.quantity),
          backgroundColor: ['#3b82f6', '#6366f1', '#eab308', '#f97316', '#14b8a6'],
          borderRadius: 4
        }
      ]
    };
  });

  readonly chartOptions = {
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      x: {
        grid: {
          display: false
        }
      },
      y: {
        grid: {
          color: 'rgba(0,0,0,0.05)'
        }
      }
    },
    maintainAspectRatio: false
  };

  readonly barChartOptions = {
    indexAxis: 'y',
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      x: {
        grid: {
          color: 'rgba(0,0,0,0.05)'
        }
      },
      y: {
        grid: {
          display: false
        }
      }
    },
    maintainAspectRatio: false
  };

  ngOnInit(): void {
    this.dashboardApi.getDashboard().subscribe({
      next: (res) => {
        this.data.set(res);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
      }
    });

    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        let formattedData = data.map(n => ({
          ...n,
          date: (n.date && !n.date.endsWith('Z')) ? n.date + 'Z' : n.date
        }));
        formattedData.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
        this.notifications.set(formattedData);
      },
      error: (err) => console.error(err)
    });
  }
}
