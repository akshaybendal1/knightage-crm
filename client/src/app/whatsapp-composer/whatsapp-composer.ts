import { Component, ElementRef, input, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageTemplate } from '../core/models';
import { buildWhatsAppLink } from '../core/whatsapp';
import { applyTemplatePlaceholders } from '../core/template-placeholders';

const PANEL_WIDTH = 260;

@Component({
  selector: 'app-whatsapp-composer',
  imports: [FormsModule],
  templateUrl: './whatsapp-composer.html',
  styleUrl: './whatsapp-composer.css',
})
export class WhatsappComposer {
  phone = input<string | null | undefined>(null);
  leadName = input.required<string>();
  yourName = input('');
  templates = input<MessageTemplate[]>([]);

  private readonly triggerRef = viewChild<ElementRef<HTMLButtonElement>>('trigger');

  open = signal(false);
  selectedTemplateId = signal('');
  messageText = signal('');
  panelTop = signal(0);
  panelLeft = signal(0);

  // Kill switch: file attachment is hidden for now (product decision, not a bug) --
  // flip back to true to re-enable, no other code changes needed.
  protected readonly fileAttachmentEnabled = false;

  // File attachment: wa.me links can only ever pre-fill text -- WhatsApp's click-to-chat
  // API has no file/media parameter on any platform, so this is the one place in the app
  // that can offer more than that. Where the browser supports it, the native OS share
  // sheet (navigator.share with files) bundles the file and the text together in one
  // action; everywhere else, the file is already sitting wherever the user just picked
  // it from on their own device, so the fallback is just the existing text-only link plus
  // an instruction to attach it manually inside WhatsApp -- there's nothing a "download"
  // button would add, since the file was never anywhere but already-local to begin with.
  selectedFile = signal<File | null>(null);

  // One-time capability check -- the per-file check (navigator.canShare({ files: [file] }))
  // happens at share time in shareWithFile(), since support can in principle vary by file
  // type/size, not just by browser.
  canNativeShare(): boolean {
    return typeof navigator !== 'undefined' && typeof navigator.share === 'function' && typeof navigator.canShare === 'function';
  }

  activeTemplates(): MessageTemplate[] {
    return this.templates().filter((t) => t.isActive);
  }

  private defaultMessage(): string {
    return applyTemplatePlaceholders('Hi {{leadName}}, this is {{yourName}} from Knightage Tech.', {
      leadName: this.leadName(),
      yourName: this.yourName(),
    });
  }

  toggleOpen(): void {
    const next = !this.open();
    this.open.set(next);
    if (next) {
      if (!this.messageText()) {
        this.messageText.set(this.defaultMessage());
      }
      this.positionPanel();
    }
  }

  close(): void {
    this.open.set(false);
  }

  // The panel is fixed-positioned (not absolute-within-the-row) specifically
  // so it can't get clipped by the Leads table's `.table-responsive`
  // scroll container -- with absolute positioning, a row far down a long
  // list would open its panel below the container's visible/overflow
  // bounds and it would be unreachable. Computed here rather than in CSS
  // since it needs the trigger's actual on-screen position, and clamped
  // horizontally so it never runs off the right edge of the viewport.
  private positionPanel(): void {
    const trigger = this.triggerRef()?.nativeElement;
    if (!trigger) {
      return;
    }
    const rect = trigger.getBoundingClientRect();
    const left = Math.min(rect.left, window.innerWidth - PANEL_WIDTH - 12);
    this.panelTop.set(rect.bottom + 6);
    this.panelLeft.set(Math.max(12, left));
  }

  onTemplateChange(): void {
    const id = this.selectedTemplateId();
    if (!id) {
      this.messageText.set(this.defaultMessage());
      return;
    }
    const template = this.activeTemplates().find((t) => t.id === id);
    if (template) {
      this.messageText.set(applyTemplatePlaceholders(template.body, { leadName: this.leadName(), yourName: this.yourName() }));
    }
  }

  sendLink(): string | null {
    return buildWhatsAppLink(this.phone(), this.messageText());
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  clearFile(): void {
    this.selectedFile.set(null);
  }

  // Must be the first thing this method does, called directly from the button's (click) --
  // no await/async gap before calling navigator.share(), or the browser's "was this a real
  // user gesture" check can silently reject it (a rejected/cancelled share is not an error,
  // just handled quietly below).
  shareWithFile(): void {
    const file = this.selectedFile();
    if (!file || !navigator.canShare({ files: [file] })) {
      return;
    }
    navigator.share({ files: [file], text: this.messageText() }).catch(() => {
      // AbortError (user cancelled the share sheet) and any other failure are both a
      // silent no-op here -- the text-only wa.me link stays available regardless.
    });
  }
}
