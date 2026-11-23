
import { Routes } from '@angular/router';

import { AdminDashboardComponent } from './pages/admin-dashboard/admin-dashboard.component';
import { SpecialtyManagementComponent } from './pages/specialty-management/specialty-management.component';
import { ScheduleManagementComponent } from './pages/schedule-management/schedule-management.component';
import { AppointmentManagementComponent } from './pages/appointment-management/appointment-management.component';
import { DoctorManagementComponent } from './pages/doctor-management/doctor-management.component';
import { PatientManagementComponent } from './pages/patient-management/patient-management.component';

export const ADMIN_ROUTES: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: AdminDashboardComponent },
  { path: 'specialties', component: SpecialtyManagementComponent },
  { path: 'schedules', component: ScheduleManagementComponent },
  { path: 'appointments', component: AppointmentManagementComponent },
  { path: 'doctors', component: DoctorManagementComponent },
  { path: 'patients', component: PatientManagementComponent }
];
