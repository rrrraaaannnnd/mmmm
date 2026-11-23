import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Doctor {
  id: number;
  name: string;
  specialty: string;
  consultationFee: number;
}

interface AppointmentSlot {
  slotId: number;
  date: string;
  startTime: string;
  endTime: string;
}

interface BookingResponse {
  message: string;
  referenceNumber: string;
  status: string;
  consultationFee: number;
}

@Component({
  selector: 'app-appointment-booking',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './appointment-booking.html',
  styleUrl: './appointment-booking.css'
})
export class AppointmentBooking implements OnInit {
  private http = inject(HttpClient);

  private apiBase = 'https://localhost:7157/api';

  doctors: Doctor[] = [];
  availableSlots: AppointmentSlot[] = [];

  doctorId: number | null = null;
  date = '';
  selectedSlotId: number | null = null;
  reason = '';

  // DEMO ONLY: replace with authenticated patient ID.
  patientId = 1;

  minDate = this.getTodayDate();

  isLoadingDoctors = false;
  isLoadingSlots = false;
  isBooking = false;

  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadDoctors();
  }

  private getTodayDate(): string {
    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  loadDoctors(): void {
    this.isLoadingDoctors = true;
    this.errorMessage = '';

    this.http.get<Doctor[]>(`${this.apiBase}/Doctors`)
      .subscribe({
        next: (data) => {
          this.doctors = data;
          this.isLoadingDoctors = false;
        },
        error: () => {
          this.isLoadingDoctors = false;
          this.errorMessage = 'Unable to load doctors.';
        }
      });
  }

  onSelectionChange(): void {
    this.selectedSlotId = null;
    this.availableSlots = [];
    this.errorMessage = '';
    this.successMessage = '';

    if (this.doctorId === null || !this.date) {
      return;
    }

    if (this.date < this.minDate) {
      this.errorMessage = 'Please choose a valid future date.';
      return;
    }

    this.loadAvailableSlots();
  }

  loadAvailableSlots(): void {
    if (this.doctorId === null || !this.date) {
      return;
    }

    this.isLoadingSlots = true;

    this.http.get<AppointmentSlot[]>(
      `${this.apiBase}/AppointmentSlots/available`,
      {
        params: {
          doctorId: String(this.doctorId),
          date: this.date
        }
      }
    ).subscribe({
      next: (data) => {
        this.availableSlots = data;
        this.isLoadingSlots = false;
      },
      error: () => {
        this.isLoadingSlots = false;
        this.errorMessage = 'Unable to load available times.';
      }
    });
  }

  confirmBooking(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.doctorId === null ||
        !this.date ||
        this.selectedSlotId === null) {
      this.errorMessage =
        'Please select a doctor, date, and available time.';
      return;
    }

    if (this.date < this.getTodayDate()) {
      this.errorMessage = 'Please choose a valid date.';
      return;
    }

    if (this.isBooking) {
      return;
    }

    this.isBooking = true;

    this.http.post<BookingResponse>(
      `${this.apiBase}/Appointments`,
      {
        patientId: this.patientId,
        slotId: this.selectedSlotId,
        reasonForVisit: this.reason
      }
    ).subscribe({
      next: (response) => {
        this.isBooking = false;
        this.successMessage =
          `Appointment booked successfully! Reference: ${response.referenceNumber}`;

        this.selectedSlotId = null;
        this.loadAvailableSlots();
      },
      error: (error) => {
        this.isBooking = false;

        if (error.status === 409) {
          this.errorMessage = 'This time is already booked.';
        } else if (error.status === 404) {
          this.errorMessage = 'This appointment slot is unavailable.';
        } else if (error.status === 400) {
          this.errorMessage = 'Please check the booking details.';
        } else {
          this.errorMessage = 'Booking failed. Please try again.';
        }

        this.selectedSlotId = null;
        this.loadAvailableSlots();
      }
    });
  }
}
