import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  email = '';
  password = '';
  rememberMe = false;
  errorMessage = '';
  submitting = false;

  constructor(private auth: Auth, private router: Router) {}

  onSubmit(event: Event) {
    event.preventDefault();
    this.errorMessage = '';
    if (!this.email || !this.password || this.submitting) return;

    this.submitting = true;
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.submitting = false;
        const currentUser = this.auth.currentUser();
        if (currentUser) {
          if (currentUser.role === 'SuperAdmin') {
            this.router.navigate(['/admin/dashboard']);
          } else {
            this.router.navigate(['/tenant/dashboard']);
          }
        }
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = getErrorMessage(err, 'Login failed. Please check your credentials and try again.');
      }
    });
  }
}
