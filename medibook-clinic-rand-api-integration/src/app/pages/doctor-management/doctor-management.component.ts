import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  AdminApiService,
  AdminDoctor,
  Specialty
} from '../../services/admin-api.service';

@Component({
  selector: 'app-doctor-management',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './doctor-management.component.html',
  styleUrl: './doctor-management.component.css'
})
export class DoctorManagementComponent implements OnInit {

  doctors: AdminDoctor[] = [];
  specialties: Specialty[] = [];

  searchText = '';

  fullName = '';
  specialtyId: number | null = null;
  qualifications = '';
  experienceYears: number | null = null;
  consultationFee: number | null = null;
  photoUrl = '';
  bio = '';

  editingId: number | null = null;
  message = '';
  isLoading = false;

  constructor(private api: AdminApiService) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      doctors: this.api.getDoctors(),
      specialties: this.api.getSpecialties()
    }).subscribe({
      next: (data) => {
        this.doctors = data.doctors;
        this.specialties = data.specialties;
        this.isLoading = false;
      },
      error: (error) => {
        this.message = 'Could not load doctors. Please check the API.';
        this.isLoading = false;
        console.error(error);
      }
    });
  }

  get totalDoctors(): number {
    return this.doctors.length;
  }

  get activeDoctors(): number {
    return this.doctors.filter(d => d.isActive === 1).length;
  }

  get inactiveDoctors(): number {
    return this.doctors.filter(d => d.isActive !== 1).length;
  }

  get filteredDoctors(): AdminDoctor[] {
    const query = this.searchText.trim().toLowerCase();

    if (!query) {
      return this.doctors;
    }

    return this.doctors.filter(doctor =>
      doctor.fullName.toLowerCase().includes(query) ||
      this.getSpecialtyName(doctor.specialtyId).toLowerCase().includes(query) ||
      (doctor.qualifications ?? '').toLowerCase().includes(query)
    );
  }

  getSpecialtyName(id: number): string {
    return this.specialties.find(s => s.id === id)?.name ?? 'Unknown';
  }

  saveDoctor(): void {
    const name = this.fullName.trim();

    if (!name) {
      this.message = 'Please enter the doctor name.';
      return;
    }

    if (this.specialtyId === null) {
      this.message = 'Please select a specialty.';
      return;
    }

    if (this.experienceYears !== null && this.experienceYears < 0) {
      this.message = 'Experience years cannot be negative.';
      return;
    }

    if (this.consultationFee !== null && this.consultationFee < 0) {
      this.message = 'Consultation fee cannot be negative.';
      return;
    }

    const doctorData: Partial<AdminDoctor> = {
      fullName: name,
      specialtyId: this.specialtyId,
      qualifications: this.qualifications.trim(),
      experienceYears: this.experienceYears,
      consultationFee: this.consultationFee,
      photoUrl: this.photoUrl.trim(),
      bio: this.bio.trim(),
      isActive: this.editingId === null
        ? 1
        : this.doctors.find(d => d.id === this.editingId)?.isActive ?? 1
    };

    this.isLoading = true;

    if (this.editingId !== null) {
      this.api.updateDoctor(this.editingId, doctorData).subscribe({
        next: () => {
          this.message = 'Doctor updated successfully.';
          this.resetForm();
          this.loadData();
        },
        error: (error) => {
          this.message = 'Could not update doctor.';
          this.isLoading = false;
          console.error(error);
        }
      });
    } else {
      this.api.addDoctor(doctorData).subscribe({
        next: () => {
          this.message = 'Doctor added successfully.';
          this.resetForm();
          this.loadData();
        },
        error: (error) => {
          this.message = 'Could not add doctor.';
          this.isLoading = false;
          console.error(error);
        }
      });
    }
  }

  editDoctor(doctor: AdminDoctor): void {
    this.editingId = doctor.id;
    this.fullName = doctor.fullName;
    this.specialtyId = doctor.specialtyId;
    this.qualifications = doctor.qualifications ?? '';
    this.experienceYears = doctor.experienceYears ?? null;
    this.consultationFee = doctor.consultationFee ?? null;
    this.photoUrl = doctor.photoUrl ?? '';
    this.bio = doctor.bio ?? '';
    this.message = '';
  }

  deleteDoctor(id: number): void {
    const doctor = this.doctors.find(d => d.id === id);

    if (!doctor) {
      return;
    }

    if (!confirm(`Are you sure you want to delete ${doctor.fullName}?`)) {
      return;
    }

    this.isLoading = true;

    this.api.deleteDoctor(id).subscribe({
      next: () => {
        this.message = 'Doctor deleted successfully.';

        if (this.editingId === id) {
          this.resetForm();
        }

        this.loadData();
      },
      error: (error) => {
        this.message = 'Could not delete doctor.';
        this.isLoading = false;
        console.error(error);
      }
    });
  }

  toggleStatus(doctor: AdminDoctor): void {
    const newStatus = doctor.isActive === 1 ? 0 : 1;

    this.api.updateDoctor(doctor.id, {
      fullName: doctor.fullName,
      specialtyId: doctor.specialtyId,
      qualifications: doctor.qualifications,
      experienceYears: doctor.experienceYears,
      consultationFee: doctor.consultationFee,
      photoUrl: doctor.photoUrl,
      bio: doctor.bio,
      isActive: newStatus
    }).subscribe({
      next: () => {
        doctor.isActive = newStatus;
        this.message = 'Doctor status updated successfully.';
      },
      error: (error) => {
        this.message = 'Could not update doctor status.';
        console.error(error);
      }
    });
  }

  resetForm(): void {
    this.fullName = '';
    this.specialtyId = null;
    this.qualifications = '';
    this.experienceYears = null;
    this.consultationFee = null;
    this.photoUrl = '';
    this.bio = '';
    this.editingId = null;
  }
}
