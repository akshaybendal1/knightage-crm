import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Lead, PipelineStage } from './models';

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
}
