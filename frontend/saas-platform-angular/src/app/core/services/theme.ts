import { Injectable, signal } from '@angular/core';

export type AppTheme = 'dark' | 'light';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'saas_theme';
  private readonly LEGACY_KEY = 'theme_preference';

  public currentTheme = signal<AppTheme>('dark');

  constructor() {
    this.initTheme();
  }

  get isDark(): boolean {
    return this.currentTheme() === 'dark';
  }

  private initTheme(): void {
    if (typeof window === 'undefined') return;

    // Check saved preference in localStorage, defaulting to 'dark'
    const saved = localStorage.getItem(this.STORAGE_KEY) || localStorage.getItem(this.LEGACY_KEY);
    const initialTheme: AppTheme = saved === 'light' ? 'light' : 'dark';

    this.applyTheme(initialTheme);
  }

  public setTheme(theme: AppTheme): void {
    this.applyTheme(theme);
  }

  public toggleTheme(): void {
    const nextTheme: AppTheme = this.currentTheme() === 'dark' ? 'light' : 'dark';
    this.applyTheme(nextTheme);
  }

  private applyTheme(theme: AppTheme): void {
    this.currentTheme.set(theme);

    if (typeof window === 'undefined') return;

    // Save to both keys for total backwards compatibility
    localStorage.setItem(this.STORAGE_KEY, theme);
    localStorage.setItem(this.LEGACY_KEY, theme);

    const root = document.documentElement;
    const body = document.body;

    root.setAttribute('data-theme', theme);
    root.style.colorScheme = theme;

    if (theme === 'dark') {
      body.classList.add('dark-theme');
      body.classList.remove('light-theme');
    } else {
      body.classList.add('light-theme');
      body.classList.remove('dark-theme');
    }
  }
}
