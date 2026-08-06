import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.Login)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard)
  },
  {
    path: 'tasks',
    loadComponent: () => import('./features/tasks/tasks').then(m => m.Tasks)
  },
  {
    path: 'inventory',
    loadComponent: () => import('./features/inventory/inventory').then(m => m.Inventory)
  }
];