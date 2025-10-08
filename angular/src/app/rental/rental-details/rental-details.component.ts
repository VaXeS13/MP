import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { RentalDto, RentalStatus } from '../../shared/models/rental.model';
import { RentalService } from '../../services/rental.service';

@Component({
  selector: 'app-rental-details',
  templateUrl: './rental-details.component.html',
  styleUrls: ['./rental-details.component.scss'],
  standalone: false
})
export class RentalDetailsComponent implements OnInit {
  rental: RentalDto | null = null;
  loading = false;
  rentalId: string = '';

  // Status enum for template
  RentalStatus = RentalStatus;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private rentalService: RentalService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.rentalId = params['id'];
      if (this.rentalId) {
        this.loadRental();
      }
    });
  }

  loadRental(): void {
    this.loading = true;
    this.rentalService.get(this.rentalId).subscribe({
      next: (rental) => {
        this.rental = rental;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading rental:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load rental details'
        });
        this.loading = false;
        this.router.navigate(['/rentals/my-rentals']);
      }
    });
  }

  getStatusClass(status: RentalStatus): string {
    switch (status) {
      case RentalStatus.Draft: return 'badge bg-secondary';
      case RentalStatus.Active: return 'badge bg-success';
      case RentalStatus.Extended: return 'badge bg-info';
      case RentalStatus.Expired: return 'badge bg-warning';
      case RentalStatus.Cancelled: return 'badge bg-danger';
      default: return 'badge bg-light text-dark';
    }
  }

  getStatusIcon(status: RentalStatus): string {
    switch (status) {
      case RentalStatus.Draft: return 'fas fa-edit';
      case RentalStatus.Active: return 'fas fa-play-circle';
      case RentalStatus.Extended: return 'fas fa-clock';
      case RentalStatus.Expired: return 'fas fa-exclamation-triangle';
      case RentalStatus.Cancelled: return 'fas fa-times-circle';
      default: return 'fas fa-question-circle';
    }
  }

  canManageItems(): boolean {
    return this.rental?.status === RentalStatus.Active || this.rental?.status === RentalStatus.Extended;
  }

  canStartRental(): boolean {
    return this.rental?.status === RentalStatus.Draft && !this.rental?.startedAt;
  }

  canCompleteRental(): boolean {
    return (this.rental?.status === RentalStatus.Active || this.rental?.status === RentalStatus.Extended)
           && this.rental?.startedAt && !this.rental?.completedAt;
  }

  startRental(): void {
    if (!this.rental) return;

    this.rentalService.startRental(this.rental.id).subscribe({
      next: (updatedRental) => {
        this.rental = updatedRental;
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Rental started successfully'
        });
      },
      error: (error) => {
        console.error('Error starting rental:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to start rental'
        });
      }
    });
  }

  completeRental(): void {
    if (!this.rental) return;

    this.rentalService.completeRental(this.rental.id).subscribe({
      next: (updatedRental) => {
        this.rental = updatedRental;
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Rental completed successfully'
        });
      },
      error: (error) => {
        console.error('Error completing rental:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to complete rental'
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/rentals/my-rentals']);
  }
}