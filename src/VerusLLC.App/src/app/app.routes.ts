import { Routes } from '@angular/router';
import { CompaniesPage } from './companies/companies-page';

export const routes: Routes = [
  { path: '', component: CompaniesPage },
  { path: 'companies', component: CompaniesPage },
  { path: '**', redirectTo: '' },
];
