import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { rootGuard } from './core/root-guard';
import { salesFeatureGuard } from './core/sales-feature-guard';
import { screenGuard } from './core/screen-guard';

export const routes: Routes = [
  // Exact-match only (pathMatch: 'full'), so this never intercepts /dashboard,
  // /leads, etc. -- those still fall through to the guarded block below and its
  // own authGuard, unchanged. Only the bare root path goes through rootGuard.
  { path: '', pathMatch: 'full', canActivate: [rootGuard], children: [] },
  {
    path: 'user-guide',
    title: 'User guide — Knightage CRM',
    data: { description: 'How to use Knightage CRM, screen by screen, with step-by-step instructions.' },
    loadComponent: () => import('./pages/user-guide/user-guide').then((m) => m.UserGuide),
  },
  {
    path: 'about',
    title: 'About — Knightage CRM',
    data: {
      description:
        'Knightage CRM: pipeline and lead tracking, WhatsApp outreach, branded quotations and invoices, custom reports, and GPS-verified field sales visits, on the web or Android.',
    },
    loadComponent: () => import('./pages/about/about').then((m) => m.About),
  },
  {
    path: 'login',
    title: 'Sign in — Knightage CRM',
    data: { description: 'Sign in to Knightage CRM to manage your sales pipeline, leads, tasks, and meetings.' },
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'auth-callback',
    title: 'Signing in… — Knightage CRM',
    loadComponent: () => import('./pages/auth-callback/auth-callback').then((m) => m.AuthCallback),
  },
  {
    path: 'forgot-password',
    title: 'Reset your password — Knightage CRM',
    loadComponent: () => import('./pages/forgot-password/forgot-password').then((m) => m.ForgotPassword),
  },
  {
    path: 'reset-password',
    title: 'Reset your password — Knightage CRM',
    loadComponent: () => import('./pages/reset-password/reset-password').then((m) => m.ResetPassword),
  },
  {
    path: 'privacy-policy',
    title: 'Privacy Policy — Knightage CRM',
    data: { description: "Knightage CRM's privacy policy: what we collect, how we use it, and your choices." },
    loadComponent: () => import('./pages/privacy-policy/privacy-policy').then((m) => m.PrivacyPolicy),
  },
  {
    path: 'terms-of-service',
    title: 'Terms of Service — Knightage CRM',
    data: { description: "Knightage CRM's terms of service: acceptable use, data ownership, and account terms." },
    loadComponent: () => import('./pages/terms-of-service/terms-of-service').then((m) => m.TermsOfService),
  },
  {
    path: 'delete-account',
    title: 'Delete your account — Knightage CRM',
    data: { description: 'How to delete your Knightage CRM account and what happens to your data.' },
    loadComponent: () => import('./pages/delete-account/delete-account').then((m) => m.DeleteAccount),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        canActivate: [screenGuard('dashboard')],
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'leads',
        canActivate: [screenGuard('leads')],
        loadComponent: () => import('./pages/leads/leads').then((m) => m.Leads),
      },
      {
        path: 'customers',
        canActivate: [screenGuard('customers')],
        loadComponent: () => import('./pages/customers/customers').then((m) => m.Customers),
      },
      {
        path: 'leads/:id',
        canActivate: [screenGuard('leads')],
        loadComponent: () => import('./pages/leads/lead-detail/lead-detail').then((m) => m.LeadDetail),
      },
      {
        path: 'pipeline-stages',
        canActivate: [screenGuard('pipeline-stages')],
        loadComponent: () => import('./pages/pipeline-stages/pipeline-stages').then((m) => m.PipelineStages),
      },
      {
        path: 'tasks',
        canActivate: [screenGuard('tasks')],
        loadComponent: () => import('./pages/tasks/tasks').then((m) => m.Tasks),
      },
      {
        path: 'calendar',
        canActivate: [screenGuard('calendar')],
        loadComponent: () => import('./pages/calendar/calendar').then((m) => m.Calendar),
      },
      {
        path: 'timeline',
        canActivate: [screenGuard('timeline')],
        loadComponent: () => import('./pages/timeline/timeline').then((m) => m.Timeline),
      },
      {
        path: 'sales-entry',
        canActivate: [screenGuard('sales-entry'), salesFeatureGuard],
        loadComponent: () => import('./pages/sales-entry/sales-entry').then((m) => m.SalesEntry),
      },
      {
        path: 'sales-dashboard',
        canActivate: [screenGuard('sales-dashboard'), salesFeatureGuard],
        loadComponent: () => import('./pages/sales-dashboard/sales-dashboard').then((m) => m.SalesDashboard),
      },
      {
        path: 'sales-coverage',
        canActivate: [screenGuard('sales-coverage'), salesFeatureGuard],
        loadComponent: () => import('./pages/sales-coverage/sales-coverage').then((m) => m.SalesCoverage),
      },
      {
        path: 'reports',
        canActivate: [screenGuard('reports')],
        loadComponent: () => import('./pages/reports/reports').then((m) => m.Reports),
      },
      {
        path: 'custom-reports/new',
        canActivate: [screenGuard('reports')],
        loadComponent: () => import('./pages/custom-reports/custom-report-builder').then((m) => m.CustomReportBuilder),
      },
      {
        path: 'custom-reports/:id',
        canActivate: [screenGuard('reports')],
        loadComponent: () => import('./pages/custom-reports/custom-report-builder').then((m) => m.CustomReportBuilder),
      },
      {
        path: 'team',
        canActivate: [screenGuard('team')],
        loadComponent: () => import('./pages/team/team').then((m) => m.Team),
      },
      {
        path: 'products',
        canActivate: [screenGuard('products')],
        loadComponent: () => import('./pages/products/products').then((m) => m.Products),
      },
      {
        path: 'product-categories',
        canActivate: [screenGuard('product-categories')],
        loadComponent: () => import('./pages/product-categories/product-categories').then((m) => m.ProductCategories),
      },
      {
        path: 'organization',
        canActivate: [screenGuard('organization')],
        loadComponent: () => import('./pages/organization/organization').then((m) => m.Organization),
      },
      {
        path: 'access-control',
        canActivate: [screenGuard('access-control')],
        loadComponent: () => import('./pages/access-control/access-control').then((m) => m.AccessControl),
      },
      {
        path: 'message-templates',
        canActivate: [screenGuard('message-templates')],
        loadComponent: () => import('./pages/message-templates/message-templates').then((m) => m.MessageTemplates),
      },
      {
        path: 'contact-us',
        canActivate: [screenGuard('contact-us')],
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
