import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { TenantService } from '../services/tenant.service';

@Injectable({
  providedIn: 'root'
})
export class TenantGuard implements CanActivate {
  
  constructor(
    private tenantService: TenantService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    const tenant = this.tenantService.getCurrentTenant();
    
    // Jeśli jesteśmy na głównej domenie, pozwól na dostęp
    if (tenant.isHost) {
      return of(true);
    }

    // Sprawdź czy tenant istnieje w bazie
    return this.tenantService.validateTenant(tenant.name!).pipe(
      map(() => true),
      catchError(() => {
        // Tenant nie istnieje, przekieruj na stronę błędu
        this.router.navigate(['/tenant-not-found']);
        return of(false);
      })
    );
  }
}