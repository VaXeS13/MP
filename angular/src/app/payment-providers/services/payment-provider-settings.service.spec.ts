import { TestBed } from '@angular/core/testing';

import { PaymentProviderSettingsService } from './payment-provider-settings.service';

describe('PaymentProviderSettingsService', () => {
  let service: PaymentProviderSettingsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PaymentProviderSettingsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
