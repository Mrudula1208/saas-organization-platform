import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-logout',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './logout.html',
  styleUrl: './logout.css'
})
export class Logout {
  constructor(private auth: Auth, private router: Router) {}

  confirmLogout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  cancel() {
    const user = this.auth.currentUser();
    if (!user) {
      this.router.navigate(['/']);
    } else if (user.role === 'Admin') {
      this.router.navigate(['/admin/dashboard']);
    } else {
      this.router.navigate(['/tenant/dashboard']);
    }
  }
}
