import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyBoothsComponent } from './my-booths.component';

describe('MyBoothsComponent', () => {
  let component: MyBoothsComponent;
  let fixture: ComponentFixture<MyBoothsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MyBoothsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MyBoothsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});