import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { GUIDE, GuideSection } from './guide-content';

/**
 * Public user guide (/user-guide): how to use every screen, step by step. Open before or after signing in, so
 * it can be linked from the login page, the About page and the sidebar. The content lives in guide-content.ts;
 * this component only lays it out, with a search box and a table of contents.
 */
@Component({
  selector: 'app-user-guide',
  imports: [FormsModule, RouterLink],
  templateUrl: './user-guide.html',
  styleUrl: './user-guide.css',
})
export class UserGuide {
  protected readonly guide = GUIDE;
  protected readonly signedIn = inject(Auth).isAuthenticated;
  protected readonly query = signal('');

  protected readonly sections = computed(() => {
    const words = this.query().toLowerCase().split(/\s+/).filter(Boolean);
    if (words.length === 0) {
      return this.guide.sections;
    }
    return this.guide.sections.filter((section) => {
      const text = searchableText(section);
      return words.every((word) => text.includes(word));
    });
  });

  protected jumpTo(id: string): void {
    if (!id) {
      return;
    }
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}

function searchableText(section: GuideSection): string {
  return [section.title, section.where ?? '', section.summary, ...(section.steps ?? []), ...(section.tips ?? []), section.roles ?? '']
    .join(' ')
    .replace(/<[^>]+>/g, '')
    .toLowerCase();
}
