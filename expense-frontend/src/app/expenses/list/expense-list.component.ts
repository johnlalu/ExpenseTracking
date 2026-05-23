import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { ExpenseService } from '../../shared/services/expense.service';
import { ErrorService } from '../../shared/services/error.service';
import { Expense } from '../../shared/models/expense.model';

/**
 * Component displaying list of user expenses with actions.
 */
@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule
  ],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.css'
})
export class ExpenseListComponent implements OnInit, OnDestroy {
  expenses: Expense[] = [];
  isLoading = false;
  selectedMonth = new Date();
  destroy$ = new Subject<void>();

  displayedColumns: string[] = ['date', 'category', 'description', 'amount', 'currency', 'actions'];

  constructor(
    private expenseService: ExpenseService,
    private errorService: ErrorService,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadExpenses(): void {
    this.isLoading = true;
    const month = this.selectedMonth.getMonth() + 1;
    const year = this.selectedMonth.getFullYear();

    this.expenseService.getByMonth(month, year)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.isLoading = false;
          this.expenses = response.items;
        },
        error: (error) => {
          this.isLoading = false;
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
        }
      });
  }

  onMonthChange(date: Date): void {
    this.selectedMonth = date;
    this.loadExpenses();
  }

  editExpense(expense: Expense): void {
    this.router.navigate(['/expenses', expense.id, 'edit']);
  }

  deleteExpense(expense: Expense): void {
    if (confirm(`Are you sure you want to delete this expense?`)) {
      this.expenseService.delete(expense.id!)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.expenses = this.expenses.filter(e => e.id !== expense.id);
          },
          error: (error) => {
            const appError = this.errorService.handleHttpError(error);
            this.errorService.setError(appError);
          }
        });
    }
  }

  createNewExpense(): void {
    this.router.navigate(['/expenses/new']);
  }

  get totalAmount(): number {
    return this.expenses.reduce((sum, expense) => sum + expense.amount, 0);
  }

  formatDate(date: string | Date | null | undefined): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
