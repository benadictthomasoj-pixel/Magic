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
});
