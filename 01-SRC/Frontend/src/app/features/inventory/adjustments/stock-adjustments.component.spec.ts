import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StockAdjustmentsComponent } from './stock-adjustments.component';

describe('StockAdjustmentsComponent', () => {
  it('initializes and renders stock adjustments header and table', async () => {
    await TestBed.configureTestingModule({
      imports: [StockAdjustmentsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(StockAdjustmentsComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Stock Adjustments');
    expect(element.querySelector('.filter-toolbar')).toBeTruthy();
  });
});
