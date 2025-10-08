import { TestBed } from '@angular/core/testing';

import { BoothService } from './booth.service';

describe('BoothService', () => {
  let service: BoothService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BoothService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
