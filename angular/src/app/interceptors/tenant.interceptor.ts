import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TenantService } from '../services/tenant.service';

@Injectable()
export class TenantInterceptor implements HttpInterceptor {
  
  constructor(private tenantService: TenantService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const tenant = this.tenantService.getCurrentTenant();
    
    // Dodaj header tenant tylko jeśli nie jesteśmy na głównej domenie
    if (tenant.name && !tenant.isHost) {
      const tenantRequest = req.clone({
        setHeaders: {
          '__tenant': tenant.name
        }
      });
      
      return next.handle(tenantRequest);
    }
    
    return next.handle(req);
  }
}