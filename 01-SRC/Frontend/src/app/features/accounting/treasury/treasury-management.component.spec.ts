import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TreasuryManagementComponent } from './treasury-management.component';

describe('TreasuryManagementComponent', () => {
  let component: TreasuryManagementComponent;
  let fixture: ComponentFixture<TreasuryManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TreasuryManagementComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TreasuryManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create treasury management component', () => {
    expect(component).toBeTruthy();
  });
});
