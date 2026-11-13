import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { CategoriesManagementComponent } from './categories-management.component';

describe('CategoriesManagementComponent', () => {
  it('renders category taxonomy view with tabs', async () => {
    await TestBed.configureTestingModule({
      imports: [CategoriesManagementComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(CategoriesManagementComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Categories & Hierarchy');
    expect(element.querySelectorAll('.tab-btn').length).toBe(3);
  });
});

