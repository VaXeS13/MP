import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubdomainTestComponent } from './subdomain-test.component';

describe('SubdomainTestComponent', () => {
  let component: SubdomainTestComponent;
  let fixture: ComponentFixture<SubdomainTestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubdomainTestComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SubdomainTestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
