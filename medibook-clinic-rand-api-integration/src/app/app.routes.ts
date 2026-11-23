
import { Routes } from '@angular/router';

// Rand's Pages
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';

// Reem's Pages
import { AppointmentBooking } from './pages/appointment-booking/appointment-booking';
import { BookingConfirmation } from './pages/booking-confirmation/booking-confirmation';
import { MyAppointments } from './pages/my-appointments/my-appointments';

// Admin Pages
import { ADMIN_ROUTES } from './admin.routes';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
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
    children: ADMIN_ROUTES
  },
  {
    path: 'admin-login',
    redirectTo: 'admin/dashboard',
    pathMatch: 'full'
  }
];
