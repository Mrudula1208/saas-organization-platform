import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TenantService } from '../../../core/services/tenant';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  name = '';
  domain = '';
  adminName = '';
  adminEmail = '';
  password = '';
  confirmPassword = '';
  plan = 'Basic';
  errorMessage = '';
  successMessage = '';

  constructor(private tenantService: TenantService, private router: Router) {}

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

    if (!this.name || !this.domain || !this.adminName || !this.adminEmail || !this.password || !this.confirmPassword) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    const payload = {
      name: this.name,
      domain: this.domain,
      adminEmail: this.adminEmail,
      adminName: this.adminName,
      plan: this.plan
    };

    this.tenantService.create(payload).subscribe({
      next: () => {
        this.successMessage = 'Organization created successfully! Redirecting you to login...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.errorMessage = err.message || 'An error occurred while creating your organization. Please try again.';
      }
    });
  }
}

