
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private authService = inject(AuthService);

  fullName = '';
  email = '';
  phone = '';
  password = '';
  message = '';
  isLoading = false;

  onRegister() {
    if (this.isLoading) return;

    this.message = '';
    this.isLoading = true;

    this.authService.register({
      fullName: this.fullName,
      email: this.email,
      phone: this.phone,
      password: this.password
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.message =
          'Registration request succeeded. Account confirmation is pending.';
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading = false;

        if (error.status === 0) {
          this.message = 'Cannot connect to the server.';
        } else if (error.status === 409) {
          this.message = 'This email is already registered.';
        } else {
          this.message = 'Registration failed. Please try again.';
        }
      }
    });
  }
}
