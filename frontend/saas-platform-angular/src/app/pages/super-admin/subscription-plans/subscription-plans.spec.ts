import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubscriptionPlans } from './subscription-plans';

describe('SubscriptionPlans', () => {
  let component: SubscriptionPlans;
  let fixture: ComponentFixture<SubscriptionPlans>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubscriptionPlans],
    }).compileComponents();

    fixture = TestBed.createComponent(SubscriptionPlans);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
