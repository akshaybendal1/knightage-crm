import { BulkStageMoveResponse } from './models';

/**
 * The one-line summary shown after a bulk stage move, e.g. "Moved 3 to Won, 1 already there; 1
 * could not be moved." -- shared by the Timeline, the Leads list and the lead page so they all
 * word it (and decide success vs. error) the same way.
 */
export function describeBulkMove(res: BulkStageMoveResponse, stageName: string): { text: string; hasFailures: boolean } {
  const parts = [`Moved ${res.moved} to ${stageName}`];
  if (res.unchanged > 0) {
    parts.push(`${res.unchanged} already there`);
  }
  if (res.failed > 0) {
    return { text: `${parts.join(', ')}; ${res.failed} could not be moved.`, hasFailures: true };
  }
  return { text: `${parts.join(', ')}.`, hasFailures: false };
}
