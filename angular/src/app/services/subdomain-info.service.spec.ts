import { TestBed } from '@angular/core/testing';

import { SubdomainInfoService } from './subdomain-info.service';

describe('SubdomainInfoService', () => {
  let service: SubdomainInfoService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SubdomainInfoService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
