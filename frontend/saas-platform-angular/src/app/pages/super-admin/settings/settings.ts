import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SettingsService } from '../../../core/services/settings';
import { getErrorMessage } from '../../../core/helpers';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  platformName = 'SaaS Platform';
  supportEmail = 'support@saas.com';
  maintenanceMode = false;
  allowRegistrations = true;
  mfaRequired = false;
  sessionTimeout = 30; // in minutes

  isDarkTheme = true;
  isLoading = false;
  isSaving = false;
  successMessage = '';
  errorMessage = '';

  constructor(private settingsService: SettingsService) {}

  ngOnInit() {
    this.restoreTheme();
    this.loadSettings();
  }

  restoreTheme() {
    if (typeof window !== 'undefined') {
      const storedTheme = localStorage.getItem('theme_preference');
      this.isDarkTheme = storedTheme !== 'light';
      if (!this.isDarkTheme) {
        document.body.classList.add('light-theme');
      } else {
        document.body.classList.remove('light-theme');
      }
    }
  }

  loadSettings() {
    this.isLoading = true;
    this.errorMessage = '';

    this.settingsService.getSettings().subscribe({
      next: (res) => {
        if (res.data) {
          this.platformName = res.data.platformName;
          this.supportEmail = res.data.supportEmail;
          this.maintenanceMode = res.data.maintenanceMode;
          this.allowRegistrations = res.data.allowRegistrations;
          this.mfaRequired = res.data.mfaRequired;
          this.sessionTimeout = res.data.sessionTimeout;
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Failed to load system settings from server.');
        this.isLoading = false;
      }
    });
  }

  toggleTheme() {
    this.isDarkTheme = !this.isDarkTheme;
    if (typeof window !== 'undefined') {
      if (this.isDarkTheme) {
        document.body.classList.remove('light-theme');
        localStorage.setItem('theme_preference', 'dark');
      } else {
        document.body.classList.add('light-theme');
        localStorage.setItem('theme_preference', 'light');
      }
    }
  }

  saveSettings() {
    this.successMessage = '';
    this.errorMessage = '';
    this.isSaving = true;

    const payload = {
      platformName: this.platformName,
      supportEmail: this.supportEmail,
      maintenanceMode: this.maintenanceMode,
      allowRegistrations: this.allowRegistrations,
      mfaRequired: this.mfaRequired,
      sessionTimeout: Number(this.sessionTimeout)
    };

    this.settingsService.updateSettings(payload).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'System configuration updated successfully!';
        this.isSaving = false;
        setTimeout(() => {
          this.successMessage = '';
        }, 4000);
      },
      error: (err) => {
        this.errorMessage = getErrorMessage(err, 'Failed to save settings.');
        this.isSaving = false;
      }
    });
  }
}
