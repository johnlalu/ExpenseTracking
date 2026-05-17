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
        this.snackBar.open(error.message, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }
}
