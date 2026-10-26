import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

// Angular's default strategy only changes document.title for routes that declare a title, so a page
// without one kept the previous page's title (e.g. "Signing in…" stayed on the Dashboard after the
// platform launcher's hand-off). Pages without their own title get the app name instead.
@Injectable({ providedIn: 'root' })
export class DefaultTitleStrategy extends TitleStrategy {
  private static readonly AppTitle = 'Knightage CRM';

  constructor(private readonly title: Title) {
    super();
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.title.setTitle(this.buildTitle(snapshot) ?? DefaultTitleStrategy.AppTitle);
  }
}
