import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { DividerModule } from 'primeng/divider';

import { ProfileService, ProfileDto } from './profile.service';
import { API_BASE_URL } from '../../core/config/api-config';
import { Auth } from '../../core/auth/auth';

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
  private readonly auth = inject(Auth);

  readonly isLoading = signal(false);
  readonly isSavingProfile = signal(false);
  readonly profilePictureUrl = signal<string | null>(null);
  
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);

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
        this.profilePictureUrl.set(profile.profilePictureUrl || null);
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

    // If there is a selected file, upload it first
    if (this.selectedFile()) {
      this.profileService.uploadProfilePicture(this.selectedFile()!).subscribe({
        next: (res) => {
          this.profilePictureUrl.set(res.profilePictureUrl);
          this.auth.updateProfilePicture(res.profilePictureUrl);
          this.selectedFile.set(null);
          this.previewUrl.set(null);
          this.updateProfileData(val); // Then update the rest of the profile
        },
        error: (err) => {
          this.isSavingProfile.set(false);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: this.extractErrorMessage(err, 'Failed to upload picture.') });
        }
      });
    } else {
      this.updateProfileData(val);
    }
  }

  private updateProfileData(val: any): void {
    const phone = val.phoneNumber?.trim();
    const payload = {
      email: val.email,
      phoneNumber: phone ? phone : null
    };

    this.profileService.updateProfile(payload).subscribe({
      next: () => {
        this.isSavingProfile.set(false);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Profile updated successfully.' });
      },
      error: (err) => {
        this.isSavingProfile.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: this.extractErrorMessage(err, 'Failed to update profile.') });
      }
    });
  }

  private extractErrorMessage(err: any, defaultMessage: string): string {
    if (!err) return defaultMessage;
    if (typeof err.error === 'string') return err.error;
    if (err.error?.errors) {
      const messages = Object.values(err.error.errors).flat();
      return messages.join(' ') || err.error.title || defaultMessage;
    }
    if (err.error?.message) return err.error.message;
    if (err.error?.title) return err.error.title;
    if (err.message) return err.message;
    return defaultMessage;
  }

  getInitials(): string {
    const username = this.profileForm.get('username')?.value || '';
    if (!username) return 'U';
    return username.charAt(0).toUpperCase();
  }

  getProfilePictureUrl(): string {
    if (this.previewUrl()) return this.previewUrl()!;
    
    const url = this.profilePictureUrl();
    if (!url) return '';
    // If it's a full URL, return it. Otherwise prepend base url (without /api)
    if (url.startsWith('http')) return url;
    
    // We assume API_BASE_URL is something like http://localhost:5000/api
    // We want http://localhost:5000
    const serverUrl = API_BASE_URL.endsWith('/api') 
      ? API_BASE_URL.substring(0, API_BASE_URL.length - 4) 
      : API_BASE_URL;
      
    return `${serverUrl}${url}`;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Save file to be uploaded later
      this.selectedFile.set(file);
      
      // Create local preview URL
      const reader = new FileReader();
      reader.onload = (e) => {
        this.previewUrl.set(e.target?.result as string);
      };
      reader.readAsDataURL(file);
      
      // Reset input so the same file can be selected again if needed
      input.value = '';
    }
  }
}
