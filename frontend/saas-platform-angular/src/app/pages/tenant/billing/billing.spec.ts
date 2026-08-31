import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { Billing } from './billing';

describe('Billing', () => {
  let component: Billing;
  let fixture: ComponentFixture<Billing>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Billing],
      providers: [provideHttpClient()],
    }).compileComponents();

    fixture = TestBed.createComponent(Billing);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
