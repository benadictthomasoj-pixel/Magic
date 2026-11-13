import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StockTransfersComponent } from './stock-transfers.component';

describe('StockTransfersComponent', () => {
  it('initializes and renders stock transfers header and table', async () => {
    await TestBed.configureTestingModule({
      imports: [StockTransfersComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(StockTransfersComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Warehouse Transfers');
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
