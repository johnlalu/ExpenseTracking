import { Routes } from '@angular/router';
import { AuthGuard } from './shared/services/auth.guard';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { ExpenseListComponent } from './expenses/list/expense-list.component';
import { ExpenseFormComponent } from './expenses/form/expense-form.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        component: LoginComponent
      },
      {
        path: 'register',
        component: RegisterComponent
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: 'login',
    redirectTo: '/auth/login'
  },
  {
    path: 'register',
    redirectTo: '/auth/register'
  },
  {
    path: 'dashboard',
    canActivate: [AuthGuard],
    redirectTo: '/expenses',
    pathMatch: 'full'
  },
  {
    path: 'expenses',
    canActivate: [AuthGuard],
    component: ExpenseListComponent
  },
  {
    path: 'expenses/new',
    canActivate: [AuthGuard],
    component: ExpenseFormComponent
  },
  {
    path: 'expenses/:id/edit',
    canActivate: [AuthGuard],
    component: ExpenseFormComponent
  },
  {
    path: 'reports',
    canActivate: [AuthGuard],
    // TODO: Create ReportViewComponent
    component: undefined as any
  },
  {
    path: 'categories',
    canActivate: [AuthGuard],
    // TODO: Create CategoryListComponent
    component: undefined as any
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
