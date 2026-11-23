import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface Appointment {
  appointmentId: number;
  referenceNumber: string;
  reasonForVisit: string | null;
  consultationFee: number;
  status: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  doctorName: string;
  specialty: string;
}

@Component({
  selector: 'app-my-appointments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-appointments.html',
  styleUrl: './my-appointments.css'
})
export class MyAppointments implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly apiUrl =
    'https://localhost:7157/api/Appointments';

  // DEMO ONLY - replace after implementing authentication.
  patientId = 1;

  appointments: Appointment[] = [];

  isLoading = false;
  cancellingId: number | null = null;

  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadAppointments();
  }

  loadAppointments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.http.get<Appointment[]>(
      `${this.apiUrl}/patient/${this.patientId}`
    ).subscribe({
      next: (data) => {
        this.appointments = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading appointments:', error);
        this.errorMessage =
          'Unable to load appointments. Please try again.';
        this.isLoading = false;
      }
    });
  }

  canCancel(appointment: Appointment): boolean {
    const status = appointment.status.toUpperCase();

    return status === 'PENDING' || status === 'CONFIRMED';
  }

  cancelAppointment(appointment: Appointment): void {
    if (!this.canCancel(appointment) || this.cancellingId !== null) {
      return;
    }

    const confirmed = confirm(
      'Are you sure you want to cancel this appointment?'
    );

    if (!confirmed) {
      return;
    }

    this.cancellingId = appointment.appointmentId;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.put(
      `${this.apiUrl}/${appointment.appointmentId}/cancel`,
      null,
      {
        params: {
          patientId: String(this.patientId)
        }
      }
    ).subscribe({
      next: () => {
        this.cancellingId = null;
        this.successMessage =
          'Appointment cancelled successfully.';

        this.loadAppointments();
      },
      error: (error) => {
        console.error('Cancellation failed:', error);

        this.cancellingId = null;

        if (error.status === 404) {
          this.errorMessage =
            'Appointment not found or cannot be cancelled.';
        } else {
          this.errorMessage =
            'Unable to cancel appointment. Please try again.';
        }

        this.loadAppointments();
      }
    });
  }

  bookAppointment(): void {
    this.router.navigate(['/appointment-booking']);
  }
}
