import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then(m => m.Register)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password').then(m => m.ResetPassword)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard)
  },
  {
    path: 'tasks',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/tasks').then(m => m.Tasks)
  },
  {
    path: 'inventory',
    canActivate: [authGuard],
    loadComponent: () => import('./features/inventory/inventory').then(m => m.Inventory)
  },
  {
    path: 'suppliers',
    canActivate: [authGuard],
    loadComponent: () => import('./features/suppliers/suppliers').then(m => m.SuppliersComponent)
  },
  {
    path: 'customers',
    canActivate: [authGuard],
    loadComponent: () => import('./features/customers/customers').then(m => m.CustomersComponent)
  }
];