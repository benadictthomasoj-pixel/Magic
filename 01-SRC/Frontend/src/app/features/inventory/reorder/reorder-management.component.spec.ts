import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { ReorderManagementComponent } from './reorder-management.component';

describe('ReorderManagementComponent', () => {
  it('initializes and renders reorder management header and tabs', async () => {
    await TestBed.configureTestingModule({
      imports: [ReorderManagementComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(ReorderManagementComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Reorder Management');
    expect(element.querySelector('.filter-toolbar')).toBeTruthy();
  });
});
