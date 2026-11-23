
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);

  // Temporary API URL - not a working backend yet
  private apiUrl = 'http://localhost:5000/api/auth';

  login(data: LoginRequest) {
    return this.http.post(
      `${this.apiUrl}/login`,
      data
    );
  }

  register(data: RegisterRequest) {
    return this.http.post(
      `${this.apiUrl}/register`,
      data
    );
  }
}
