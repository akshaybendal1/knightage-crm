import { Side } from 'driver.js';

/**
 * Plain data, deliberately not colocated with the pages it describes. Every
 * page is lazy-loaded via loadComponent in app.routes.ts -- importing
 * per-page step arrays here would force-eager-load every page's chunk the
 * moment Tour is constructed, defeating route-level code-splitting.
 */
export interface TourStepDef {
  /** Route to navigate to before highlighting this step, or null to stay put (e.g. a nav-highlight step reachable from wherever the tour currently is). */
  route: string | null;
  /** CSS selector for the element to highlight, ignored when navKey is set. */
  element: string;
  /** Resolves against .sidebar or .bottom-nav depending on viewport width instead of a fixed element selector. */
  navKey?: string;
  title: string;
  description: string;
  side?: Side;
  /** Only shown to Owner/Admin roles. */
  adminOnly?: boolean;
  /** Only included when the org has at least one lead to point at. */
  requiresLead?: boolean;
  /** Only shown to the Sales role -- see buildTourSteps, which short-circuits to
   * ONLY these steps for a Sales user, since every other step targets a page
   * non-sales-guard blocks them from reaching at all. */
  salesOnly?: boolean;
  /** Only included when the org has Field Force Tracking enabled (Owner/Admin steps). */
  requiresFieldSales?: boolean;
}

const TOUR_STEPS: TourStepDef[] = [
  {
    route: '/dashboard',
    element: '[data-tour="pipeline-overview"]',
    title: 'Welcome to Knightage CRM',
    description: 'Your dashboard tracks pipeline, wins/losses and overdue tasks at a glance.',
    side: 'bottom',
  },
  {
    route: null,
    navKey: 'nav-leads',
    element: '',
    title: 'Leads',
    description: 'Every lead your team is working lives here.',
    side: 'right',
  },
  {
    route: '/leads',
    element: '[data-tour="new-lead-button"]',
    title: 'Add a lead',
    description: 'Add one lead here, or bulk-import a CSV further down the page.',
    side: 'left',
  },
  {
    route: '/leads',
    element: '[data-tour="bulk-whatsapp-button"]',
    title: 'Bulk WhatsApp',
    description: 'Select leads with the checkboxes, then message them all at once.',
    side: 'bottom',
  },
  {
    route: '/leads/:id',
    element: '[data-tour="lead-timeline"]',
    title: 'Lead activity',
    description: 'Notes, tasks and stage changes for this lead, all in one timeline.',
    side: 'top',
    requiresLead: true,
  },
  {
    route: '/pipeline-stages',
    element: '[data-tour="new-stage-button"]',
    title: 'Pipeline stages',
    description: 'Define the stages leads move through, and mark which ones count as won or lost.',
    side: 'left',
  },
  {
    route: '/tasks',
    element: '[data-tour="page-header"]',
    title: 'Tasks',
    description: 'Everything assigned to you, due today or overdue.',
    side: 'bottom',
  },
  {
    route: '/calendar',
    element: '[data-tour="page-header"]',
    title: 'Calendar',
    description: 'The same tasks, laid out by due date.',
    side: 'bottom',
  },
  {
    route: '/reports',
    element: '[data-tour="page-header"]',
    title: 'Reports',
    description:
      'Lead volume, conversion, product and owner breakdowns, all in one place — click any source in the breakdown to see the actual leads behind that number.',
    side: 'bottom',
    adminOnly: true,
  },
  {
    route: '/reports',
    element: '[data-tour="custom-report-button"]',
    title: 'Custom reports',
    description:
      'Build your own report -- pick fields like stage, owner, product or source, view it as a table, chart or pivot, then save it for next time.',
    side: 'bottom',
    adminOnly: true,
  },
  {
    route: '/team',
    element: '[data-tour="add-teammate-button"]',
    title: 'Team',
    description: 'Invite teammates and manage their roles.',
    side: 'left',
    adminOnly: true,
  },
  {
    route: '/products',
    element: '[data-tour="new-product-button"]',
    title: 'Products',
    description: 'Manage what you sell here, then optionally link a lead to one when you create or edit it.',
    side: 'left',
    adminOnly: true,
  },
  {
    route: '/message-templates',
    element: '[data-tour="new-template-button"]',
    title: 'Message templates',
    description: 'Reusable WhatsApp message templates for bulk sends.',
    side: 'left',
    adminOnly: true,
  },
  {
    route: '/sales-dashboard',
    element: '[data-tour="page-header"]',
    title: 'Sales Dashboard',
    description: "Every rep's field sales entries, plus a rep-by-rep comparison chart.",
    side: 'bottom',
    adminOnly: true,
    requiresFieldSales: true,
  },
  {
    route: '/sales-coverage',
    element: '[data-tour="page-header"]',
    title: 'Sales Coverage',
    description: 'See the ground each rep has covered on a map, with a computed coverage area by day, week or month.',
    side: 'bottom',
    adminOnly: true,
    requiresFieldSales: true,
  },
  {
    route: '/organization',
    element: '[data-tour="page-header"]',
    title: 'Organization',
    description: 'Your org profile and subscription status.',
    side: 'bottom',
    adminOnly: true,
  },
  {
    route: '/contact-us',
    element: '[data-tour="page-header"]',
    title: "You're all set",
    description: 'Reach us here any time you need help.',
    side: 'bottom',
  },
  {
    route: '/sales-entry',
    element: '[data-tour="page-header"]',
    title: 'Add Sales Entry',
    description: "Log a visit here — customer details and products, plus your device's location, are captured automatically when you submit.",
    side: 'bottom',
    salesOnly: true,
  },
  {
    route: '/sales-dashboard',
    element: '[data-tour="page-header"]',
    title: 'Sales Dashboard',
    description: 'Your own logged entries and their total value, all in one place.',
    side: 'bottom',
    salesOnly: true,
  },
];

export interface TourContext {
  role: string | undefined;
  firstLeadId: string | null;
  fieldForceTrackingEnabled: boolean;
}

export function buildTourSteps(ctx: TourContext): TourStepDef[] {
  // A Sales user can't reach any of the general steps below -- non-sales-guard blocks
  // every one of those routes -- so their tour is a short, separate mini-tour instead
  // of a filtered version of the general one.
  if (ctx.role === 'Sales') {
    return TOUR_STEPS.filter((s) => s.salesOnly);
  }

  const isAdmin = ctx.role === 'Owner' || ctx.role === 'Admin';
  return TOUR_STEPS.filter((s) => !s.salesOnly)
    .filter((s) => !s.adminOnly || isAdmin)
    .filter((s) => !s.requiresLead || ctx.firstLeadId)
    .filter((s) => !s.requiresFieldSales || ctx.fieldForceTrackingEnabled)
    .map((s) => (s.route === '/leads/:id' ? { ...s, route: `/leads/${ctx.firstLeadId}` } : s));
}
