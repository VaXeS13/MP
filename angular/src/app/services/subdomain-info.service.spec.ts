import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { SubdomainService } from './subdomain-info.service';

describe('SubdomainService', () => {
  let service: SubdomainService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SubdomainService]
    });
    service = TestBed.inject(SubdomainService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
