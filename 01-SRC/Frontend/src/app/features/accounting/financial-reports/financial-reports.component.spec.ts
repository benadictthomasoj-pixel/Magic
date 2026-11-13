import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FinancialReportsComponent } from './financial-reports.component';

describe('FinancialReportsComponent', () => {
  let component: FinancialReportsComponent;
  let fixture: ComponentFixture<FinancialReportsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FinancialReportsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FinancialReportsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create financial reports component', () => {
    expect(component).toBeTruthy();
  });
});
