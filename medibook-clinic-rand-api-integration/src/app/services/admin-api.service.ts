import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';


export type AppointmentStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Completed'
  | 'Cancelled';

export interface AdminAppointment {
  id: number;
  patientId: number;
  doctorId: number;
  appointmentDate: string;
  status: AppointmentStatus;
  notes: string;
}

export interface AdminPatient {
  id: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  createdAt: string;
}

export interface DashboardAppointment {
  id: number;
  patientName: string;
  doctorName: string;
  specialty: string;
  date: string;
  time: string;
  status: AppointmentStatus;
}

export interface DashboardData {
  totalDoctors: number;
  totalPatients: number;
  totalAppointments: number;
  pendingAppointments: number;
  appointments: DashboardAppointment[];
}

export interface Specialty {
  id: number;
  name: string;
  description: string;
  isActive?: boolean;
}

export interface AdminDoctor {
  id: number;
  specialtyId: number;
  fullName: string;
  photoUrl?: string | null;
  qualifications?: string | null;
  experienceYears?: number | null;
  consultationFee?: number | null;
  bio?: string | null;
  isActive: number;
}

export interface AdminSchedule {
  id: number;
  doctorId: number;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  slotDuration: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminApiService {

  private readonly apiUrl = 'https://localhost:7157/api/admin';

  constructor(private http: HttpClient) {}

  getDashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${this.apiUrl}/dashboard`);
  }

  updateAppointmentStatus(
    id: number,
    status: AppointmentStatus
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/appointments/${id}/status?status=${status}`,
      {}
    );
  }

  getSpecialties(): Observable<Specialty[]> {
    return this.http.get<Specialty[]>(`${this.apiUrl}/specialties`);
  }

  getAppointments(): Observable<AdminAppointment[]> {
    return this.http.get<AdminAppointment[]>(`${this.apiUrl}/appointments`);
  }

  getPatients(): Observable<AdminPatient[]> {
    return this.http.get<AdminPatient[]>(`${this.apiUrl}/patients`);
  }

  updatePatient(
    id: number,
    patient: Partial<AdminPatient>
  ): Observable<AdminPatient> {
    return this.http.put<AdminPatient>(
      `${this.apiUrl}/patients/${id}`,
      patient
    );
  }

  deletePatient(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/patients/${id}`
    );
  }

  getDoctors(): Observable<AdminDoctor[]> {
    return this.http.get<AdminDoctor[]>(`${this.apiUrl}/doctors`);
  }

  addDoctor(doctor: Partial<AdminDoctor>): Observable<AdminDoctor> {
    return this.http.post<AdminDoctor>(
      `${this.apiUrl}/doctors`,
      doctor
    );
  }

  updateDoctor(
    id: number,
    doctor: Partial<AdminDoctor>
  ): Observable<AdminDoctor> {
    return this.http.put<AdminDoctor>(
      `${this.apiUrl}/doctors/${id}`,
      doctor
    );
  }

  deleteDoctor(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/doctors/${id}`
    );
  }

  addSpecialty(specialty: Partial<Specialty>): Observable<Specialty> {
    return this.http.post<Specialty>(
      `${this.apiUrl}/specialties`,
      specialty
    );
  }

  updateSpecialty(
    id: number,
    specialty: Partial<Specialty>
  ): Observable<Specialty> {
    return this.http.put<Specialty>(
      `${this.apiUrl}/specialties/${id}`,
      specialty
    );
  }

  deleteSpecialty(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/specialties/${id}`
    );
  }

  getSchedules(): Observable<AdminSchedule[]> {
    return this.http.get<AdminSchedule[]>(`${this.apiUrl}/schedules`);
  }

  addSchedule(schedule: Partial<AdminSchedule>): Observable<AdminSchedule> {
    return this.http.post<AdminSchedule>(
      `${this.apiUrl}/schedules`,
      schedule
    );
  }

  updateSchedule(
    id: number,
    schedule: Partial<AdminSchedule>
  ): Observable<AdminSchedule> {
    return this.http.put<AdminSchedule>(
      `${this.apiUrl}/schedules/${id}`,
      schedule
    );
  }

  deleteSchedule(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/schedules/${id}`
    );
  }
}

