import { Routes } from '@angular/router';
import { AppointmentBooking } from './pages/appointment-booking/appointment-booking';
import { BookingConfirmation } from './pages/booking-confirmation/booking-confirmation';
import { MyAppointments } from './pages/my-appointments/my-appointments';
import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';

export const routes: Routes = [
  {
    path: 'admin-dashboard',
    component: AdminDashboardComponent
  },
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
  },
  {
    path: 'admin',
    loadChildren: () => import('./admin.routes').then(m => m.ADMIN_ROUTES)
  }
];