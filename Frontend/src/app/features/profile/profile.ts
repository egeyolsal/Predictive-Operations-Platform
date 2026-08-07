import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { DividerModule } from 'primeng/divider';

import { ProfileService, ProfileDto } from './profile.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, CardModule, InputTextModule, 
    ButtonModule, DividerModule
  ],
  templateUrl: './profile.html',
  styleUrls: ['./profile.scss']
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly messageService = inject(MessageService);

  readonly isLoading = signal(false);
  readonly isSavingProfile = signal(false);

  readonly profileForm = this.fb.nonNullable.group({
    username: [{ value: '', disabled: true }],
    role: [{ value: '', disabled: true }],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['']
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.profileService.getProfile().subscribe({
      next: (profile) => {
        this.profileForm.patchValue({
          username: profile.username,
          role: profile.role,
          email: profile.email,
          phoneNumber: profile.phoneNumber || ''
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Could not load profile data.' });
      }
    });
  }

  onSaveProfile(): void {
    if (this.profileForm.invalid) return;

    this.isSavingProfile.set(true);
    const val = this.profileForm.getRawValue();

    this.profileService.updateProfile({ email: val.email, phoneNumber: val.phoneNumber }).subscribe({
      next: () => {
        this.isSavingProfile.set(false);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Profile updated successfully.' });
      },
      error: (err) => {
        this.isSavingProfile.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error || 'Failed to update profile.' });
      }
    });
  }
}
