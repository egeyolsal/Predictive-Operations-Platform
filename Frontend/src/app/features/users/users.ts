import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { AvatarModule } from 'primeng/avatar';
import { MessageService } from 'primeng/api';
import { Auth } from '../../core/auth/auth';
import { API_BASE_URL } from '../../core/config/api-config';
import { DialogModule } from 'primeng/dialog';

import { UsersService } from './users.service';
import { UserAdminListDto } from './users.models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TableModule, SelectModule, AvatarModule, InputTextModule, ButtonModule, DialogModule],
  templateUrl: './users.html',
  styleUrls: ['./users.scss']
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(Auth);

  readonly users = signal<UserAdminListDto[]>([]);
  readonly isLoading = signal(false);
  readonly searchTerm = signal('');
  
  readonly showAddUserDialog = signal(false);
  readonly isAddingUser = signal(false);

  readonly addUserForm = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['Worker', [Validators.required]]
  });

  readonly filteredUsers = computed(() => {
    const term = this.searchTerm().trim();
    if (!term) return this.users();

    const lowerTerm = term.replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();

    return this.users().filter((user) => {
      const username = (user.username || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
      const email = (user.email || '').trim().replace(/I/g, 'ı').replace(/İ/g, 'i').toLowerCase();
      
      const usernameWords = username.split(' ');
      const emailWords = email.split(' ');

      const matchUsername = usernameWords.some(word => word.startsWith(lowerTerm));
      const matchEmail = emailWords.some(word => word.startsWith(lowerTerm));

      return matchUsername || matchEmail || username.startsWith(lowerTerm) || email.startsWith(lowerTerm);
    });
  });

  readonly roleOptions = [
    { label: 'Admin', value: 'Admin' },
    { label: 'Analyst', value: 'Analyst' },
    { label: 'Worker', value: 'Worker' }
  ];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.usersService.getAdminUserList().subscribe({
      next: (data) => {
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not load users.' });
      }
    });
  }

  onRoleChange(user: UserAdminListDto, newRole: string): void {
    this.usersService.updateUserRole(user.id, { role: newRole }).subscribe({
      next: (res) => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: res.message });
      },
      error: (err) => {
        // Revert role in UI if failed
        this.loadUsers();
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || err.error || 'Failed to update role.' });
      }
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }

  openAddUserDialog(): void {
    this.addUserForm.reset({ role: 'Worker' });
    this.showAddUserDialog.set(true);
  }

  onAddUserSubmit(): void {
    if (this.addUserForm.invalid) return;

    this.isAddingUser.set(true);
    this.usersService.adminCreateUser(this.addUserForm.getRawValue()).subscribe({
      next: (res) => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: res.message });
        this.showAddUserDialog.set(false);
        this.isAddingUser.set(false);
        this.loadUsers();
      },
      error: (err) => {
        this.isAddingUser.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || err.error || 'Failed to create user.' });
      }
    });
  }

  getAvatarUrl(user: UserAdminListDto): string {
    const url = user.profilePictureUrl;
    if (!url) return '';
    if (url.startsWith('http')) return url;
    
    const serverUrl = API_BASE_URL.endsWith('/api') 
      ? API_BASE_URL.substring(0, API_BASE_URL.length - 4) 
      : API_BASE_URL;
      
    return `${serverUrl}${url}`;
  }

  getInitials(name: string): string {
    if (!name) return '?';
    return name.charAt(0).toUpperCase();
  }
}
