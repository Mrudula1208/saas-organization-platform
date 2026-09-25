import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { Revenue } from './revenue';

describe('Revenue', () => {
  let component: Revenue;
  let fixture: ComponentFixture<Revenue>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Revenue],
      providers: [
        provideHttpClient(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Revenue);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
