import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {
  email = '';
  errorMessage = '';
  successMessage = '';
  isSubmitted = false;
  submitting = false;

  constructor(private auth: Auth) {}

  onSubmit(event: Event) {
    event.preventDefault();
    this.errorMessage = '';
    this.successMessage = '';
    if (!this.email || this.submitting) return;

    this.submitting = true;
    this.auth.forgotPassword(this.email).subscribe({
      next: () => {
        this.submitting = false;
        this.isSubmitted = true;
        this.successMessage = `We have sent a password reset link to ${this.email}. Please check your inbox.`;
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = getErrorMessage(err, 'An error occurred. Please try again.');
      }
    });
  }
}
