import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SubdomainHeaderInterceptor } from './subdomain-header.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

describe('SubdomainHeaderInterceptor', () => {
  let interceptor: SubdomainHeaderInterceptor;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        SubdomainHeaderInterceptor,
        { provide: HTTP_INTERCEPTORS, useClass: SubdomainHeaderInterceptor, multi: true }
      ]
    });

    interceptor = TestBed.inject(SubdomainHeaderInterceptor);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });
});
