import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { UserOrganizationalUnitService } from '@proxy/controllers/user-organizational-units.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-registration-code-input',
  standalone: true,
  imports: [CommonModule, FormsModule, PrimeNGModule],
  templateUrl: './registration-code-input.component.html',
  styleUrls: ['./registration-code-input.component.scss'],
  providers: [MessageService],
})
export class RegistrationCodeInputComponent {
  code: string = '';
  isLoading = false;
  error: string | null = null;

  constructor(
    private userOrgUnitService: UserOrganizationalUnitService,
    private messageService: MessageService,
    private router: Router
  ) {}

  /**
   * Validate and join unit with registration code
   */
  validateAndJoin(): void {
    if (!this.code || this.code.trim().length === 0) {
      this.error = 'Please enter a registration code';
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.userOrgUnitService.joinUnitWithCode({ code: this.code }).subscribe({
      next: (result: any) => {
        this.isLoading = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Joined unit successfully. Redirecting...`,
        });

        // Redirect to dashboard after success
        setTimeout(() => {
          this.router.navigate(['/']);
        }, 1500);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Failed to join unit', error);

        // Handle different error scenarios
        if (error.status === 400) {
          this.error = 'Invalid or expired registration code';
        } else if (error.status === 404) {
          this.error = 'Code not found';
        } else if (error.status === 409) {
          this.error = 'Code usage limit reached';
        } else {
          this.error = 'Failed to join unit. Please try again.';
        }
      },
    });
  }

  /**
   * Handle Enter key press
   */
  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.validateAndJoin();
    }
  }

  /**
   * Clear error message
   */
  clearError(): void {
    this.error = null;
  }
}
