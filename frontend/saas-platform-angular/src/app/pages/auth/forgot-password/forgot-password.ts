import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';

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

  constructor(private auth: Auth) {}

  onSubmit(event: Event) {
    event.preventDefault();
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.email) {
      this.errorMessage = 'Please enter your email address.';
      return;
    }

    this.auth.forgotPassword(this.email).subscribe({
      next: (res) => {
        this.isSubmitted = true;
        this.successMessage = `We have sent a password reset link to ${this.email}. Please check your inbox.`;
      },
      error: (err) => {
        this.errorMessage = err.message || 'An error occurred. Please try again.';
      }
    });
  }
}

