import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CrmApi } from '../../../core/crm-api';
import { IdentityApi } from '../../../core/identity-api';
import { LeadTask, Meeting } from '../../../core/models';
import { buildWhatsAppLink } from '../../../core/whatsapp';
import { isOverdue } from '../../../core/task-status';
import { Loader } from '../../../loader/loader';

const REMINDER_WINDOW_HOURS = 24;

// Self-contained Dashboard widget: the current user's own tasks due soon + meetings
// starting soon, each with a manual "remind myself on WhatsApp" link (no automatic
// WhatsApp sending exists in this app -- see ReminderDigestService for the automatic
// email side of this same feature). Fetches its own data rather than being passed any,
// matching how Tasks/Calendar pages already self-fetch.
@Component({
  selector: 'app-upcoming-reminders',
  imports: [DatePipe, Loader],
  templateUrl: './upcoming-reminders.html',
})
export class UpcomingReminders implements OnInit {
  tasks = signal<LeadTask[]>([]);
  meetings = signal<Meeting[]>([]);
  myPhoneNumber = signal<string | null>(null);
  loading = signal(true);

  readonly isOverdue = isOverdue;

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
  ) {}

  ngOnInit(): void {
    let pending = 3;
    const settle = () => {
      pending--;
      if (pending === 0) {
        this.loading.set(false);
      }
    };

    this.api.getMyTasks('Open', false, 1, 5).subscribe({
      next: (result) => {
        this.tasks.set(result.items);
        settle();
      },
      error: settle,
    });

    this.api.getMeetingsMine(REMINDER_WINDOW_HOURS).subscribe({
      next: (meetings) => {
        this.meetings.set(meetings);
        settle();
      },
      error: settle,
    });

    this.identityApi.getMe().subscribe({
      next: (user) => {
        this.myPhoneNumber.set(user.phoneNumber ?? null);
        settle();
      },
      error: settle,
    });
  }

  hasReminders(): boolean {
    return this.tasks().length > 0 || this.meetings().length > 0;
  }

  taskWhatsAppLink(task: LeadTask): string | null {
    const dueLabel = this.isOverdue(task) ? 'was due' : 'is due';
    const dueDate = new Date(task.dueDate).toLocaleDateString();
    return buildWhatsAppLink(this.myPhoneNumber(), `Reminder: "${task.title}" (${task.leadName ?? 'a lead'}) ${dueLabel} ${dueDate}.`);
  }

  meetingWhatsAppLink(meeting: Meeting): string | null {
    const startsAt = new Date(meeting.startAtUtc).toLocaleString();
    return buildWhatsAppLink(this.myPhoneNumber(), `Reminder: "${meeting.title}" (${meeting.leadName ?? 'a lead'}) starts ${startsAt}.`);
  }
}
