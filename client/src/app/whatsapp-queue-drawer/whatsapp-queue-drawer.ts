import { Component, computed, input, output, signal } from '@angular/core';
import { buildWhatsAppLink } from '../core/whatsapp';
import { applyTemplatePlaceholders } from '../core/template-placeholders';

export interface QueueLead {
  id: string;
  name: string;
  phone?: string | null;
}

interface QueueRow {
  lead: QueueLead;
  message: string;
  link: string | null;
}

@Component({
  selector: 'app-whatsapp-queue-drawer',
  templateUrl: './whatsapp-queue-drawer.html',
  styleUrl: './whatsapp-queue-drawer.css',
})
export class WhatsappQueueDrawer {
  open = input(false);
  leads = input<QueueLead[]>([]);
  messageTemplate = input('');
  yourName = input('');

  closed = output<void>();

  openedIds = signal<Set<string>>(new Set());

  readonly resolvedRows = computed<QueueRow[]>(() =>
    this.leads()
      .filter((l) => !!l.phone)
      .map((l) => {
        const message = applyTemplatePlaceholders(this.messageTemplate(), { leadName: l.name, yourName: this.yourName() });
        return { lead: l, message, link: buildWhatsAppLink(l.phone, message) };
      }),
  );

  readonly skippedCount = computed(() => this.leads().filter((l) => !l.phone).length);

  isOpened(id: string): boolean {
    return this.openedIds().has(id);
  }

  markOpened(id: string): void {
    const next = new Set(this.openedIds());
    next.add(id);
    this.openedIds.set(next);
  }

  close(): void {
    this.openedIds.set(new Set());
    this.closed.emit();
  }
}
