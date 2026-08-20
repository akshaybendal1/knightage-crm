import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Lead, LeadActivity, LeadTask, PagedActivities, PipelineStage } from './models';

@Injectable({
  providedIn: 'root',
})
export class CrmApi {
  constructor(private readonly http: HttpClient) {}

  // Pipeline stages
  getPipelineStages() {
    return this.http.get<PipelineStage[]>('/api/pipeline-stages');
  }

  createPipelineStage(payload: { name: string; sortOrder: number }) {
    return this.http.post<PipelineStage>('/api/pipeline-stages', payload);
  }

  // Leads
  getLeads(pipelineStageId?: string) {
    const params: Record<string, string> = {};
    if (pipelineStageId) {
      params['pipelineStageId'] = pipelineStageId;
    }
    return this.http.get<Lead[]>('/api/leads', { params });
  }

  getLead(id: string) {
    return this.http.get<Lead>(`/api/leads/${id}`);
  }

  createLead(payload: {
    name: string;
    email?: string;
    phone?: string;
    company?: string;
    pipelineStageId: string;
    notes?: string;
  }) {
    return this.http.post<Lead>('/api/leads', payload);
  }

  updateLead(
    id: string,
    payload: {
      name: string;
      email?: string;
      phone?: string;
      company?: string;
      pipelineStageId: string;
      notes?: string;
    },
  ) {
    return this.http.put<Lead>(`/api/leads/${id}`, payload);
  }

  importLeads(file: File, pipelineStageId: string) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('pipelineStageId', pipelineStageId);
    return this.http.post<{ importedCount: number; errors: string[] }>('/api/leads/import', formData);
  }

  // Lead activity timeline
  getLeadActivities(leadId: string, page = 1, pageSize = 20) {
    return this.http.get<PagedActivities>(`/api/leads/${leadId}/activities`, {
      params: { page, pageSize },
    });
  }

  createLeadActivity(leadId: string, payload: { content: string; type?: string }) {
    return this.http.post<LeadActivity>(`/api/leads/${leadId}/activities`, payload);
  }

  // Tasks
  getLeadTasks(leadId: string) {
    return this.http.get<LeadTask[]>(`/api/leads/${leadId}/tasks`);
  }

  createTask(leadId: string, payload: { title: string; description?: string; dueDate: string; assignedToUserId?: string }) {
    return this.http.post<LeadTask>(`/api/leads/${leadId}/tasks`, payload);
  }

  getMyTasks(status?: string) {
    const params: Record<string, string> = {};
    if (status) {
      params['status'] = status;
    }
    return this.http.get<LeadTask[]>('/api/tasks', { params });
  }

  updateTaskStatus(id: string, status: 'Open' | 'Completed') {
    return this.http.put<LeadTask>(`/api/tasks/${id}`, { status });
  }
}
