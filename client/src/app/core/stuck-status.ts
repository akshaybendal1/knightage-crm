import { Lead, LeadProduct, PipelineStage, ProductStage } from './models';

// No org-settings/config table exists anywhere in this codebase -- kept in sync
// by hand with DashboardController.cs's StuckThresholdDays and the Android
// client's StuckStatus.kt STUCK_THRESHOLD_DAYS.
export const STUCK_THRESHOLD_DAYS = 14;

function isStale(changedAtUtc: string | null | undefined, fallbackUtc: string): boolean {
  const reference = new Date(changedAtUtc ?? fallbackUtc);
  const cutoff = new Date();
  cutoff.setUTCDate(cutoff.getUTCDate() - STUCK_THRESHOLD_DAYS);
  return reference < cutoff;
}

export function isLeadStuck(lead: Lead, stage: PipelineStage | undefined): boolean {
  if (!stage || stage.isWon || stage.isLost) {
    return false;
  }
  return isStale(lead.pipelineStageChangedAtUtc, lead.createdAtUtc);
}

export function isLeadProductStuck(item: LeadProduct, stage: ProductStage | undefined): boolean {
  if (!stage || stage.isWon || stage.isLost) {
    return false;
  }
  return isStale(item.statusChangedAtUtc, item.createdAtUtc);
}
