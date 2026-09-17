import { Component, Injector, OnInit, afterNextRender, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { Auth } from '../../core/auth';
import { LeadTask, OrgUser } from '../../core/models';
import { dueDateOnly, isOverdue, isDueToday } from '../../core/task-status';
import { Loader } from '../../loader/loader';

type ChipStatus = 'overdue' | 'due-today' | 'upcoming' | 'completed';

function dateKey(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function buildGrid(month: Date): Date[] {
  const year = month.getFullYear();
  const monthIndex = month.getMonth();
  const firstOfMonth = new Date(year, monthIndex, 1);
  const gridStart = new Date(year, monthIndex, 1 - firstOfMonth.getDay());
  const days: Date[] = [];
  for (let i = 0; i < 42; i++) {
    const day = new Date(gridStart);
    day.setDate(gridStart.getDate() + i);
    days.push(day);
  }
  return days;
}

@Component({
  selector: 'app-calendar',
  imports: [DatePipe, Loader],
  templateUrl: './calendar.html',
  styleUrl: './calendar.css',
})
export class Calendar implements OnInit {
  currentMonth = signal<Date>(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  viewScope = signal<'mine' | 'team'>('mine');
  tasks = signal<LeadTask[]>([]);
  orgUsers = signal<OrgUser[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  expandedDay = signal<string | null>(null);

  readonly weekdayLabels = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  gridDays = computed(() => buildGrid(this.currentMonth()));

  monthLabel = computed(() => this.currentMonth().toLocaleDateString('en-US', { month: 'long', year: 'numeric' }));

  tasksByDay = computed(() => {
    const map = new Map<string, LeadTask[]>();
    for (const task of this.tasks()) {
      const key = dateKey(dueDateOnly(task.dueDate));
      const existing = map.get(key);
      if (existing) {
        existing.push(task);
      } else {
        map.set(key, [task]);
      }
    }
    return map;
  });

  agendaDays = computed(() => {
    const map = this.tasksByDay();
    const todayKey = dateKey(new Date());
    // Today always gets an entry, even with zero tasks -- otherwise "Today" has
    // nothing to scroll to on a day with nothing due, and the button looks broken.
    return this.gridDays()
      .map((day) => ({ day, key: dateKey(day), tasks: map.get(dateKey(day)) ?? [] }))
      .filter((entry) => entry.tasks.length > 0 || entry.key === todayKey);
  });

  private readonly injector = inject(Injector);

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    protected readonly auth: Auth,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    if (this.canSeeTeam()) {
      this.identityApi.getOrganizationUsers().subscribe({
        next: (users) => this.orgUsers.set(users),
        error: () => {
          // Non-fatal: team view just falls back to showing raw user ids.
        },
      });
    }
    this.loadTasks();
  }

  canSeeTeam(): boolean {
    const role = this.auth.currentUser()?.role;
    return role === 'Owner' || role === 'Admin';
  }

  setScope(scope: 'mine' | 'team'): void {
    this.viewScope.set(scope);
    this.loadTasks();
  }

  previousMonth(): void {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
    this.loadTasks();
  }

  nextMonth(): void {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
    this.loadTasks();
  }

  goToToday(): void {
    const today = new Date();
    this.currentMonth.set(new Date(today.getFullYear(), today.getMonth(), 1));
    this.pendingScrollToToday = true;
    this.loadTasks();
  }

  // Switching to the current month re-renders the same grid/agenda in place --
  // nothing scrolls or draws the eye to today's cell on its own, so the button
  // looked like it did nothing if today was already off-screen. The scroll can't
  // happen right after currentMonth.set(): loadTasks() flips loading() to true
  // synchronously, which swaps the whole grid/agenda out for "Loading..." on the
  // very next render, so an afterNextRender scheduled here would fire against
  // that empty loading state, not the populated one. This flag defers the actual
  // scroll to loadTasks()'s success handler, once loading() is back to false and
  // the grid/agenda (and the .is-today cell in it) are genuinely in the DOM.
  private pendingScrollToToday = false;

  private scrollToToday(): void {
    afterNextRender(
      () => {
        const candidates = document.querySelectorAll<HTMLElement>('.calendar-day.is-today, .calendar-agenda-day.is-today');
        const visible = Array.from(candidates).find((el) => el.offsetParent !== null);
        visible?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      },
      { injector: this.injector },
    );
  }

  loadTasks(): void {
    const days = this.gridDays();
    const from = dateKey(days[0]);
    const to = dateKey(days[days.length - 1]);
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.getTasksCalendar(from, to, this.viewScope() === 'team').subscribe({
      next: (tasks) => {
        this.tasks.set(tasks);
        this.loading.set(false);
        if (this.pendingScrollToToday) {
          this.pendingScrollToToday = false;
          this.scrollToToday();
        }
      },
      error: () => {
        this.errorMessage.set('Could not load the calendar.');
        this.loading.set(false);
        this.pendingScrollToToday = false;
      },
    });
  }

  tasksForDay(day: Date): LeadTask[] {
    return this.tasksByDay().get(dateKey(day)) ?? [];
  }

  isCurrentMonth(day: Date): boolean {
    return day.getMonth() === this.currentMonth().getMonth();
  }

  isToday(day: Date): boolean {
    return dateKey(day) === dateKey(new Date());
  }

  isExpanded(day: Date): boolean {
    return this.expandedDay() === dateKey(day);
  }

  toggleExpand(day: Date): void {
    const key = dateKey(day);
    this.expandedDay.set(this.expandedDay() === key ? null : key);
  }

  chipStatus(task: LeadTask): ChipStatus {
    if (task.status === 'Completed') {
      return 'completed';
    }
    if (isOverdue(task)) {
      return 'overdue';
    }
    if (isDueToday(task)) {
      return 'due-today';
    }
    return 'upcoming';
  }

  assigneeLabel(userId: string): string {
    if (userId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return this.orgUsers().find((u) => u.id === userId)?.displayName ?? `User ${userId.slice(0, 8)}`;
  }

  openTask(task: LeadTask): void {
    this.router.navigate(['/leads', task.leadId]);
  }

  trackByDay(_: number, entry: { key: string }): string {
    return entry.key;
  }

  trackByTask(_: number, task: LeadTask): string {
    return task.id;
  }
}
