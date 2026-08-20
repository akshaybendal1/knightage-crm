import { LeadTask } from './models';

// DueDate is a calendar date, not a point in time -- parse just the "yyyy-MM-dd"
// portion as local midnight so a UTC-serialized "T00:00:00Z" doesn't roll back a
// day for anyone west of UTC.
function dueDateOnly(isoDate: string): Date {
  return new Date(`${isoDate.slice(0, 10)}T00:00:00`);
}

function todayOnly(): Date {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return today;
}

export function isOverdue(task: LeadTask): boolean {
  return task.status !== 'Completed' && dueDateOnly(task.dueDate) < todayOnly();
}

export function isDueToday(task: LeadTask): boolean {
  return task.status !== 'Completed' && dueDateOnly(task.dueDate).getTime() === todayOnly().getTime();
}
