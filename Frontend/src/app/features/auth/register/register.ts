import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Auth } from '../../../core/auth/auth';

// Custom validator: passwords must match
function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirm  = control.get('confirmPassword')?.value;
  return password === confirm ? null : { mismatch: true };
}

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private readonly fb          = inject(FormBuilder);
  private readonly authService = inject(Auth);
  private readonly router      = inject(Router);

  readonly errorMessage   = signal<string | null>(null);
  readonly isSubmitting   = signal(false);
  showPassword            = false;
  showConfirmPassword     = false;

  readonly form = this.fb.group({
    username:        ['', [Validators.required, Validators.minLength(3)]],
    email:           ['', [Validators.required, Validators.email]],
    password:        ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordsMatch });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.isSubmitting.set(true);

    const { username, email, password } = this.form.getRawValue();

    this.authService.register({ username: username!, email: email!, password: password! }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err?.error ?? 'Registration failed. Please try again.');
      }
    });
  }
}
