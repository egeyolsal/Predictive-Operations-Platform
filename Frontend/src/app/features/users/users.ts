import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { Auth } from '../../core/auth/auth';

import { UsersService } from './users.service';
import { UserAdminListDto } from './users.models';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, SelectModule],
  templateUrl: './users.html',
  styleUrls: ['./users.scss']
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly messageService = inject(MessageService);
  readonly auth = inject(Auth);

  readonly users = signal<UserAdminListDto[]>([]);
  readonly isLoading = signal(false);

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
}
