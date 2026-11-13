import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { PurchaseOrdersComponent } from './purchase-orders.component';

describe('PurchaseOrdersComponent', () => {
  it('initializes and renders purchase orders header and table', async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseOrdersComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(PurchaseOrdersComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Purchase Orders');
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
