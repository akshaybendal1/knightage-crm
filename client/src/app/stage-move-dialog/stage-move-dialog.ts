import { Component, ElementRef, effect, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface StageMoveDialogItem {
  id: string;
  label: string;
  sublabel?: string | null;
}

/** One entry per item being moved; note is '' when the user left it blank. */
export interface StageMoveDialogResult {
  items: { id: string; note: string }[];
}

type NoteMode = 'same' | 'each';

/**
 * Asks for an optional note before a lead (or product line) is moved to another stage, and --
 * when several are moved at once -- lets the user write one note for all of them or a
 * different note for each. It only collects the notes; the caller performs the move, and
 * decides what a cancel means (e.g. putting a dragged card back).
 */
@Component({
  selector: 'app-stage-move-dialog',
  imports: [FormsModule],
  templateUrl: './stage-move-dialog.html',
  styleUrl: './stage-move-dialog.css',
  host: { '(document:keydown.escape)': 'onEscape()' },
})
export class StageMoveDialog {
  open = input(false);
  noun = input<'lead' | 'product'>('lead');
  stageName = input('');
  items = input<StageMoveDialogItem[]>([]);
  /** True while the caller is saving the move, so the buttons can't be pressed twice. */
  busy = input(false);

  confirmed = output<StageMoveDialogResult>();
  closed = output<void>();

  readonly maxNoteLength = 1000;

  mode = signal<NoteMode>('same');
  sharedNote = signal('');
  perItemNotes = signal<Record<string, string>>({});

  private firstField = viewChild<ElementRef<HTMLTextAreaElement>>('firstField');

  constructor() {
    // Every opening starts clean, so a note typed for one move can't leak into the next one.
    effect(() => {
      if (this.open()) {
        this.mode.set('same');
        this.sharedNote.set('');
        this.perItemNotes.set({});
      }
    });
    // Put the cursor in the note box as soon as the dialog appears.
    effect(() => {
      const field = this.firstField();
      if (field) {
        queueMicrotask(() => field.nativeElement.focus());
      }
    });
  }

  title(): string {
    const items = this.items();
    if (items.length === 1) {
      return `Move ${items[0].label} to ${this.stageName()}`;
    }
    return `Move ${items.length} ${this.noun()}s to ${this.stageName()}`;
  }

  noteFor(id: string): string {
    return this.perItemNotes()[id] ?? '';
  }

  setNoteFor(id: string, value: string): void {
    this.perItemNotes.update((notes) => ({ ...notes, [id]: value }));
  }

  confirm(): void {
    if (this.busy()) {
      return;
    }
    const shared = this.sharedNote().trim();
    const each = this.mode() === 'each';
    this.confirmed.emit({
      items: this.items().map((item) => ({
        id: item.id,
        note: (each ? this.noteFor(item.id) : shared).trim(),
      })),
    });
  }

  cancel(): void {
    if (!this.busy()) {
      this.closed.emit();
    }
  }

  onEscape(): void {
    if (this.open()) {
      this.cancel();
    }
  }
}
