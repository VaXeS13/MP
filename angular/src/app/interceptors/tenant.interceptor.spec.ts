import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TenantInterceptor } from './tenant.interceptor';
import { TenantService } from '../services/tenant.service';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

describe('TenantInterceptor', () => {
  let interceptor: TenantInterceptor;
  let httpMock: HttpTestingController;
  let tenantService: jasmine.SpyObj<TenantService>;

  beforeEach(() => {
    const tenantServiceSpy = jasmine.createSpyObj('TenantService', ['getCurrentTenant']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        TenantInterceptor,
        { provide: TenantService, useValue: tenantServiceSpy },
        { provide: HTTP_INTERCEPTORS, useClass: TenantInterceptor, multi: true }
      ]
    });

    interceptor = TestBed.inject(TenantInterceptor);
    httpMock = TestBed.inject(HttpTestingController);
    tenantService = TestBed.inject(TenantService) as jasmine.SpyObj<TenantService>;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });
});
