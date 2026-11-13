import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ReceivablesManagementComponent } from './receivables-management.component';

describe('ReceivablesManagementComponent', () => {
  let component: ReceivablesManagementComponent;
  let fixture: ComponentFixture<ReceivablesManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReceivablesManagementComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReceivablesManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create receivables management component', () => {
    expect(component).toBeTruthy();
  });
});
