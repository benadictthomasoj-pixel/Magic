import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { ProductsListComponent } from './products-list.component';

describe('ProductsListComponent', () => {
  it('initializes and renders catalog page header', async () => {
    await TestBed.configureTestingModule({
      imports: [ProductsListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductsListComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Product & Pricing Catalog');
    expect(element.querySelector('.scanner-toolbar')).toBeTruthy();
    expect(element.querySelector('.filter-toolbar')).toBeTruthy();
  });

  it('renders quick barcode scan input for testing lookup', async () => {
    await TestBed.configureTestingModule({
      imports: [ProductsListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ProductsListComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const scannerInput = element.querySelector('.scanner-input') as HTMLInputElement;
    expect(scannerInput).toBeTruthy();
    expect(scannerInput.placeholder).toContain('Scan or enter Barcode');
  });
});
