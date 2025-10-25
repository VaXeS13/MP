import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { CurrentUnitDto } from '../proxy/organizational-units/dtos/models';
import { OrganizationalUnitService as ProxyOrganizationalUnitService } from '../proxy/organizational-units/organizational-unit.service';

const CURRENT_UNIT_STORAGE_KEY = 'mp_current_unit';

@Injectable({
  providedIn: 'root',
})
export class CurrentOrganizationalUnitService {
  private currentUnitSubject = new BehaviorSubject<CurrentUnitDto | null>(null);
  public currentUnit$: Observable<CurrentUnitDto | null> = this.currentUnitSubject.asObservable();

  constructor(private proxyService: ProxyOrganizationalUnitService) {
    this.initializeCurrentUnit();
  }

  /**
   * Initialize current unit from localStorage or API
   */
  private initializeCurrentUnit(): void {
    const storedUnit = this.getStoredUnit();
    if (storedUnit) {
      this.currentUnitSubject.next(storedUnit);
    }
  }

  /**
   * Get the current unit synchronously
   */
  getCurrentUnit(): CurrentUnitDto | null {
    return this.currentUnitSubject.getValue();
  }

  /**
   * Set the current unit
   */
  setCurrentUnit(unit: CurrentUnitDto): void {
    if (unit) {
      this.currentUnitSubject.next(unit);
      this.storeUnit(unit);
    }
  }

  /**
   * Switch to a different unit and load its data
   */
  switchUnit(unitId: string): Observable<CurrentUnitDto> {
    return this.proxyService.switchUnit(unitId).pipe(
      map((response: any) => {
        // Assuming the switch endpoint returns updated unit data
        const unitData: CurrentUnitDto = {
          unitId: response.unitId || unitId,
          unitName: response.unitName,
          unitCode: response.unitCode,
          currency: response.currency,
          userRole: response.userRole,
          settings: response.settings || {},
        };
        this.setCurrentUnit(unitData);
        return unitData;
      })
    );
  }

  /**
   * Load current unit from API endpoint
   */
  loadCurrentUnit(): Observable<CurrentUnitDto> {
    return this.proxyService.getCurrentUnit().pipe(
      map((unit: CurrentUnitDto) => {
        this.setCurrentUnit(unit);
        return unit;
      })
    );
  }

  /**
   * Clear the current unit
   */
  clearCurrentUnit(): void {
    this.currentUnitSubject.next(null);
    localStorage.removeItem(CURRENT_UNIT_STORAGE_KEY);
  }

  /**
   * Check if user has a current unit assigned
   */
  hasCurrentUnit(): boolean {
    return this.currentUnitSubject.getValue() !== null;
  }

  /**
   * Get the current unit ID
   */
  getCurrentUnitId(): string | null {
    const unit = this.currentUnitSubject.getValue();
    return unit?.unitId || null;
  }

  /**
   * Store unit in localStorage
   */
  private storeUnit(unit: CurrentUnitDto): void {
    try {
      localStorage.setItem(CURRENT_UNIT_STORAGE_KEY, JSON.stringify(unit));
    } catch (error) {
      console.error('Failed to store current unit in localStorage', error);
    }
  }

  /**
   * Retrieve unit from localStorage
   */
  private getStoredUnit(): CurrentUnitDto | null {
    try {
      const stored = localStorage.getItem(CURRENT_UNIT_STORAGE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch (error) {
      console.error('Failed to retrieve current unit from localStorage', error);
      return null;
    }
  }
}
