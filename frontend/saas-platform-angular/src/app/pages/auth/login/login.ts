import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { SettingsService } from '../../../core/services/settings';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login implements OnInit {
  email = '';
  password = '';
  rememberMe = false;
  errorMessage = '';
  submitting = false;

  // Platform configuration state
  platformName = 'SaaS Platform';
  maintenanceMode = false;

  constructor(
    private auth: Auth,
    private router: Router,
    private settingsService: SettingsService
  ) {}

  ngOnInit() {
    this.settingsService.getPublicConfig().subscribe({
      next: (config) => {
        if (config) {
          this.platformName = config.platformName || 'SaaS Platform';
          this.maintenanceMode = !!config.maintenanceMode;
        }
      },
      error: () => {
        // Continue silently if public config cannot be retrieved
      }
    });
  }

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
