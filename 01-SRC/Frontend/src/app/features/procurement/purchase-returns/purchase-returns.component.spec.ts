import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { PurchaseReturnsComponent } from './purchase-returns.component';

describe('PurchaseReturnsComponent', () => {
  it('initializes and renders purchase returns header and table', async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseReturnsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(PurchaseReturnsComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Purchase Returns');
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
