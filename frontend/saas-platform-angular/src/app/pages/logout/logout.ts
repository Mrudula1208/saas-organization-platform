import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-logout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './logout.html',
  styleUrl: './logout.css'
})
export class Logout implements OnInit {
  isLoggingOut = false;

  constructor(
    private auth: Auth,
    private router: Router
  ) {}

  ngOnInit() {}

  confirmLogout() {
    this.isLoggingOut = true;
    setTimeout(() => {
      this.auth.logout();
      this.router.navigate(['/login']);
    }, 400);
  }

  cancel() {
    const user = this.auth.currentUser();
    if (!user) {
      this.router.navigate(['/login']);
    } else if (user.role === 'SuperAdmin') {
      this.router.navigate(['/super-admin/dashboard']);
    } else {
      this.router.navigate(['/tenant/dashboard']);
    }
  }
}
