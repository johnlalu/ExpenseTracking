import { Component, signal, OnInit } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NavbarComponent } from './shared/components/navbar.component';
import { AuthService } from './shared/services/auth.service';
import { ErrorService } from './shared/services/error.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent, MatSnackBarModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
// eslint-disable-next-line @angular-eslint/component-class-suffix
export class App implements OnInit {
  protected readonly title = signal('expense-frontend');
  isAuthenticated = false;

  constructor(
    private authService: AuthService,
    private errorService: ErrorService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check initial authentication state
    this.isAuthenticated = this.authService.isAuthenticated();

    // Subscribe to route changes to update auth state
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.isAuthenticated = this.authService.isAuthenticated();
      });

    // Subscribe to error notifications
    this.errorService.error$.subscribe(error => {
      if (error) {
        let errorMessage = error.message;
        if (error.details) {
          const detailsStr = Object.entries(error.details)
            .map(([key, values]) => `${key}: ${(values as string[]).join(', ')}`)
            .join(' | ');
          errorMessage = `${error.message} - ${detailsStr}`;
        }
        this.snackBar.open(errorMessage, 'Close', {
          duration: 7000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }
}
