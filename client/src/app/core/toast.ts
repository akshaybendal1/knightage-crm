import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  type: 'success' | 'error';
  text: string;
}

const SUCCESS_DURATION_MS = 4000;
const ERROR_DURATION_MS = 6000;

@Injectable({ providedIn: 'root' })
export class Toast {
  private nextId = 1;
  readonly messages = signal<ToastMessage[]>([]);

  success(text: string): void {
    this.push('success', text, SUCCESS_DURATION_MS);
  }

  error(text: string): void {
    this.push('error', text, ERROR_DURATION_MS);
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.id !== id));
  }

  private push(type: ToastMessage['type'], text: string, durationMs: number): void {
    const id = this.nextId++;
    this.messages.update((list) => [...list, { id, type, text }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }
}
