
import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-booking-confirmation',
  standalone: true,
  templateUrl: './booking-confirmation.html',
  styleUrl: './booking-confirmation.css'
})
export class BookingConfirmation {
  doctor = '';
  date = '';
  time = '';
  reason = '';

  constructor(private router: Router) {
    const appointment = history.state;

    this.doctor = appointment?.doctor || '';
    this.date = appointment?.date || '';
    this.time = appointment?.time || '';
    this.reason = appointment?.reason || '';
  }

  goToBooking() {
    this.router.navigate(['/appointment-booking']);
  }

  goToAppointments() {
    this.router.navigate(['/my-appointments'], {
      state: {
        doctor: this.doctor,
        date: this.date,
        time: this.time,
        reason: this.reason
      }
    });
  }
}
