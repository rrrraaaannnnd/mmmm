

import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

interface ApiSpecialty {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
}

interface ApiDoctor {
  id: number;
  specialtyId: number;
}

interface Specialty {
  id: number;
  name: string;
  description: string;
  doctorsCount: number;
  active: boolean;
}

@Component({
  selector: 'app-specialty-management',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './specialty-management.component.html',
  styleUrl: './specialty-management.component.css'
})
export class SpecialtyManagementComponent implements OnInit {

  private http = inject(HttpClient);

  private readonly apiUrl =
    'https://localhost:7157/api/admin';

  searchText = '';
  specialtyName = '';
  specialtyDescription = '';
  editingId: number | null = null;
  message = '';
  isLoading = false;

  specialties: Specialty[] = [];

  ngOnInit(): void {
    this.loadSpecialties();
  }

  loadSpecialties(): void {
    this.isLoading = true;

    forkJoin({
      specialties: this.http.get<ApiSpecialty[]>(
        `${this.apiUrl}/specialties`
      ),
      doctors: this.http.get<ApiDoctor[]>(
        `${this.apiUrl}/doctors`
      )
    }).subscribe({
      next: ({ specialties, doctors }) => {
        this.specialties = specialties.map(s => ({
          id: s.id,
          name: s.name,
          description: s.description ?? '',
          active: s.isActive,
          doctorsCount: doctors.filter(
            d => d.specialtyId === s.id
          ).length
        }));

        this.isLoading = false;
      },
      error: (error) => {
        console.error('Loading specialties failed:', error);
        this.message =
          'Unable to load specialties from the API.';
        this.isLoading = false;
      }
    });
  }

  get totalSpecialties(): number {
    return this.specialties.length;
  }

  get activeCount(): number {
    return this.specialties.filter(s => s.active).length;
  }

  get totalDoctors(): number {
    return this.specialties.reduce(
      (total, s) => total + s.doctorsCount,
      0
    );
  }

  get filteredSpecialties(): Specialty[] {
    const query = this.searchText.trim().toLowerCase();

    return this.specialties.filter(s =>
      s.name.toLowerCase().includes(query) ||
      s.description.toLowerCase().includes(query)
    );
  }

  saveSpecialty(): void {
    if (this.isLoading) return;

    const name = this.specialtyName.trim();
    const description = this.specialtyDescription.trim();

    if (!name) {
      this.message = 'Please enter a specialty name.';
      return;
    }

    const duplicate = this.specialties.some(
      s => s.name.toLowerCase() === name.toLowerCase()
        && s.id !== this.editingId
    );

    if (duplicate) {
      this.message = 'This specialty already exists.';
      return;
    }

    const data = {
      name,
      description,
      isActive: true
    };

    this.isLoading = true;
    this.message = '';

    const request = this.editingId === null
      ? this.http.post(
          `${this.apiUrl}/specialties`, data
        )
      : this.http.put(
          `${this.apiUrl}/specialties/${this.editingId}`,
          data
        );

    const isEditing = this.editingId !== null;

    request.subscribe({
      next: () => {
        this.message = isEditing
          ? 'Specialty updated successfully.'
          : 'Specialty added successfully.';

        this.resetForm();
        this.loadSpecialties();
      },
      error: (error) => {
        console.error('Saving specialty failed:', error);

        this.message = error.status === 409
          ? 'This specialty name already exists.'
          : 'Unable to save specialty.';

        this.isLoading = false;
      }
    });
  }

  editSpecialty(specialty: Specialty): void {
    this.editingId = specialty.id;
    this.specialtyName = specialty.name;
    this.specialtyDescription = specialty.description;
    this.message = '';
  }

  deleteSpecialty(id: number): void {
    if (this.isLoading) return;

    const specialty = this.specialties.find(s => s.id === id);

    if (!specialty) return;

    if (specialty.doctorsCount > 0) {
      this.message =
        'Cannot delete a specialty assigned to doctors.';
      return;
    }

    if (!confirm(
      `Delete specialty "${specialty.name}"?`
    )) {
      return;
    }

    this.isLoading = true;
    this.message = '';

    this.http.delete(
      `${this.apiUrl}/specialties/${id}`
    ).subscribe({
      next: () => {
        this.message = 'Specialty deleted successfully.';

        if (this.editingId === id) {
          this.resetForm();
        }

        this.loadSpecialties();
      },
      error: (error) => {
        console.error('Deleting specialty failed:', error);

        this.message = error.status === 409
          ? 'Cannot delete this specialty because it has related records.'
          : 'Unable to delete specialty.';

        this.isLoading = false;
      }
    });
  }

  toggleStatus(specialty: Specialty): void {
    this.message =
      'Changing specialty status is not supported by the current API.';
  }

  resetForm(): void {
    this.specialtyName = '';
    this.specialtyDescription = '';
    this.editingId = null;
  }
}
