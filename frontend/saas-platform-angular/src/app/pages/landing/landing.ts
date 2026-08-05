import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.html',
  styleUrl: './landing.css'
})
export class LandingPage {
  
  submitContact(event: Event) {
    event.preventDefault();
    alert('Thank you for contacting us! We have received your message and will get back to you shortly.');
    const form = event.target as HTMLFormElement;
    form.reset();
  }
}
