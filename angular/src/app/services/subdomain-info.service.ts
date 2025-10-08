import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface SubdomainInfo {
  hasSubdomain: boolean;
  subdomain?: string;
  clientId?: string;
  clientInfo?: {
    clientId: string;
    displayName: string;
    redirectUris: string[];
    isActive: boolean;
  };
  origin?: string;
  detectionSource?: string;
  isValidClient?: boolean;
  isAuthenticated?: boolean;
  userName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SubdomainService {
  private subdomainInfo$ = new BehaviorSubject<SubdomainInfo | null>(null);

  constructor(private http: HttpClient) {
    this.loadSubdomainInfo();
  }

  private loadSubdomainInfo(): void {
    this.getSubdomainInfo().subscribe({
      next: (info) => {
        this.subdomainInfo$.next(info);
        console.log('Loaded subdomain info:', info);
      },
      error: (error) => {
        console.error('Error loading subdomain info:', error);
        this.subdomainInfo$.next(null);
      }
    });
  }

  getSubdomainInfo(): Observable<SubdomainInfo> {
    return this.http.get<SubdomainInfo>('/api/app/subdomain/info');
  }

  getSubdomainInfo$(): Observable<SubdomainInfo | null> {
    return this.subdomainInfo$.asObservable();
  }

  getDebugInfo(): Observable<any> {
    return this.http.get('/api/app/subdomain/debug');
  }

  getCurrentSubdomain(): string | null {
    const hostname = window.location.hostname;
    const parts = hostname.split('.');

    // Development: cto.localhost
    if (parts.length >= 2 && parts[1] === 'localhost') {
      return parts[0];
    }

    // Production: cto.mp.com
    if (parts.length >= 3 && parts[1] === 'mp' && parts[2] === 'com') {
      return parts[0];
    }

    return null;
  }

  getCompanyName(): string {
    const info = this.subdomainInfo$.value;
    
    if (info?.clientInfo?.displayName) {
      return info.clientInfo.displayName;
    }

    const subdomain = this.getCurrentSubdomain();
    if (!subdomain) return 'MP Application';

    // Mapowanie nazw firm
    const companyNames: { [key: string]: string } = {
      'cto': 'CTO Corporation',
      'kiss': 'KISS Solutions'
    };

    return companyNames[subdomain.toLowerCase()] || 
           subdomain.charAt(0).toUpperCase() + subdomain.slice(1);
  }

  refreshSubdomainInfo(): void {
    this.loadSubdomainInfo();
  }
}