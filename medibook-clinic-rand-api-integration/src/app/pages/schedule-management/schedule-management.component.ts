
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { forkJoin } from 'rxjs';

interface ApiDoctor {
  id: number;
  fullName: string;
  specialtyId: number;
}

interface ApiSpecialty {
  id: number;
  name: string;
}

interface ApiSchedule {
  id: number;
  doctorId: number;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  slotDuration: number;
  isActive: number;
}

interface Doctor {
  id: number;
  name: string;
  specialty: string;
}

interface DoctorSchedule {
  id: number;
  doctorId: number;
  doctorName: string;
  specialty: string;
  day: string;
  startTime: string;
  endTime: string;
  slotDuration: number;
  isActive: number;
}

@Component({
  selector: 'app-schedule-management',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './schedule-management.component.html',
  styleUrl: './schedule-management.component.css'
})
export class ScheduleManagementComponent implements OnInit {

  private http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7157/api/admin';

  doctors: Doctor[] = [];

  days: string[] = [
    'Sunday',
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday'
  ];

  selectedDoctorId = 0;
  selectedDay = 'Sunday';
  startTime = '09:00';
  endTime = '14:00';
  slotDuration = 30;

  searchText = '';
  editingId: number | null = null;
  message = '';
  isLoading = false;

