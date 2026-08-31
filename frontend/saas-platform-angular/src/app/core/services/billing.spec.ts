import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { BillingService } from './billing';

describe('BillingService', () => {
  let service: BillingService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient()],
    });
    service = TestBed.inject(BillingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
