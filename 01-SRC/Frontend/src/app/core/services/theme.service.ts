import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';

export type ThemePreference = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'magic-color-scheme';

const PREFERENCE_TO_META: Record<ThemePreference, 'light dark' | 'light' | 'dark'> = {
  system: 'light dark',
  light: 'light',
  dark: 'dark',
};

const META_TO_PREFERENCE: Record<string, ThemePreference> = {
  'light dark': 'system',
  light: 'light',
  dark: 'dark',
};

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly preferenceSignal = signal<ThemePreference>(this.readStoredPreference());

  readonly preference = this.preferenceSignal.asReadonly();
  readonly metaContent = computed(() => PREFERENCE_TO_META[this.preferenceSignal()]);

  constructor() {
    this.syncMetaElement(this.metaContent());

    const win = this.document.defaultView;
    if (win) {
      const onStorage = (event: StorageEvent): void => {
        if (event.key !== STORAGE_KEY) {
          return;
        }
        const next = META_TO_PREFERENCE[event.newValue ?? ''] ?? 'system';
        this.preferenceSignal.set(next);
        this.syncMetaElement(PREFERENCE_TO_META[next]);
      };

      win.addEventListener('storage', onStorage);
      this.destroyRef.onDestroy(() => win.removeEventListener('storage', onStorage));
    }
  }

  setPreference(preference: ThemePreference): void {
    const metaValue = PREFERENCE_TO_META[preference];
    this.preferenceSignal.set(preference);
    this.syncMetaElement(metaValue);
    this.persistMetaValue(metaValue);
  }

  private readStoredPreference(): ThemePreference {
    try {
      const stored = this.document.defaultView?.localStorage?.getItem(STORAGE_KEY) ?? null;
      return stored ? (META_TO_PREFERENCE[stored] ?? 'system') : 'system';
    } catch {
      return 'system';
    }
  }

  private persistMetaValue(metaValue: string): void {
    try {
      this.document.defaultView?.localStorage?.setItem(STORAGE_KEY, metaValue);
    } catch {
      // Ignore storage failures when private mode or hardened WebView blocks persistence.
    }
  }

  private syncMetaElement(metaValue: string): void {
    const meta = this.document.querySelector('meta[name="color-scheme"]');
    if (meta) {
      meta.setAttribute('content', metaValue);
    }
  }
}

