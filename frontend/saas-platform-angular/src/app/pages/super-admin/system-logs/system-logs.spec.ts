import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemLogs } from './system-logs';

describe('SystemLogs', () => {
  let component: SystemLogs;
  let fixture: ComponentFixture<SystemLogs>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SystemLogs],
    }).compileComponents();

    fixture = TestBed.createComponent(SystemLogs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
