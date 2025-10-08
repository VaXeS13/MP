import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PaymentProvidersManagementComponent } from './payment-providers-management.component';

describe('PaymentProvidersManagementComponent', () => {
  let component: PaymentProvidersManagementComponent;
  let fixture: ComponentFixture<PaymentProvidersManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaymentProvidersManagementComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PaymentProvidersManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
