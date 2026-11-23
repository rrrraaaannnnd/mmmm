import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

interface Doctor {
  id: number;
  name: string;
  specialty: string;
  consultationFee: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  private http = inject(HttpClient);

  clinicName = 'MediBook Clinic';

  searchTerm = '';
  doctors: Doctor[] = [];

  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDoctors();
  }

  loadDoctors(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.http.get<Doctor[]>('https://localhost:7157/api/Doctors')
      .subscribe({
        next: (data) => {
          this.doctors = data;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading doctors:', error);
          this.errorMessage =
            'Unable to load doctors. Please try again.';
          this.isLoading = false;
        }
      });
  }

  get filteredDoctors(): Doctor[] {
    const term = this.searchTerm.trim().toLowerCase();

    if (!term) {
      return this.doctors;
    }

    return this.doctors.filter(doctor =>
      doctor.name.toLowerCase().includes(term) ||
      doctor.specialty.toLowerCase().includes(term)
    );
  }
}


