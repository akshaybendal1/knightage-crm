import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login').then((m) => m.Login) },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'leads', pathMatch: 'full' },
      { path: 'leads', loadComponent: () => import('./pages/leads/leads').then((m) => m.Leads) },
      {
        path: 'pipeline-stages',
        loadComponent: () => import('./pages/pipeline-stages/pipeline-stages').then((m) => m.PipelineStages),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
