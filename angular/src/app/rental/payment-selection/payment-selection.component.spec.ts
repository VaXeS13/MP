import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PaymentSelectionComponent } from './payment-selection.component';

describe('PaymentSelectionComponent', () => {
  let component: PaymentSelectionComponent;
  let fixture: ComponentFixture<PaymentSelectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [PaymentSelectionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PaymentSelectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
