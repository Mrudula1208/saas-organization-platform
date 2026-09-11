import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

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
  successMessage = '';

  ngOnInit() {
    this.restoreSettings();
  }

  restoreSettings() {
    if (typeof window !== 'undefined') {
      const storedTheme = localStorage.getItem('theme_preference');
      this.isDarkTheme = storedTheme !== 'light';
      if (!this.isDarkTheme) {
        document.body.classList.add('light-theme');
      } else {
        document.body.classList.remove('light-theme');
      }

      const storedConfig = localStorage.getItem('saas_config');
      if (storedConfig) {
        const config = JSON.parse(storedConfig);
        this.platformName = config.platformName || this.platformName;
        this.supportEmail = config.supportEmail || this.supportEmail;
        this.maintenanceMode = config.maintenanceMode ?? this.maintenanceMode;
        this.allowRegistrations = config.allowRegistrations ?? this.allowRegistrations;
        this.mfaRequired = config.mfaRequired ?? this.mfaRequired;
        this.sessionTimeout = config.sessionTimeout ?? this.sessionTimeout;
      }
    }
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
    const config = {
      platformName: this.platformName,
      supportEmail: this.supportEmail,
      maintenanceMode: this.maintenanceMode,
      allowRegistrations: this.allowRegistrations,
      mfaRequired: this.mfaRequired,
      sessionTimeout: this.sessionTimeout
    };

    if (typeof window !== 'undefined') {
      localStorage.setItem('saas_config', JSON.stringify(config));
    }

    this.successMessage = 'System configuration updated successfully!';
    setTimeout(() => {
      this.successMessage = '';
    }, 3000);
  }
}

