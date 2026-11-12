import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { DesktopBridgeMessage, DesktopHostCapabilitiesDto } from '../contracts/api.models';

export type DesktopHostMode = 'webview2' | 'browser';

interface WebView2Host {
  postMessage(message: string): void;
  addEventListener(type: 'message', listener: (event: { data: unknown }) => void): void;
  removeEventListener(type: 'message', listener: (event: { data: unknown }) => void): void;
}

/**
 * Safe bridge communicating with the Windows WPF WebView2 host when present,
 * degrading cleanly to standalone browser diagnostics during local dev or LAN browser use.
 */
@Injectable({ providedIn: 'root' })
export class DesktopBridgeService {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  private readonly hostModeSignal = signal<DesktopHostMode>(
    this.getWebViewHost() ? 'webview2' : 'browser'
  );
  private readonly capabilitiesSignal = signal<DesktopHostCapabilitiesDto | null>(null);

  readonly hostMode = this.hostModeSignal.asReadonly();
  readonly capabilities = this.capabilitiesSignal.asReadonly();

  constructor() {
    const webView = this.getWebViewHost();
    if (!webView) {
      return;
    }

    const onMessage = (event: { data: unknown }): void => {
      try {
        const raw = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
        const envelope = raw as DesktopBridgeMessage | null;
        if (envelope?.messageType === 'desktop.capabilities.response' && envelope.payloadJson) {
          this.capabilitiesSignal.set(
            JSON.parse(envelope.payloadJson) as DesktopHostCapabilitiesDto
          );
        }
      } catch {
        // Ignore malformed host messages without disrupting the shell.
      }
    };

    webView.addEventListener('message', onMessage);
    this.destroyRef.onDestroy(() => webView.removeEventListener('message', onMessage));
    this.requestCapabilities();
  }

  requestCapabilities(): boolean {
    const webView = this.getWebViewHost();
    if (!webView) {
      return false;
    }

    const correlationId =
      typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
        ? crypto.randomUUID()
        : `${Date.now()}`;

    const envelope: DesktopBridgeMessage = {
      messageType: 'desktop.capabilities.query',
      payloadJson: '{}',
      correlationId,
    };

    webView.postMessage(JSON.stringify(envelope));
    return true;
  }

  private getWebViewHost(): WebView2Host | null {
    const win = this.document.defaultView as
      | (Window & { chrome?: { webview?: WebView2Host } })
      | null;
    return win?.chrome?.webview ?? null;
  }
}
