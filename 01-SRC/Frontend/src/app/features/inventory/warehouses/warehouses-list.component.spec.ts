import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { WarehousesListComponent } from './warehouses-list.component';

describe('WarehousesListComponent', () => {
  it('initializes and renders warehouse management header and table', async () => {
    await TestBed.configureTestingModule({
      imports: [WarehousesListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(WarehousesListComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Warehouse Management');
    expect(element.querySelector('.filter-toolbar')).toBeTruthy();
    expect(element.querySelector('.data-table')).toBeTruthy();
  });
});
