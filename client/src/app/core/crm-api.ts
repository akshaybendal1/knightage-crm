import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  CustomRange,
  CustomReportDefinition,
  CustomReportRow,
  DocumentType,
  FieldSalesEntryItem,
  Lead,
  LeadActivity,
  LeadDocument,
  LeadProduct,
  LeadsByCategoryItem,
  LeadsByOwnerRow,
  LeadsByProductItem,
  LeadSourceBreakdownItem,
  LeadSourceDetailItem,
  LeadTask,
  LeadTrendPoint,
  Meeting,
  MeetingStatus,
  MessageTemplate,
  OrganizationDetails,
  PagedActivities,
  PagedFieldSalesEntries,
  PagedLeads,
  PagedLeadProductTimelineItems,
  PagedLeadTimelineItems,
  PagedTasks,
  PipelineStage,
  PipelineSummaryItem,
  Product,
  ProductCategory,
  ProductStage,
  ReportExportKey,
  ReportRange,
  SalesByRepRow,
  SavedCustomReport,
  SavedCustomReportSummary,
  TaskTrendPoint,
  TeamPerformanceRow,
  WonLostSummary,
} from './models';

@Injectable({
  providedIn: 'root',
})
export class CrmApi {
  constructor(private readonly http: HttpClient) {}

  // Pipeline stages
  getPipelineStages() {
    return this.http.get<PipelineStage[]>('/api/pipeline-stages');
  }

  createPipelineStage(payload: { name: string; sortOrder: number; isWon?: boolean; isLost?: boolean; isDefault?: boolean }) {
    return this.http.post<PipelineStage>('/api/pipeline-stages', payload);
  }

  // Product stages
  getProductStages() {
    return this.http.get<ProductStage[]>('/api/product-stages');
  }

  createProductStage(payload: { name: string; sortOrder: number; isWon?: boolean; isLost?: boolean; isDefault?: boolean }) {
    return this.http.post<ProductStage>('/api/product-stages', payload);
  }

  updateProductStage(id: string, payload: { name: string; sortOrder: number; isWon: boolean; isLost: boolean; isDefault: boolean }) {
    return this.http.post<ProductStage>(`/api/product-stages/${id}`, payload);
  }

  updatePipelineStage(id: string, payload: { name: string; sortOrder: number; isWon: boolean; isLost: boolean; isDefault: boolean }) {
    return this.http.post<PipelineStage>(`/api/pipeline-stages/${id}`, payload);
  }

  deletePipelineStage(id: string) {
    return this.http.post<void>(`/api/pipeline-stages/${id}/status`, { isActive: false });
  }

  deleteProductStage(id: string) {
    return this.http.post<void>(`/api/product-stages/${id}/status`, { isActive: false });
  }

  // Product categories
  getProductCategories() {
    return this.http.get<ProductCategory[]>('/api/product-categories');
  }

  createProductCategory(payload: { name: string; sortOrder: number }) {
    return this.http.post<ProductCategory>('/api/product-categories', payload);
  }

  updateProductCategory(id: string, payload: { name: string; sortOrder: number }) {
    return this.http.post<ProductCategory>(`/api/product-categories/${id}`, payload);
  }

  deleteProductCategory(id: string) {
    return this.http.post<void>(`/api/product-categories/${id}/status`, { isActive: false });
  }

  // Leads
  getLeads(pipelineStageId?: string, productId?: string, ownerUserId?: string, search?: string, page = 1, pageSize = 20) {
    const params: Record<string, string | number> = { page, pageSize };
    if (pipelineStageId) {
      params['pipelineStageId'] = pipelineStageId;
    }
    if (productId) {
      params['productId'] = productId;
    }
    if (ownerUserId) {
      params['ownerUserId'] = ownerUserId;
    }
    if (search) {
      params['search'] = search;
    }
    return this.http.get<PagedLeads>('/api/leads', { params });
  }

