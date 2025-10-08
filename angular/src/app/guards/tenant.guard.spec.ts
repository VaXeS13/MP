import { TestBed } from '@angular/core/testing';
import { CanActivateFn } from '@angular/router';

import { tenantGuard } from './tenant.guard';

describe('tenantGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) => 
      TestBed.runInInjectionContext(() => tenantGuard(...guardParameters));

  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
