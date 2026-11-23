
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-appointment-booking',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './appointment-booking.html',
  styleUrl: './appointment-booking.css'
})
export class AppointmentBooking {
  doctor = '';
  date = '';
  time = '';
  reason = '';

  confirmBooking() {
    if (!this.doctor || !this.date || !this.time) {
      alert('Please select a doctor, date, and time.');
      return;
    }

    const doctorNames: { [key: string]: string } = {
      doctor1: 'Dr. Ahmad',
      doctor2: 'Dr. Sara',
      doctor3: 'Dr. Omar'
    };

    const selectedDoctor = doctorNames[this.doctor];

    alert(
      'Please review your appointment:\n\n' +
      'Doctor: ' + selectedDoctor + '\n' +
      'Date: ' + this.date + '\n' +
      'Time: ' + this.time + '\n' +
      'Reason: ' + (this.reason || 'Not specified')
    );
  }
}
