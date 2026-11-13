import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { CustomersListComponent } from './customers-list.component';

describe('CustomersListComponent', () => {
  it('renders customer directory header and filters', async () => {
    await TestBed.configureTestingModule({
      imports: [CustomersListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(CustomersListComponent);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.page-title')?.textContent).toContain('Customer Directory');
    expect(element.querySelector('.filter-toolbar')).toBeTruthy();
  });
});

