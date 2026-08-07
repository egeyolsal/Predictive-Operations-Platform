import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';
import { adminGuard } from './core/auth/admin-guard';
import { LayoutComponent } from './core/layout/layout';

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
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
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
      },
      {
        path: 'suppliers',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/suppliers/suppliers').then(m => m.SuppliersComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./features/customers/customers').then(m => m.CustomersComponent)
      },
      { path: 'profile', loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent) },
      { path: 'settings', loadComponent: () => import('./features/settings/settings').then(m => m.SettingsComponent) },
      {
        path: 'invoices',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/invoices/invoices').then(m => m.InvoicesComponent)
      }
    ]
  }
];