  schedules: DoctorSchedule[] = [];

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      doctors: this.http.get<ApiDoctor[]>(
        `${this.apiUrl}/doctors`
      ),
      specialties: this.http.get<ApiSpecialty[]>(
        `${this.apiUrl}/specialties`
      ),
      schedules: this.http.get<ApiSchedule[]>(
        `${this.apiUrl}/schedules`
      )
    }).subscribe({
      next: ({ doctors, specialties, schedules }) => {

        this.doctors = doctors.map(doctor => {
          const specialty = specialties.find(
            s => s.id === doctor.specialtyId
          );

          return {
            id: doctor.id,
            name: doctor.fullName,
            specialty: specialty?.name ?? 'Unknown'
          };
        });

        this.schedules = schedules.map(schedule => {
          const doctor = this.doctors.find(
            d => d.id === schedule.doctorId
          );

          const day = schedule.dayOfWeek.toLowerCase();

          return {
            id: schedule.id,
            doctorId: schedule.doctorId,
            doctorName: doctor?.name ?? 'Unknown Doctor',
            specialty: doctor?.specialty ?? 'Unknown',
            day: day.charAt(0).toUpperCase() + day.slice(1),
            startTime: schedule.startTime,
            endTime: schedule.endTime,
            slotDuration: schedule.slotDuration,
            isActive: schedule.isActive
          };
        });

        if (
          this.editingId === null &&
          !this.doctors.some(
            d => d.id === Number(this.selectedDoctorId)
          )
        ) {
          this.selectedDoctorId = this.doctors[0]?.id ?? 0;
        }

        this.isLoading = false;
      },

      error: (error) => {
        console.error('Loading schedules failed:', error);
        this.message =
          'Unable to load schedules from the API.';
        this.isLoading = false;
      }
    });
  }

  get totalSchedules(): number {
    return this.schedules.length;
  }

  get totalDoctors(): number {
    return new Set(
      this.schedules.map(s => s.doctorId)
    ).size;
  }

  get filteredSchedules(): DoctorSchedule[] {
    const query = this.searchText.trim().toLowerCase();

    return this.schedules.filter(schedule =>
      schedule.doctorName.toLowerCase().includes(query) ||
      schedule.specialty.toLowerCase().includes(query) ||
      schedule.day.toLowerCase().includes(query)
    );
  }

  private toMinutes(time: string): number {
    const parts = time.split(':');

    if (parts.length !== 2) return NaN;

    const hours = Number(parts[0]);
    const minutes = Number(parts[1]);

    if (
      !Number.isInteger(hours) ||
      !Number.isInteger(minutes) ||
      hours < 0 || hours > 23 ||
      minutes < 0 || minutes > 59
    ) {
      return NaN;
    }

    return hours * 60 + minutes;
  }

  saveSchedule(): void {
    if (this.isLoading) return;

    const doctorId = Number(this.selectedDoctorId);
    const doctor = this.doctors.find(
      d => d.id === doctorId
    );

    if (!doctor) {
      this.message = 'Please select a doctor.';
      return;
    }

    const start = this.toMinutes(this.startTime);
    const end = this.toMinutes(this.endTime);
    const duration = Number(this.slotDuration);

    if (
      !Number.isFinite(start) ||
      !Number.isFinite(end) ||
      start >= end
    ) {
      this.message =
        'End time must be later than start time.';
      return;
    }

    if (
      !Number.isInteger(duration) ||
      duration <= 0
    ) {
      this.message =
        'Appointment duration must be positive.';
      return;
    }

    const hasOverlap = this.schedules.some(schedule =>
      schedule.id !== this.editingId &&
      schedule.doctorId === doctorId &&
      schedule.day.toLowerCase() ===
        this.selectedDay.toLowerCase() &&
      start < this.toMinutes(schedule.endTime) &&
      end > this.toMinutes(schedule.startTime)
    );

    if (hasOverlap) {
      this.message =
        'This schedule overlaps with an existing schedule.';
      return;
    }

    const existing = this.schedules.find(
      s => s.id === this.editingId
    );

    const data = {
      doctorId,
      dayOfWeek: this.selectedDay.toUpperCase(),
      startTime: this.startTime,
      endTime: this.endTime,
      slotDuration: duration,
      isActive: existing?.isActive ?? 1
    };

    const isEditing = this.editingId !== null;

    const request = isEditing
      ? this.http.put(
          `${this.apiUrl}/schedules/${this.editingId}`,
          data
        )
      : this.http.post(
          `${this.apiUrl}/schedules`,
          data
        );

    this.isLoading = true;
    this.message = '';

    request.subscribe({
      next: () => {
        this.message = isEditing
          ? 'Schedule updated successfully.'
          : 'Schedule added successfully.';

        this.resetForm();
        this.loadData();
      },
      error: (error) => {
        console.error('Saving schedule failed:', error);

        this.message = error.status === 409
          ? 'Schedule conflicts with an existing record.'
          : 'Unable to save schedule.';

        this.isLoading = false;
      }
    });
  }

  editSchedule(schedule: DoctorSchedule): void {
    this.editingId = schedule.id;
    this.selectedDoctorId = schedule.doctorId;
    this.selectedDay = schedule.day;
    this.startTime = schedule.startTime;
    this.endTime = schedule.endTime;
    this.slotDuration = schedule.slotDuration;
    this.message = '';
  }

  deleteSchedule(id: number): void {
    if (this.isLoading) return;

    const schedule = this.schedules.find(
      s => s.id === id
    );

    if (!schedule) return;

    if (!confirm(
      `Delete schedule #${id} for ${schedule.doctorName}?`
    )) {
      return;
    }

    this.isLoading = true;
    this.message = '';

    this.http.delete(
      `${this.apiUrl}/schedules/${id}`
    ).subscribe({
      next: () => {
        this.message =
          'Schedule deleted successfully.';

        if (this.editingId === id) {
          this.resetForm();
        }

        this.loadData();
      },
      error: (error) => {
        console.error('Deleting schedule failed:', error);

        this.message = error.status === 409
          ? 'Cannot delete this schedule because related slots exist.'
          : 'Unable to delete schedule.';

        this.isLoading = false;
      }
    });
  }

  resetForm(): void {
    this.selectedDoctorId = this.doctors[0]?.id ?? 0;
    this.selectedDay = 'Sunday';
    this.startTime = '09:00';
    this.endTime = '14:00';
    this.slotDuration = 30;
    this.editingId = null;
  }
}
