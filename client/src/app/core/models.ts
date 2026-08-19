export interface PipelineStage {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface Lead {
  id: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  pipelineStageId: string;
  notes?: string | null;
  source: string;
  createdAtUtc: string;
}

export interface LeadActivity {
  id: string;
  leadId: string;
  type: string;
  content: string;
  createdByUserId?: string | null;
  createdAtUtc: string;
}

export interface AuthResult {
  token: string;
  expiresAtUtc: string;
  userId: string;
  organizationId: string;
  email: string;
  displayName: string;
  role: string;
}
