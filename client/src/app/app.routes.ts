import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login').then((m) => m.Login) },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard) },
      { path: 'leads', loadComponent: () => import('./pages/leads/leads').then((m) => m.Leads) },
      {
        path: 'leads/:id',
        loadComponent: () => import('./pages/leads/lead-detail/lead-detail').then((m) => m.LeadDetail),
      },
      {
        path: 'pipeline-stages',
        loadComponent: () => import('./pages/pipeline-stages/pipeline-stages').then((m) => m.PipelineStages),
      },
      { path: 'tasks', loadComponent: () => import('./pages/tasks/tasks').then((m) => m.Tasks) },
    ],
  },
  { path: '**', redirectTo: '' },
];
