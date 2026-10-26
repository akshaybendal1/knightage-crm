export interface PipelineStage {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  isWon: boolean;
  isLost: boolean;
  isDefault: boolean;
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
  ownerUserId?: string | null;
  createdByUserId?: string | null;
  createdAtUtc: string;
  updatedByUserId?: string | null;
  updatedAtUtc?: string | null;
  // Set on create and whenever pipelineStageId actually changes -- unlike
  // updatedAtUtc (clobbered by any field edit), this is stage-specific and
  // powers "stuck" detection (see core/stuck-status.ts).
  pipelineStageChangedAtUtc?: string | null;
  totalCost: number;
}

export interface Product {
  id: string;
  name: string;
  description?: string | null;
  category?: string | null;
  price: number;
  isActive: boolean;
  createdByUserId?: string | null;
  createdAtUtc: string;
  updatedByUserId?: string | null;
  updatedAtUtc?: string | null;
}

export interface ProductStage {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  isWon: boolean;
  isLost: boolean;
  isDefault: boolean;
}

export interface ProductCategory {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface LeadProduct {
  id: string;
  leadId: string;
  productId: string;
  productName?: string | null;
  quantity: number;
  unitPrice: number;
  createdAtUtc: string;
  // A customizable per-org stage, mirroring Lead.pipelineStageId exactly
  // (see ProductStage) -- was a fixed Active/Won/Lost string before.
  productStageId: string;
  productStageName?: string | null;
  // Set on attach and whenever productStageId actually changes -- powers
  // "stuck" detection (see core/stuck-status.ts).
  statusChangedAtUtc?: string | null;
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

// Access Control: which screens each role may open (see ScreenCatalog on the server).
export type ScreenKind = 'Configurable' | 'AdminOnly' | 'Everyone';
export type ConfigurableRole = 'Admin' | 'Member' | 'Sales';

export interface ScreenAccessRow {
  key: string;
  label: string;
  group: string;
  kind: ScreenKind;
  defaults: Record<ConfigurableRole, boolean>;
  allowed: Record<ConfigurableRole, boolean>;
}

export interface ScreenAccessMatrix {
  roles: ConfigurableRole[];
  screens: ScreenAccessRow[];
}

export interface MyScreens {
  role: string;
  screens: string[];
}

// Bulk stage moves: one entry per lead / product line, each with its own optional note.
export interface StageMoveItem {
  id: string;
  note?: string;
}

export interface StageMoveResult {
  id: string;
  status: 'Moved' | 'Unchanged' | 'NotFound' | 'Failed';
  message?: string | null;
}

export interface BulkStageMoveResponse {
  moved: number;
  unchanged: number;
  failed: number;
  results: StageMoveResult[];
}

export interface PagedActivities {
  items: LeadActivity[];
  hasMore: boolean;
}

export interface PagedLeads {
  items: Lead[];
  totalCount: number;
}

export interface FieldSalesEntryItem {
  id: string;
  name: string;
  company?: string | null;
  email?: string | null;
  phone?: string | null;
  notes?: string | null;
  ownerUserId?: string | null;
  ownerDisplayName?: string | null;
  createdAtUtc: string;
  latitude?: number | null;
  longitude?: number | null;
  locationCapturedAtUtc?: string | null;
  totalCost: number;
  products?: string | null;
}

export interface PagedFieldSalesEntries {
  items: FieldSalesEntryItem[];
  totalCount: number;
}

export interface SalesByRepRow {
  ownerUserId: string;
  ownerName: string;
  entryCount: number;
  totalValue: number;
}

export interface PagedTasks {
  items: LeadTask[];
  totalCount: number;
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
  updatedByUserId?: string | null;
  updatedAtUtc?: string | null;
  completedAtUtc?: string | null;
  leadName?: string | null;
}

export type MeetingStatus = 'Scheduled' | 'Cancelled' | 'Completed';
export type GoogleSyncStatus = 'NotSynced' | 'Synced' | 'Failed' | 'ReauthRequired';

export interface Meeting {
  id: string;
  leadId: string;
  title: string;
  description?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  location?: string | null;
  status: MeetingStatus;
  ownerUserId: string;
  googleEventId?: string | null;
  googleSyncStatus: GoogleSyncStatus;
  createdByUserId?: string | null;
  createdAtUtc: string;
  updatedByUserId?: string | null;
  updatedAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  leadName?: string | null;
}

export interface CalendarConnection {
  connected: boolean;
  googleAccountEmail?: string | null;
  connectedAtUtc?: string | null;
}

export interface OrgUser {
  id: string;
  displayName: string;
  email: string;
}

export type TeamRole = 'Owner' | 'Admin' | 'Member' | 'Sales';

export interface TeamMember {
  id: string;
  displayName: string;
  email: string;
  role: TeamRole;
  isActive: boolean;
  createdAtUtc: string;
  phoneNumber?: string | null;
}

export interface PipelineSummaryItem {
  stageId: string;
  stageName: string;
  sortOrder: number;
  leadCount: number;
  // Sum of quantity * unitPrice across LeadProducts for leads in this stage --
  // same no-status-filter math as Lead.totalCost, aggregated per stage.
  leadValue: number;
}

export interface LeadTimelineItem {
  leadId: string;
  leadName: string;
  company?: string | null;
  phone?: string | null;
  source: string;
  ownerUserId?: string | null;
  totalCost: number;
  pipelineStageId: string;
  stageName: string;
  stageChangedAtUtc: string;
  daysInStage: number;
}

export interface LeadProductTimelineItem {
  leadProductId: string;
  leadId: string;
  leadName: string;
  company?: string | null;
  phone?: string | null;
  productName?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  productStageId: string;
  productStageName: string;
  statusChangedAtUtc: string;
  daysInStatus: number;
}

export interface PagedLeadTimelineItems {
  items: LeadTimelineItem[];
  totalCount: number;
}

export interface PagedLeadProductTimelineItems {
  items: LeadProductTimelineItem[];
  totalCount: number;
}

export type DocumentType = 'Quotation' | 'Invoice';
export type DocumentStatus = 'Draft' | 'Sent' | 'Paid';

export interface LeadDocument {
  id: string;
  leadId: string;
  type: DocumentType;
  number: string;
  status: DocumentStatus;
  discountPercent: number;
  discountAmount: number;
  taxRatePercent: number;
  subtotal: number;
  taxAmount: number;
  total: number;
  createdByUserId?: string | null;
  createdAtUtc: string;
  sentAtUtc?: string | null;
  paidAtUtc?: string | null;
}

export interface WonLostSummary {
  won: number;
  lost: number;
  range: string;
}

export type ReportRange = 'week' | 'month' | 'all' | 'custom';

/** Only meaningful when range === 'custom' -- both are plain yyyy-MM-dd date strings. */
export interface CustomRange {
  from: string;
  to: string;
}

export interface LeadTrendPoint {
  bucketStart: string;
  leadCount: number;
}

export interface LeadSourceBreakdownItem {
  source: string;
  leadCount: number;
}

export interface TeamPerformanceRow {
  userId: string;
  displayName: string;
  leadsCreated: number;
  tasksCompleted: number;
  tasksOverdue: number;
}

export interface TaskTrendPoint {
  bucketStart: string;
  completedCount: number;
}

export interface LeadsByProductItem {
  productId: string | null;
  productName: string;
  leadCount: number;
  totalValue: number;
}

export interface LeadsByCategoryItem {
  category: string;
  leadCount: number;
  totalValue: number;
}

export interface LeadsByOwnerRow {
  ownerUserId: string | null;
  ownerName: string;
  leadCount: number;
}

export interface LeadSourceDetailItem {
  id: string;
  name: string;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  pipelineStageId: string;
  stageName: string;
  ownerUserId?: string | null;
  createdAtUtc: string;
}

export type ReportExportKey =
  | 'lead-trends'
  | 'lead-sources'
  | 'team-performance'
  | 'task-trends'
  | 'leads-by-product'
  | 'leads-by-category'
  | 'leads-by-owner';

// Custom reports -- a whitelisted set of group-by fields, mirrored exactly against the
// backend's CustomReportDimensions (Core/Interfaces/ICustomReportRepository.cs). Not an
// open-ended field picker: the backend rejects anything outside this set.
export type ReportDimensionKey = 'lead' | 'stage' | 'owner' | 'product' | 'category' | 'source' | 'createdDate';

export type CustomReportViewType = 'table' | 'bar' | 'line' | 'pie' | 'pivot';

export interface CustomReportRow {
  dim1Value: string;
  dim1Label: string;
  dim2Value?: string | null;
  dim2Label?: string | null;
  leadCount: number;
  totalValue: number;
}

export interface CustomReportDefinition {
  dimensions: ReportDimensionKey[];
  viewType: CustomReportViewType;
  range: ReportRange;
  from?: string | null;
  to?: string | null;
  stageIds?: string[] | null;
  ownerUserIds?: string[] | null;
  productIds?: string[] | null;
  sources?: string[] | null;
}

export interface SavedCustomReportSummary {
  id: string;
  name: string;
  updatedAtUtc?: string | null;
}

export interface SavedCustomReport {
  id: string;
  name: string;
  definition: CustomReportDefinition;
}

export interface MessageTemplate {
  id: string;
  name: string;
  body: string;
  isActive: boolean;
  createdByUserId?: string | null;
  createdAtUtc: string;
  updatedByUserId?: string | null;
  updatedAtUtc?: string | null;
}

export type SubscriptionStatus = 'Active' | 'ReadOnly' | 'Disabled' | 'NotSubscribed';

export interface PlanUsage {
  current: number;
  max: number;
}

export interface OrganizationDetails {
  name: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  address?: string | null;
  subscriptionStatus: SubscriptionStatus;
  subscriptionStartDate?: string | null;
  subscriptionEndDate?: string | null;
  accentColor?: string | null;
  fieldForceTrackingEnabled: boolean;
  recordUsage: PlanUsage | null;
  userUsage: PlanUsage | null;
}

export interface CurrentUser {
  id: string;
  displayName: string;
  email: string;
  role: string;
  hasSeenTour: boolean;
  phoneNumber?: string | null;
  // False for a Google-only account -- decides whether the delete-account form asks for a password.
  hasPassword?: boolean;
}

// A person or company that bought (a lead that reached a Won stage) or that was added by hand.
export interface Customer {
  id: string;
  name: string;
  company?: string | null;
  email?: string | null;
  phone?: string | null;
  source: 'WonLead' | 'Manual' | string;
  isActive: boolean;
  inventoryCustomerId?: string | null;
  inventorySyncedAtUtc?: string | null;
  createdAtUtc: string;
  leadCount: number;
}

export interface CustomerList {
  items: Customer[];
  // Customers the Inventory portal doesn't have yet.
  pendingInventory: number;
  // Leads in a Won stage that were never turned into customers (won before customers existed).
  wonLeadsWithoutCustomer: number;
}

export interface CustomerSaveResponse {
  customer: Customer;
  // True when the same person was already saved, so nothing new was created.
  alreadyExisted: boolean;
}

export interface InventorySyncResult {
  available: boolean;
  pushed: number;
  pending: number;
  message?: string | null;
}

// A won lead that is not a customer yet (Customers screen -> "Choose leads").
export interface WonLeadListItem {
  id: string;
  name: string;
  company: string | null;
  email: string | null;
  phone: string | null;
  stageName: string | null;
  wonAtUtc: string;
}

export interface WonLeadPage {
  items: WonLeadListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface WonLeadImportResult {
  imported: number;
  linkedToExisting: number;
  remaining: number;
  inventory: InventorySyncResult;
}

// Duplicate check for a lead typed in by hand (GET /api/leads/duplicates).
export type MatchReason = 'email' | 'phone' | 'name+company';

export interface LeadMatch {
  id: string;
  name: string;
  company?: string | null;
  email?: string | null;
  phone?: string | null;
  stageName?: string | null;
  ownerUserId?: string | null;
  createdAtUtc: string;
  matchedOn: MatchReason;
}

export interface CustomerMatch {
  id: string;
  name: string;
  company?: string | null;
  email?: string | null;
  phone?: string | null;
  matchedOn: MatchReason;
}

export interface DuplicateResult {
  leads: LeadMatch[];
  customers: CustomerMatch[];
}

// Result of linking a lead to a customer, or making a customer from a lead.
export interface LeadCustomerResult {
  customer: Customer | null;
  // A new customer record was made.
  created: boolean;
  // The lead was linked to a customer that already existed.
  matchedExisting: boolean;
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
