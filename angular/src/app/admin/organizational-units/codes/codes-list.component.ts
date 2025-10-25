import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MessageService, ConfirmationService } from 'primeng/api';
import { RegistrationCodeService } from '@proxy/organizational-units/registration-code.service';
import { RegistrationCodeDto, CreateRegistrationCodeDto } from '@proxy/organizational-units/dtos/models';

@Component({
  selector: 'app-codes-list',
  standalone: true,
  imports: [CommonModule, PrimeNGModule],
  templateUrl: './codes-list.component.html',
  styleUrls: ['./codes-list.component.scss'],
  providers: [MessageService, ConfirmationService],
})
export class CodesListComponent implements OnInit, OnDestroy {
  codes: RegistrationCodeDto[] = [];
  isLoading = false;
  showDialog = false;
  generatedCode: string | null = null;

  // Form fields
  selectedRole: string | null = null;
  maxUsageCount: number | null = null;
  expirationDays: number = 30;

  private destroy$ = new Subject<void>();

  constructor(
    private codeService: RegistrationCodeService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService
  ) {}

  ngOnInit(): void {
    this.loadCodes();
  }

  /**
   * Load all registration codes
   */
  loadCodes(): void {
    this.isLoading = true;
    // Note: This assumes the API provides a method to get codes for current unit
    // Adjust based on actual API implementation
    this.codeService.getList().pipe(takeUntil(this.destroy$)).subscribe({
      next: (codes) => {
        this.codes = codes;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load codes', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load registration codes',
        });
        this.isLoading = false;
      },
    });
  }

  /**
   * Open dialog for generating new code
   */
  openGenerateDialog(): void {
    this.selectedRole = null;
    this.maxUsageCount = null;
    this.expirationDays = 30;
    this.generatedCode = null;
    this.showDialog = true;
  }

  /**
   * Generate new registration code
   */
  generateCode(): void {
    const input: CreateRegistrationCodeDto = {
      roleId: this.selectedRole || undefined,
      maxUsageCount: this.maxUsageCount || undefined,
      expirationDays: this.expirationDays,
    };

    this.codeService.create(input).pipe(takeUntil(this.destroy$)).subscribe({
      next: (response: any) => {
        this.generatedCode = response.code;
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Code generated successfully',
        });
        this.loadCodes();
      },
      error: (error) => {
        console.error('Failed to generate code', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to generate code',
        });
      },
    });
  }

  /**
   * Copy code to clipboard
   */
  copyToClipboard(code: string): void {
    navigator.clipboard.writeText(code).then(() => {
      this.messageService.add({
        severity: 'success',
        summary: 'Copied',
        detail: 'Code copied to clipboard',
      });
    });
  }

  /**
   * Revoke registration code
   */
  revokeCode(code: RegistrationCodeDto): void {
    if (!code.id) return;

    this.confirmationService.confirm({
      message: 'Are you sure you want to revoke this code?',
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.codeService.delete(code.id!).pipe(takeUntil(this.destroy$)).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Code revoked successfully',
            });
            this.loadCodes();
          },
          error: (error) => {
            console.error('Failed to revoke code', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to revoke code',
            });
          },
        });
      },
    });
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.showDialog = false;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
