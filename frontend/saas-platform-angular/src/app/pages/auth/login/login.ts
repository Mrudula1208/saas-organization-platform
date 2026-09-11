import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';

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

  constructor(private auth: Auth, private router: Router) {}

  onSubmit(event: Event) {
    event.preventDefault();
    this.errorMessage = '';

    if (!this.email || !this.password) return;

    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        const currentUser = this.auth.currentUser();
        if (currentUser) {
          if (currentUser.role === 'Admin') {
            this.router.navigate(['/admin/dashboard']);
          } else {
            this.router.navigate(['/tenant/dashboard']);
          }
        }
      },
      error: (err) => {
        this.errorMessage = err.message || 'Login failed. Please check your credentials and try again.';
      }
    });
  }
}
