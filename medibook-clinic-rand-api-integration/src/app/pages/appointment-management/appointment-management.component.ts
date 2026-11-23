
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

type AppointmentStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Completed'
  | 'Cancelled';

interface ApiAppointment {
  appointmentId: number;
  patientId: number;
  patientName: string;
  slotId: number;
  doctorName: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  consultationFee: number;
  status: string;
}

interface Appointment {
  id: number;
  patientName: string;
  doctorName: string;
  specialty: string;
  date: string;
  time: string;
  fee: number;
  status: AppointmentStatus;
}

@Component({
  selector: 'app-appointment-management',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './appointment-management.component.html',
  styleUrl: './appointment-management.component.css'
})
export class AppointmentManagementComponent implements OnInit {

  private http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7157/api/admin/appointments';

  searchText = '';
  statusFilter = 'All';

  appointments: Appointment[] = [];

  loading = false;
  updatingId: number | null = null;

  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadAppointments();
  }

  private normalizeStatus(value: string): AppointmentStatus {
    switch (value.toUpperCase()) {
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

  loadAppointments(): void {
    this.loading = true;
    this.errorMessage = '';

    this.http.get<ApiAppointment[]>(this.apiUrl)
      .subscribe({
        next: (data) => {
          this.appointments = data.map(item => ({
            id: item.appointmentId,
            patientName: item.patientName,
            doctorName: item.doctorName,

            // Current admin API does not return specialty.
            specialty: '—',

            date: item.appointmentDate.substring(0, 10),
            time: `${item.startTime} - ${item.endTime}`,
            fee: item.consultationFee,
            status: this.normalizeStatus(item.status)
          }));

          this.loading = false;
        },
        error: (error) => {
          console.error('Loading appointments failed:', error);
          this.loading = false;
          this.errorMessage =
            'Unable to load appointments from Oracle API.';
        }
      });
  }

  get totalAppointments(): number {
    return this.appointments.length;
  }

  get pendingCount(): number {
    return this.appointments.filter(
      a => a.status === 'Pending'
    ).length;
  }

  get confirmedCount(): number {
    return this.appointments.filter(
      a => a.status === 'Confirmed'
    ).length;
  }

  get cancelledCount(): number {
    return this.appointments.filter(
      a => a.status === 'Cancelled'
    ).length;
  }

  get filteredAppointments(): Appointment[] {
    const query = this.searchText.trim().toLowerCase();

    return this.appointments.filter(a => {
      const matchesSearch =
        a.patientName.toLowerCase().includes(query) ||
        a.doctorName.toLowerCase().includes(query) ||
        a.specialty.toLowerCase().includes(query) ||
        String(a.id).includes(query);

      const matchesStatus =
        this.statusFilter === 'All' ||
        a.status === this.statusFilter;

      return matchesSearch && matchesStatus;
    });
  }

  updateStatus(id: number, status: string): void {
    const appointment = this.appointments.find(
      a => a.id === id
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
      `${this.apiUrl}/${id}/status`,
      { status: newStatus.toUpperCase() }
    ).subscribe({
      next: () => {
        this.updatingId = null;
        this.successMessage =
          `Appointment #${id} updated successfully.`;

        this.loadAppointments();
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
