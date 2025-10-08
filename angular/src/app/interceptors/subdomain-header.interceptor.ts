import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable()
export class SubdomainHeaderInterceptor implements HttpInterceptor {
  private currentSubdomain: string | null = null;

  constructor() {
    this.detectSubdomain();
  }

  private detectSubdomain(): void {
    const hostname = window.location.hostname;
    const parts = hostname.split('.');

    if (environment.production) {
      // Production: cto.mp.com
      if (parts.length >= 3 && parts[1] === 'mp' && parts[2] === 'com') {
        this.currentSubdomain = parts[0];
      }
    } else {
      // Development: cto.localhost
      if (parts.length >= 2 && parts[1] === 'localhost') {
        this.currentSubdomain = parts[0];
      }
    }

    console.log('SubdomainHeaderInterceptor - Detected subdomain:', this.currentSubdomain);
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Tylko dla requestów do API
    if (!this.isApiRequest(req.url) || !this.currentSubdomain) {
      return next.handle(req);
    }

    const modifiedReq = req.clone({
      setHeaders: {
        'X-Client-Subdomain': this.currentSubdomain,
        'X-Client-Origin': window.location.origin,
        'X-Client-Host': window.location.host
      }
    });

    console.log('Adding subdomain headers:', {
      subdomain: this.currentSubdomain,
      origin: window.location.origin,
      url: req.url
    });

    return next.handle(modifiedReq);
  }

  private isApiRequest(url: string): boolean {
    // Dopasuj do Twojego API
    return url.includes('/api/') || 
           url.includes('/connect/') ||
           url.startsWith(environment.apiUrl) ||
           url.startsWith('https://localhost:44377');
  }
}