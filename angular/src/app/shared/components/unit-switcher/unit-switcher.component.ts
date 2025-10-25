import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { OrganizationalUnitService } from '@services/organizational-unit.service';
import { CurrentOrganizationalUnitService } from '@services/current-organizational-unit.service';
import { OrganizationalUnitDto, CurrentUnitDto } from '@proxy/organizational-units/dtos/models';

@Component({
  selector: 'app-unit-switcher',
  standalone: true,
  imports: [CommonModule, FormsModule, PrimeNGModule],
  templateUrl: './unit-switcher.component.html',
  styleUrls: ['./unit-switcher.component.scss'],
})
export class UnitSwitcherComponent implements OnInit, OnDestroy {
  units: OrganizationalUnitDto[] = [];
  currentUnit$: Observable<CurrentUnitDto | null>;
  selectedUnitId: string | null = null;
  isLoading = false;
  showSwitcher = false;

  private destroy$ = new Subject<void>();

  constructor(
    private orgUnitService: OrganizationalUnitService,
    private currentUnitService: CurrentOrganizationalUnitService
  ) {
    this.currentUnit$ = this.currentUnitService.currentUnit$;
  }

  ngOnInit(): void {
    this.loadUnits();
    this.initializeCurrentUnit();
  }

  /**
   * Load all organizational units for the current user
   */
  private loadUnits(): void {
    this.isLoading = true;
    this.orgUnitService
      .getUnits()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (units) => {
          this.units = units;
          this.showSwitcher = units.length > 1; // Only show if user has multiple units
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load organizational units', error);
          this.isLoading = false;
        },
      });
  }

  /**
   * Initialize the currently selected unit
   */
  private initializeCurrentUnit(): void {
    this.currentUnit$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (unit) => {
        if (unit) {
          this.selectedUnitId = unit.unitId || null;
        }
      },
    });
  }

  /**
   * Switch to a different unit
   */
  switchToUnit(unit: OrganizationalUnitDto): void {
    if (!unit.id) return;

    this.isLoading = true;
    this.currentUnitService.switchUnit(unit.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.selectedUnitId = unit.id || null;
        this.isLoading = false;
        // Reload page to update all components with new unit context
        window.location.reload();
      },
      error: (error) => {
        console.error('Failed to switch unit', error);
        this.isLoading = false;
      },
    });
  }

  /**
   * Get current unit name for display
   */
  getCurrentUnitName(): string {
    const currentUnit = this.currentUnitService.getCurrentUnit();
    return currentUnit?.unitName || 'No Unit';
  }

  /**
   * Get current unit code for badge
   */
  getCurrentUnitCode(): string {
    const currentUnit = this.currentUnitService.getCurrentUnit();
    return currentUnit?.unitCode?.substring(0, 1).toUpperCase() || '?';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
