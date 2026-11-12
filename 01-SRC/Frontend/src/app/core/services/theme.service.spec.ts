import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let doc: Document;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    doc = TestBed.inject(DOCUMENT);

    let meta = doc.querySelector('meta[name="color-scheme"]');
    if (!meta) {
      meta = doc.createElement('meta');
      meta.setAttribute('name', 'color-scheme');
      meta.setAttribute('content', 'light dark');
      doc.head.appendChild(meta);
    } else {
      meta.setAttribute('content', 'light dark');
    }
  });

  it('defaults to system preference and updates meta color-scheme cleanly', () => {
    const service = TestBed.inject(ThemeService);
    expect(service.preference()).toBe('system');
    expect(service.metaContent()).toBe('light dark');

    service.setPreference('dark');
    expect(service.preference()).toBe('dark');
    expect(doc.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe('dark');
    expect(localStorage.getItem('magic-color-scheme')).toBe('dark');

    service.setPreference('light');
    expect(doc.querySelector('meta[name="color-scheme"]')?.getAttribute('content')).toBe('light');
  });
});

