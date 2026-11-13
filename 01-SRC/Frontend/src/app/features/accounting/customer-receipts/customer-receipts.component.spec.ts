import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomerReceiptsComponent } from './customer-receipts.component';

describe('CustomerReceiptsComponent', () => {
  let component: CustomerReceiptsComponent;
  let fixture: ComponentFixture<CustomerReceiptsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerReceiptsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerReceiptsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create customer receipts component', () => {
    expect(component).toBeTruthy();
  });
});
