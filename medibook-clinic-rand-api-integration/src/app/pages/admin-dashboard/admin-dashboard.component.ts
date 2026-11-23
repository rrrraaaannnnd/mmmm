
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

import {
  AdminApiService,
  DashboardAppointment,
  AppointmentStatus
} from '../../services/admin-api.service';

interface ApiAppointment {
  appointmentId: number;
  patientName: string;
  doctorName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {

  private http = inject(HttpClient);
  private adminApi = inject(AdminApiService);

  private readonly appointmentsUrl =
    'https://localhost:7157/api/admin/appointments';

  searchText = '';

  totalDoctors = 0;
  totalPatients = 0;
  totalAppointments = 0;
  pendingCount = 0;

  appointments: DashboardAppointment[] = [];

  loading = false;
  errorMessage = '';
  successMessage = '';

  updatingId: number | null = null;

  ngOnInit(): void {
    this.loadDashboard();
  }

  private normalizeStatus(value: string): AppointmentStatus {
    switch ((value || '').toUpperCase()) {
      case 'CONFIRMED':
        return 'Confirmed';
      case 'COMPLETED':
        return 'Completed';
      case 'CANCELLED':
        return 'Cancelled';
      default:
        return 'Pending';
    }
  }

  loadDashboard(): void {
    this.loading = true;
    this.errorMessage = '';

    // 1. Load dashboard statistics
    this.adminApi.getDashboard().subscribe({
      next: (data) => {
        this.totalDoctors = data.totalDoctors;
        this.totalPatients = data.totalPatients;
        this.totalAppointments = data.totalAppointments;
        this.pendingCount = data.pendingAppointments;

        this.loading = false;
      },
      error: (error) => {
        console.error('Dashboard statistics failed:', error);

        this.errorMessage =
          'Unable to load dashboard statistics.';

        this.loading = false;
      }
    });

    // 2. Load real appointments from Oracle
    this.loadAppointments();
  }

  loadAppointments(): void {
    this.http.get<ApiAppointment[]>(this.appointmentsUrl)
      .subscribe({
        next: (data) => {
          this.appointments = data.map(item => ({
            id: item.appointmentId,
            patientName: item.patientName,
            doctorName: item.doctorName,

            // Not returned by the current Admin API.
            specialty: '—',

            date: item.appointmentDate
              ? item.appointmentDate.substring(0, 10)
              : '',

            time: `${item.startTime} - ${item.endTime}`,

            status: this.normalizeStatus(item.status)
          }));
        },
        error: (error) => {
          console.error('Recent appointments failed:', error);

          this.appointments = [];
          this.errorMessage =
            'Unable to load recent appointments.';
        }
      });
  }

  get filteredAppointments(): DashboardAppointment[] {
    const query = this.searchText.trim().toLowerCase();

    if (!query) {
      return this.appointments;
    }

    return this.appointments.filter(appointment =>
      appointment.patientName.toLowerCase().includes(query) ||
      appointment.doctorName.toLowerCase().includes(query) ||
      appointment.specialty.toLowerCase().includes(query) ||
      String(appointment.id).includes(query)
    );
  }

  updateStatus(id: number, status: string): void {
    const appointment = this.appointments.find(
      item => item.id === id
    );

    if (!appointment || this.updatingId !== null) {
      return;
    }

    const newStatus = this.normalizeStatus(status);
    const oldStatus = appointment.status;

    if (newStatus === oldStatus) {
      return;
    }

    const allowed =
      (oldStatus === 'Pending' &&
        ['Confirmed', 'Cancelled'].includes(newStatus)) ||
      (oldStatus === 'Confirmed' &&
        ['Completed', 'Cancelled'].includes(newStatus));

    if (!allowed) {
      this.errorMessage =
        'This status change is not allowed.';

      this.loadAppointments();
      return;
    }

    if (!confirm(
      `Change appointment #${id} from ${oldStatus} to ${newStatus}?`
    )) {
      this.loadAppointments();
      return;
    }

    this.updatingId = id;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.put(
      `${this.appointmentsUrl}/${id}/status`,
      { status: newStatus.toUpperCase() }
    ).subscribe({
      next: () => {
        this.updatingId = null;
        this.successMessage =
          `Appointment #${id} updated successfully.`;

        this.loadDashboard();
      },
      error: (error) => {
        console.error('Status update failed:', error);

        this.updatingId = null;
        this.errorMessage =
          error.status === 409
            ? 'This status transition is not allowed.'
            : 'Unable to update appointment status.';

        this.loadAppointments();
      }
    });
  }
}
