import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PayablesManagementComponent } from './payables-management.component';

describe('PayablesManagementComponent', () => {
  let component: PayablesManagementComponent;
  let fixture: ComponentFixture<PayablesManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayablesManagementComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PayablesManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create payables management component', () => {
    expect(component).toBeTruthy();
  });

  it('should load confirmed GRNs on supplier change (DEF-INT-003)', () => {
    (component as any).billSupplierId = '11111111-1111-1111-1111-111111111111';
    (component as any).onSupplierChange();
    expect((component as any).selectedGrnId).toBe('');
    expect((component as any).billGrnId).toBeNull();
    expect((component as any).billPoId).toBeNull();
  });

  it('should clear GRN selection when direct billing is selected', () => {
    (component as any).selectedGrnId = '';
    (component as any).onGrnSelected();
    expect((component as any).billGrnId).toBeNull();
    expect((component as any).billPoId).toBeNull();
  });
});
