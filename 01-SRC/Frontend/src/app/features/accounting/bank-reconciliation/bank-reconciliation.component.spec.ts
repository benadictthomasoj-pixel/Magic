import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BankReconciliationComponent } from './bank-reconciliation.component';

describe('BankReconciliationComponent', () => {
  let component: BankReconciliationComponent;
  let fixture: ComponentFixture<BankReconciliationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BankReconciliationComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BankReconciliationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create bank reconciliation component', () => {
    expect(component).toBeTruthy();
  });
});
