import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { IdentityApi } from '../../core/identity-api';
import { Auth } from '../../core/auth';
import { TeamMember } from '../../core/models';

@Component({
  selector: 'app-team',
  imports: [FormsModule, DatePipe],
  templateUrl: './team.html',
  styleUrl: './team.css',
})
export class Team implements OnInit {
  members = signal<TeamMember[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  displayName = '';
  email = '';
  password = '';
  role: 'Admin' | 'Member' = 'Member';
  adding = signal(false);

  editingId = signal<string | null>(null);
  editRole: 'Admin' | 'Member' = 'Member';

  constructor(
    private readonly identityApi: IdentityApi,
    protected readonly auth: Auth,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.identityApi.getTeam().subscribe({
      next: (members) => {
        this.members.set(members);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load your team.');
        this.loading.set(false);
      },
    });
  }

  isSelf(member: TeamMember): boolean {
    return member.id === this.auth.currentUser()?.userId;
  }

  addMember(): void {
    this.errorMessage.set(null);
    this.adding.set(true);
    this.identityApi
      .addTeamMember({
        displayName: this.displayName.trim(),
        email: this.email.trim(),
        password: this.password,
        role: this.role,
      })
      .subscribe({
        next: () => {
          this.displayName = '';
          this.email = '';
          this.password = '';
          this.role = 'Member';
          this.adding.set(false);
          this.load();
        },
        error: (err) => {
          this.adding.set(false);
          this.errorMessage.set(err?.error?.message ?? 'Could not add this teammate.');
        },
      });
  }

  startEditRole(member: TeamMember): void {
    this.editingId.set(member.id);
    this.editRole = member.role as 'Admin' | 'Member';
  }

  cancelEditRole(): void {
    this.editingId.set(null);
  }

  saveRole(member: TeamMember): void {
    this.identityApi.updateTeamMemberRole(member.id, this.editRole).subscribe({
      next: () => {
        this.editingId.set(null);
        this.load();
      },
      error: (err) => this.errorMessage.set(err?.error?.message ?? 'Could not update this role.'),
    });
  }

  toggleActive(member: TeamMember): void {
    this.identityApi.setTeamMemberActive(member.id, !member.isActive).subscribe({
      next: () => this.load(),
      error: (err) => this.errorMessage.set(err?.error?.message ?? 'Could not update this member.'),
    });
  }
}
