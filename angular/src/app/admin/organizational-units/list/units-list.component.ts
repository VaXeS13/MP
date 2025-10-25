import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { OrganizationalUnitService } from '@services/organizational-unit.service';
import { OrganizationalUnitDto } from '@proxy/organizational-units/dtos/models';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ConfirmationService, MessageService } from 'primeng/api';
import { UnitDialogComponent } from '../dialogs/unit-dialog.component';

@Component({
  selector: 'app-units-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PrimeNGModule, UnitDialogComponent],
  templateUrl: './units-list.component.html',
  styleUrls: ['./units-list.component.scss'],
  providers: [ConfirmationService, MessageService],
})
export class UnitsListComponent implements OnInit, OnDestroy {
  units: OrganizationalUnitDto[] = [];
  isLoading = false;
  selectedUnits: OrganizationalUnitDto[] = [];
  searchText = '';
  showDialog = false;
  selectedUnit: OrganizationalUnitDto | null = null;
  isEditMode = false;

  private destroy$ = new Subject<void>();

  constructor(
    private orgUnitService: OrganizationalUnitService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService
  ) {}

  ngOnInit(): void {
    this.loadUnits();
  }

  /**
   * Load all organizational units
   */
  loadUnits(): void {
    this.isLoading = true;
    this.orgUnitService
      .getUnits()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (units) => {
          this.units = units;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load units', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load organizational units',
          });
          this.isLoading = false;
        },
      });
  }

  /**
   * Open dialog for creating new unit
   */
  createUnit(): void {
    this.selectedUnit = null;
    this.isEditMode = false;
    this.showDialog = true;
  }

  /**
   * Open dialog for editing existing unit
   */
  editUnit(unit: OrganizationalUnitDto): void {
    this.selectedUnit = unit;
    this.isEditMode = true;
    this.showDialog = true;
  }

  /**
   * Delete a unit with confirmation
   */
  deleteUnit(unit: OrganizationalUnitDto): void {
    if (!unit.id) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to delete unit "${unit.name}"?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.orgUnitService
          .deleteUnit(unit.id!)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Unit deleted successfully',
              });
              this.loadUnits();
            },
            error: (error) => {
              console.error('Failed to delete unit', error);
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to delete unit',
              });
            },
          });
      },
    });
  }

  /**
   * Handle dialog save event
   */
  onDialogSave(unit: OrganizationalUnitDto): void {
    this.showDialog = false;
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: this.isEditMode ? 'Unit updated successfully' : 'Unit created successfully',
    });
    this.loadUnits();
  }

  /**
   * Handle dialog close event
   */
  onDialogClose(): void {
    this.showDialog = false;
  }

  /**
   * Get filtered units based on search
   */
  getFilteredUnits(): OrganizationalUnitDto[] {
    if (!this.searchText) {
      return this.units;
    }
    const search = this.searchText.toLowerCase();
    return this.units.filter(
      (unit) =>
        unit.name?.toLowerCase().includes(search) ||
        unit.code?.toLowerCase().includes(search) ||
        unit.email?.toLowerCase().includes(search)
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
