import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SupplierPaymentsComponent } from './supplier-payments.component';

describe('SupplierPaymentsComponent', () => {
  let component: SupplierPaymentsComponent;
  let fixture: ComponentFixture<SupplierPaymentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupplierPaymentsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SupplierPaymentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create supplier payments component', () => {
    expect(component).toBeTruthy();
  });
});
