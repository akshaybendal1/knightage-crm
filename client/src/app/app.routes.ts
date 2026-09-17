import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { adminGuard } from './core/admin-guard';
import { nonSalesGuard } from './core/non-sales-guard';
import { salesFeatureGuard } from './core/sales-feature-guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login').then((m) => m.Login) },
  {
    path: 'auth-callback',
    loadComponent: () => import('./pages/auth-callback/auth-callback').then((m) => m.AuthCallback),
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./pages/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./pages/reset-password/reset-password').then((m) => m.ResetPassword),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'leads',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/leads/leads').then((m) => m.Leads),
      },
      {
        path: 'leads/:id',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/leads/lead-detail/lead-detail').then((m) => m.LeadDetail),
      },
      {
        path: 'pipeline-stages',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/pipeline-stages/pipeline-stages').then((m) => m.PipelineStages),
      },
      {
        path: 'tasks',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/tasks/tasks').then((m) => m.Tasks),
      },
      {
        path: 'calendar',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/calendar/calendar').then((m) => m.Calendar),
      },
      {
        path: 'timeline',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/timeline/timeline').then((m) => m.Timeline),
      },
      {
        path: 'sales-entry',
        canActivate: [salesFeatureGuard],
        loadComponent: () => import('./pages/sales-entry/sales-entry').then((m) => m.SalesEntry),
      },
      {
        path: 'sales-dashboard',
        canActivate: [salesFeatureGuard],
        loadComponent: () => import('./pages/sales-dashboard/sales-dashboard').then((m) => m.SalesDashboard),
      },
      {
        path: 'sales-coverage',
        canActivate: [salesFeatureGuard, adminGuard],
        loadComponent: () => import('./pages/sales-coverage/sales-coverage').then((m) => m.SalesCoverage),
      },
      {
        path: 'reports',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/reports/reports').then((m) => m.Reports),
      },
      {
        path: 'custom-reports/new',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/custom-reports/custom-report-builder').then((m) => m.CustomReportBuilder),
      },
      {
        path: 'custom-reports/:id',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/custom-reports/custom-report-builder').then((m) => m.CustomReportBuilder),
      },
      {
        path: 'team',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/team/team').then((m) => m.Team),
      },
      {
        path: 'products',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/products/products').then((m) => m.Products),
      },
      {
        path: 'product-categories',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/product-categories/product-categories').then((m) => m.ProductCategories),
      },
      {
        path: 'organization',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/organization/organization').then((m) => m.Organization),
      },
      {
        path: 'message-templates',
        canActivate: [adminGuard],
        loadComponent: () => import('./pages/message-templates/message-templates').then((m) => m.MessageTemplates),
      },
      {
        path: 'contact-us',
        canActivate: [nonSalesGuard],
        loadComponent: () => import('./pages/contact-us/contact-us').then((m) => m.ContactUs),
      },
      {
        path: 'profile',
        loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
