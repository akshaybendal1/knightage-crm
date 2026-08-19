import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { Lead, PipelineStage } from '../../core/models';

@Component({
  selector: 'app-leads',
  imports: [FormsModule, RouterLink],
  templateUrl: './leads.html',
  styleUrl: './leads.css',
})
export class Leads implements OnInit {
  stages = signal<PipelineStage[]>([]);
  leads = signal<Lead[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  filterStageId = '';

  // Manual create form
  name = '';
  email = '';
  phone = '';
  company = '';
  pipelineStageId = '';
  notes = '';

  // CSV import form
  importStageId = '';
  importFile: File | null = null;
  importing = signal(false);
  importMessage = signal<string | null>(null);

  // Inline edit state
  editingId = signal<string | null>(null);
  editName = '';
  editEmail = '';
  editPhone = '';
  editCompany = '';
  editStageId = '';
  editNotes = '';

  constructor(private readonly api: CrmApi) {}

  ngOnInit(): void {
    this.api.getPipelineStages().subscribe((stages) => this.stages.set(stages));
    this.loadLeads();
  }

  stageName(id: string): string {
    return this.stages().find((s) => s.id === id)?.name ?? id;
  }

  onFilterChange(): void {
    this.loadLeads();
  }

  loadLeads(): void {
    this.loading.set(true);
    this.api.getLeads(this.filterStageId || undefined).subscribe({
      next: (leads) => {
        this.leads.set(leads);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load leads.');
        this.loading.set(false);
      },
    });
  }

  createLead(): void {
    this.errorMessage.set(null);
    if (!this.pipelineStageId) {
      this.errorMessage.set('Pick a pipeline stage.');
      return;
    }
    this.api
      .createLead({
        name: this.name,
        email: this.email || undefined,
        phone: this.phone || undefined,
        company: this.company || undefined,
        pipelineStageId: this.pipelineStageId,
        notes: this.notes || undefined,
      })
      .subscribe({
        next: () => {
          this.name = '';
          this.email = '';
          this.phone = '';
          this.company = '';
          this.notes = '';
          this.loadLeads();
        },
        error: () => this.errorMessage.set('Could not create lead.'),
      });
  }

  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.importFile = input.files?.[0] ?? null;
  }

  importLeads(): void {
    if (!this.importFile || !this.importStageId) {
      this.errorMessage.set('Pick a pipeline stage and a CSV file first.');
      return;
    }
    this.importing.set(true);
    this.errorMessage.set(null);
    this.api.importLeads(this.importFile, this.importStageId).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.importFile = null;
        const errorNote = result.errors.length ? ` (${result.errors.length} line(s) skipped)` : '';
        this.importMessage.set(`Imported ${result.importedCount} lead(s)${errorNote}.`);
        this.loadLeads();
      },
      error: () => {
        this.importing.set(false);
        this.errorMessage.set('Could not import this file.');
      },
    });
  }

  startEdit(lead: Lead): void {
    this.editingId.set(lead.id);
    this.editName = lead.name;
    this.editEmail = lead.email ?? '';
    this.editPhone = lead.phone ?? '';
    this.editCompany = lead.company ?? '';
    this.editStageId = lead.pipelineStageId;
    this.editNotes = lead.notes ?? '';
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: string): void {
    this.api
      .updateLead(id, {
        name: this.editName,
        email: this.editEmail || undefined,
        phone: this.editPhone || undefined,
        company: this.editCompany || undefined,
        pipelineStageId: this.editStageId,
        notes: this.editNotes || undefined,
      })
      .subscribe({
        next: () => {
          this.editingId.set(null);
          this.loadLeads();
        },
        error: () => this.errorMessage.set('Could not save changes.'),
      });
  }
}
