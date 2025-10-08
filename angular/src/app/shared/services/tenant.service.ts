import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface TenantInfo {
  id?: string;
  name?: string;
  isHost?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private tenantSubject = new BehaviorSubject<TenantInfo | null>(null);
  public tenant$ = this.tenantSubject.asObservable();

  constructor() {
    this.resolveTenantFromDomain();
  }

  private resolveTenantFromDomain(): void {
    const host = window.location.hostname;
    const port = window.location.port;
    
    console.log('Resolving tenant from host:', host);

    // Localhost bez subdomeny = host tenant
    if (host === 'localhost' && port === '4200') {
      this.setTenant({ isHost: true, name: 'host' });
      return;
    }

    // Subdomena na localhost
    if (host.includes('.localhost')) {
      const subdomain = host.split('.')[0];
      this.setTenant({ name: subdomain, isHost: false });
      return;
    }

    // Production domain
    const parts = host.split('.');
    if (parts.length > 2) {
      const subdomain = parts[0];
      this.setTenant({ name: subdomain, isHost: false });
      return;
    }

    // Fallback to host
    this.setTenant({ isHost: true, name: 'host' });
  }

  setTenant(tenant: TenantInfo | null): void {
    this.tenantSubject.next(tenant);
    
    // Zapisz w localStorage dla persistence
    if (tenant) {
      localStorage.setItem('__tenant', JSON.stringify(tenant));
    } else {
      localStorage.removeItem('__tenant');
    }

    console.log('Tenant set to:', tenant);
  }

  getCurrentTenant(): TenantInfo | null {
    return this.tenantSubject.value;
  }

  getCurrentTenantName(): string | null {
    const tenant = this.getCurrentTenant();
    return tenant?.name || null;
  }

  isHostTenant(): boolean {
    const tenant = this.getCurrentTenant();
    return tenant?.isHost === true;
  }

  // Helper dla dynamicznych URL-i
  getClientUrl(path: string = ''): string {
    const tenant = this.getCurrentTenant();
    const protocol = window.location.protocol;
    const port = window.location.port ? `:${window.location.port}` : '';
    
    if (tenant?.isHost) {
      return `${protocol}//localhost${port}${path}`;
    } else {
      return `${protocol}//${tenant?.name}.localhost${port}${path}`;
    }
  }
}