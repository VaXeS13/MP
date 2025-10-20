import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TenantGuard } from './tenant.guard';
import { TenantService } from '../services/tenant.service';

describe('TenantGuard', () => {
  let guard: TenantGuard;
  let tenantService: jasmine.SpyObj<TenantService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const tenantServiceSpy = jasmine.createSpyObj('TenantService', ['getCurrentTenant', 'validateTenant']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        TenantGuard,
        { provide: TenantService, useValue: tenantServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(TenantGuard);
    tenantService = TestBed.inject(TenantService) as jasmine.SpyObj<TenantService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });
});
