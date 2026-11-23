
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private authService = inject(AuthService);

  email = '';
  password = '';
  message = '';
  isLoading = false;

  onLogin() {
    if (this.isLoading) return;

    this.message = '';
    this.isLoading = true;

    this.authService.login({
      email: this.email,
      password: this.password
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.message =
          'Login request succeeded. Authentication handling is pending.';
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading = false;

        if (error.status === 0) {
          this.message = 'Cannot connect to the server.';
        } else if (error.status === 401) {
          this.message = 'Invalid email or password.';
        } else {
          this.message = 'Login failed. Please try again.';
        }
      }
    });
  }
}
