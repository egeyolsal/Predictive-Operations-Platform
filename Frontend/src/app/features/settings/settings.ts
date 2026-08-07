import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { PasswordModule } from 'primeng/password';
import { DividerModule } from 'primeng/divider';
import { ProfileService } from '../profile/profile.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, CardModule, 
    ButtonModule, PasswordModule, DividerModule
  ],
  templateUrl: './settings.html',
  styleUrls: ['./settings.scss']
})
export class SettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly messageService = inject(MessageService);

  readonly isSavingPassword = signal(false);

  readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [
      Validators.required, 
      Validators.minLength(8), 
      Validators.pattern(/^(?=.*[A-Z])(?=.*\d).{8,}$/)
    ]],
    confirmPassword: ['', Validators.required]
  });

  onSavePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();

    if (newPassword !== confirmPassword) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'New passwords do not match.' });
      return;
    }

    this.isSavingPassword.set(true);
    this.profileService.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.isSavingPassword.set(false);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Password changed successfully.' });
        this.passwordForm.reset();
      },
      error: (err) => {
        this.isSavingPassword.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || err.error || 'Failed to change password.' });
      }
    });
  }
}
