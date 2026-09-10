import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { SettingsService } from '../../../core/services/settings';
import { getErrorMessage, isValidEmail } from '../../../core/helpers';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register implements OnInit {
  name = '';
  domain = '';
  adminName = '';
  adminEmail = '';
  password = '';
  confirmPassword = '';
  plan = 'Basic';
  errorMessage = '';
  successMessage = '';
  submitting = false;

  // Platform configuration state
  platformName = 'SaaS Platform';
  maintenanceMode = false;
  allowRegistrations = true;
  loadingConfig = false;

  constructor(
    private auth: Auth,
    private router: Router,
    private settingsService: SettingsService
  ) {}

  ngOnInit() {
    this.loadPlatformConfig();
  }

  loadPlatformConfig() {
    this.loadingConfig = true;
    this.settingsService.getPublicConfig().subscribe({
      next: (config) => {
        this.loadingConfig = false;
        if (config) {
          this.platformName = config.platformName || 'SaaS Platform';
          this.maintenanceMode = !!config.maintenanceMode;
          this.allowRegistrations = config.allowRegistrations !== false;
        }
      },
      error: () => {
        // Fallback gracefully if public config endpoint is unreachable
        this.loadingConfig = false;
      }
    });
  }

  onNameChange() {
    // Automatically generate a slug domain name on typing the organization name
    if (this.name) {
      this.domain = `${this.name.toLowerCase().replace(/[^a-z0-9]/g, '')}.saasapp.com`;
    } else {
      this.domain = '';
    }
  }

  onSubmit(event: Event) {
    event.preventDefault();
    this.errorMessage = '';
    this.successMessage = '';

    if (this.maintenanceMode) {
      this.errorMessage = 'Registrations are temporarily paused while the platform is undergoing maintenance.';
      return;
    }

    if (!this.allowRegistrations) {
      this.errorMessage = 'Public organization registration is currently disabled.';
      return;
    }

    if (!this.name || !this.domain || !this.adminName || !this.adminEmail || !this.password || !this.confirmPassword) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    if (!isValidEmail(this.adminEmail)) {
      this.errorMessage = 'Please enter a valid admin email address.';
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    if (this.submitting) return;

    const payload = {
      name: this.name,
      domain: this.domain,
      adminEmail: this.adminEmail,
      adminName: this.adminName,
      password: this.password,
      confirmPassword: this.confirmPassword,
      plan: this.plan
    };

    this.submitting = true;
    this.auth.registerTenant(payload).subscribe({
      next: () => {
        this.submitting = false;
        this.successMessage = 'Organization created successfully! Redirecting you to login...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = getErrorMessage(err, 'An error occurred while creating your organization. Please try again.');
      }
    });
  }
}
