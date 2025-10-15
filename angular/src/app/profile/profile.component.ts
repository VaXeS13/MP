import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { UserProfileService, UserProfileDto } from '../proxy/account';
import { AuthService, ConfigStateService } from '@abp/ng.core';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ]
})
export class ProfileComponent implements OnInit {
  profileForm: FormGroup;
  isLoading = false;
  isSaving = false;
  currentUser$: Observable<any>;
  currentUser: any;
  successMessage: string = '';
  errorMessage: string = '';

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService,
    private authService: AuthService,
    private configState: ConfigStateService
  ) {
    this.profileForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(50)]],
      surname: ['', [Validators.required, Validators.maxLength(50)]],
      email: [{ value: '', disabled: true }, [Validators.email]],
      bankAccountNumber: ['', [
        Validators.maxLength(50),
        Validators.pattern(/^(PL)?\d{26}$|^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$/)
      ]]
    });
  }

  ngOnInit(): void {
    // Get current user observable
    this.currentUser$ = this.configState.getOne$('currentUser');
    this.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.userProfileService.get().subscribe({
      next: (profile: UserProfileDto) => {
        this.profileForm.patchValue({
          name: profile.name || '',
          surname: profile.surname || '',
          email: profile.email || '',
          bankAccountNumber: profile.bankAccountNumber || ''
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading profile:', error);
        this.errorMessage = 'Nie udało się załadować danych profilu';
        setTimeout(() => this.errorMessage = '', 5000);
        this.isLoading = false;
      }
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.markFormGroupTouched(this.profileForm);
      return;
    }

    this.isSaving = true;
    const profileData: UserProfileDto = this.profileForm.value;

    this.userProfileService.update(profileData).subscribe({
      next: (updatedProfile: UserProfileDto) => {
        this.successMessage = 'Profil został zaktualizowany pomyślnie';
        setTimeout(() => this.successMessage = '', 5000);
        this.isSaving = false;
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.errorMessage = 'Nie udało się zaktualizować profilu';
        setTimeout(() => this.errorMessage = '', 5000);
        this.isSaving = false;
      }
    });
  }

  // Helper method to mark all controls as touched
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getErrorMessage(controlName: string): string {
    const control = this.profileForm.get(controlName);
    if (!control || !control.errors) return '';

    if (control.errors['required']) return 'To pole jest wymagane';
    if (control.errors['email']) return 'Nieprawidłowy format adresu email';
    if (control.errors['maxlength']) return `Maksymalna długość to ${control.errors['maxlength'].requiredLength} znaków`;
    if (control.errors['pattern']) return 'Nieprawidłowy format numeru konta bankowego (26 cyfr lub format IBAN)';

    return 'Pole zawiera błędy';
  }

  get username(): string {
    return this.currentUser?.userName || this.currentUser?.username || '';
  }
}