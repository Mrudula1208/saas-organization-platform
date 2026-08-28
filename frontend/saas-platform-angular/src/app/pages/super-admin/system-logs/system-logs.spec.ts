import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { SystemLogs } from './system-logs';

describe('SystemLogs', () => {
  let component: SystemLogs;
  let fixture: ComponentFixture<SystemLogs>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SystemLogs],
      providers: [provideHttpClient()],
    }).compileComponents();

    fixture = TestBed.createComponent(SystemLogs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});