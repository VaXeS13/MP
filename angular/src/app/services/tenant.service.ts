import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from 'src/environments/environment';

export interface TenantInfo {
  id: string | null;
  name: string | null;
  isHost: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private currentTenantSubject = new BehaviorSubject<TenantInfo>({
    id: null,
    name: null,
    isHost: true
  });

  public currentTenant$ = this.currentTenantSubject.asObservable();

  constructor(private http: HttpClient) {
    this.initializeTenant();
  }

  private initializeTenant(): void {
    const subdomain = this.getSubdomainFromUrl();
    
    if (subdomain) {
      this.setCurrentTenant({
        id: null, // ID zostanie ustawione po zalogowaniu
        name: subdomain,
        isHost: false
      });
    }
  }

  private getSubdomainFromUrl(): string | null {
    const hostname = window.location.hostname;
    
    if (hostname.includes('.localhost')) {
      const parts = hostname.split('.');
      return parts.length >= 2 && parts[0] !== 'www' ? parts[0] : null;
    } else {
      const parts = hostname.split('.');
      return parts.length >= 3 && parts[0] !== 'www' ? parts[0] : null;
    }
  }

  getCurrentTenant(): TenantInfo {
    return this.currentTenantSubject.value;
  }

  setCurrentTenant(tenant: TenantInfo): void {
    this.currentTenantSubject.next(tenant);
  }

  // Metoda do sprawdzania czy tenant istnieje w bazie
  validateTenant(tenantName: string): Observable<any> {
    return this.http.get(`${environment.apis.default.url}/api/abp/multi-tenancy/tenants/by-name/${tenantName}`);
  }

  // HTTP Interceptor helper
  getTenantHeaders(): HttpHeaders {
    const tenant = this.getCurrentTenant();
    let headers = new HttpHeaders();
    
    if (tenant.name && !tenant.isHost) {
      headers = headers.set('__tenant', tenant.name);
    }
    
    return headers;
  }
}