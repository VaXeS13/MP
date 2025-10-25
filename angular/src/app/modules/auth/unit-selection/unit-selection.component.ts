import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { OrganizationalUnitService } from '@services/organizational-unit.service';
import { CurrentOrganizationalUnitService } from '@services/current-organizational-unit.service';
import { OrganizationalUnitDto } from '@proxy/organizational-units/dtos/models';

@Component({
  selector: 'app-unit-selection',
  standalone: true,
  imports: [CommonModule, PrimeNGModule],
  templateUrl: './unit-selection.component.html',
  styleUrls: ['./unit-selection.component.scss'],
})
export class UnitSelectionComponent implements OnInit {
  units: OrganizationalUnitDto[] = [];
  isLoading = true;
  selectedUnitId: string | null = null;

  constructor(
    private orgUnitService: OrganizationalUnitService,
    private currentUnitService: CurrentOrganizationalUnitService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserUnits();
  }

  /**
   * Load all units available to current user
   */
  private loadUserUnits(): void {
    this.orgUnitService.getUnits().subscribe({
      next: (units) => {
        this.units = units;
        this.isLoading = false;

        // If user has only one unit, select it automatically
        if (units.length === 1) {
          this.selectUnit(units[0]);
        }
      },
      error: (error) => {
        console.error('Failed to load units', error);
        this.isLoading = false;
      },
    });
  }

  /**
   * Select a unit and proceed
   */
  selectUnit(unit: OrganizationalUnitDto): void {
    if (!unit.id) return;

    const currentUnit = {
      unitId: unit.id,
      unitName: unit.name,
      unitCode: unit.code,
      currency: unit.settings?.currency,
      userRole: 'Member',
      settings: unit.settings,
    };

    this.currentUnitService.setCurrentUnit(currentUnit as any);
    this.router.navigate(['/']);
  }

  /**
   * Skip selection (use default or no unit)
   */
  skipSelection(): void {
    this.router.navigate(['/']);
  }
}
