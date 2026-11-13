import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { GoodsReceiptsComponent } from './goods-receipts.component';

describe('GoodsReceiptsComponent', () => {
  it('initializes and renders goods receipt notes header and table', async () => {
    await TestBed.configureTestingModule({
      imports: [GoodsReceiptsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(GoodsReceiptsComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Goods Receipt Notes');
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
