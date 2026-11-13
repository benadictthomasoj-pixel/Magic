import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { InventoryStatusComponent } from './inventory-status.component';

describe('InventoryStatusComponent', () => {
  it('initializes and renders stock status header and tabs', async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryStatusComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(InventoryStatusComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Inventory Ledger & Status');
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
