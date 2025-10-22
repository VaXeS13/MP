import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CustomerDashboardService } from '@proxy/application/customer-dashboard';
import { SettlementSummaryDto, SettlementItemDto } from '@proxy/application/contracts/customer-dashboard';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-settlements',
  templateUrl: './settlements.component.html',
  styleUrls: ['./settlements.component.scss'],
  standalone: false
})
export class SettlementsComponent implements OnInit, OnDestroy {
  settlementSummary: SettlementSummaryDto | null = null;
  settlements: SettlementItemDto[] = [];
  loading = false;
  settlementsLoading = false;
  displayRequestDialog = false;

  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private customerDashboardService: CustomerDashboardService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadSettlementData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadSettlementData(): void {
    this.loading = true;
    this.settlementsLoading = true;

    // Load summary
    this.customerDashboardService.getSettlementSummary()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (summary) => {
          this.settlementSummary = summary;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading settlement summary:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować podsumowania rozliczeń'
          });
          this.loading = false;
        }
      });

    // Load settlements history
    this.customerDashboardService.getMySettlements({
      skipCount: 0,
      maxResultCount: 50,
      sorting: 'CreationTime DESC'
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.settlements = result.items || [];
          this.settlementsLoading = false;
        },
        error: (error) => {
          console.error('Error loading settlements history:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować historii rozliczeń'
          });
          this.settlementsLoading = false;
        }
      });
  }

  requestSettlement(): void {
    // Check if user has bank account
    // This will be implemented in next step with proper user profile check
    this.messageService.add({
      severity: 'warn',
      summary: 'Ostrzeżenie',
      detail: 'Proszę uzupełnić numer konta bankowego w profilu przed zgłoszeniem wypłaty',
      sticky: true
    });

    // Redirect to profile with return URL
    this.router.navigate(['/profile'], {
      queryParams: { returnUrl: '/customer-dashboard/settlements' }
    });
  }

  formatCurrency(amount: number | undefined): string {
    if (amount === undefined || amount === null) {
      amount = 0;
    }
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount);
  }

  getStatusLabel(status: string): string {
    const statusMap: { [key: string]: string } = {
      'Pending': 'Oczekujące',
      'Processing': 'W trakcie',
      'Completed': 'Wypłacone',
      'Cancelled': 'Anulowane'
    };
    return statusMap[status] || status;
  }

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Processing':
        return 'info';
      case 'Pending':
        return 'warning';
      case 'Cancelled':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  trackBySettlementId(index: number, settlement: SettlementItemDto): string {
    return settlement.id;
  }
}
