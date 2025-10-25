import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router,
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { CurrentOrganizationalUnitService } from '@services/current-organizational-unit.service';

@Injectable({
  providedIn: 'root',
})
export class UnitAccessGuard implements CanActivate {
  constructor(
    private currentUnitService: CurrentOrganizationalUnitService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    // Check if current unit is set
    const currentUnit = this.currentUnitService.getCurrentUnit();

    if (currentUnit && currentUnit.unitId) {
      return of(true);
    }

    // Load current unit from API
    return this.currentUnitService.loadCurrentUnit().pipe(
      map((unit) => {
        if (unit && unit.unitId) {
          return true;
        }

        // No unit available, redirect to error page
        this.router.navigate(['/no-unit-access']);
        return false;
      }),
      catchError(() => {
        // Error loading unit, redirect to error page
        this.router.navigate(['/no-unit-access']);
        return of(false);
      })
    );
  }
}
