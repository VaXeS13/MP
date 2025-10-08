import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RentalBoothSelectionComponent } from './rental-booth-selection.component';

describe('RentalBoothSelectionComponent', () => {
  let component: RentalBoothSelectionComponent;
  let fixture: ComponentFixture<RentalBoothSelectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [RentalBoothSelectionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RentalBoothSelectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
