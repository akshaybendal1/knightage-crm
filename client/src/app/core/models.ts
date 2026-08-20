export interface PipelineStage {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  isWon: boolean;
  isLost: boolean;
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
  metadata?: string | null;
  createdByUserId?: string | null;
  createdAtUtc: string;
}

export interface PagedActivities {
  items: LeadActivity[];
  hasMore: boolean;
}

export interface LeadTask {
  id: string;
  leadId: string;
  title: string;
  description?: string | null;
  dueDate: string;
  status: 'Open' | 'Completed';
  assignedToUserId: string;
  createdByUserId?: string | null;
  createdAtUtc: string;
  completedAtUtc?: string | null;
  leadName?: string | null;
}

export interface OrgUser {
  id: string;
  displayName: string;
  email: string;
}

export interface PipelineSummaryItem {
  stageId: string;
  stageName: string;
  sortOrder: number;
  leadCount: number;
}

export interface WonLostSummary {
  won: number;
  lost: number;
  range: string;
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
