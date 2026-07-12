import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatSidenavModule,
    MatListModule
  ],
  template: `
    <mat-toolbar color="primary" class="app-navbar">
      <div class="navbar-container">
        <div class="navbar-left">
          <button mat-icon-button (click)="sidenav.toggle()" class="menu-button">
            <mat-icon>menu</mat-icon>
          </button>
          <span class="app-title">Expense Tracker</span>
        </div>
        
        <div class="navbar-right">
          <button mat-icon-button [matMenuTriggerFor]="userMenu" class="user-menu">
            <mat-icon>account_circle</mat-icon>
          </button>
          <mat-menu #userMenu="matMenu">
            <button mat-menu-item (click)="logout()">
              <mat-icon>logout</mat-icon>
              <span>Logout</span>
            </button>
          </mat-menu>
        </div>
      </div>
    </mat-toolbar>

    <mat-sidenav-container class="sidenav-container" autosize>
      <mat-sidenav #sidenav class="sidenav" mode="side">
        <mat-nav-list>
          <mat-list-item (click)="navigateTo('/expenses')" (keydown.enter)="navigateTo('/expenses')" tabindex="0">
            <mat-icon matListItemIcon>receipt</mat-icon>
            <span matListItemTitle>Expenses</span>
          </mat-list-item>
          <mat-list-item (click)="navigateTo('/reports')" (keydown.enter)="navigateTo('/reports')" tabindex="0">
            <mat-icon matListItemIcon>bar_chart</mat-icon>
            <span matListItemTitle>Reports</span>
          </mat-list-item>
          <mat-list-item (click)="navigateTo('/categories')" (keydown.enter)="navigateTo('/categories')" tabindex="0">
            <mat-icon matListItemIcon>category</mat-icon>
            <span matListItemTitle>Categories</span>
          </mat-list-item>
        </mat-nav-list>
      </mat-sidenav>

      <mat-sidenav-content>
        <ng-content></ng-content>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [`
    .app-navbar {
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      position: sticky;
      top: 0;
      z-index: 1000;
    }

    .navbar-container {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 0 16px;
    }

    .navbar-left {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .app-title {
      font-size: 20px;
      font-weight: 500;
    }

    .navbar-right {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .user-menu {
      color: white;
    }

    .menu-button {
      color: white;
    }

    .sidenav-container {
      height: calc(100vh - 64px);
    }

    .sidenav {
      width: 250px;
    }

    ::ng-deep .mat-mdc-list-item {
      cursor: pointer;
    }

    ::ng-deep .mat-mdc-list-item:hover {
      background-color: rgba(0, 0, 0, 0.04);
    }
  `]
})
export class NavbarComponent {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
