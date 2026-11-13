import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { SuppliersListComponent } from './suppliers-list.component';

describe('SuppliersListComponent', () => {
  it('renders suppliers directory header and register button', async () => {
    await TestBed.configureTestingModule({
      imports: [SuppliersListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(SuppliersListComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Suppliers & Vendor Master');
    expect(element.querySelector('.header-actions .btn-primary')?.textContent).toContain('Register Supplier');
  });
});

