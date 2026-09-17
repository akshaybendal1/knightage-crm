import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageTemplate } from '../core/models';

const DEFAULT_MESSAGE = 'Hi {{leadName}}, this is {{yourName}} from Knightage Tech.';

@Component({
  selector: 'app-whatsapp-bulk-picker',
  imports: [FormsModule],
  templateUrl: './whatsapp-bulk-picker.html',
  styleUrl: './whatsapp-bulk-picker.css',
})
export class WhatsappBulkPicker {
  open = input(false);
  templates = input<MessageTemplate[]>([]);
  selectedCount = input(0);

  confirmed = output<string>();
  closed = output<void>();

  selectedTemplateId = signal('');
  messageText = signal(DEFAULT_MESSAGE);

  activeTemplates(): MessageTemplate[] {
    return this.templates().filter((t) => t.isActive);
  }

  onTemplateChange(): void {
    const id = this.selectedTemplateId();
    if (!id) {
      this.messageText.set(DEFAULT_MESSAGE);
      return;
    }
    const template = this.activeTemplates().find((t) => t.id === id);
    if (template) {
      this.messageText.set(template.body);
    }
  }

  confirm(): void {
    this.confirmed.emit(this.messageText());
  }

  close(): void {
    this.closed.emit();
  }
}