  exportLeads(pipelineStageId?: string, productId?: string, ownerUserId?: string, search?: string) {
    const params: Record<string, string> = {};
    if (pipelineStageId) {
      params['pipelineStageId'] = pipelineStageId;
    }
    if (productId) {
      params['productId'] = productId;
    }
    if (ownerUserId) {
      params['ownerUserId'] = ownerUserId;
    }
    if (search) {
      params['search'] = search;
    }
    return this.http.get('/api/leads/export', { params, responseType: 'blob' });
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
    ownerUserId?: string;
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
      ownerUserId?: string;
    },
  ) {
    return this.http.post<Lead>(`/api/leads/${id}`, payload);
  }

  deleteLead(id: string) {
    return this.http.post<void>(`/api/leads/${id}/status`, { isActive: false });
  }

  importLeads(file: File, pipelineStageId: string) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('pipelineStageId', pipelineStageId);
    return this.http.post<{ importedCount: number; errors: string[] }>('/api/leads/import', formData);
  }

  // Field Force Tracking -- sales entries
  createSalesEntry(payload: {
    customerName: string;
    company?: string;
    email?: string;
    phone?: string;
    notes?: string;
    latitude: number;
    longitude: number;
    products: { productId: string; quantity: number; productStageId?: string }[];
  }) {
    return this.http.post<Lead>('/api/sales-entries', payload);
  }

  getMySalesEntries(page = 1, pageSize = 20) {
    return this.http.get<PagedFieldSalesEntries>('/api/sales-entries/mine', { params: { page, pageSize } });
  }

  getSalesEntries(ownerUserId?: string, page = 1, pageSize = 20) {
    const params: Record<string, string | number> = { page, pageSize };
    if (ownerUserId) {
      params['ownerUserId'] = ownerUserId;
    }
    return this.http.get<PagedFieldSalesEntries>('/api/sales-entries', { params });
  }

  // Admin-only Sales Coverage page: one rep's sales-entry locations in a date range,
  // unpaged. from/to are yyyy-MM-dd date strings.
  getSalesEntryLocations(ownerUserId: string, from: string, to: string) {
    return this.http.get<{ items: FieldSalesEntryItem[] }>('/api/sales-entries/coverage', {
      params: { ownerUserId, from, to },
    });
  }

  // Admin-only Sales Dashboard chart: org-wide per-rep totals for a range.
  getSalesByRep(range: ReportRange, custom?: CustomRange) {
    return this.http.get<{ items: SalesByRepRow[] }>('/api/sales-entries/by-rep', { params: this.reportRangeParams(range, custom) });
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

  getMyTasks(status?: string, all = false, page = 1, pageSize = 20) {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) {
      params['status'] = status;
    }
    if (all) {
      params['all'] = 'true';
    }
    return this.http.get<PagedTasks>('/api/tasks', { params });
  }

  getDueTaskCount() {
    return this.http.get<{ count: number }>('/api/tasks/due-count');
  }

  getTasksCalendar(from: string, to: string, all = false) {
    const params: Record<string, string> = { from, to };
    if (all) {
      params['all'] = 'true';
    }
    return this.http.get<LeadTask[]>('/api/tasks/calendar', { params });
  }

  updateTaskStatus(id: string, status: 'Open' | 'Completed') {
    return this.http.post<LeadTask>(`/api/tasks/${id}`, { status });
  }

  // Meetings
  getLeadMeetings(leadId: string) {
    return this.http.get<Meeting[]>(`/api/leads/${leadId}/meetings`);
  }

  createMeeting(
    leadId: string,
    payload: { title: string; description?: string; startAtUtc: string; endAtUtc: string; location?: string; ownerUserId?: string },
  ) {
    return this.http.post<Meeting>(`/api/leads/${leadId}/meetings`, payload);
  }

  updateMeeting(
    id: string,
    payload: { title: string; description?: string; startAtUtc: string; endAtUtc: string; location?: string },
  ) {
    return this.http.post<Meeting>(`/api/meetings/${id}`, payload);
  }

  updateMeetingStatus(id: string, status: MeetingStatus) {
    return this.http.post<Meeting>(`/api/meetings/${id}/status`, { status });
  }

  getMeetingsMine(hours = 24) {
    return this.http.get<Meeting[]>('/api/meetings/mine', { params: { hours } });
  }

  // Lead products (line items -- product + quantity, priced at Lead Cost)
  getLeadProducts(leadId: string) {
    return this.http.get<LeadProduct[]>(`/api/leads/${leadId}/products`);
  }

  addLeadProduct(leadId: string, payload: { productId: string; quantity: number; productStageId: string }) {
    return this.http.post<LeadProduct>(`/api/leads/${leadId}/products`, payload);
  }

  updateLeadProductQuantity(id: string, quantity: number) {
    return this.http.post<LeadProduct>(`/api/lead-products/${id}`, { quantity });
  }

  updateLeadProductStage(id: string, productStageId: string) {
    return this.http.post<LeadProduct>(`/api/lead-products/${id}/stage`, { productStageId });
  }

  removeLeadProduct(id: string) {
    return this.http.post<void>(`/api/lead-products/${id}/remove`, {});
  }

  // Documents (quotations & invoices)
  getLeadDocuments(leadId: string) {
    return this.http.get<LeadDocument[]>(`/api/leads/${leadId}/documents`);
  }

  createDocument(leadId: string, payload: { type: DocumentType; taxRatePercent: number; discountPercent: number }) {
    return this.http.post<LeadDocument>(`/api/leads/${leadId}/documents`, payload);
  }

  sendDocument(id: string) {
    return this.http.post<{ status: DocumentType; sentAtUtc: string }>(`/api/documents/${id}/send`, {});
  }

  markDocumentPaid(id: string) {
    return this.http.post<{ status: string; paidAtUtc: string }>(`/api/documents/${id}/mark-paid`, {});
  }

  downloadDocument(id: string) {
    return this.http.get(`/api/documents/${id}/download`, { responseType: 'blob' });
  }

  // Timeline
  getLeadsTimeline(pipelineStageId: string, productId?: string, ownerUserId?: string, search?: string, page = 1, pageSize = 15) {
    const params: Record<string, string | number> = { pipelineStageId, page, pageSize };
    if (productId) {
      params['productId'] = productId;
    }
    if (ownerUserId) {
      params['ownerUserId'] = ownerUserId;
    }
    if (search) {
      params['search'] = search;
    }
    return this.http.get<PagedLeadTimelineItems>('/api/timeline/leads', { params });
  }

  getLeadProductsTimeline(productStageId: string, productId?: string, ownerUserId?: string, search?: string, page = 1, pageSize = 15) {
    const params: Record<string, string | number> = { productStageId, page, pageSize };
    if (productId) {
      params['productId'] = productId;
    }
    if (ownerUserId) {
      params['ownerUserId'] = ownerUserId;
    }
    if (search) {
      params['search'] = search;
    }
    return this.http.get<PagedLeadProductTimelineItems>('/api/timeline/lead-products', { params });
  }

  updateLeadStage(id: string, pipelineStageId: string) {
    return this.http.post<Lead>(`/api/leads/${id}/stage`, { pipelineStageId });
  }

  // Dashboard
  getPipelineSummary() {
    return this.http.get<PipelineSummaryItem[]>('/api/dashboard/pipeline-summary');
  }

  getWonLostSummary(range: ReportRange, custom?: CustomRange) {
    return this.http.get<WonLostSummary>('/api/dashboard/won-lost', { params: this.reportRangeParams(range, custom) });
  }

  getActivitySummary() {
    return this.http.get<{ count: number }>('/api/dashboard/activity-summary');
  }

  getOverdueTaskCount() {
    return this.http.get<{ count: number }>('/api/dashboard/overdue-tasks');
  }

  getStuckLeadsCount() {
    return this.http.get<{ count: number }>('/api/dashboard/stuck-leads');
  }

  // Reports
  // range='custom' needs from/to too -- every report endpoint accepts the same
  // range[+from+to] shape, so build the query params once here rather than
  // repeating the same "if custom, add from/to" branch in every method below.
  private reportRangeParams(range: ReportRange, custom?: CustomRange): Record<string, string> {
    const params: Record<string, string> = { range };
    if (range === 'custom' && custom) {
      params['from'] = custom.from;
      params['to'] = custom.to;
    }
    return params;
  }

  getLeadTrends(range: ReportRange, custom?: CustomRange) {
    return this.http.get<LeadTrendPoint[]>('/api/reports/lead-trends', { params: this.reportRangeParams(range, custom) });
  }

  getLeadSourceBreakdown(range: ReportRange, custom?: CustomRange) {
    return this.http.get<LeadSourceBreakdownItem[]>('/api/reports/lead-sources', { params: this.reportRangeParams(range, custom) });
  }

  getTeamPerformance(range: ReportRange, custom?: CustomRange) {
    return this.http.get<TeamPerformanceRow[]>('/api/reports/team-performance', { params: this.reportRangeParams(range, custom) });
  }

  getTaskCompletionTrend(range: ReportRange, custom?: CustomRange) {
    return this.http.get<TaskTrendPoint[]>('/api/reports/task-trends', { params: this.reportRangeParams(range, custom) });
  }

  getLeadsByProduct(range: ReportRange, custom?: CustomRange) {
    return this.http.get<LeadsByProductItem[]>('/api/reports/leads-by-product', { params: this.reportRangeParams(range, custom) });
  }

  getLeadsByCategory(range: ReportRange, custom?: CustomRange) {
    return this.http.get<LeadsByCategoryItem[]>('/api/reports/leads-by-category', { params: this.reportRangeParams(range, custom) });
  }

  getLeadsByOwner(range: ReportRange, custom?: CustomRange) {
    return this.http.get<LeadsByOwnerRow[]>('/api/reports/leads-by-owner', { params: this.reportRangeParams(range, custom) });
  }

  getLeadSourceDetail(source: string, range: ReportRange, custom?: CustomRange) {
    return this.http.get<LeadSourceDetailItem[]>('/api/reports/lead-sources/detail', { params: { ...this.reportRangeParams(range, custom), source } });
  }

  exportReport(key: ReportExportKey, range: ReportRange, custom?: CustomRange) {
    return this.http.get('/api/reports/export', { params: { ...this.reportRangeParams(range, custom), report: key }, responseType: 'blob' });
  }

  // Custom reports -- dimensions/viewType/range/filters all live on the flat
  // CustomReportDefinition itself (unlike the fixed reports above, there's no separate
  // range+custom pairing), so these build their own HttpParams rather than reusing
  // reportRangeParams().
  private customReportParams(definition: CustomReportDefinition): HttpParams {
    let params = new HttpParams();
    for (const dimension of definition.dimensions) {
      params = params.append('dimensions', dimension);
    }
    params = params.set('viewType', definition.viewType).set('range', definition.range);
    if (definition.from) params = params.set('from', definition.from);
    if (definition.to) params = params.set('to', definition.to);
    for (const id of definition.stageIds ?? []) params = params.append('stageIds', id);
    for (const id of definition.ownerUserIds ?? []) params = params.append('ownerUserIds', id);
    for (const id of definition.productIds ?? []) params = params.append('productIds', id);
    for (const s of definition.sources ?? []) params = params.append('sources', s);
    return params;
  }

  runCustomReport(definition: CustomReportDefinition) {
    return this.http.post<{ items: CustomReportRow[] }>('/api/custom-reports/run', definition);
  }

  getSavedCustomReports() {
    return this.http.get<SavedCustomReportSummary[]>('/api/custom-reports');
  }

  getSavedCustomReport(id: string) {
    return this.http.get<SavedCustomReport>(`/api/custom-reports/${id}`);
  }

  runSavedCustomReport(id: string) {
    return this.http.get<SavedCustomReport & { items: CustomReportRow[] }>(`/api/custom-reports/${id}/run`);
  }

  saveCustomReport(name: string, definition: CustomReportDefinition) {
    return this.http.post<{ id: string; name: string }>('/api/custom-reports', { name, definition });
  }

  updateCustomReport(id: string, name: string, definition: CustomReportDefinition) {
    return this.http.post<{ id: string; name: string }>(`/api/custom-reports/${id}`, { name, definition });
  }

  deleteCustomReport(id: string) {
    return this.http.post<void>(`/api/custom-reports/${id}/status`, { isActive: false });
  }

  exportCustomReport(definition: CustomReportDefinition) {
    return this.http.get('/api/custom-reports/export', { params: this.customReportParams(definition), responseType: 'blob' });
  }

  // Products
  getProducts() {
    return this.http.get<Product[]>('/api/products');
  }

  createProduct(payload: { name: string; description?: string; category?: string | null; price: number }) {
    return this.http.post<Product>('/api/products', payload);
  }

  updateProduct(id: string, payload: { name: string; description?: string; category?: string | null; price: number }) {
    return this.http.post<Product>(`/api/products/${id}`, payload);
  }

  setProductActive(id: string, isActive: boolean) {
    return this.http.post<void>(`/api/products/${id}/status`, { isActive });
  }

  // Message templates
  getMessageTemplates() {
    return this.http.get<MessageTemplate[]>('/api/message-templates');
  }

  createMessageTemplate(payload: { name: string; body: string }) {
    return this.http.post<MessageTemplate>('/api/message-templates', payload);
  }

  updateMessageTemplate(id: string, payload: { name: string; body: string }) {
    return this.http.post<MessageTemplate>(`/api/message-templates/${id}`, payload);
  }

  setMessageTemplateActive(id: string, isActive: boolean) {
    return this.http.post<void>(`/api/message-templates/${id}/status`, { isActive });
  }

  // Organization
  getOrganization() {
    return this.http.get<OrganizationDetails>('/api/organization');
  }

  updateOrganization(payload: { name: string; contactEmail?: string; contactPhone?: string; address?: string }) {
    return this.http.put<void>('/api/organization', payload);
  }

  // Branding
  getBrandingLogo(): Observable<Blob | null> {
    return this.http.get('/api/branding/logo', { responseType: 'blob' }).pipe(catchError(() => of(null)));
  }

  uploadBrandingLogo(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<void>('/api/branding/logo', form);
  }

  deleteBrandingLogo() {
    return this.http.post<void>('/api/branding/logo/remove', {});
  }

  requestSubscription() {
    return this.http.post<{ message: string }>('/api/organization/subscription-request', {});
  }
}
