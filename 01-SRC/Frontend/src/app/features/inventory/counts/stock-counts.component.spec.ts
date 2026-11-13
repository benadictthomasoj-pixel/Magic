import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StockCountsComponent } from './stock-counts.component';

describe('StockCountsComponent', () => {
  it('initializes and renders stock count audit session table', async () => {
    await TestBed.configureTestingModule({
      imports: [StockCountsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(StockCountsComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Stock Counting');
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
