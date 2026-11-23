
import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AdminApiService,
  AdminPatient
} from '../../services/admin-api.service';

@Component({
  selector: 'app-patient-management',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './patient-management.component.html',
  styleUrl: './patient-management.component.css'
})
export class PatientManagementComponent implements OnInit {

  patients: AdminPatient[] = [];
  searchText = '';

  fullName = '';
  email = '';
  phoneNumber = '';

  editingId: number | null = null;
  message = '';
  isLoading = false;

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading = true;

    this.adminApi.getPatients().subscribe({
      next: (data) => {
        this.patients = data;
        this.isLoading = false;
      },
      error: () => {
        this.message = 'Failed to load patients.';
        this.isLoading = false;
      }
    });
  }

  get totalPatients(): number {
    return this.patients.length;
  }

  get filteredPatients(): AdminPatient[] {
    const search = this.searchText.toLowerCase().trim();

    if (!search) {
      return this.patients;
    }

    return this.patients.filter(patient =>
      patient.fullName.toLowerCase().includes(search) ||
      patient.email.toLowerCase().includes(search) ||
      patient.phoneNumber.includes(search)
    );
  }

  savePatient(): void {
    if (
      !this.fullName.trim() ||
      !this.email.trim() ||
      !this.phoneNumber.trim()
    ) {
      this.message = 'Please fill in all required fields.';
      return;
    }

    if (!this.editingId) {
      this.message = 'Please select a patient to edit.';
      return;
    }

    const patient: Partial<AdminPatient> = {
      fullName: this.fullName.trim(),
      email: this.email.trim(),
      phoneNumber: this.phoneNumber.trim()
    };

    this.isLoading = true;

    this.adminApi.updatePatient(this.editingId, patient).subscribe({
      next: () => {
        this.message = 'Patient updated successfully.';
        this.resetForm();
        this.loadPatients();
      },
      error: (error) => {
        this.message =
          error.error || 'Failed to update patient.';
        this.isLoading = false;
      }
    });
  }

  editPatient(patient: AdminPatient): void {
    this.editingId = patient.id;
    this.fullName = patient.fullName;
    this.email = patient.email;
    this.phoneNumber = patient.phoneNumber;
    this.message = '';
  }

  deletePatient(id: number): void {
    const confirmed = confirm(
      'Are you sure you want to delete this patient?'
    );

    if (!confirmed) {
      return;
    }

    this.adminApi.deletePatient(id).subscribe({
      next: () => {
        this.message = 'Patient deleted successfully.';
        if (this.editingId === id) {
          this.resetForm();
        }
        this.loadPatients();
      },
      error: () => {
        this.message = 'Failed to delete patient.';
      }
    });
  }

  resetForm(): void {
    this.editingId = null;
    this.fullName = '';
    this.email = '';
    this.phoneNumber = '';
  }
}
