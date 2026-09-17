import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CrmApi } from '../../core/crm-api';
import { MessageTemplate } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-message-templates',
  imports: [FormsModule, Loader],
  templateUrl: './message-templates.html',
})
export class MessageTemplates implements OnInit {
  templates = signal<MessageTemplate[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showForm = signal(false);

  name = '';
  body = '';
  savingTemplate = signal(false);

  editingId = signal<string | null>(null);
  editName = '';
  editBody = '';
  savingEdit = signal(false);

  constructor(
    private readonly api: CrmApi,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getMessageTemplates().subscribe({
      next: (templates) => {
        this.templates.set(templates);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load message templates.');
        this.loading.set(false);
      },
    });
  }

  createTemplate(): void {
    this.errorMessage.set(null);
    this.savingTemplate.set(true);
    this.api.createMessageTemplate({ name: this.name, body: this.body }).subscribe({
      next: () => {
        this.savingTemplate.set(false);
        this.name = '';
        this.body = '';
        this.showForm.set(false);
        this.toast.success('Template created.');
        this.load();
      },
      error: () => {
        this.savingTemplate.set(false);
        this.toast.error('Could not create this template.');
      },
    });
  }

  startEdit(template: MessageTemplate): void {
    this.editingId.set(template.id);
    this.editName = template.name;
    this.editBody = template.body;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: string): void {
    this.savingEdit.set(true);
    this.api.updateMessageTemplate(id, { name: this.editName, body: this.editBody }).subscribe({
      next: () => {
        this.savingEdit.set(false);
        this.editingId.set(null);
        this.toast.success('Template updated.');
        this.load();
      },
      error: () => {
        this.savingEdit.set(false);
        this.toast.error('Could not save changes.');
      },
    });
  }

  toggleActive(template: MessageTemplate): void {
    const activating = !template.isActive;
    this.api.setMessageTemplateActive(template.id, activating).subscribe({
      next: () => {
        this.toast.success(activating ? 'Template reactivated.' : 'Template deactivated.');
        this.load();
      },
      error: () => this.toast.error('Could not update this template.'),
    });
  }
}
