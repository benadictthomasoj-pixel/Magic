import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FinancialVouchersComponent } from './financial-vouchers.component';

describe('FinancialVouchersComponent', () => {
  let component: FinancialVouchersComponent;
  let fixture: ComponentFixture<FinancialVouchersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FinancialVouchersComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FinancialVouchersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create financial vouchers component', () => {
    expect(component).toBeTruthy();
  });
});
