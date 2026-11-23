import { Routes } from '@angular/router';

import { AppointmentBooking } from './pages/appointment-booking/appointment-booking';
import { BookingConfirmation } from './pages/booking-confirmation/booking-confirmation';
import { MyAppointments } from './pages/my-appointments/my-appointments';

export const routes: Routes = [
  {
    path: 'appointment-booking',
    component: AppointmentBooking
  },
  {
    path: 'booking-confirmation',
    component: BookingConfirmation
  },
  {
    path: 'my-appointments',
    component: MyAppointments
  }
];