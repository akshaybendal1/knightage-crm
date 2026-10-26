import { Component, OnInit, computed, signal } from '@angular/core';
import { CrmApi } from '../../core/crm-api';
import { ConfigurableRole, ScreenAccessMatrix, ScreenAccessRow } from '../../core/models';
import { ScreenAccess } from '../../core/screen-access';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

type Draft = Record<string, Record<ConfigurableRole, boolean>>;

// Short notes shown under a screen's name where a choice has a consequence the name doesn't convey.
const HINTS: Record<string, string> = {
  'sales-entry': 'Also needs Field Force Tracking turned on for your organization.',
  'sales-dashboard': 'Also needs Field Force Tracking turned on for your organization.',
  'sales-coverage': 'Also needs Field Force Tracking. The coverage data is blocked for other roles too.',
  reports: 'Includes custom reports. The report data is blocked for roles without access.',
  products: 'Adding and editing products is blocked for roles without access (everyone can still pick products in other screens).',
  'message-templates': 'Adding and editing templates is blocked for roles without access (everyone can still use them to message a lead).',
  team: "Always limited to the Owner and Admins: it manages other people's accounts.",
  organization: "Always limited to the Owner and Admins: it holds the organization's settings and subscription.",
  'access-control': 'Always limited to the Owner and Admins, so this screen can never lock itself away.',
  profile: 'Everyone can open their own profile.',
};

@Component({
  selector: 'app-access-control',
  imports: [Loader],
  templateUrl: './access-control.html',
  styleUrl: './access-control.css',
})
export class AccessControl implements OnInit {
  matrix = signal<ScreenAccessMatrix | null>(null);
  draft = signal<Draft>({});
  loading = signal(true);
  saving = signal(false);
  errorMessage = signal<string | null>(null);

  roles = computed<ConfigurableRole[]>(() => this.matrix()?.roles ?? []);

  groups = computed(() => {
    const out: { name: string; screens: ScreenAccessRow[] }[] = [];
    for (const row of this.matrix()?.screens ?? []) {
      let group = out.find((g) => g.name === row.group);
      if (!group) {
        group = { name: row.group, screens: [] };
        out.push(group);
      }
      group.screens.push(row);
    }
    return out;
  });

  // What the saved settings say, for comparing against the draft the user is editing.
  private saved = computed<Draft>(() => this.toDraft(this.matrix()));

  changedCount = computed(() => {
    const draft = this.draft();
    const saved = this.saved();
    let n = 0;
    for (const row of this.matrix()?.screens ?? []) {
      for (const role of this.roles()) {
        if (draft[row.key]?.[role] !== saved[row.key]?.[role]) {
          n++;
        }
      }
    }
    return n;
  });

  // Roles that would be left with no screen to open except My Profile -- allowed, but worth saying.
  emptyRoles = computed(() =>
    this.roles().filter((role) => !(this.matrix()?.screens ?? []).some((row) => row.key !== 'profile' && this.cell(row, role))),
  );

  constructor(
    private readonly api: CrmApi,
    private readonly access: ScreenAccess,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  hint(row: ScreenAccessRow): string | null {
    return HINTS[row.key] ?? null;
  }

  isLocked(row: ScreenAccessRow): boolean {
    return row.kind !== 'Configurable';
  }

  cell(row: ScreenAccessRow, role: ConfigurableRole): boolean {
    // A locked row can't be edited, so it always shows what the server says.
    return this.isLocked(row) ? row.allowed[role] : (this.draft()[row.key]?.[role] ?? row.allowed[role]);
  }

  toggle(row: ScreenAccessRow, role: ConfigurableRole): void {
    if (this.isLocked(row) || this.saving()) {
      return;
    }
    this.draft.update((d) => ({ ...d, [row.key]: { ...d[row.key], [role]: !this.cell(row, role) } }));
  }

  /** True when a cell's draft value differs from what is saved -- highlighted so edits are easy to review. */
  changed(row: ScreenAccessRow, role: ConfigurableRole): boolean {
    return !this.isLocked(row) && this.draft()[row.key]?.[role] !== this.saved()[row.key]?.[role];
  }

  isDefault(row: ScreenAccessRow, role: ConfigurableRole): boolean {
    return this.cell(row, role) === row.defaults[role];
  }

  discard(): void {
    this.draft.set(this.toDraft(this.matrix()));
  }

  resetToDefaults(): void {
    const next: Draft = {};
    for (const row of this.matrix()?.screens ?? []) {
      next[row.key] = { ...row.defaults };
    }
    this.draft.set(next);
  }

  save(): void {
    const matrix = this.matrix();
    if (!matrix || this.changedCount() === 0) {
      return;
    }
    // The server takes the complete matrix; locked rows go along unchanged and are checked there.
    const allowed: Record<string, Record<string, boolean>> = {};
    for (const role of matrix.roles) {
      allowed[role] = {};
      for (const row of matrix.screens) {
        allowed[role][row.key] = this.cell(row, role);
      }
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.api.saveScreenAccess(allowed).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Access updated.');
        this.access.refresh(); // the menu of the person saving may change too (e.g. their own role)
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Could not save the access settings.');
      },
    });
  }

  private load(): void {
    this.api.getScreenAccessMatrix().subscribe({
      next: (matrix) => {
        this.matrix.set(matrix);
        this.draft.set(this.toDraft(matrix));
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load the access settings.');
        this.loading.set(false);
      },
    });
  }

  private toDraft(matrix: ScreenAccessMatrix | null): Draft {
    const draft: Draft = {};
    for (const row of matrix?.screens ?? []) {
      draft[row.key] = { ...row.allowed };
    }
    return draft;
  }
}
